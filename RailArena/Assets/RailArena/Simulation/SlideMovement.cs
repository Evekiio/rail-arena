using UnityEngine;

namespace RailArena.Simulation
{
    /// <summary>Tick-based state restored before client prediction replay.</summary>
    public struct SlideState
    {
        public int RemainingTicks;
        public int CooldownTicks;
        public bool CrouchHeld;
        public bool WasSprinting;
        public bool Active => RemainingTicks > 0;
    }

    public static class SlideMovement
    {
        public const int DurationTicks = 36;
        public const int CooldownTicks = 48;
        public const float MinimumSpeed = 11;
        public const float Deceleration = 15;
        public const float SteeringDegreesPerSecond = 45;

        public static bool Step(ref SlideState state, Vector3 velocity, MovementCommand command, bool grounded, MovementTuning tuning = null)
        {
            if (state.CooldownTicks > 0) state.CooldownTicks--;
            if (!grounded || command.Jump) state.RemainingTicks = 0;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            bool sprinting = grounded && !command.Jump && command.Sprint && !command.Walk &&
                !command.Crouch && command.Move.y > 0;
            if (!state.Active && state.CooldownTicks == 0 && grounded && !command.Jump &&
                command.Slide && !command.Crouch &&
                command.Sprint && !command.Walk && command.Move.y > 0 && speed >= (tuning != null ? tuning.SlideMinimumSpeed : MinimumSpeed))
            {
                state.RemainingTicks = tuning != null ? tuning.SlideDurationTicks : DurationTicks;
                state.CooldownTicks = state.RemainingTicks + (tuning != null ? tuning.SlideCooldownTicks : CooldownTicks);
            }
            state.CrouchHeld = command.Crouch;
            state.WasSprinting = sprinting;
            bool sliding = state.Active;
            if (sliding) state.RemainingTicks--;
            return sliding;
        }

        public static Vector3 Integrate(Vector3 velocity, MovementCommand command, MovementTuning tuning, float delta)
        {
            Vector3 horizontal = new(velocity.x, 0, velocity.z);
            float speed = Mathf.Min(horizontal.magnitude, Mathf.Max(tuning.GroundSpeed * tuning.CrouchSpeedMultiplier,
                horizontal.magnitude - tuning.SlideDeceleration * delta));
            horizontal = horizontal.normalized;
            Vector2 input = Vector2.ClampMagnitude(command.Move, 1);
            Vector3 wish = Quaternion.Euler(0, command.Yaw, 0) * new Vector3(input.x, 0, input.y);
            if (wish.sqrMagnitude > 0)
                horizontal = Vector3.RotateTowards(horizontal.normalized, wish.normalized,
                    tuning.SlideSteeringDegrees * Mathf.Deg2Rad * delta, 0);
            return new Vector3(horizontal.x * speed, -1 - tuning.Gravity * delta, horizontal.z * speed);
        }
    }
}

