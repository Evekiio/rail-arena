using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RailArena
{
    public sealed class GraphicsPreferences
    {
        public const string AntialiasingKey = "graphicsSmaa";
        public const string MotionBlurKey = "graphicsMotionBlur";
        public const string TierKey = "graphicsTier";
        public bool Smaa { get; set; } = true;
        public bool MotionBlur { get; set; } = true;
        public GraphicsTier Tier { get; set; } = GraphicsTier.High;
        public bool MotionBlurSupported { get; private set; }
        private RenderPipelineAsset originalPipeline;
        private UniversalRenderPipelineAsset ownedPipeline;
        private float originalLodBias;
        public void Load() { Smaa = PlayerPrefs.GetInt(AntialiasingKey, 1) != 0; MotionBlur = PlayerPrefs.GetInt(MotionBlurKey, 1) != 0; Tier = GraphicsTierProfiles.Normalize(PlayerPrefs.GetInt(TierKey, 0)); }
        public void Save() { PlayerPrefs.SetInt(AntialiasingKey, Smaa ? 1 : 0); PlayerPrefs.SetInt(MotionBlurKey, MotionBlur ? 1 : 0); PlayerPrefs.SetInt(TierKey, (int)GraphicsTierProfiles.Normalize((int)Tier)); PlayerPrefs.Save(); }
        public void Apply(Camera camera)
        {
            ApplyTier();
            var data = camera.GetUniversalAdditionalCameraData();
            data.antialiasing = Smaa ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            data.antialiasingQuality = AntialiasingQuality.High;
            MotionBlurSupported = false;
            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (volume.enabled && volume.isGlobal && (data.volumeLayerMask.value & (1 << volume.gameObject.layer)) != 0 && volume.sharedProfile != null && volume.sharedProfile.TryGet<UnityEngine.Rendering.Universal.MotionBlur>(out _))
                    if (volume.profile.TryGet<UnityEngine.Rendering.Universal.MotionBlur>(out var blur)) { MotionBlurSupported = true; blur.active = MotionBlur; }
        }

        private void ApplyTier()
        {
            Tier = GraphicsTierProfiles.Normalize((int)Tier);
            if (Tier == GraphicsTier.High) { RestoreTier(); return; }
            if (ownedPipeline != null && QualitySettings.renderPipeline != ownedPipeline) RestoreTier();
            if (ownedPipeline == null)
            {
                originalPipeline = QualitySettings.renderPipeline;
                var source = (originalPipeline != null ? originalPipeline : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
                if (source == null) return;
                originalLodBias = QualitySettings.lodBias;
                ownedPipeline = Object.Instantiate(source);
                ownedPipeline.name = source.name + " (runtime graphics tier)";
                ownedPipeline.hideFlags = HideFlags.DontSave;
            }
            var profile = GraphicsTierProfiles.Get(Tier);
            ownedPipeline.renderScale = profile.RenderScale;
            ownedPipeline.shadowDistance = profile.ShadowDistance;
            ownedPipeline.shadowCascadeCount = profile.ShadowCascades;
            ownedPipeline.mainLightShadowmapResolution = profile.MainShadowResolution;
            QualitySettings.lodBias = profile.LodBias;
            if (QualitySettings.renderPipeline != ownedPipeline) QualitySettings.renderPipeline = ownedPipeline;
        }

        public void RestoreTier()
        {
            if (ownedPipeline == null) return;
            if (QualitySettings.renderPipeline == ownedPipeline)
            {
                QualitySettings.renderPipeline = originalPipeline;
                QualitySettings.lodBias = originalLodBias;
            }
            if (Application.isPlaying) Object.Destroy(ownedPipeline); else Object.DestroyImmediate(ownedPipeline);
            ownedPipeline = null;
            originalPipeline = null;
        }
    }
}
