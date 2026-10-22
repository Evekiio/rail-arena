#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace RailArena
{
    public sealed partial class ArenaSession
    {
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
