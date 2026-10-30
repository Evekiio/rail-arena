using UnityEngine;
namespace RailArena
{
    public enum TraversalSound { PortalIdle, PadIdle, PortalEnter, PortalExit, PadLaunch }
    /// <summary>Original mono synthesis with seamless idle loops and bounded event envelopes.</summary>
    public static class TraversalAudio
    {
        public const int SampleRate = 44100;
        public static AudioClip Create(TraversalSound sound)
        {
            bool loop = sound == TraversalSound.PortalIdle || sound == TraversalSound.PadIdle;
            float duration = loop ? 2 : sound == TraversalSound.PadLaunch ? .8f : .7f;
            var data = new float[Mathf.RoundToInt(duration * SampleRate)];
            var random = new System.Random(811 + (int)sound); float noise = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)SampleRate; float signal;
                noise = Mathf.Lerp(noise, (float)random.NextDouble() * 2 - 1, .12f);
                if (sound == TraversalSound.PortalIdle)
                    signal = (.45f * Sin(96 * t) + .2f * Sin(144 * t) + .1f * Sin(432 * t)) * (.16f + .035f * Sin(.5f * t));
                else if (sound == TraversalSound.PadIdle)
                    signal = (.4f * Sin(120 * t + .1f * Sin(t)) + .15f * Sin(480 * t)) * .2f;
                else if (sound == TraversalSound.PortalEnter)
                    signal = (.38f * Sin(620 * t - 360 * t * t) + .2f * noise + .2f * Sin(65 * t)) * Mathf.Exp(-t * 3.8f);
                else if (sound == TraversalSound.PortalExit)
                    signal = (.3f * Sin(120 * t + 440 * t * t) + .18f * Sin(360 * t) + .2f * noise) * Mathf.Exp(-t * 4);
                else
                    signal = (.46f * Sin(70 * t) * Mathf.Exp(-t * 12) + .3f * Sin(180 * t + 1100 * t * t) + .23f * noise) * Mathf.Exp(-t * 3.5f);
                if (!loop) signal *= Mathf.Min(1, t / .008f) * Mathf.Clamp01((duration - t) / .09f);
                data[i] = Mathf.Clamp(signal, -.75f, .75f);
            }
            var clip = AudioClip.Create("Original traversal " + sound, data.Length, 1, SampleRate, false); clip.SetData(data, 0); return clip;
        }
        private static float Sin(float cycles) => Mathf.Sin(2 * Mathf.PI * cycles);
    }
}
