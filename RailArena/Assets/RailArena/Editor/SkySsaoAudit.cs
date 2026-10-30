using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public static class SkySsaoAudit
    {
        public static void BuildPostProcessAuditPlayer()
        {
            const string output = "../Builds/SkyGardens-PostProcess-Audit-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Audit output already exists");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!scenes.Contains(SkyGardensBuilder.ScenePath)) throw new Exception("Sky Gardens is not in the build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Audit build failed: " + report.summary.result);
            Debug.Log("POST_PROCESS_AUDIT_BUILD " + Path.GetFullPath(output));
        }
        public static void BuildProfilingPlayer()
        {
            const string output = "../Builds/SkyGardens-SSAO-Profiling-2026-10-29/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Profiling output already exists");
            bool original = PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.enableFrameTimingStats = true;
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (!scenes.Contains(SkyGardensBuilder.ScenePath)) throw new Exception("Sky Gardens is not in the build");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Profiling build failed: " + report.summary.result);
                Debug.Log("SKY_SSAO_PROFILING_PLAYER_BUILT " + Path.GetFullPath(output));
            }
            finally { PlayerSettings.enableFrameTimingStats = original; }
        }
        public static void CaptureComparison()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new Exception("Active quality does not select URP");
            var serialized = new SerializedObject(pipeline);
            var renderers = serialized.FindProperty("m_RendererDataList");
            var index = serialized.FindProperty("m_DefaultRendererIndex").intValue;
            var renderer = renderers.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
            if (renderer == null || AssetDatabase.GetAssetPath(renderer) != "Assets/Settings/PC_Renderer.asset") throw new Exception("Unexpected active renderer");
            var feature = renderer.rendererFeatures.Single(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            bool active = feature.isActive; if (!active) throw new Exception("SSAO is disabled");
            var settingsField = feature.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic);
            var settings = settingsField?.GetValue(feature) ?? throw new Exception("SSAO settings are unavailable");
            var samplesField = settings.GetType().GetField("Samples", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (samplesField == null) throw new Exception("SSAO sample setting is unavailable");
            var oldSamples = samplesField.GetValue(settings);
            var medium = Enum.Parse(samplesField.FieldType, "Medium"); var high = Enum.Parse(samplesField.FieldType, "High");
            var camera = Camera.main; var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/SsaoComparison"));
            Directory.CreateDirectory(output);
            try
            {
                target.Create(); camera.targetTexture = target;
                foreach (var view in new[] { ("Underpass", new Vector3(0, 1.6f, -11), new Vector3(0, 1.6f, 4)), ("Summit", new Vector3(6, 24, -5), new Vector3(0, 23, 0)) })
                {
                    camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                    foreach (var mode in new[] { ("Off", false, medium), ("Medium", true, medium), ("High", true, high) })
                    {
                        samplesField.SetValue(settings, mode.Item3); feature.SetActive(mode.Item2);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, view.Item1 + "-" + mode.Item1 + ".png"), image.EncodeToPNG());
                    }
                }
            }
            finally { samplesField.SetValue(settings, oldSamples); feature.SetActive(active); camera.targetTexture = oldTarget; RenderTexture.active = oldActive; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
            Debug.Log("SKY_SSAO_COMPARISON renderer=" + AssetDatabase.GetAssetPath(renderer) + " output=" + output);
        }
    }
}
