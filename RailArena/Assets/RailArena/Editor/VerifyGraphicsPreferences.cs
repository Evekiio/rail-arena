using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace RailArena.Editor
{
    public static class VerifyGraphicsPreferences
    {
        public static void Check()
        {
            var keys = new[] { GraphicsPreferences.AntialiasingKey, GraphicsPreferences.MotionBlurKey, GraphicsPreferences.TierKey };
            var existed = new bool[keys.Length]; var old = new int[keys.Length];
            for (var i = 0; i < keys.Length; i++) { existed[i] = PlayerPrefs.HasKey(keys[i]); old[i] = PlayerPrefs.GetInt(keys[i]); }
            var root = new GameObject("Graphics validation"); var other = new GameObject("Other camera");
            try
            {
                var camera = root.AddComponent<Camera>(); var otherCamera = other.AddComponent<Camera>();
                otherCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
                var setting = new GraphicsPreferences { Smaa = false }; setting.Save();
                var loaded = new GraphicsPreferences(); loaded.Load(); loaded.Apply(camera);
                if (loaded.Smaa || camera.GetUniversalAdditionalCameraData().antialiasing != AntialiasingMode.None) throw new System.Exception("Off setting failed");
                loaded.Smaa = true; loaded.Save(); var restored = new GraphicsPreferences(); restored.Load(); restored.Apply(camera);
                if (!restored.Smaa || camera.GetUniversalAdditionalCameraData().antialiasing != AntialiasingMode.SubpixelMorphologicalAntiAliasing || otherCamera.GetUniversalAdditionalCameraData().antialiasing != AntialiasingMode.None) throw new System.Exception("SMAA setting or camera isolation failed");
                Debug.Log("GRAPHICS_PREFERENCES_VERIFIED");
            }
            finally { for (var i = 0; i < keys.Length; i++) { if (existed[i]) PlayerPrefs.SetInt(keys[i], old[i]); else PlayerPrefs.DeleteKey(keys[i]); } PlayerPrefs.Save(); Object.DestroyImmediate(root); Object.DestroyImmediate(other); }
        }
    }
}
