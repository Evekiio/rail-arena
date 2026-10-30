using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class SkyLeafCandidate
    {
        public static void CaptureNormalCandidate()
        {
            const string folder = "Assets/Validation/SkyLeafCandidate";
            const string normalPath = folder + "/LeafNormalCandidate.png";
            if (File.Exists(normalPath)) throw new IOException("Normal candidate already exists");
            if (!AssetDatabase.CopyAsset(folder + "/LeafAlbedo.png", normalPath)) throw new IOException("Could not copy texture");
            var importer = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = true;
            importer.heightmapScale = .025f;
            importer.SaveAndReimport();
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat");
            if (normal == null || original == null) throw new IOException("Normal or foliage material missing");
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == original).ToArray();
            if (filters.Length != 12) throw new InvalidOperationException("Unexpected foliage count");
            var originalMeshes = filters.Select(f => f.sharedMesh).ToArray();
            var copies = originalMeshes.Select(m => { var copy = Object.Instantiate(m); copy.RecalculateTangents(); return copy; }).ToArray();
            var candidate = new Material(original); candidate.SetTexture("_BumpMap", normal); candidate.SetFloat("_BumpScale", .35f); candidate.EnableKeyword("_NORMALMAP");
            var camera = Camera.main; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldActive = RenderTexture.active; var target = new RenderTexture(960, 540, 24); var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyLeafNormalComparison");
            if (Directory.Exists(output)) throw new IOException("Comparison output exists");
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                foreach (var view in new[] {
                    ("shrub", new Vector3(29.5f, 1.15f, -17.4f), new Vector3(29.5f, .55f, -16)),
                    ("tree", new Vector3(28, 4, -17.5f), new Vector3(29.4f, 4, -16)) })
                {
                    camera.transform.SetPositionAndRotation(view.Item2, Quaternion.LookRotation(view.Item3 - view.Item2));
                    foreach (bool useCandidate in new[] { false, true })
                    {
                        for (int i = 0; i < filters.Length; i++)
                        {
                            filters[i].sharedMesh = useCandidate ? copies[i] : originalMeshes[i];
                            filters[i].GetComponent<MeshRenderer>().sharedMaterial = useCandidate ? candidate : original;
                        }
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(output, $"{view.Item1}-{(useCandidate ? "normal" : "baseline")}.png"), pixels.EncodeToPNG());
                    }
                }
            }
            finally
            {
                for (int i = 0; i < filters.Length; i++) { filters[i].sharedMesh = originalMeshes[i]; filters[i].GetComponent<MeshRenderer>().sharedMaterial = original; Object.DestroyImmediate(copies[i]); }
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = oldActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); Object.DestroyImmediate(candidate);
            }
            Debug.Log("SKY_LEAF_NORMAL_COMPARISON " + output);
        }

        public static void CaptureTiling()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Validation/SkyBarkNatural/TilingDiagnostic.shader");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RailArena/Generated/SkyGardensArt/LeafNaturalAlbedo.png");
            if (shader == null || texture == null) throw new IOException("Tiling diagnostic input missing");
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyLeafTiling.png");
            if (File.Exists(output)) throw new IOException("Tiling capture already exists");
            var material = new Material(shader); var target = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1024, 1024, TextureFormat.RGB24, false); var oldActive = RenderTexture.active;
            try
            {
                target.Create(); Graphics.Blit(texture, target, material); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); pixels.Apply();
                File.WriteAllBytes(output, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels); Object.DestroyImmediate(material);
            }
            Debug.Log("SKY_LEAF_TILING " + output);
        }

        public static void BuildPlaytest()
        {
            const string output = "../Builds/SkyGardens-NaturalLeaf-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Build output already exists");
            Verify();
            ArenaBuilder.BuildWindowsPlaytestTo(output);
            Debug.Log("SKY_LEAF_BUILD " + Path.GetFullPath(output));
        }

        public static void Apply()
        {
            const string originalPath = "Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat";
            const string sourcePath = "Assets/Validation/SkyLeafCandidate/LeafAlbedo.png";
            const string texturePath = "Assets/RailArena/Generated/SkyGardensArt/LeafNaturalAlbedo.png";
            string backup = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/FoliageMaterialBeforeNatural");
            var material = AssetDatabase.LoadAssetAtPath<Material>(originalPath);
            if (material == null || material.GetTexture("_BaseMap") != null) throw new InvalidOperationException("Foliage material is missing or already textured");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath) == null || File.Exists(texturePath) || Directory.Exists(backup)) throw new IOException("Source is missing or output already exists");
            Directory.CreateDirectory(backup);
            File.Copy(originalPath, Path.Combine(backup, Path.GetFileName(originalPath)));
            File.Copy(originalPath + ".meta", Path.Combine(backup, Path.GetFileName(originalPath) + ".meta"));
            if (!AssetDatabase.CopyAsset(sourcePath, texturePath)) throw new IOException("Could not copy leaf texture");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new IOException("Copied leaf texture did not import");
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", Vector2.one * 4);
            material.SetColor("_BaseColor", new Color(.65f, .72f, .5f, 1));
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RailArena/Generated/SkyGardensArt/LeafNaturalAlbedo.png");
            if (material == null || texture == null || material.GetTexture("_BaseMap") != texture || material.GetTextureScale("_BaseMap") != Vector2.one * 4)
                throw new InvalidOperationException("Playable leaf material does not match the intended texture setup");
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == material).ToArray();
            if (filters.Length != 12) throw new InvalidOperationException("Unexpected foliage renderer count: " + filters.Length);
            Debug.Log("SKY_LEAF_APPLIED renderers=" + filters.Length + " texture=" + AssetDatabase.GetAssetPath(texture));
        }

        public static void CaptureCloseViews()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat");
            var candidate = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/SkyLeafCandidate/LeafCandidate.mat");
            if (original == null || candidate == null) throw new IOException("Foliage material missing");
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == original).ToArray();
            if (filters.Length == 0) throw new InvalidOperationException("No foliage renderers found");
            var ratios = new System.Collections.Generic.List<float>();
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh; var uv = mesh.uv; var vertices = mesh.vertices; var indices = mesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = indices[i], b = indices[i + 1];
                    float length = Vector3.Distance(filter.transform.TransformPoint(vertices[a]), filter.transform.TransformPoint(vertices[b]));
                    if (length > .001f) ratios.Add(Vector2.Distance(uv[a], uv[b]) / length);
                }
            }
            ratios.Sort();
            Debug.Log($"SKY_LEAF_UV_SCALE samples={ratios.Count} median_uv_per_m={ratios[ratios.Count / 2]:F4}");
            var camera = Camera.main; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldScale = candidate.GetTextureScale("_BaseMap"); var oldActive = RenderTexture.active;
            var target = new RenderTexture(960, 540, 24); var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyLeafCloseComparison");
            if (Directory.Exists(output)) throw new IOException("Comparison output exists");
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                foreach (var view in new[] {
                    ("shrub", new Vector3(29.5f, 1.15f, -17.4f), new Vector3(29.5f, .55f, -16)),
                    ("tree", new Vector3(28, 4, -17.5f), new Vector3(29.4f, 4, -16)) })
                {
                    camera.transform.SetPositionAndRotation(view.Item2, Quaternion.LookRotation(view.Item3 - view.Item2));
                    foreach (int scale in new[] { 0, 1, 4, 8 })
                    {
                        foreach (var filter in filters) filter.GetComponent<MeshRenderer>().sharedMaterial = scale == 0 ? original : candidate;
                        if (scale > 0) candidate.SetTextureScale("_BaseMap", Vector2.one * scale);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(output, $"{view.Item1}-{(scale == 0 ? "baseline" : "scale-" + scale)}.png"), pixels.EncodeToPNG());
                    }
                }
            }
            finally
            {
                candidate.SetTextureScale("_BaseMap", oldScale);
                foreach (var filter in filters) filter.GetComponent<MeshRenderer>().sharedMaterial = original;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = oldActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
            Debug.Log("SKY_LEAF_CLOSE_COMPARISON " + output);
        }

        public static void InspectUv()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat");
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == original).ToArray();
            int triangles = 0, degenerate = 0;
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                var uv = mesh.uv;
                var indices = mesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector2 a = uv[indices[i]], b = uv[indices[i + 1]], c = uv[indices[i + 2]];
                    if (Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 1e-8f) degenerate++;
                    triangles++;
                }
                Debug.Log($"SKY_LEAF_MESH {mesh.name} vertices={mesh.vertexCount} bounds={filter.GetComponent<Renderer>().bounds}");
            }
            Debug.Log($"SKY_LEAF_UV triangles={triangles} degenerate={degenerate} meshes={filters.Length}");
        }
        public static void CreateAndCapture()
        {
            const string folder = "Assets/Validation/SkyLeafCandidate";
            const string materialPath = folder + "/LeafCandidate.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) throw new IOException("Candidate material already exists");
            var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "/LeafAlbedo.png");
            if (importer == null) throw new IOException("Leaf texture is missing");
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/LeafAlbedo.png");
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foliage.mat");
            if (texture == null || original == null) throw new IOException("Required assets missing");
            var candidate = new Material(original) { name = "Sky leaf candidate" };
            candidate.SetTexture("_BaseMap", texture);
            candidate.SetColor("_BaseColor", new Color(.65f, .72f, .5f, 1));
            AssetDatabase.CreateAsset(candidate, materialPath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
                .Where(f => f.TryGetComponent<MeshRenderer>(out var renderer) && renderer.sharedMaterial == original).ToArray();
            if (filters.Length == 0) throw new InvalidOperationException("No foliage renderers found");
            int missingUv = filters.Count(f => f.sharedMesh == null || f.sharedMesh.uv.Length != f.sharedMesh.vertexCount);
            int missingTangents = filters.Count(f => f.sharedMesh == null || f.sharedMesh.tangents.Length != f.sharedMesh.vertexCount);
            Debug.Log($"SKY_LEAF_AUDIT renderers={filters.Length} missingUV={missingUv} missingTangents={missingTangents}");
            var camera = Camera.main;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(960, 540, 24);
            var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyLeafComparison");
            if (Directory.Exists(output)) throw new IOException("Comparison output exists");
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                var position = new Vector3(25, 2, -16);
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(29, 2, -16) - position));
                foreach (bool useCandidate in new[] { false, true })
                {
                    foreach (var filter in filters) filter.GetComponent<MeshRenderer>().sharedMaterial = useCandidate ? candidate : original;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.Combine(output, useCandidate ? "candidate.png" : "baseline.png"), pixels.EncodeToPNG());
                }
            }
            finally
            {
                foreach (var filter in filters) filter.GetComponent<MeshRenderer>().sharedMaterial = original;
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
            Debug.Log("SKY_LEAF_COMPARISON " + output);
        }
    }
}
