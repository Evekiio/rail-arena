using UnityEngine;

namespace RailArena
{
    public sealed class SoundPreferences
    {
        public const string MasterKey = "volume", EffectsKey = "effectsVolume";
        private float master = .65f, effects = 1;
        public float Master { get => master; set => master = Normalize(value, .65f); }
        public float Effects { get => effects; set => effects = Normalize(value, 1); }
        private static float Normalize(float value, float fallback) => float.IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        public void Load()
        {
            Master = PlayerPrefs.GetFloat(MasterKey, .65f);
            Effects = PlayerPrefs.GetFloat(EffectsKey, 1);
        }
        public void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, Master);
            PlayerPrefs.SetFloat(EffectsKey, Effects);
            PlayerPrefs.Save();
        }
        public void ApplyMaster() => AudioListener.volume = Master;
    }
}
