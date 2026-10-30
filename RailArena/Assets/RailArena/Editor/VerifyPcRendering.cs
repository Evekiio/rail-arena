using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RailArena.Editor
{
    public static class VerifyPcRendering
    {
        public static void Check()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (asset == null) throw new System.Exception("PC URP asset is missing");
            int original = QualitySettings.GetQualityLevel();
            try
            {
                var names = QualitySettings.names; int pc = System.Array.IndexOf(names, "PC");
                if (pc < 0) throw new System.Exception("PC quality level is missing");
                QualitySettings.SetQualityLevel(pc, true);
                if (QualitySettings.renderPipeline != asset) throw new System.Exception("PC quality does not use the expected URP asset");
                if (PlayerSettings.colorSpace != ColorSpace.Linear) throw new System.Exception("Linear color space is required");
                if (!asset.supportsHDR || !asset.supportsCameraDepthTexture || !asset.supportsCameraOpaqueTexture) throw new System.Exception("PC HDR, depth and opaque textures are required");
                if (!asset.supportsMainLightShadows || asset.mainLightShadowmapResolution < 2048 || asset.shadowCascadeCount != 4 || !asset.supportsSoftShadows) throw new System.Exception("PC shadow configuration is incomplete");
                Debug.Log("PC_RENDERING_CONFIGURATION_VERIFIED quality=" + pc + " shadow=" + asset.mainLightShadowmapResolution + " cascades=" + asset.shadowCascadeCount);
            }
            finally { QualitySettings.SetQualityLevel(original, true); }
        }
    }
}
