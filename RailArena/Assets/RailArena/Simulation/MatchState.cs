using System;
using System.Collections.Generic;

namespace RailArena.Simulation
{
    public enum MatchPhase : byte { Countdown, Playing, SuddenDeath, Results }

    /// <summary>Authority owns this state in every mode. Presentation observes it.</summary>
    public sealed class MatchState
    {
        private readonly Dictionary<int, int> scores = new();
        private readonly HashSet<int> tiedLeaders = new();
        public IReadOnlyDictionary<int, int> Scores => scores;
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
            }
            int minimum = practice ? 1 : 2;
            if (scores.Count < minimum || scores.Count > 8)
                throw new ArgumentException($"This mode requires {minimum} to eight participants.", nameof(participants));
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
            if (Phase == MatchPhase.SuddenDeath && tiedLeaders.Contains(killer)) Finish(killer);
            return true;
        }

        public bool RecordVoidDeath(int victim)
        {
            if (!IsActive || !scores.ContainsKey(victim)) return false;
            scores[victim]--;
            return true;
        }

        public bool IsActive => Phase == MatchPhase.Playing || Phase == MatchPhase.SuddenDeath;

        public static MatchState FromAuthority(int tick, MatchPhase phase, int winner, int firstScore, int secondScore)
        {
            if (tick < 0 || phase > MatchPhase.Results || winner < -1 || winner > 1 ||
                (phase == MatchPhase.Results) != (winner >= 0))
                throw new ArgumentException("Invalid authoritative match state.");
            var state = new MatchState(new[] { 0, 1 }, false) { Tick = tick, Phase = phase, Winner = winner };
            state.scores[0] = firstScore;
            state.scores[1] = secondScore;
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
