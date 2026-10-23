using UnityEngine;

namespace RailArena
{
    public enum RailCue { Shot, Hit, Elimination, Hurt }

    /// <summary>Original deterministic synthesis. No external samples or runtime random state.</summary>
    public static class RailAudio
    {
        public const int SampleRate = 44100;

        public static AudioClip Create(RailCue cue)
        {
            float duration = cue == RailCue.Shot ? .55f : cue == RailCue.Elimination ? .24f : .14f;
            var samples = new float[Mathf.CeilToInt(duration * SampleRate)];
            var random = new System.Random(197 + (int)cue);
            float filteredNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2 - 1;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, .18f);
                float attack = Mathf.Min(1, t / .0015f);
                float release = Mathf.Clamp01((duration - t) / .025f);
                float signal;
                if (cue == RailCue.Shot)
                {
                    float crack = noise * Mathf.Exp(-t * 145);
                    float body = Mathf.Sin(2 * Mathf.PI * (145 * t + 28 * (1 - Mathf.Exp(-t * 22)))) * Mathf.Exp(-t * 16);
                    float coil = Mathf.Sin(2 * Mathf.PI * (1550 * t - 950 * t * t)) * Mathf.Exp(-t * 11);
                    signal = .45f * crack + .34f * body + .18f * coil + .16f * filteredNoise * Mathf.Exp(-t * 12);
                }
                else if (cue == RailCue.Hurt)
                    signal = (.5f * filteredNoise + .32f * Mathf.Sin(2 * Mathf.PI * 95 * t)) * Mathf.Exp(-t * 27);
                else
                {
                    float phase = cue == RailCue.Elimination ? 1050 * Mathf.Min(t, .075f) + 1575 * Mathf.Max(0, t - .075f) : 1350 * t;
                    float envelope = cue == RailCue.Elimination ? Mathf.Exp(-t * 13) : Mathf.Exp(-t * 38);
                    signal = (.48f * Mathf.Sin(2 * Mathf.PI * phase) + .12f * noise) * envelope;
                }
                samples[i] = Mathf.Clamp(signal * attack * release, -.9f, .9f);
            }
            var clip = AudioClip.Create("Original rail " + cue, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
