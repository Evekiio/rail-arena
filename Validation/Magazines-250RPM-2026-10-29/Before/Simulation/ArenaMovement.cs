using UnityEngine;

namespace RailArena.Simulation
{
    public struct MovementCommand
    {
        public Vector2 Move;
        public float Yaw;
        public float Pitch;
        public bool Jump;
        public bool Fire;
        public bool Sprint;
        public bool Walk;
        public bool Crouch;
        public int Sequence;
    }

    /// <summary>Shared velocity integration. Collision resolution is supplied by the world.</summary>
    public static class ArenaMovement
    {
        public static Vector3 Integrate(Vector3 velocity, MovementCommand command,
            bool grounded, MovementTuning tuning, float delta)
        {
            var input = Vector2.ClampMagnitude(command.Move, 1);
            var wish = Quaternion.Euler(0, command.Yaw, 0) * new Vector3(input.x, 0, input.y);
            var horizontal = new Vector3(velocity.x, 0, velocity.z);
            float speedScale = command.Crouch ? tuning.CrouchSpeedMultiplier : command.Walk ? tuning.WalkSpeedMultiplier :
                command.Sprint && input.y > 0 ? tuning.SprintSpeedMultiplier : 1;
            // Deliberate grounded slow movement must also shed inherited hop/pad momentum.
            // Airborne momentum and the unmodified movement path remain unchanged.
            if (grounded && (command.Crouch || command.Walk))
                horizontal = Vector3.ClampMagnitude(horizontal, tuning.GroundSpeed * speedScale);
            bool jumping = grounded && command.Jump;
            if (grounded && !jumping)
            {
                float speed = horizontal.magnitude;
                if (speed > 0)
                {
                    float drop = Mathf.Max(speed, tuning.StopSpeed) * tuning.Friction * delta;
                    horizontal *= Mathf.Max(0, speed - drop) / speed;
                }
            }

            if (wish.sqrMagnitude > 0)
            {
                float wishSpeed = tuning.GroundSpeed * speedScale * input.magnitude;
                Vector3 direction = wish.normalized;
                float remaining = wishSpeed - Vector3.Dot(horizontal, direction);
                if (remaining > 0)
                {
                    float acceleration = grounded ? tuning.GroundAcceleration : tuning.AirAcceleration;
                    horizontal += direction * Mathf.Min(remaining, acceleration * wishSpeed * delta);
                }
            }

            float vertical = jumping ? tuning.JumpSpeed : grounded ? -1 : velocity.y;
            vertical -= tuning.Gravity * delta;
            return new Vector3(horizontal.x, vertical, horizontal.z);
        }
    }
}
