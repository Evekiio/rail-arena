using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class AutomaticRifleTests
    {
        [Test]
        public void HeldInputUses250RpmScheduleAndReleaseStopsWithoutQueuedBurst()
        {
            var trigger = new WeaponTrigger(); trigger.Sample(true, true, true);
            int nextShot = 0, shots = 0; var magazine = RifleMagazine.Full;
            for (int tick = 0; tick < 72; tick++)
                if (trigger.Consume() && magazine.TryFire(tick, ref nextShot))
                { shots++; }
            Assert.That(shots, Is.EqualTo(5));
            Assert.That(nextShot, Is.EqualTo(72));
            trigger.Sample(false, false, true);
            for (int tick = 60; tick < 120; tick++) Assert.That(trigger.Consume(), Is.False);
            Assert.That(CombatRules.Damage(HitRegion.Body), Is.EqualTo(50));
        }

        [Test]
        public void NetworkHeldFireKeepsOriginalAimDuringShortGapExpiresAndHonorsRelease()
        {
            var queue = new DuelInputQueue();
            var held = new DuelInput { ViewedTick = 96, Command = new MovementCommand { Sequence = 1, Fire = true, Yaw = 30 } };
            queue.Add(new[] { held }); queue.Consume(100, out _);
            for (int tick = 101; tick <= 103; tick++)
            {
                queue.Consume(tick, out var command);
                Assert.That(command.Command.Fire, Is.True);
                Assert.That(queue.Shot.Value.ViewedTick, Is.EqualTo(96));
                Assert.That(queue.Shot.Value.Command.Yaw, Is.EqualTo(30));
            }
            queue.Consume(104, out var expired);
            Assert.That(expired.Command.Fire, Is.False); Assert.That(queue.Shot, Is.Null);
            held.Command.Sequence = 2; held.ViewedTick = 104; queue.Add(new[] { held }); queue.Consume(105, out _);
            var released = held; released.Command.Sequence = 3; released.Command.Fire = false;
            queue.Add(new[] { released }); queue.Consume(106, out var release);
            Assert.That(release.Command.Fire, Is.False);
            queue.Add(new[] { held }); queue.Consume(107, out var redundant);
            Assert.That(redundant.Command.Fire, Is.False);
        }

        [Test]
        public void SpringKickIsFastBoundedAndSettlesExactlyAfterReset()
        {
            var spring = new WeaponRecoil(); spring.Fire(); float peak = 0;
            for (int frame = 0; frame < 12; frame++) { spring.Step(1f / 240); peak = Mathf.Max(peak, spring.Kick); }
            Assert.That(peak, Is.InRange(.8f, 1.4f));
            Assert.That(Quaternion.Angle(spring.Rotation, Quaternion.identity), Is.GreaterThan(.1f));
            spring.Step(1);
            Assert.That(spring.Kick, Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(spring.Rotation, Quaternion.identity), Is.LessThan(.001f));
            spring.Fire(); spring.Reset(); spring.Step(1f / 60);
            Assert.That(spring.Kick, Is.Zero); Assert.That(spring.Offset, Is.EqualTo(Vector3.zero));
            Assert.That(spring.Rotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void SustainedFireStaysBoundedAndRecoveryIsIndependentOfRenderRate()
        {
            Quaternion Sample(int fps)
            {
                var spring = new WeaponRecoil();
                for (int frame = 0; frame < fps * 10; frame++)
                {
                    if (frame % (fps / 10) == 0) spring.Fire();
                    spring.Step(1f / fps);
                    Assert.That(spring.Kick, Is.InRange(0, 1.4f));
                    Assert.That(spring.Offset.magnitude, Is.LessThan(.013f));
                    Assert.That(Quaternion.Angle(spring.Rotation, Quaternion.identity), Is.LessThan(8));
                }
                return spring.Rotation;
            }
            Assert.That(Quaternion.Angle(Sample(30), Sample(120)), Is.LessThan(.02f));
            var stressed = new WeaponRecoil();
            for (int i = 0; i < 1000; i++) stressed.Fire();
            stressed.Step(.5f);
            Assert.That(float.IsNaN(stressed.Kick), Is.False);
            Assert.That(stressed.Kick, Is.InRange(0, 1.4f));
        }
    }
}
