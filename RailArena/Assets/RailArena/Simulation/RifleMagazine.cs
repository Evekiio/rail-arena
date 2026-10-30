namespace RailArena.Simulation
{
    /// <summary>Shared authority/prediction state. Reserve is unlimited; only the loaded magazine is consumed.</summary>
    [System.Serializable]
    public struct RifleMagazine
    {
        public const int Capacity = 30;
        public const int ReloadTicks = 120;
        public int Rounds;
        public int ReloadEndTick;
        public int CadenceRemainder;
        public int LastShotInterval;
        public bool Reloading => ReloadEndTick > 0;
        public static RifleMagazine Full => new() { Rounds = Capacity, LastShotInterval = 15 };

        public void Advance(int tick, bool reload)
        {
            if (Reloading && tick >= ReloadEndTick)
            { Rounds = Capacity; ReloadEndTick = 0; }
            if (reload && !Reloading && Rounds < Capacity)
                ReloadEndTick = tick + ReloadTicks;
        }

        public bool TryFire(int tick, ref int nextShotTick)
        {
            if (Reloading || Rounds <= 0 || !CombatRules.CanFire(tick, nextShotTick, true)) return false;
            Rounds--;
            // 60 Hz / (250 RPM / 60) = 72/5 ticks. Preserve the fractional phase in snapshots.
            int interval = 72 + CadenceRemainder;
            LastShotInterval = interval / 5;
            CadenceRemainder = interval % 5;
            nextShotTick = tick + LastShotInterval;
            return true;
        }
    }
}
