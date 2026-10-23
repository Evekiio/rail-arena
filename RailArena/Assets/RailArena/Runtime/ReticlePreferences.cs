using UnityEngine;

namespace RailArena
{
    public sealed class ReticlePreferences
    {
        public const string SizeKey = "reticleSize", ColorKey = "reticleColor";
        public const float MinimumSize = 18, MaximumSize = 64, DefaultSize = 36;
        public const int ColorCount = 5;
        private float size = DefaultSize;
        private int colorIndex;
        public float Size
        {
            get => size;
            set => size = float.IsNaN(value) || float.IsInfinity(value) ? DefaultSize : Mathf.Clamp(value, MinimumSize, MaximumSize);
        }
        public int ColorIndex { get => colorIndex; set => colorIndex = value >= 0 && value < ColorCount ? value : 0; }
        public string ColorName => ColorIndex switch { 1 => "WHITE", 2 => "LIME", 3 => "MAGENTA", 4 => "AMBER", _ => "CYAN" };
        public Color Color => ColorIndex switch
        {
            1 => UnityEngine.Color.white, 2 => new Color(.65f, 1, .2f), 3 => new Color(1, .35f, .9f),
            4 => new Color(1, .7f, .3f), _ => new Color(.25f, .95f, 1)
        };

        public void Load()
        {
            Size = PlayerPrefs.GetFloat(SizeKey, DefaultSize);
            ColorIndex = PlayerPrefs.GetInt(ColorKey, 0);
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(SizeKey, Size);
            PlayerPrefs.SetInt(ColorKey, ColorIndex);
            PlayerPrefs.Save();
        }

        public void Reset() { Size = DefaultSize; ColorIndex = 0; }
    }
}
