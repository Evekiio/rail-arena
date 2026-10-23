using System;

namespace RailArena.Simulation
{
    public readonly struct CombatStatistics
    {
        public int Kills { get; }
        public int Deaths { get; }
        public int Shots { get; }
        public int Hits { get; }
        public int Headshots { get; }
        public int VoidDeaths { get; }
        public float Accuracy => Shots == 0 ? 0 : 100f * Hits / Shots;

        public CombatStatistics(int kills, int deaths, int shots, int hits, int headshots, int voidDeaths)
        {
            if (kills < 0 || deaths < 0 || shots < 0 || hits < 0 || headshots < 0 || voidDeaths < 0 ||
                hits > shots || headshots > hits || voidDeaths > deaths)
                throw new ArgumentException("Invalid combat statistics.");
            Kills=kills; Deaths=deaths; Shots=shots; Hits=hits; Headshots=headshots; VoidDeaths=voidDeaths;
        }
    }
}
