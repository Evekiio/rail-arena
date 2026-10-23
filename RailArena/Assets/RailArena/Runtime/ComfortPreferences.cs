using UnityEngine;

namespace RailArena
{
    public sealed class ComfortPreferences
    {
        public const string FlashKey = "reducedFlash", MotionKey = "weaponMotion";
        public bool ReducedFlash { get; set; }
        public bool WeaponMotion { get; set; } = true;
        public float DamageEdgeOpacity => ReducedFlash ? .175f : .7f;

        public void Load()
        {
            ReducedFlash = PlayerPrefs.GetInt(FlashKey, 0) == 1;
            WeaponMotion = PlayerPrefs.GetInt(MotionKey, 1) != 0;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(FlashKey, ReducedFlash ? 1 : 0);
            PlayerPrefs.SetInt(MotionKey, WeaponMotion ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
