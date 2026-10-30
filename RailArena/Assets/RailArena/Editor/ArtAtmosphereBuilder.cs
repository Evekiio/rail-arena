using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public static class ArtAtmosphereBuilder
    {
        public const string PipelinePath = "Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset";
        public static void Prepare()
        {
            const string root = "Assets/RailArena/Art/ArtPass/Atmosphere/";
            Directory.CreateDirectory(root); AssetDatabase.Refresh();
            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/AtmosphereCandidate/AtmosphereMaterial.mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(root + "SkyGardensAtmosphere.mat");
            if (material == null) { material = new Material(sourceMaterial); AssetDatabase.CreateAsset(material, root + "SkyGardensAtmosphere.mat"); }
            material.shader = Resources.Load<Shader>("Effects/ArtAtmosphere"); EditorUtility.SetDirty(material);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(root + "SkyGardensRenderer.asset");
            if (renderer == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Validation/AtmosphereCandidate/AtmosphereRenderer.asset");
                renderer = UnityEngine.Object.Instantiate(source); renderer.name = "Sky Gardens production renderer";
                renderer.rendererFeatures.Clear(); AssetDatabase.CreateAsset(renderer, root + "SkyGardensRenderer.asset");
                foreach (var feature in source.rendererFeatures)
                {
                    ScriptableRendererFeature clone;
                    if (feature.GetType().Name == "AtmosphereCandidateFeature")
                    {
                        var atmosphere = ScriptableObject.CreateInstance<Rendering.ArtAtmosphereFeature>();
                        atmosphere.material = material; clone = atmosphere;
                    }
                    else clone = UnityEngine.Object.Instantiate(feature);
                    clone.name = feature.name.Replace("candidate", "production");
                    AssetDatabase.AddObjectToAsset(clone, renderer); renderer.rendererFeatures.Add(clone);
                }
                EditorUtility.SetDirty(renderer);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset"));
                pipeline.name = "Sky Gardens art pipeline"; AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            var serialized = new SerializedObject(pipeline);
            if (new SerializedObject(renderer).FindProperty("m_RenderingMode").intValue != 2 || serialized.FindProperty("m_SoftShadowQuality").intValue != 3)
                throw new InvalidOperationException("Art shader variant contract requires Forward+ and High soft-shadow quality; update the build variant policy before changing these settings");
            if (!File.ReadAllText(BasementRouteBuilder.ScenePath).Contains("m_FogMode: 1"))
                throw new InvalidOperationException("Art shader variant contract requires the current linear-fog scene");
            var renderers = serialized.FindProperty("m_RendererDataList"); renderers.arraySize = 1; renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssets();
            if (material.shader == null || renderer.rendererFeatures.Count == 0) throw new InvalidOperationException("Atmosphere production dependencies missing");
            Debug.Log("ART_ATMOSPHERE_PROMOTED runtime renderer, shader, material and pipeline are production build inputs");
        }
    }
}
