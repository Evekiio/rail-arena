using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using RailArena.GraphicsReview;

namespace RailArena.Editor
{
    public sealed class CombinedGraphicsReviewBuilder : IProcessSceneWithReport
    {
        static bool active;
        public int callbackOrder => 0;
        static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception("Missing review asset: " + path);
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!active || (scene.path != "Assets/RailArena/Generated/SkyGardens.unity" && scene.path != BasementRouteBuilder.ScenePath)) return;
            var item = new GameObject("Combined graphics review effects").AddComponent<ReviewEffects>();
            item.waterShader = Load<Shader>("Assets/Validation/SkyWaterCandidate/LightResponsiveWater.shader");
            item.water = Load<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-water.mat");
            item.foam = Load<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foam.mat");
            item.rifleSurface = Load<Material>("Assets/RailArena/Art/PrecisionCarbine/Surface.mat");
            item.rifleAo = Load<Texture2D>("Assets/Validation/CarbineCandidate/AO-white-background.png");
            item.rifleModels = Enumerable.Range(0, 3).Select(i => Load<GameObject>($"Assets/Validation/CarbineCandidate/Carbine_LOD{i}.fbx")).ToArray();
            Debug.Log("COMBINED_REVIEW_INJECTED " + scene.path);
        }
        public static void Build()
        {
            const string output = "../Builds/SkyGardens-BasementRoutes-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Output already exists");
            var candidate = Load<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var expected = Load<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (QualitySettings.renderPipeline != expected) throw new Exception("Unexpected current pipeline");
            try
            {
                active = true; QualitySettings.renderPipeline = candidate;
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (result.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
                Debug.Log("COMBINED_REVIEW_BUILD " + Path.GetFullPath(output));
            }
            finally { active = false; QualitySettings.renderPipeline = Load<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"); }
        }
    }
}














