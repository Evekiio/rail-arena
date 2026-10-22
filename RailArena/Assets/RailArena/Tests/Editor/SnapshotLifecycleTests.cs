using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class SnapshotLifecycleTests
    {
        private GameObject root;
        private ArenaSession session;
        private MovementTuning tuning;
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Vector3 Origin = new(4000, 0, 4000);

        private T Field<T>(string name) => (T)typeof(ArenaSession).GetField(name, PrivateInstance).GetValue(session);
        private void Apply(DuelSnapshot state) => typeof(ArenaSession).GetMethod("ApplySnapshot", PrivateInstance).Invoke(session, new object[] { state });

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Snapshot life fixture");
            var world = root.AddComponent<ArenaWorld>();
            Transform Point(Vector3 position)
            {
                var point = new GameObject("Spawn").transform;
                point.SetParent(root.transform); point.position = position;
                return point;
            }
            world.Spawns = new[] { Point(Origin), Point(Origin + Vector3.right * 10) };
            world.Nodes = world.Spawns;
            world.Pads = Array.Empty<JumpPad>();
            world.Pickups = new HealthPickup[3];
            for (int i = 0; i < 3; i++)
            {
                var pickup = new GameObject("Pickup"); pickup.transform.SetParent(root.transform);
                world.Pickups[i] = pickup.AddComponent<HealthPickup>();
                world.Pickups[i].Visuals = Array.Empty<Renderer>();
            }
            session = root.AddComponent<ArenaSession>(); session.World = world;
            tuning = ScriptableObject.CreateInstance<MovementTuning>(); session.Tuning = tuning;
            session.StartSession(true, 1, 1, TargetScenario.Combat);
            typeof(ArenaSession).GetProperty(nameof(ArenaSession.LocalPlayerId)).SetValue(session, 1);
            Assert.That(session.LocalPlayerId, Is.EqualTo(1));
            Apply(State(1, 200, 1, 1));
        }

        private static DuelSnapshot State(int round, int tick, int remoteLife, int localLife) => new()
        {
            Round = round, Tick = tick, Phase = MatchPhase.Playing,
            Actors = new[]
            {
                new ActorSnapshot { Health = 100, Life = remoteLife, Position = Origin, Grounded = true,
                    RespawnTick = remoteLife > 1 ? tick - 1 : 0 },
                new ActorSnapshot { Health = 100, Life = localLife, Position = Origin + Vector3.right * 10, Grounded = true,
                    RespawnTick = localLife > 1 ? tick - 1 : 0 }
            }
        };

        [TearDown]
        public void Cleanup()
        {
            if (session != null) foreach (var actor in session.Actors) if (actor != null) Object.DestroyImmediate(actor.gameObject);
            if (root != null) Object.DestroyImmediate(root);
            if (tuning != null) Object.DestroyImmediate(tuning);
        }

        [Test]
        public void MissedDeathSnapshotStillAdvancesLifeAndResetsPresentation()
        {
            var actor = session.Human;
            actor.Presentation.transform.localPosition = Vector3.down * 3;
            actor.Presentation.Weapon.gameObject.SetActive(false);
            Apply(State(1, 326, 1, 2));
            Assert.That(actor.Life, Is.EqualTo(2));
            Assert.That(actor.Presentation.Weapon.gameObject.activeSelf, Is.True);
            Assert.That(actor.Presentation.transform.localPosition.y, Is.EqualTo(-actor.Controller.skinWidth).Within(.001f));
        }

        [Test]
        public void RemoteRespawnSnapsToSpawnAndDiscardsPreviousLifeInterpolation()
        {
            var state = State(1, 326, 2, 1);
            state.Actors[0].Position += Vector3.forward * 40;
            Apply(state);
            Assert.That(session.Actors[0].transform.position, Is.EqualTo(state.Actors[0].Position));
            Assert.That(session.Actors[0].Life, Is.EqualTo(2));
            Assert.That(Field<List<DuelSnapshot>>("interpolation").Count, Is.EqualTo(1));
            Assert.That(session.Actors[0].Controller.enabled, Is.False);
        }

        [Test]
        public void LocalNewLifeDiscardsOldPredictionAndViewCorrection()
        {
            Field<List<DuelInput>>("prediction").Add(new DuelInput { Command = new MovementCommand
                { Sequence = 1, Move = Vector2.up, Jump = true, Fire = true } });
            var view = Field<ReconciliationView>("reconciliationView");
            view.Correct(Vector3.forward, Vector3.zero);
            typeof(ArenaSession).GetField("recoil", PrivateInstance).SetValue(session, 1f);
            Apply(State(1, 326, 1, 2));
            Assert.That(session.PendingInputs, Is.Zero);
            Assert.That(session.Human.transform.position, Is.EqualTo(Origin + Vector3.right * 10));
            Assert.That(view.Offset, Is.EqualTo(Vector3.zero));
            Assert.That(Field<float>("recoil"), Is.Zero);
        }

        [Test]
        public void RematchRaisesViewWeaponEvenWhenLifeNumberReturnsToOne()
        {
            var motion = Field<FirstPersonWeaponMotion>("weaponMotion");
            motion.Reset(false);
            typeof(ArenaSession).GetField("recoil", PrivateInstance).SetValue(session, 1f);
            Apply(State(2, 200, 1, 1));
            motion.Step(.01f, Vector3.zero, true, Vector2.zero, 0);
            Assert.That(motion.Offset.y, Is.LessThan(-.1f));
            Assert.That(Field<float>("recoil"), Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void CodecRejectsNonpositiveLifeNumbers(int life)
        {
            var state = State(1, 200, 1, life);
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state }));
        }
    }
}
