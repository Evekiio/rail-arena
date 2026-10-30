using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class SlideMovementTests
    {
        private static MovementCommand Sprint => new() { Move = Vector2.up, Sprint = true };
        private static MovementCommand Crouch => new() { Move = Vector2.up, Crouch = true };
        private static MovementCommand Slide => new() { Move = Vector2.up, Sprint = true, Slide = true };

        [Test]
        public void EntryRequiresSlideRequestGroundedSprintAndMinimumSpeed()
        {
            SlideState state = default;
            Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13.5f, Crouch, true), Is.False);
            foreach (int rejected in new[] { 0, 1, 2, 3, 4 })
            {
                state = default;
                SlideMovement.Step(ref state, Vector3.forward * 13.5f, Sprint, true);
                var command = Slide;
                if (rejected == 2) command.Walk = true;
                if (rejected == 3) command.Move = Vector2.down;
                if (rejected == 4) command.Jump = true;
                Assert.That(SlideMovement.Step(ref state, Vector3.forward * (rejected == 0 ? 10 : 13.5f),
                    command, rejected != 1), Is.False, "rejected condition " + rejected);
            }
            state = default;
            SlideMovement.Step(ref state, Vector3.forward * 13.5f, Sprint, true);
            Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13.5f, Slide, true), Is.True);
        }

        [Test]
        public void SlideLastsExactly36TicksDeceleratesWithoutBoostAndCannotChainWhileHeld()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                SlideState state = default; Vector3 velocity = Vector3.forward * 13.5f;
                SlideMovement.Step(ref state, velocity, Sprint, true);
                int ticks = 0;
                var slideCommand = Slide;
                while (SlideMovement.Step(ref state, velocity, slideCommand, true))
                {
                    slideCommand = Sprint;
                    float before = new Vector2(velocity.x, velocity.z).magnitude;
                    velocity = SlideMovement.Integrate(velocity, Crouch, tuning, 1f / 60);
                    Assert.That(new Vector2(velocity.x, velocity.z).magnitude, Is.LessThanOrEqualTo(before + .00001f));
                    Assert.That(++ticks, Is.LessThanOrEqualTo(36));
                }
                Assert.That(ticks, Is.EqualTo(36));
                Assert.That(velocity.z, Is.EqualTo(4.5f).Within(.001f));
                for (int i = 0; i < 120; i++)
                    Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13.5f, Crouch, true), Is.False);
                SlideMovement.Step(ref state, velocity, Sprint, true);
                Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13.5f, Slide, true), Is.True);
            }
            finally { Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void CooldownBlocksFreshPressAndAirOrJumpCancelsSlide()
        {
            foreach (bool jump in new[] { false, true })
            {
                var state = new SlideState { RemainingTicks = 20, CooldownTicks = 60, CrouchHeld = true };
                var command = Crouch; command.Jump = jump;
                Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13, command, jump), Is.False);
                Assert.That(state.RemainingTicks, Is.Zero);
                Assert.That(state.CooldownTicks, Is.EqualTo(59));
                SlideMovement.Step(ref state, Vector3.forward * 13, Sprint, true);
                Assert.That(SlideMovement.Step(ref state, Vector3.forward * 13, Crouch, true), Is.False);
            }
        }

        [Test]
        public void SteeringHasAngularLimitAndNeverAddsSpeed()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                var command = Crouch; command.Move = Vector2.right;
                var result = SlideMovement.Integrate(Vector3.forward * 13.5f, command, tuning, 1f / 60);
                Vector3 horizontal = new(result.x, 0, result.z);
                Assert.That(Vector3.Angle(Vector3.forward, horizontal), Is.EqualTo(.75f).Within(.01f));
                Assert.That(horizontal.magnitude, Is.EqualTo(13.25f).Within(.001f));
                result = SlideMovement.Integrate(Vector3.forward * 13.5f, default, tuning, 1f / 60);
                Assert.That(result.z, Is.EqualTo(13.25f).Within(.001f), "coasting with released movement keys");
                result = SlideMovement.Integrate(Vector3.forward, default, tuning, 1f / 60);
                Assert.That(result.z, Is.LessThanOrEqualTo(1), "collision-reduced speed must never gain a boost");
            }
            finally { Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void ActorSlideTravelsExpectedDistanceThenCrouchesAndCannotPassThroughWall()
        {
            var root = new GameObject("Slide collision") { layer = 9 };
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            tuning.GroundSpeed = 10; tuning.GroundAcceleration = 10; tuning.SlideMinimumSpeed = 11;
            try
            {
                var actor = root.AddComponent<ArenaActor>(); actor.Controller = root.AddComponent<CharacterController>();
                actor.Controller.radius = .36f;
                Vector3 origin = new(3000, 100, 3000);
                wall.transform.position = origin + new Vector3(0, 1, 20);
                wall.transform.localScale = new Vector3(5, 3, .5f); Physics.SyncTransforms();
                void Begin()
                {
                    actor.Place(origin); actor.Velocity = Vector3.forward * 13.5f;
                    actor.Command = Sprint; actor.GroundedOverride = true; actor.Move(tuning);
                    actor.Command = Slide;
                }
                Begin(); float start = actor.transform.position.z;
                for (int i = 0; i < 36; i++) { actor.GroundedOverride = true; actor.Move(tuning); actor.Command = Crouch; }
                Assert.That(actor.transform.position.z - start, Is.EqualTo(5.325f).Within(.02f));
                Assert.That(actor.IsSliding, Is.False);
                Assert.That(actor.IsCrouched, Is.True);
                actor.GroundedOverride = true; actor.Move(tuning);
                Assert.That(actor.Velocity.z, Is.EqualTo(4.5f).Within(.001f));
                wall.transform.position = origin + new Vector3(0, 1, 3); Physics.SyncTransforms();
                Begin();
                for (int i = 0; i < 36; i++) { actor.GroundedOverride = true; actor.Move(tuning); actor.Command = Crouch; }
                Assert.That(actor.transform.position.z - origin.z, Is.LessThan(2.7f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(wall); Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void SnapshotRoundTripsSlideHistoryBoundsTimersAndRejectsPreviousProtocol()
        {
            var state = new DuelSnapshot();
            state.Actors[0] = new ActorSnapshot { Life = 1, Health = 100, Crouched = true,
                Slide = new SlideState { RemainingTicks = 20, CooldownTicks = 68, CrouchHeld = true, WasSprinting = true } };
            state.Actors[1] = new ActorSnapshot { Life = 1, Health = 100 };
            byte[] bytes = DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state });
            Assert.That(DuelProtocol.TryDecode(bytes, out var decoded), Is.True);
            Assert.That(decoded.Snapshot.Actors[0].Slide, Is.EqualTo(state.Actors[0].Slide));
            bytes[4] = (byte)(DuelProtocol.Version - 1);
            Assert.That(DuelProtocol.TryDecode(bytes, out _), Is.False);
            state.Actors[0].Slide.RemainingTicks = 256;
            Assert.Throws<System.ArgumentException>(() => DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state }));
        }

        [Test]
        public void SlideUsesCrouchClearanceAndPlacementResetsTimersAndInputHistory()
        {
            var root = new GameObject("Slide clearance") { layer = 9 };
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                root.transform.position = new Vector3(3000, 100, 3000);
                var actor = root.AddComponent<ArenaActor>(); actor.Controller = root.AddComponent<CharacterController>();
                actor.Controller.radius = .36f; actor.SetCrouched(false);
                actor.Slide = new SlideState { RemainingTicks = 2, CooldownTicks = 50, CrouchHeld = true };
                actor.Velocity = Vector3.forward * 5;
                roof.transform.position = root.transform.position + Vector3.up * 1.5f;
                roof.transform.localScale = new Vector3(10, .1f, 10); Physics.SyncTransforms();
                for (int i = 0; i < 3; i++) { actor.GroundedOverride = true; actor.Move(tuning); }
                Assert.That(actor.IsSliding, Is.False);
                Assert.That(actor.IsCrouched, Is.True);
                Assert.That(actor.Controller.height, Is.EqualTo(ArenaActor.CrouchingHeight));
                Object.DestroyImmediate(roof); Physics.SyncTransforms(); actor.GroundedOverride = true; actor.Move(tuning);
                Assert.That(actor.IsCrouched, Is.False);
                actor.Place(new Vector3(3000, 100, 3000));
                Assert.That(actor.Slide, Is.EqualTo(default(SlideState)));
            }
            finally { Object.DestroyImmediate(root); if (roof != null) Object.DestroyImmediate(roof); Object.DestroyImmediate(tuning); }
        }
    }
}



