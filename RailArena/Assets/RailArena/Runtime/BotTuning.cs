using System;
using UnityEngine;

namespace RailArena
{
    [CreateAssetMenu(menuName = "Rail Arena/Bot tuning")]
    public sealed class BotTuning : ScriptableObject
    {
        [Serializable]
        public struct DifficultyProfile
        {
            [Range(1, 120)] public int ReactionTicks;
            [Range(0, 20)] public float AimErrorDegrees;
            [Range(1, 720)] public float TurnDegreesPerSecond;
            [Range(1, 120)] public int DecisionIntervalTicks;

            public DifficultyProfile(int reaction, float error, float turn, int decision)
            { ReactionTicks = reaction; AimErrorDegrees = error; TurnDegreesPerSecond = turn; DecisionIntervalTicks = decision; }
        }

        public DifficultyProfile Cadet = new(48, 5, 150, 39);
        public DifficultyProfile Veteran = new(27, 2.5f, 240, 27);
        public DifficultyProfile Ace = new(14, 1, 330, 15);
        [Range(1, 120)] public int AimRefreshTicks = 12;
        [Range(0, 100)] public int SeekHealthAtOrBelow = 50;
        [Range(.1f, 45)] public float FireYawToleranceDegrees = 9;
        [Range(0, 1)] public float AimBelowEyeMetres = .35f;

        public BotSettings SettingsFor(int difficulty)
        {
            Validate();
            var profile = difficulty switch
            {
                0 => Cadet, 1 => Veteran, 2 => Ace,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };
            return new BotSettings(profile, AimRefreshTicks, SeekHealthAtOrBelow, FireYawToleranceDegrees, AimBelowEyeMetres);
        }

        public void Validate()
        {
            foreach (var profile in new[] { Cadet, Veteran, Ace })
            {
                Require(profile.ReactionTicks, 1, 120, "reaction ticks");
                Require(profile.AimErrorDegrees, 0, 20, "aim error");
                Require(profile.TurnDegreesPerSecond, 1, 720, "turn rate");
                Require(profile.DecisionIntervalTicks, 1, 120, "decision interval");
            }
            Require(AimRefreshTicks, 1, 120, "aim refresh interval");
            Require(SeekHealthAtOrBelow, 0, 100, "health-seeking threshold");
            Require(FireYawToleranceDegrees, .1f, 45, "fire yaw tolerance");
            Require(AimBelowEyeMetres, 0, 1, "aim offset");
        }

        private static void Require(float value, float minimum, float maximum, string name)
        {
            if (!(value >= minimum && value <= maximum))
                throw new ArgumentException($"Bot {name} must be finite and between {minimum} and {maximum}.");
        }
    }

    /// <summary>A validated session snapshot, unaffected by later Inspector edits to the source asset.</summary>
    public readonly struct BotSettings
    {
        public readonly int ReactionTicks, DecisionIntervalTicks, AimRefreshTicks, SeekHealthAtOrBelow;
        public readonly float AimErrorDegrees, TurnDegreesPerSecond, FireYawToleranceDegrees, AimBelowEyeMetres;

        internal BotSettings(BotTuning.DifficultyProfile profile, int refresh, int health, float tolerance, float offset)
        {
            ReactionTicks = profile.ReactionTicks; DecisionIntervalTicks = profile.DecisionIntervalTicks;
            AimErrorDegrees = profile.AimErrorDegrees; TurnDegreesPerSecond = profile.TurnDegreesPerSecond;
            AimRefreshTicks = refresh; SeekHealthAtOrBelow = health;
            FireYawToleranceDegrees = tolerance; AimBelowEyeMetres = offset;
        }
    }
}
