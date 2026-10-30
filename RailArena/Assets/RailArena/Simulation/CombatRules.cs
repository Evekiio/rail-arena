using System;

namespace RailArena.Simulation
{
    public enum HitRegion : byte { Head, Body, Limb }

    /// <summary>Integer ticks keep authority, prediction and tests on the same clock.</summary>
    public static class CombatRules
    {
        public const int TickRate = 60;
        public const int MaximumHealth = 100;
        public const int MaximumContenders = 4;
        public const int MaximumBots = MaximumContenders - 1;
        public const int ShotIntervalTicks = 15; // Maximum quantized interval; RifleMagazine schedules exact 250 RPM.
        public const int RoundsPerMinute = 250;
        public const int RespawnTicks = 120;
        public const int PickupRespawnTicks = 1200;
        public const int CountdownTicks = 180;
        public const int MatchTicks = 36000;
        public const int MaximumRewindTicks = 12;

        public static int Damage(HitRegion region) => region switch
        {
            HitRegion.Head => 100,
            HitRegion.Body => 50,
            HitRegion.Limb => 34,
            _ => throw new ArgumentOutOfRangeException(nameof(region))
        };

        public static int ApplyDamage(int health, HitRegion region) =>
            Math.Max(0, health - Damage(region));

        public static int RestoreHealth(int health) =>
            health <= 0 ? 0 : Math.Min(MaximumHealth, health + 25);

        public static bool CanFire(int tick, int nextShotTick, bool alive) =>
            alive && tick >= nextShotTick;
    }
}
