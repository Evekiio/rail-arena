using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Linq;
using RailArena.Validation;
namespace RailArena.Editor
{
    public static class AtmosphereCandidateBuilder
    {
        public static void BuildPreviewPlayer()
        {
            const string output = "../Builds/SkyGardens-AtmospherePreview-v2-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Preview output already exists");
            var candidate = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var expected = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var original = QualitySettings.renderPipeline;
            if (candidate == null || expected == null || original != expected) throw new InvalidOperationException("Current quality is not the expected PC pipeline");
            string originalPath = AssetDatabase.GetAssetPath(original);
            try
            {
                QualitySettings.renderPipeline = candidate;
                ArenaBuilder.BuildWindowsPlaytestTo(output);
                Debug.Log("ATMOSPHERE_PREVIEW_BUILD " + Path.GetFullPath(output));
            }
            finally
            {
                var restored = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(originalPath);
                if (restored == null) throw new InvalidOperationException("Could not reload original PC pipeline after preview build");
                QualitySettings.renderPipeline = restored;
            }
        }

        public static void BuildProfilingPlayer()
        {
            const string output = "../Builds/SkyGardens-Atmosphere-Profiling-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Profiling output already exists");
            var candidate = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            if (candidate == null) throw new InvalidOperationException("Atmosphere pipeline is missing");
            var originalPipeline = QualitySettings.renderPipeline;
            if (originalPipeline == null) throw new InvalidOperationException("Current quality has no render pipeline; refusing to replace it for the profiling build");
            string originalPipelinePath = AssetDatabase.GetAssetPath(originalPipeline);
            if (string.IsNullOrEmpty(originalPipelinePath)) throw new InvalidOperationException("Current quality pipeline is not a persistent asset");
            bool originalTiming = PlayerSettings.enableFrameTimingStats;
            try
            {
                QualitySettings.renderPipeline = candidate;
                PlayerSettings.enableFrameTimingStats = true;
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Profiling build failed: " + report.summary.result);
                Debug.Log("ATMOSPHERE_PROFILE_BUILD " + Path.GetFullPath(output));
            }
            finally
            {
                var restored = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(originalPipelinePath);
                if (restored == null) throw new InvalidOperationException("Could not reload original pipeline after profiling build");
                QualitySettings.renderPipeline = restored;
                PlayerSettings.enableFrameTimingStats = originalTiming;
            }
        }
        public static void Create()
        {
            const string folder = "Assets/Validation/AtmosphereCandidate/";
            const string rendererPath = folder + "AtmosphereRenderer.asset";
            const string pipelinePath = folder + "AtmospherePipeline.asset";
            const string materialPath = folder + "AtmosphereMaterial.mat";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rendererPath) != null || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pipelinePath) != null || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(materialPath) != null) throw new InvalidOperationException("Candidate assets already exist");
            if (!AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", rendererPath) || !AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset", pipelinePath)) throw new InvalidOperationException("Could not copy renderer or pipeline");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(folder + "AtmosphereCandidate.shader"); if (shader == null) throw new InvalidOperationException("Shader missing");
            var material = new Material(shader) { name = "Atmosphere candidate" };
            material.SetColor("_AtmosphereColor", new Color(.55f, .70f, .78f, 1)); material.SetVector("_AtmosphereParams", new Vector4(.006f, .02f, 100f, .8f));
            AssetDatabase.CreateAsset(material, materialPath);
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (renderer == null || pipeline == null) throw new InvalidOperationException("Copied assets did not load");
            var feature = ScriptableObject.CreateInstance<AtmosphereCandidateFeature>(); feature.name = "Atmosphere candidate"; feature.material = material;
            AssetDatabase.AddObjectToAsset(feature, renderer); renderer.rendererFeatures.Add(feature); renderer.SetDirty(); EditorUtility.SetDirty(renderer);
            var serialized = new SerializedObject(pipeline); var list = serialized.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) throw new InvalidOperationException("Pipeline has no renderer slot");
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets(); Debug.Log("ATMOSPHERE_CANDIDATE_CREATED " + pipelinePath);
        }
    }
}
