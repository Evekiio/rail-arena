using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class ValidationMovementInputTests
    {
        [Test]
        public void SprintFacesTheExistingRouteWithoutChangingItsWorldDirection()
        {
            foreach (float yaw in new[] { 0f, 90f, 213f })
            foreach (var move in new[] { Vector2.down, Vector2.left, new Vector2(.4f, -.3f), Vector2.zero })
            {
                var original = new MovementCommand { Move = move, Yaw = yaw, Pitch = 15, Fire = true, Jump = true, Sequence = 23 };
                var actual = ValidationMovementInput.Apply(original, 240, true);
                Vector3 World(MovementCommand input) => Quaternion.Euler(0, input.Yaw, 0)*new Vector3(input.Move.x, 0, input.Move.y);
                Assert.That(Vector3.Distance(World(actual), World(original)), Is.LessThan(.00001f));
                Assert.That(actual.Move.y, Is.GreaterThanOrEqualTo(0));
                Assert.That(actual.Sprint, Is.True); Assert.That(actual.Walk || actual.Crouch || actual.Jump, Is.False);
                Assert.That(actual.Slide, Is.True); Assert.That(actual.Fire, Is.EqualTo(original.Fire));
                Assert.That(actual.Pitch, Is.EqualTo(original.Pitch)); Assert.That(actual.Sequence, Is.EqualTo(original.Sequence));
                Assert.That(ValidationMovementInput.Apply(original, 240, false), Is.EqualTo(original), "Ordinary controls remain untouched");
            }
        }

        [Test]
        public void BackwardFacingBotRouteCanActuallyReachSlideEligibility()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            var slide = new SlideState();
            Vector3 velocity = Vector3.zero;
            bool entered = false;
            try
            {
                for (int tick = 180; tick < 360; tick++)
                {
                    var command = ValidationMovementInput.Apply(new MovementCommand { Move = Vector2.down, Yaw = 0 }, tick, true);
                    bool sliding = SlideMovement.Step(ref slide, velocity, command, true, tuning);
                    entered |= sliding;
                    velocity = sliding ? SlideMovement.Integrate(velocity, command, tuning, 1f/60) : ArenaMovement.Integrate(velocity, command, true, tuning, 1f/60);
                    Assert.That(velocity.z, Is.LessThanOrEqualTo(0));
                }
                Assert.That(entered, Is.True, "Coverage needs real forward sprint speed; a Sprint flag on backward input cannot start a slide");
            }
            finally { Object.DestroyImmediate(tuning); }
        }
    }
}
