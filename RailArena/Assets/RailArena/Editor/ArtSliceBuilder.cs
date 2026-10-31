using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public sealed class ArtSliceBuilder : IProcessSceneWithReport
    {
        public int callbackOrder => 100;
        static bool building;
        public static bool Building => building;

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
            PrepareRifleReload();
            ContenderMotionArtBuilder.Prepare();
            ContenderArtBuilder.Prepare();
            SkyNatureArtBuilder.Prepare();
            HealthCoreArtBuilder.Prepare();
            RifleSurfaceArtBuilder.Prepare();
            SkyAtmosphereArtBuilder.Prepare();
            StoneSurfaceArtBuilder.Prepare();
            BasementDeckArtBuilder.Prepare();
            BridgeStoneArtBuilder.Prepare();
            SummitStoneArtBuilder.Prepare();
            ArtAtmosphereBuilder.Prepare();
            var errors = ShaderUtil.GetShaderMessages(profile.SurfaceShader).Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(m => m.message)));
            Debug.Log("ART_SLICE_PROFILE_READY");
        }

        [Serializable] sealed class ReloadSource
        {
            public int fps;
            public float durationSeconds;
            public Vector3 magazinePivot;
            public RifleReloadArt.Pose[] samples;
            public string canonicalMasterSha256;
        }

        static void PrepareRifleReload()
        {
            const string folder = "../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/";
            var source = JsonUtility.FromJson<ReloadSource>(File.ReadAllText(folder + "ReloadAction.json"));
            if (source.fps != Simulation.CombatRules.TickRate || source.durationSeconds != 2 || source.samples.Length != Simulation.RifleMagazine.ReloadTicks + 1)
                throw new InvalidOperationException("Reload action timing must match existing authority");
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(folder + "Carbine-Reload-v1.blend"))).Replace("-", "").ToLowerInvariant();
                if (actual != source.canonicalMasterSha256) throw new InvalidOperationException("Authored action master hash mismatch");
            }
            const string path = "Assets/Resources/Art/RifleReload.asset";
            var art = AssetDatabase.LoadAssetAtPath<RifleReloadArt>(path);
            if (art == null) { art = ScriptableObject.CreateInstance<RifleReloadArt>(); AssetDatabase.CreateAsset(art, path); }
            art.magazinePivot = source.magazinePivot; art.samples = source.samples; art.sourceSha256 = source.canonicalMasterSha256;
            if (art.Evaluate(0).magazineOffset.sqrMagnitude > 1e-8f || art.Evaluate(1).magazineOffset.sqrMagnitude > 1e-8f)
                throw new InvalidOperationException("Reload must begin and end with seated magazine");
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            Debug.Log("ART_RIFLE_RELOAD_IMPORTED " + art.samples.Length + " samples / 2s");
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
            string folder = Environment.GetEnvironmentVariable("RA_ART_BUILD_OUTPUT") ?? "../Builds/RailArena-v1Alpha-ArtPass-Review-01";
            string output = Path.Combine(folder, "RailArena.exe");
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Review output already exists; choose a new build identity");
            byte[] priorIdentity = File.ReadAllBytes("Assets/Resources/BuildIdentity.txt");
            byte[] priorQuality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            int priorQualityLevel = QualitySettings.GetQualityLevel();
            string priorPipelinePath = SavedPipelinePath(priorQuality, priorQualityLevel);
            try
            {
                building = true;
                QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ArtAtmosphereBuilder.PipelinePath);
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Art slice build failed");
                File.WriteAllText(Path.Combine(folder,"BuildIdentity.txt"), File.ReadAllText("Assets/Resources/BuildIdentity.txt"));
                File.WriteAllLines(Path.Combine(folder,"BuildInputs.txt"), BuildIdentityGenerator.InputPaths());
                string config = Path.Combine(folder, "BuildConfiguration/ProjectSettings");
                Directory.CreateDirectory(config);
                File.Copy("ProjectSettings/QualitySettings.asset", Path.Combine(config, "QualitySettings.asset"));
                Debug.Log("ART_PASS_REVIEW_BUILD " + Path.GetFullPath(output));
            }
            finally
            {
                building = false; RestoreBuildInputs(priorPipelinePath, priorQualityLevel, priorIdentity, priorQuality);
            }
        }

        static string SavedPipelinePath(byte[] quality, int level)
        {
            var entries = System.Text.RegularExpressions.Regex.Matches(System.Text.Encoding.UTF8.GetString(quality), @"customRenderPipeline:\s*\{[^}]*\}");
            if (level < 0 || level >= entries.Count) throw new InvalidOperationException("Cannot resolve the saved quality-level pipeline");
            var guid = System.Text.RegularExpressions.Regex.Match(entries[level].Value, @"guid:\s*([a-f0-9]{32})");
            if (!guid.Success) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid.Groups[1].Value);
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Saved render pipeline is missing");
            return path;
        }

        public static void RestoreBuildInputs(string pipelinePath, int level, byte[] identity, byte[] quality)
        {
            // BuildPipeline can unload the previous asset; restore its persistent identity rather than a stale Object reference.
            QualitySettings.SetQualityLevel(level, false);
            QualitySettings.renderPipeline = string.IsNullOrEmpty(pipelinePath) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>(pipelinePath);
            File.WriteAllBytes("Assets/Resources/BuildIdentity.txt", identity);
            AssetDatabase.ImportAsset("Assets/Resources/BuildIdentity.txt", ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllBytes("ProjectSettings/QualitySettings.asset", quality);
        }
    }
}
