using NUnit.Framework;
using RailArena.Simulation;
using RailArena.Networking;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class SprintFireSpreadTests
    {
        [Test]
        public void MovementAndCrouchUseDistinctConesAndStationaryAimRemainsExact()
        {
            var command = new MovementCommand { Move = Vector2.up };
            Assert.That(SprintFireSpread.ShotHalfAngle(true, false, command, Vector3.zero), Is.Zero);
            Assert.That(SprintFireSpread.ShotHalfAngle(true, false, command, Vector3.forward), Is.EqualTo(1f));
            Assert.That(SprintFireSpread.ShotHalfAngle(true, true, command, Vector3.forward), Is.EqualTo(.5f));
            Assert.That(SprintFireSpread.ShotHalfAngle(false, false, command, Vector3.forward), Is.Zero);
            command.Sprint = true;
            Assert.That(SprintFireSpread.ShotHalfAngle(true, false, command, Vector3.forward), Is.EqualTo(SprintFireSpread.HalfAngleDegrees));
            foreach (var angle in new[] { 1f, .5f, SprintFireSpread.HalfAngleDegrees })
                Assert.That(Vector3.Angle(Vector3.forward, SprintFireSpread.Direction(Vector3.forward, Vector2.right, angle)), Is.EqualTo(angle).Within(.01f));
        }
        [Test]
        public void ReticleProjectionMatchesConeAtDifferentFieldsOfViewAndScales()
        {
            foreach (float fov in new[] { 60f, 100f, 110f }) foreach (float scale in new[] { 1f, 1.5f, 2f })
            {
                float r = SprintFireSpread.ScreenRadius(1080, fov, scale);
                float angle = Mathf.Atan(r * scale / 540f * Mathf.Tan(fov * .5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                Assert.That(angle, Is.EqualTo(SprintFireSpread.HalfAngleDegrees).Within(.0001f));
            }
            var command = new MovementCommand { Sprint = true, Move = Vector2.up };
            Assert.That(SprintFireSpread.Active(true, false, command), Is.True);
            Assert.That(SprintFireSpread.Active(false, false, command), Is.False);
            Assert.That(SprintFireSpread.Active(true, true, command), Is.False);
            command.Walk = true;
            Assert.That(SprintFireSpread.Active(true, false, command), Is.False);
        }
        [Test]
        public void SlideInputSurvivesWireAndIsNotRepeatedOnMissingPackets()
        {
            var source = new DuelPacket { Kind = DuelMessage.Input, Inputs = new[] { new DuelInput { Command = new MovementCommand { Sequence = 1, Slide = true, Sprint = true, Move = Vector2.up } } } };
            Assert.That(DuelProtocol.TryDecode(DuelProtocol.Encode(source), out var packet), Is.True);
            Assert.That(packet.Inputs[0].Command.Slide, Is.True);
            var queue = new DuelInputQueue(); queue.Add(packet.Inputs);
            Assert.That(queue.Consume(1, out var first), Is.True); Assert.That(first.Command.Slide, Is.True);
            Assert.That(queue.Consume(2, out var next), Is.True); Assert.That(next.Command.Slide, Is.False);
        }
        [Test]
        public void DirectionStaysInsideConeAndPreservesCenter()
        {
            var aim = Quaternion.Euler(-20, 60, 0) * Vector3.forward;
            Assert.That(Vector3.Angle(aim, SprintFireSpread.Direction(aim, Vector2.zero)), Is.LessThan(.01f));
            foreach (var point in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down, Vector2.one })
                Assert.That(Vector3.Angle(aim, SprintFireSpread.Direction(aim, point)), Is.LessThanOrEqualTo(SprintFireSpread.HalfAngleDegrees + .01f));
        }
    }
}



