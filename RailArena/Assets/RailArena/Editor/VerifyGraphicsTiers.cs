using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public static class VerifyGraphicsTiers
    {
        public static void CheckWithAntialiasing() { Check(); VerifyGraphicsPreferences.Check(); }
        public static void Check()
        {
            var keys = new[] { GraphicsPreferences.TierKey, GraphicsPreferences.AntialiasingKey, GraphicsPreferences.MotionBlurKey };
            var existed = new bool[keys.Length]; var old = new int[keys.Length];
            for (var i = 0; i < keys.Length; i++) { existed[i] = PlayerPrefs.HasKey(keys[i]); old[i] = PlayerPrefs.GetInt(keys[i]); }
            var previousPipeline = QualitySettings.renderPipeline;
            var source = previousPipeline as UniversalRenderPipelineAsset;
            if (source == null) throw new Exception("Expected PC URP quality asset");
            var originalLod = QualitySettings.lodBias;
            var initialScale = source.renderScale; var initialDistance = source.shadowDistance;
            var initialCascades = source.shadowCascadeCount; var initialResolution = source.mainLightShadowmapResolution;
            var root = new GameObject("Graphics tier validation"); var settings = new GraphicsPreferences();
            try
            {
                var camera = root.AddComponent<Camera>();
                PlayerPrefs.DeleteKey(GraphicsPreferences.TierKey);
                settings.Load(); settings.Apply(camera);
                if (settings.Tier != GraphicsTier.High || QualitySettings.renderPipeline != previousPipeline) throw new Exception("Default changed the PC pipeline");
                foreach (var tier in new[] { GraphicsTier.Balanced, GraphicsTier.Performance })
                {
                    settings.Tier = tier; settings.Save();
                    var loaded = new GraphicsPreferences(); loaded.Load();
                    if (loaded.Tier != tier) throw new Exception("Tier did not persist");
                    settings.Apply(camera);
                    var active = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                    var profile = GraphicsTierProfiles.Get(tier);
                    if (active == null || active == source || Mathf.Abs(active.renderScale - profile.RenderScale) > .001f ||
                        Mathf.Abs(active.shadowDistance - profile.ShadowDistance) > .001f || active.shadowCascadeCount != profile.ShadowCascades ||
                        active.mainLightShadowmapResolution != profile.MainShadowResolution || Mathf.Abs(QualitySettings.lodBias - profile.LodBias) > .001f)
                        throw new Exception("Tier was not applied: " + tier);
                    if (source.renderScale != initialScale || source.shadowDistance != initialDistance ||
                        source.shadowCascadeCount != initialCascades || source.mainLightShadowmapResolution != initialResolution)
                        throw new Exception("Source pipeline asset was modified");
                }
                settings.Tier = GraphicsTier.High; settings.Apply(camera);
                if (QualitySettings.renderPipeline != previousPipeline || Mathf.Abs(QualitySettings.lodBias - originalLod) > .001f)
                    throw new Exception("High did not restore the original pipeline and LOD bias");
                PlayerPrefs.SetInt(GraphicsPreferences.TierKey, 99); settings.Load();
                if (settings.Tier != GraphicsTier.High) throw new Exception("Invalid setting did not fall back to High");
                Debug.Log("GRAPHICS_TIERS_VERIFIED");
            }
            finally
            {
                settings.RestoreTier();
                for (var i = 0; i < keys.Length; i++) { if (existed[i]) PlayerPrefs.SetInt(keys[i], old[i]); else PlayerPrefs.DeleteKey(keys[i]); }
                PlayerPrefs.Save(); UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
