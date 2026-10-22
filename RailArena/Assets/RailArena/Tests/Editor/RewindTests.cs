using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class RewindTests
    {
        private readonly List<GameObject> objects = new();
        private ArenaActor shooter;
        private ArenaActor target;
        private HitboxHistory history;

        [SetUp]
        public void SetUp()
        {
            shooter = Actor("Shooter", new Vector3(1000, 0, 1000));
            target = Actor("Target", new Vector3(1000, 0, 1010));
            Box(shooter, HitRegion.Head);
            Box(target, HitRegion.Body);
        }

        [TearDown]
        public void TearDown()
        {
            history?.Dispose();
            history = null;
            foreach (var obj in objects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            objects.Clear();
            Physics.SyncTransforms();
        }

        [Test]
        public void EarlierPoseCanBeHitAfterTargetMovesWithoutMovingLiveActors()
        {
            history = new HitboxHistory(new[] { shooter, target });
            history.Capture(100);
            target.transform.position += Vector3.right * 4;
            history.Capture(101);
            var previous = history.Trace(101, 100, shooter);
            var current = history.Trace(101, 101, shooter);
            Assert.That(previous.ValidTime, Is.True);
            Assert.That(previous.Actor, Is.SameAs(target));
            Assert.That(previous.Region, Is.EqualTo(HitRegion.Body));
            Assert.That(current.Actor, Is.Null);
            Assert.That(target.transform.position.x, Is.EqualTo(1004));
            Assert.That(target.Health, Is.EqualTo(100), "History traces never apply damage themselves.");
        }

        [Test]
        public void ExactlyTwelveTicksAreAllowedButFutureExpiredAndUncapturedTimesAreRejected()
        {
            history = new HitboxHistory(new[] { shooter, target });
            for (int tick = 100; tick <= 112; tick++) history.Capture(tick);
            Assert.That(history.Trace(112, 100, shooter).Actor, Is.SameAs(target));
            Assert.That(history.Trace(112, 99, shooter).ValidTime, Is.False);
            Assert.That(history.Trace(112, 113, shooter).ValidTime, Is.False);
            Assert.That(history.Trace(113, 112, shooter).ValidTime, Is.False);
            history.Capture(113);
            Assert.That(history.Trace(113, 100, shooter).ValidTime, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => history.Capture(113));
        }

        [Test]
        public void StaticWorldBlocksRewoundTargets()
        {
            var wall = New("Wall");
            wall.transform.position = new Vector3(1000, 1.58f, 1005);
            wall.AddComponent<BoxCollider>().size = new Vector3(4, 4, 1);
            history = new HitboxHistory(new[] { shooter, target });
            history.Capture(10);
            Physics.SyncTransforms();
            var hit = history.Trace(10, 10, shooter);
            Assert.That(hit.ValidTime, Is.True);
            Assert.That(hit.Actor, Is.Null);
            Assert.That(hit.End.z, Is.EqualTo(1004.5f).Within(.001f));
        }

        [Test]
        public void PreviousLifeCannotDamageRespawnedTargetAndNewShooterUsesCurrentOrigin()
        {
            history = new HitboxHistory(new[] { shooter, target });
            history.Capture(20);
            target.Life++;
            history.Capture(21);
            Assert.That(history.Trace(21, 20, shooter).Actor, Is.Null);
            Assert.That(history.Trace(21, 21, shooter).Actor, Is.SameAs(target));
            shooter.Life++;
            history.Capture(22);
            Assert.That(history.Trace(22, 21, shooter).Actor, Is.SameAs(target));
            shooter.transform.position += Vector3.right * 5;
            Assert.That(history.Trace(22, 21, shooter).Actor, Is.Null, "Client history cannot choose the firing origin.");
            shooter.transform.position -= Vector3.right * 5;
            target.Health = 0;
            Assert.That(history.Trace(22, 22, shooter).Actor, Is.Null);
        }

        [Test]
        public void RotatedScaledCapsuleUsesItsRecordedGeometryAndRegion()
        {
            UnityEngine.Object.DestroyImmediate(target.GetComponentInChildren<Hitbox>().gameObject);
            var limb = New("Limb");
            limb.layer = 8;
            limb.transform.SetParent(target.transform, false);
            limb.transform.localPosition = Vector3.up * 1.58f;
            limb.transform.localRotation = Quaternion.Euler(0, 0, 45);
            limb.transform.localScale = new Vector3(1.2f, .8f, 1.2f);
            var collider = limb.AddComponent<CapsuleCollider>();
            collider.radius = .25f; collider.height = 1; collider.isTrigger = true;
            var box = limb.AddComponent<Hitbox>(); box.Actor = target; box.Region = HitRegion.Limb;
            Physics.SyncTransforms();
            Assert.That(collider.Raycast(new Ray(shooter.Eye, shooter.AimDirection), out var native, 180), Is.True);
            history = new HitboxHistory(new[] { shooter, target });
            history.Capture(50);
            target.transform.position += Vector3.left * 5;
            history.Capture(51);
            var hit = history.Trace(51, 50, shooter);
            Assert.That(hit.Actor, Is.SameAs(target));
            Assert.That(hit.Region, Is.EqualTo(HitRegion.Limb));
            Assert.That(Vector3.Distance(hit.End, native.point), Is.LessThan(.001f));
        }

        private ArenaActor Actor(string name, Vector3 position)
        {
            var obj = New(name); obj.layer = 9; obj.transform.position = position;
            return obj.AddComponent<ArenaActor>();
        }
        private void Box(ArenaActor actor, HitRegion region)
        {
            var obj = New("Hurtbox"); obj.layer = 8;
            obj.transform.SetParent(actor.transform, false);
            obj.transform.localPosition = Vector3.up * 1.58f;
            var collider = obj.AddComponent<BoxCollider>(); collider.size = Vector3.one * .8f; collider.isTrigger = true;
            var box = obj.AddComponent<Hitbox>(); box.Actor = actor; box.Region = region;
        }
        private GameObject New(string name) { var obj = new GameObject(name); objects.Add(obj); return obj; }
    }
}
