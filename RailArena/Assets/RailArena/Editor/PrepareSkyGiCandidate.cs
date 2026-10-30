using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RailArena.Editor
{
    public static class PrepareSkyGiCandidate
    {
        private const string Folder="Assets/Validation/SkyGiCandidate";
        private const string ImprovedScene=Folder+"/SkyGiCandidateImproved.unity";
        private const string ProbeScene=Folder+"/SkyGiCandidateProbeCoverage.unity";
        private const string FillScene=Folder+"/SkyGiCandidateFillTuned.unity";
        private const string DirectedFillScene=Folder+"/SkyGiCandidateDirectedFill.unity";
        public static void AuditPostProcessing()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var camera = Camera.main;
            var additional = camera.GetComponent<UniversalAdditionalCameraData>();
            Debug.Log("SKY_PP_CAMERA enabled=" + (additional != null && additional.renderPostProcessing) + " volumeMask=" + (additional == null ? 0 : additional.volumeLayerMask.value));
            foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var profile = volume.sharedProfile;
                Debug.Log("SKY_PP_VOLUME name=" + volume.name + " enabled=" + volume.enabled + " global=" + volume.isGlobal + " weight=" + volume.weight + " layer=" + volume.gameObject.layer + " profile=" + (profile == null ? "null" : AssetDatabase.GetAssetPath(profile)));
                if (profile != null) foreach (var component in profile.components)
                    Debug.Log("SKY_PP_COMPONENT " + (component == null ? "null" : component.GetType().Name + " active=" + component.active));
            }
        }
        public static void CapturePostProcessingComparison()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var camera = Camera.main; var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null || !data.renderPostProcessing) throw new Exception("Main camera post-processing is not enabled");
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/PostProcessing"));
            Directory.CreateDirectory(output);
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            try
            {
                target.Create(); camera.targetTexture = target; camera.transform.position = new Vector3(0, 1.6f, -11); camera.transform.LookAt(new Vector3(0, 1.6f, 4));
                foreach (bool enabled in new[] { false, true })
                {
                    data.renderPostProcessing = enabled;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, enabled ? "Post-On.png" : "Post-Off.png"), image.EncodeToPNG());
                }
            }
            finally { data.renderPostProcessing = true; camera.targetTexture = oldTarget; camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = oldActive; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
            Debug.Log("SKY_PP_COMPARISON " + output);
        }
        public static void VerifyDirected()
        {
            VerifyScene(DirectedFillScene);
        }
        public static void VerifyIntegrated()
        {
            VerifyScene(SkyGardensBuilder.ScenePath);
        }
        public static void BuildIntegratedPlaytest()
        {
            const string output = "../Builds/SkyGardens-GI-2026-10-29/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Playtest output already exists; choose a fresh path");
            VerifyScene(SkyGardensBuilder.ScenePath);
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == SkyGardensBuilder.ScenePath)) throw new Exception("Sky Gardens is not enabled in build settings");
            ArenaBuilder.BuildWindowsPlaytestTo(output);
            Debug.Log("SKY_GI_PLAYTEST_BUILT " + Path.GetFullPath(output));
        }
        private static void VerifyScene(string path)
        {
            EditorSceneManager.OpenScene(path);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lighting settings missing");
            var maps = LightmapSettings.lightmaps;
            var probes = LightmapSettings.lightProbes;
            var renderers = UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0).ToArray();
            int assigned = renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < maps.Length && r.sharedMaterial != null && r.GetComponent<MeshFilter>().sharedMesh != null);
            if (Lightmapping.lightingDataAsset == null || maps.Length == 0 || maps.Any(m => m.lightmapColor == null) || probes == null || probes.count != 298 || assigned != renderers.Length || assigned != 204)
                throw new Exception("Staged lighting references incomplete: maps=" + maps.Length + " probes=" + (probes == null ? 0 : probes.count) + " renderers=" + assigned + "/" + renderers.Length);
            Debug.Log("SKY_GI_STAGED_VERIFIED maps=" + maps.Length + " probes=" + probes.count + " renderers=" + assigned);
        }
        public static void ExportDirectedDependencies()
        {
            var dependencies = AssetDatabase.GetDependencies(DirectedFillScene, true).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/gi-directed-dependencies.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllLines(output, dependencies);
            Debug.Log("SKY_GI_DEPENDENCIES count=" + dependencies.Length + " output=" + output);
        }
        public static void PrepareDirectedFill()
        {
            if (File.Exists(DirectedFillScene)) throw new IOException("Directed fill candidate already exists");
            EditorSceneManager.OpenScene(FillScene);
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), DirectedFillScene, true)) throw new Exception("Could not copy tuned scene");
            EditorSceneManager.OpenScene(DirectedFillScene);
            var light = GameObject.Find("Underpass center fill").GetComponent<Light>();
            light.type = LightType.Spot; light.spotAngle = 120f; light.innerSpotAngle = 95f;
            light.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); Debug.Log("SKY_GI_DIRECTED_FILL_PREPARED");
        }
        public static void BakeDirectedFill()
        {
            EditorSceneManager.OpenScene(DirectedFillScene);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lightmapper is not configured");
            if (!Lightmapping.Bake()) throw new Exception("Directed fill bake failed or was cancelled");
            if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count < 100) throw new Exception("Bake lacks expected lightmaps or probes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); Debug.Log("SKY_GI_DIRECTED_FILL_BAKED probes=" + LightmapSettings.lightProbes.count);
        }
        public static void PrepareFillTuned()
        {
            if (File.Exists(FillScene)) throw new IOException("Tuned fill candidate already exists");
            EditorSceneManager.OpenScene(ProbeScene);
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), FillScene, true)) throw new Exception("Could not copy probe candidate");
            EditorSceneManager.OpenScene(FillScene); Physics.SyncTransforms();
            var root = GameObject.Find("GI candidate probes and underpass fill");
            var position = new Vector3(0, 3.2f, 0);
            if (Physics.CheckSphere(position, .15f, 1 << 0, QueryTriggerInteraction.Ignore)) throw new Exception("Center fill position is obstructed");
            var obj = new GameObject("Underpass center fill"); obj.transform.SetParent(root.transform); obj.transform.position = position;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.lightmapBakeType = LightmapBakeType.Baked;
            light.color = new Color(.68f, .79f, 1f); light.intensity = 2.4f; light.range = 12f; light.shadows = LightShadows.Soft;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); Debug.Log("SKY_GI_FILL_TUNED_PREPARED");
        }
        public static void BakeFillTuned()
        {
            EditorSceneManager.OpenScene(FillScene);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lightmapper is not configured");
            if (!Lightmapping.Bake()) throw new Exception("Tuned fill bake failed or was cancelled");
            if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count < 100) throw new Exception("Bake lacks expected lightmaps or probes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); Debug.Log("SKY_GI_FILL_TUNED_BAKED probes=" + LightmapSettings.lightProbes.count);
        }
        public static void DiagnoseLightmapSamples()
        {
            foreach (var scene in new[] { ("Original", Folder + "/SkyGiCandidate.unity"), ("Fill", ProbeScene), ("Tuned", FillScene), ("Directed", DirectedFillScene) })
            {
                EditorSceneManager.OpenScene(scene.Item2); Physics.SyncTransforms();
                var map = LightmapSettings.lightmaps[0].lightmapColor;
                var rt = RenderTexture.GetTemporary(map.width, map.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                var texture = new Texture2D(map.width, map.height, TextureFormat.RGBAFloat, false, true);
                var old = RenderTexture.active;
                try
                {
                    Graphics.Blit(map, rt); RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, map.width, map.height), 0, 0); texture.Apply();
                    foreach (float z in new[] { -6f, 0f, 6f })
                    {
                        if (!Physics.Raycast(new Vector3(0, 3.2f, z), Vector3.down, out var hit, 5f, 1 << 0, QueryTriggerInteraction.Ignore)) throw new Exception("No floor below fill " + z);
                        var filter = hit.collider.GetComponent<MeshFilter>(); var renderer = hit.collider.GetComponent<MeshRenderer>();
                        if (filter == null || renderer == null || renderer.lightmapIndex != 0) throw new Exception("Floor does not use expected lightmap: " + hit.collider.name);
                        var mesh = filter.sharedMesh; var triangles = mesh.triangles; var uv = mesh.uv2; int i = hit.triangleIndex * 3;
                        var bary = hit.barycentricCoordinate;
                        var coord = uv[triangles[i]] * bary.x + uv[triangles[i+1]] * bary.y + uv[triangles[i+2]] * bary.z;
                        var scale = renderer.lightmapScaleOffset; coord = new Vector2(coord.x * scale.x + scale.z, coord.y * scale.y + scale.w);
                        Debug.Log("SKY_GI_SAMPLE " + scene.Item1 + " z=" + z + " hit=" + hit.collider.name + " y=" + hit.point.y + " uv=" + coord.ToString("F4") + " color=" + texture.GetPixelBilinear(coord.x, coord.y).ToString("F4"));
                    }
                }
                finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
            }
        }
        public static void PrepareProbeCoverage()
        {
            if (File.Exists(ProbeScene)) throw new IOException("Probe coverage candidate already exists");
            EditorSceneManager.OpenScene(ImprovedScene);
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ProbeScene, true)) throw new Exception("Could not copy improved scene");
            EditorSceneManager.OpenScene(ProbeScene);
            var root = GameObject.Find("GI candidate probes and underpass fill");
            if (root == null) throw new Exception("Improved candidate root missing");
            var points = new List<Vector3>(); Physics.SyncTransforms();
            for (int x = -32; x <= 32; x += 4) for (int z = -20; z <= 20; z += 4)
                foreach (float height in new[] { 3f, 8f, 26f })
                    if (Physics.Raycast(new Vector3(x, height, z), Vector3.down, out var hit, 5, 1 << 0, QueryTriggerInteraction.Ignore) && hit.normal.y > .7f)
                        foreach (float offset in new[] { 1.2f, 2.6f })
                        {
                            var point = hit.point + Vector3.up * offset;
                            if (!Physics.CheckSphere(point, .4f, 1 << 0, QueryTriggerInteraction.Ignore) && !points.Any(p => (p-point).sqrMagnitude < .1f)) points.Add(point);
                        }
            if (points.Count < 100 || points.Count(p => p.y > 22) < 8) throw new Exception("Probe coverage insufficient: " + points.Count + " total, " + points.Count(p => p.y > 22) + " summit");
            root.GetComponent<LightProbeGroup>().probePositions = points.ToArray();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SKY_GI_COVERAGE_PREPARED probes=" + points.Count + " summit=" + points.Count(p => p.y > 22));
        }
        public static void BakeProbeCoverage()
        {
            EditorSceneManager.OpenScene(ProbeScene);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lightmapper is not configured");
            if (!Lightmapping.Bake()) throw new Exception("Probe coverage bake failed or was cancelled");
            if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count < 100) throw new Exception("Bake lacks expected lightmaps or probes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SKY_GI_COVERAGE_BAKED lightmaps=" + LightmapSettings.lightmaps.Length + " probes=" + LightmapSettings.lightProbes.count);
        }
        public static void DiagnoseImproved()
        {
            EditorSceneManager.OpenScene(ImprovedScene);
            Physics.SyncTransforms();
            var root = GameObject.Find("GI candidate probes and underpass fill");
            if (root == null) throw new Exception("Improved candidate root missing");
            var group = root.GetComponent<LightProbeGroup>();
            foreach (var light in root.GetComponentsInChildren<Light>())
            {
                var overlaps = Physics.OverlapSphere(light.transform.position, .15f, 1 << 0, QueryTriggerInteraction.Ignore);
                Debug.Log("SKY_GI_FILL " + light.name + " position=" + light.transform.position.ToString("F2") + " overlaps=" + string.Join(",", overlaps.Select(c => c.name)));
            }
            var ps = group.probePositions;
            Debug.Log("SKY_GI_PROBES count=" + ps.Length + " heights=" + string.Join(",", ps.GroupBy(p => Mathf.RoundToInt(p.y * 10) / 10f).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count())));
            foreach (var r in UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World.GetComponentsInChildren<MeshRenderer>())
                if (r.bounds.Contains(new Vector3(0, 0, 8)) || (r.bounds.min.y < 1 && r.bounds.max.y > -.5f && r.bounds.min.x < 0 && r.bounds.max.x > 0 && r.bounds.min.z < 8 && r.bounds.max.z > 8))
                    Debug.Log("SKY_GI_UNDERPASS " + r.name + " bounds=" + r.bounds + " static=" + GameObjectUtility.GetStaticEditorFlags(r.gameObject) + " lightmap=" + r.lightmapIndex);
        }
        public static void PrepareImproved()
        {
            if (File.Exists(ImprovedScene)) throw new IOException("Improved candidate already exists; preserve it before preparing another");
            EditorSceneManager.OpenScene(Folder+"/SkyGiCandidate.unity");
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ImprovedScene, true)) throw new Exception("Could not copy candidate scene");
            EditorSceneManager.OpenScene(ImprovedScene);
            var root = new GameObject("GI candidate probes and underpass fill");
            var probes = root.AddComponent<LightProbeGroup>();
            var points = new List<Vector3>();
            Physics.SyncTransforms();
            for (int x = -40; x <= 40; x += 8) for (int z = -24; z <= 24; z += 8)
                foreach (float height in new[] { 3f, 8f, 26f })
                    if (Physics.Raycast(new Vector3(x, height, z), Vector3.down, out var hit, 5, 1 << 0, QueryTriggerInteraction.Ignore) && hit.normal.y > .7f)
                    {
                        var point = hit.point + Vector3.up * 1.5f;
                        if (!Physics.CheckSphere(point, .4f, 1 << 0, QueryTriggerInteraction.Ignore) && !points.Any(p => (p-point).sqrMagnitude < .1f)) points.Add(point);
                    }
            if (points.Count < 20) throw new Exception("Too few unobstructed light probes: " + points.Count);
            probes.probePositions = points.ToArray();
            foreach (float z in new[] { -6f, 6f })
            {
                var obj = new GameObject("Underpass indirect fill " + z); obj.transform.SetParent(root.transform); obj.transform.position = new Vector3(0, 3.2f, z);
                var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.lightmapBakeType = LightmapBakeType.Baked;
                light.color = new Color(.68f, .79f, 1f); light.intensity = 1.2f; light.range = 14f; light.shadows = LightShadows.Soft;
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SKY_GI_IMPROVED_PREPARED probes=" + points.Count);
        }
        public static void BakeImproved()
        {
            EditorSceneManager.OpenScene(ImprovedScene);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lightmapper is not configured");
            if (!Lightmapping.Bake()) throw new Exception("Improved lighting bake failed or was cancelled");
            if (LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count < 20) throw new Exception("Improved bake lacks lightmaps or light probes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SKY_GI_IMPROVED_BAKED lightmaps=" + LightmapSettings.lightmaps.Length + " probes=" + LightmapSettings.lightProbes.count);
        }
        public static void CaptureComparison()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/GiComparison"));
            Directory.CreateDirectory(output);
            foreach (var scene in new[] { ("Baseline", SkyGardensBuilder.ScenePath), ("Candidate", Folder + "/SkyGiCandidate.unity"), ("Improved", ImprovedScene), ("ProbeCoverage", ProbeScene), ("FillTuned", FillScene), ("DirectedFill", DirectedFillScene) })
            {
                EditorSceneManager.OpenScene(scene.Item2);
                var camera = Camera.main;
                if (camera == null) throw new Exception("No main camera in " + scene.Item2);
                var target = new RenderTexture(1600, 900, 24);
                var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                var previous = camera.targetTexture; var active = RenderTexture.active;
                try
                {
                    target.Create(); camera.targetTexture = target; camera.fieldOfView = 75;
                    foreach (var view in new[] {
                        ("Overview", new Vector3(48,40,-55), new Vector3(0,8,0)),
                        ("Garden", new Vector3(25,2,-16), new Vector3(29,2,-16)),
                        ("Crown", new Vector3(-8,5.7f,7), new Vector3(4,5,0)),
                        ("Underpass", new Vector3(0,1.6f,-11), new Vector3(0,1.6f,4)),
                        ("Summit", new Vector3(6,24,-5), new Vector3(0,23,0)) })
                    {
                        camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, scene.Item1 + "-" + view.Item1 + ".png"), image.EncodeToPNG());
                    }
                }
                finally { camera.targetTexture = previous; RenderTexture.active = active; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
            }
            Debug.Log("SKY_GI_COMPARISON_CAPTURED " + output);
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(Folder+"/SkyGiCandidate.unity");
            var renderers = UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0).ToArray();
            int assigned = renderers.Count(r => r.lightmapIndex >= 0 && r.lightmapIndex < LightmapSettings.lightmaps.Length);
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU || LightmapSettings.lightmaps.Length == 0 || assigned != renderers.Length || assigned == 0)
                throw new Exception("Saved candidate is missing GPU settings or renderer lightmap assignments: " + assigned + "/" + renderers.Length);
            Debug.Log("SKY_GI_VERIFY lightmaps=" + LightmapSettings.lightmaps.Length + " assigned=" + assigned + " total=" + renderers.Length);
        }
        public static void Bake()
        {
            EditorSceneManager.OpenScene(Folder+"/SkyGiCandidate.unity");
            if (!Lightmapping.TryGetLightingSettings(out var settings) || settings.lightmapper != LightingSettings.Lightmapper.ProgressiveGPU) throw new Exception("GPU lightmapper is not configured");
            if (!Lightmapping.Bake()) throw new Exception("Lighting bake failed or was cancelled");
            if (LightmapSettings.lightmaps.Length == 0) throw new Exception("Bake completed without lightmaps");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("SKY_GI_BAKE_COMPLETED lightmaps="+LightmapSettings.lightmaps.Length);
        }
        public static void Prepare()
        {
            if(AssetDatabase.IsValidFolder(Folder))throw new IOException("Lighting candidate already exists; preserve it and use a fresh isolated project");
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            const string scenePath=Folder+"/SkyGiCandidate.unity";
            if(!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),scenePath,true))throw new Exception("Could not save lighting scene copy");
            EditorSceneManager.OpenScene(scenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World;
            var copies=new Dictionary<Mesh,Mesh>();int renderers=0,failed=0;
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.TryGetComponent<MeshRenderer>(out var renderer)||!renderer.enabled||filter.GetComponent<SkyGardenMotion>()!=null)continue;
                var material=renderer.sharedMaterial;var original=filter.sharedMesh;
                if(original==null||material==null||material.renderQueue>=2500||!AssetDatabase.GetAssetPath(original).StartsWith("Assets/RailArena/Generated/SkyGardensArt/",StringComparison.Ordinal))continue;
                if(!copies.TryGetValue(original,out var copy))
                {
                    copy=UnityEngine.Object.Instantiate(original);copy.name=original.name+" lightmap candidate";
                    if(!Unwrapping.GenerateSecondaryUVSet(copy)||copy.uv2.Length!=copy.vertexCount){UnityEngine.Object.DestroyImmediate(copy);failed++;continue;}
                    AssetDatabase.CreateAsset(copy,Folder+"/"+original.name+".asset");copies.Add(original,copy);
                }
                filter.sharedMesh=copy;
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject,GameObjectUtility.GetStaticEditorFlags(filter.gameObject)|StaticEditorFlags.ContributeGI);renderers++;
            }
            var settings=new LightingSettings{name="Sky Gardens GPU bake candidate"};settings.lightmapper=LightingSettings.Lightmapper.ProgressiveGPU;
            settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapResolution=12;settings.lightmapMaxSize=1024;settings.directSampleCount=32;settings.indirectSampleCount=128;
            AssetDatabase.CreateAsset(settings,Folder+"/LightingSettings.asset");Lightmapping.lightingSettings=settings;
            var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(light=>light.type==LightType.Directional);
            if(sun==null)throw new Exception("No directional light");sun.lightmapBakeType=LightmapBakeType.Mixed;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("SKY_GI_CANDIDATE_READY meshes="+copies.Count+" renderers="+renderers+" unwrapFailures="+failed);
            if(renderers==0||failed!=0)throw new Exception("Candidate UV preparation incomplete");
        }
    }
}
