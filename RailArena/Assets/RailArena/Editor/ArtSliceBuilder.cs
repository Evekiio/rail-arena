using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RailArena.Editor
{
    public sealed class ArtSliceBuilder : IProcessSceneWithReport
    {
        public int callbackOrder => 100;
        static bool building;

        public static void Prepare()
        {
            const string path = "Assets/Resources/Art/SkyGardensCel.asset";
            Directory.CreateDirectory("Assets/Resources/Art");
            AssetDatabase.Refresh();
            var profile = AssetDatabase.LoadAssetAtPath<CelArtProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<CelArtProfile>(); AssetDatabase.CreateAsset(profile, path); }
            profile.SurfaceShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Effects/CelSurface.shader");
            if (profile.SurfaceShader == null) throw new InvalidOperationException("Cel shader not imported");
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            PrepareStoneCover();
            var errors = ShaderUtil.GetShaderMessages(profile.SurfaceShader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(m => m.message)));
            Debug.Log("ART_SLICE_PROFILE_READY");
        }

        static void PrepareStoneCover()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RailArena/Art/ArtSlice/StoneCover-v1.fbx");
            if (model == null) throw new InvalidOperationException("Stone cover export is missing");
            string[] names = { "Ivory", "Teal", "Bronze", "Stone" };
            Color[] colors = { new(.9f,.89f,.8f), new(.14f,.4f,.4f), new(.64f,.4f,.18f), new(.63f,.69f,.67f) };
            var materials = new Material[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/Resources/Art/StoneCover-" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
                material.SetColor("_BaseColor", colors[i]);
                material.SetFloat("_Metallic", i == 2 ? .55f : 0);
                material.SetFloat("_Smoothness", i == 2 ? .35f : .15f);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material); materials[i] = material;
            }
            var instance = UnityEngine.Object.Instantiate(model);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[Array.FindIndex(names, n => m.name == n)]).ToArray();
                }
                if ((bounds.size - new Vector3(2, 1.2f, 1)).sqrMagnitude > .0001f || bounds.center.sqrMagnitude > .0001f) throw new InvalidOperationException("Stone export scale or centered pivot mismatch: " + bounds);
                if (instance.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Visual stone must not add collision");
                PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Art/StoneCover.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("ART_SLICE_STONE_IMPORT " + bounds + " materials=4 colliders=0");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!building || scene.path != BasementRouteBuilder.ScenePath) return;
            new GameObject("Sky Gardens art slice presentation").AddComponent<ArtSlicePresentation>().Profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
        }

        public static void Build()
        {
            Prepare();
            const string output = "../Builds/ArtSlice-Gate2-Review/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Review output already exists; choose a new build identity");
            try
            {
                building = true;
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Art slice build failed");
            }
            finally { building = false; }
        }
    }
}
