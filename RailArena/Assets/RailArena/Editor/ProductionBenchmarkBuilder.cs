using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    /// <summary>Isolated art benchmark import. Does not replace live weapons or map geometry.</summary>
    public static class ProductionBenchmarkBuilder
    {
        private const string Folder = "Assets/RailArena/Art/ProductionBenchmarks";
        private static string Repository => Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));

        [MenuItem("Rail Arena/Build production benchmarks")]
        public static void Build()
        {
            if (Application.unityVersion != "6000.6.7f1")
                throw new InvalidOperationException("Use pinned Unity 6000.6.7f1.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/FoundryPalette.png");
            if (texture == null) throw new InvalidOperationException("Generate Blender benchmark exports first.");
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(Folder + "/FoundryPalette.png");
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.mipmapEnabled = false;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.SaveAndReimport();
            var palette = Material("FoundryPalette", Color.white, .22f, .57f);
            palette.SetTexture("_BaseMap", texture);
            var cyan = Material("CyanInlay", new Color(.015f,.52f,.58f), .1f, .7f);
            cyan.EnableKeyword("_EMISSION");
            cyan.SetColor("_EmissionColor", new Color(.015f,.65f,.72f) * .65f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var report = new List<string>();
            foreach (string asset in new[] { "RailRifle", "LowWingCover" })
            {
                var root = new GameObject(asset + " Benchmark");
                try
                {
                    var motion = new GameObject("VisualMotion"); motion.transform.SetParent(root.transform,false);
                    var levels = new LOD[2];
                    Bounds? bounds = null;
                    for (int level = 0; level < 2; level++)
                    {
                        string path = Folder + "/" + asset + "_LOD" + level + ".fbx";
                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (importer == null) throw new InvalidOperationException("Missing " + path);
                        importer.importAnimation = false;
                        importer.importCameras = importer.importLights = false;
                        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                        importer.generateSecondaryUV = false;
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), motion.transform);
                        instance.name = "LOD" + level;
                        var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                        int triangles = 0;
                        foreach (var renderer in renderers)
                        {
                            renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                            {
                                if (m == null) throw new InvalidOperationException("Unresolved material slot");
                                if (m.name.StartsWith("CyanInlay")) return cyan;
                                if (m.name.StartsWith("FoundryPalette")) return palette;
                                throw new InvalidOperationException("Unexpected material " + m.name);
                            }).ToArray();
                            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                            if (mesh.uv.Length != mesh.vertexCount) throw new InvalidOperationException("Missing UV coordinates");
                            triangles += mesh.triangles.Length / 3;
                            if (level == 0) { if (bounds.HasValue) { var b = bounds.Value; b.Encapsulate(renderer.bounds); bounds = b; } else bounds = renderer.bounds; }
                        }
                        if (triangles == 0 || triangles > 18000) throw new InvalidOperationException("Invalid triangle budget");
                        report.Add(asset + " LOD" + level + ": " + triangles + " triangles, " + renderers.Length + " mesh renderer(s)");
                        levels[level] = new LOD(level == 0 ? .20f : .025f, renderers);
                        if (asset == "RailRifle")
                        {
                            var muzzle = instance.GetComponentsInChildren<Transform>().Single(t => t.name.Split('.')[0] == "Muzzle");
                            foreach (var socket in instance.GetComponentsInChildren<Transform>().Where(t =>
                                new[] {"Muzzle","RightGrip","LeftSupport","AimLine","StockContact"}.Contains(t.name.Split('.')[0])))
                                socket.name = socket.name.Split('.')[0];
                            if (Mathf.Abs(muzzle.localPosition.z - .598f) > .002f || Mathf.Abs(muzzle.localPosition.y - .09f) > .002f)
                                throw new InvalidOperationException("Rifle axes or scale incorrect: " + muzzle.localPosition);
                        }
                    }
                    var group = root.AddComponent<LODGroup>(); group.SetLODs(levels); group.RecalculateBounds();
                    if (asset == "RailRifle")
                    {
                        const string clipPath = Folder + "/RifleFireRecovery.anim";
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip,clipPath); }
                        clip.legacy = true; clip.frameRate = 60; clip.wrapMode = WrapMode.Once;
                        clip.SetCurve("VisualMotion", typeof(Transform), "localPosition.z",
                            new AnimationCurve(new Keyframe(0,0),new Keyframe(.045f,-.012f),new Keyframe(.22f,0),new Keyframe(.6f,0)));
                        clip.SetCurve("VisualMotion", typeof(Transform), "localEulerAnglesRaw.x",
                            new AnimationCurve(new Keyframe(0,0),new Keyframe(.045f,-2),new Keyframe(.22f,0),new Keyframe(.6f,0)));
                        var animation = root.AddComponent<Animation>(); animation.playAutomatically = false;
                        animation.AddClip(clip,"FireRecovery"); animation.clip = clip;
                        clip.SampleAnimation(root,.045f);
                        if (Mathf.Abs(motion.transform.localPosition.z + .012f) > .0001f)
                            throw new InvalidOperationException("Fire animation did not sample correctly");
                        clip.SampleAnimation(root,.6f);
                        if (motion.transform.localPosition.sqrMagnitude > .000001f)
                            throw new InvalidOperationException("Fire animation did not reset");
                        EditorUtility.SetDirty(clip);
                        report.Add("RifleFireRecovery: 0.6s, recoil and return sampled; no charge-up or reload");
                    }
                    if (asset == "LowWingCover")
                    {
                        Vector3 size = bounds.Value.size;
                        if ((size - new Vector3(2,1.2f,1)).sqrMagnitude > .0001f)
                            throw new InvalidOperationException("Cover dimensions drifted: " + size);
                        var collision = root.AddComponent<BoxCollider>();
                        collision.center = new Vector3(0,.6f,0); collision.size = new Vector3(2,1.2f,1);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + asset + ".prefab");
                    var review = UnityEngine.Object.Instantiate(root);
                    review.name = asset + " Review";
                    review.transform.position = asset == "RailRifle" ? new Vector3(0,1.7f,-1) : Vector3.zero;
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var light = new GameObject("Key light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.4f;
            light.transform.rotation = Quaternion.Euler(40,-30,0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f,.55f,.6f);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(3,2.7f,-4);
            camera.transform.LookAt(new Vector3(0,1,0));
            camera.backgroundColor = new Color(.09f,.12f,.14f); camera.clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene, Folder + "/BenchmarkReview.unity");
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.Combine(Repository,"Validation"));
            File.WriteAllLines(Path.Combine(Repository,"Validation/production-benchmarks-unity.txt"), report.Concat(new[] {
                "PASS: pinned version, nonempty imports, UV coverage, material mapping, muzzle axes, cover envelope, LOD prefab and isolated scene creation.",
                "No graphics render, gameplay grip fit, collision playtest or performance certification performed." }));
            Debug.Log("PRODUCTION_BENCHMARKS_UNITY_COMPLETE");
        }

        private static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(m); return m;
        }

        public static void RenderReview()
        {
            EditorSceneManager.OpenScene(Folder + "/BenchmarkReview.unity");
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            camera.nearClipPlane = .03f; camera.farClipPlane = 20;
            var rifle = GameObject.Find("RailRifle Review");
            var cover = GameObject.Find("LowWingCover Review");
            var target = new RenderTexture(1400,1000,24);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                camera.targetTexture = target;
                foreach (var subject in new[] {rifle,cover})
                {
                    rifle.SetActive(subject == rifle); cover.SetActive(subject == cover);
                    subject.GetComponent<LODGroup>().ForceLOD(0);
                    camera.orthographic = true;
                    var focus = subject.transform.position + (subject == rifle ? Vector3.up * .05f : Vector3.up * .6f);
                    camera.orthographicSize = subject == rifle ? .53f : 1.2f;
                    camera.transform.position = focus + (subject == rifle ? new Vector3(1.7f,.8f,1.4f) : new Vector3(3,2,-3));
                    camera.transform.LookAt(focus);
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
                    RenderTexture.active = target;
                    var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try
                    {
                        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                        var colors = image.GetPixels32();
                        if (colors.Select(c => (c.r<<16)|(c.g<<8)|c.b).Distinct().Take(32).Count()<32)
                            throw new InvalidOperationException("Review capture lacks rendered detail");
                        string name = subject == rifle ? "RailRifle" : "LowWingCover";
                        File.WriteAllBytes(Path.Combine(Repository,"ArtSource/Original/ProductionBenchmarks-v1/" + name + "-Unity.png"),image.EncodeToPNG());
                    }
                    finally {UnityEngine.Object.DestroyImmediate(image);}
                }
            }
            finally {camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
            Debug.Log("PRODUCTION_BENCHMARKS_RENDER_COMPLETE");
        }
    }
}
