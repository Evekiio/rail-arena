namespace RailArena
{
    /// <summary>Held fire repeats on simulation ticks; a quick tap survives until one sample.</summary>
    public sealed class WeaponTrigger
    {
        private bool held, pending;
        public void Sample(bool isHeld, bool pressedThisFrame, bool allowed)
        {
            if (!allowed) { Reset(); return; }
            held = isHeld;
            pending |= pressedThisFrame;
        }
        public bool Consume() { bool fire = held || pending; pending = false; return fire; }
        public void Reset() { held = pending = false; }
    }
}
