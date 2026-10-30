using System;
using System.Collections.Generic;

namespace RailArena.Simulation
{
    public enum MatchPhase : byte { Countdown, Playing, SuddenDeath, Results }

    /// <summary>Authority owns this state in every mode. Presentation observes it.</summary>
    public sealed class MatchState
    {
        private readonly Dictionary<int, int> scores = new();
        private readonly Dictionary<int, CombatStatistics> statistics = new();
        private readonly HashSet<int> tiedLeaders = new();
        public IReadOnlyDictionary<int, int> Scores => scores;
        public IReadOnlyDictionary<int, CombatStatistics> Statistics => statistics;
        public MatchPhase Phase { get; private set; }
        public int Tick { get; private set; }
        public int Winner { get; private set; } = -1;
        public bool Practice { get; }
        public int RemainingTicks => Math.Max(0, CombatRules.CountdownTicks + CombatRules.MatchTicks - Tick);

        public MatchState(IEnumerable<int> participants, bool practice)
        {
            foreach (int id in participants)
            {
                if (id < 0 || !scores.TryAdd(id, 0))
                    throw new ArgumentException("Participant IDs must be unique and nonnegative.", nameof(participants));
                statistics.Add(id,default);
            }
            int minimum = practice ? 1 : 2;
            if (scores.Count < minimum || scores.Count > CombatRules.MaximumContenders)
                throw new ArgumentException($"This mode requires {minimum} to {CombatRules.MaximumContenders} participants.", nameof(participants));
            Practice = practice;
            Phase = MatchPhase.Countdown;
        }

        public void Advance()
        {
            if (Phase == MatchPhase.Results) return;
            Tick++;
            if (Phase == MatchPhase.Countdown && Tick >= CombatRules.CountdownTicks)
                Phase = MatchPhase.Playing;
            if (!Practice && Phase == MatchPhase.Playing && RemainingTicks == 0)
                ResolveTimeLimit();
        }

        public bool RecordKill(int killer, int victim)
        {
            if (!IsActive || killer == victim || !scores.ContainsKey(killer) || !scores.ContainsKey(victim))
                return false;
            scores[killer]++;
            var a=statistics[killer]; var b=statistics[victim];
            statistics[killer]=new CombatStatistics(a.Kills+1,a.Deaths,a.Shots,a.Hits,a.Headshots,a.VoidDeaths);
            statistics[victim]=new CombatStatistics(b.Kills,b.Deaths+1,b.Shots,b.Hits,b.Headshots,b.VoidDeaths);
            if (Phase == MatchPhase.SuddenDeath && tiedLeaders.Contains(killer)) Finish(killer);
            return true;
        }

        public bool RecordVoidDeath(int victim)
        {
            if (!IsActive || !scores.ContainsKey(victim)) return false;
            scores[victim]--;
            var value=statistics[victim];
            statistics[victim]=new CombatStatistics(value.Kills,value.Deaths+1,value.Shots,value.Hits,value.Headshots,value.VoidDeaths+1);
            return true;
        }

        public bool RecordShot(int shooter, HitRegion? hit)
        {
            if (!IsActive || !statistics.ContainsKey(shooter) || (hit.HasValue && hit.Value > HitRegion.Limb)) return false;
            var value=statistics[shooter];
            statistics[shooter]=new CombatStatistics(value.Kills,value.Deaths,value.Shots+1,
                value.Hits+(hit.HasValue?1:0),value.Headshots+(hit==HitRegion.Head?1:0),value.VoidDeaths);
            return true;
        }

        public bool IsActive => Phase == MatchPhase.Playing || Phase == MatchPhase.SuddenDeath;

        public static MatchState FromAuthority(int tick, MatchPhase phase, int winner, int firstScore, int secondScore,
            CombatStatistics firstStatistics = default, CombatStatistics secondStatistics = default)
        {
            if (tick < 0 || phase > MatchPhase.Results || winner < -1 || winner > 1 ||
                (phase == MatchPhase.Results) != (winner >= 0))
                throw new ArgumentException("Invalid authoritative match state.");
            var state = new MatchState(new[] { 0, 1 }, false) { Tick = tick, Phase = phase, Winner = winner };
            state.scores[0] = firstScore;
            state.scores[1] = secondScore;
            state.statistics[0]=firstStatistics; state.statistics[1]=secondStatistics;
            return state;
        }

        private void ResolveTimeLimit()
        {
            int best = int.MinValue;
            foreach (var entry in scores)
            {
                if (entry.Value > best)
                {
                    best = entry.Value;
                    tiedLeaders.Clear();
                }
                if (entry.Value == best) tiedLeaders.Add(entry.Key);
            }
            if (tiedLeaders.Count == 1)
            {
                foreach (int leader in tiedLeaders) Finish(leader);
            }
            else Phase = MatchPhase.SuddenDeath;
        }

        private void Finish(int winner)
        {
            Winner = winner;
            Phase = MatchPhase.Results;
        }
    }
}
