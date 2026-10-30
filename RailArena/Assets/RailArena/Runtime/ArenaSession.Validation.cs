#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace RailArena
{
    public sealed partial class ArenaSession
    {
        public bool ValidationMovementModes { get; set; }

        private Simulation.MovementCommand ValidationMovement(Simulation.MovementCommand command, int tick)
        {
            if (!ValidationMovementModes) return command;
            int mode = tick / 180 % 4;
            command.Sprint = mode == 1;
            command.Walk = mode == 2;
            command.Crouch = mode == 3;
            command.Slide = mode == 1 && tick % 180 >= 120 && tick % 180 < 156;
            if (mode == 1) command.Jump = false;
            return command;
        }

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
}
#endif
