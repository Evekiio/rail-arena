using UnityEngine;

namespace RailArena
{
    public sealed class FramePacingPreferences
    {
        public const string SyncKey = "frameVSync", LimitKey = "frameLimit";
        private static readonly int[] Limits = { 60, 120, 144, 240, -1 };
        private int limit = 144;
        public bool VSync { get; set; }
        public int Limit
        {
            get => limit;
            set => limit = System.Array.IndexOf(Limits, value) >= 0 ? value : 144;
        }
        public string LimitLabel => Limit < 0 ? "UNLIMITED" : Limit + " FPS";
        public void CycleLimit() => Limit = Limits[(System.Array.IndexOf(Limits, Limit) + 1) % Limits.Length];
        public void Load()
        {
            VSync = PlayerPrefs.GetInt(SyncKey, 0) == 1;
            Limit = PlayerPrefs.GetInt(LimitKey, 144);
        }
        public void Save()
        {
            PlayerPrefs.SetInt(SyncKey, VSync ? 1 : 0);
            PlayerPrefs.SetInt(LimitKey, Limit);
            PlayerPrefs.Save();
        }
        public void Apply()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : Limit;
        }
    }
}
