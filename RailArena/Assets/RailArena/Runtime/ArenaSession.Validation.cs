#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaSession
    {
        public bool ValidationMovementModes { get; set; }

        private Simulation.MovementCommand ValidationMovement(Simulation.MovementCommand command, int tick)
            => ValidationMovementInput.Apply(command, tick, ValidationMovementModes);

        // Deterministic standalone scenarios own the clock while the ordinary FixedUpdate is paused.
        // Gameplay still passes through exactly the same authority method as offline and host play.
        internal void AdvanceValidationTick()
        {
            if (!Paused || IsOnline || !Running)
                throw new InvalidOperationException("A validation tick requires a paused offline session.");
            Paused = false;
            try { AdvanceAuthority(); }
            finally { Paused = true; }
        }
    }

    public static class ValidationMovementInput
    {
        public static Simulation.MovementCommand Apply(Simulation.MovementCommand command, int tick, bool enabled)
        {
            if (!enabled) return command;
            int mode = tick / 180 % 4;
            command.Sprint = mode == 1;
            command.Walk = mode == 2;
            command.Crouch = mode == 3;
            command.Slide = mode == 1 && tick % 180 >= 30;
            if (mode == 1)
            {
                command.Jump = false;
                if (command.Move.sqrMagnitude > .0001f)
                {
                    // The bot aims at opponents while following paths sideways/backward. Sprint and
                    // slide require forward input: face the same route instead of only setting a flag.
                    Vector3 travel = Quaternion.Euler(0, command.Yaw, 0)*new Vector3(command.Move.x, 0, command.Move.y);
                    command.Yaw = Mathf.Atan2(travel.x, travel.z)*Mathf.Rad2Deg;
                    command.Move = new Vector2(0, command.Move.magnitude);
                }
            }
            return command;
        }
    }
}
#endif
