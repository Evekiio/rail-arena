using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class SkyBronzeAudit
    {
        public static void CaptureSurfaceDetail()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat");
            var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var renderers = all.OfType<MeshRenderer>().Where(r => r.sharedMaterials.Contains(material)).ToArray();
            MeshRenderer renderer = null; Vector3 center = default, normal = default; float maxArea = 0;
            foreach (var candidate in renderers)
            {
                var mesh = candidate.GetComponent<MeshFilter>().sharedMesh; var vertices = mesh.vertices; var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = candidate.transform.TransformPoint(vertices[triangles[i]]); var b = candidate.transform.TransformPoint(vertices[triangles[i + 1]]); var c = candidate.transform.TransformPoint(vertices[triangles[i + 2]]);
                    var cross = Vector3.Cross(b - a, c - a); float area = cross.magnitude;
                    if (area <= maxArea) continue;
                    renderer = candidate; maxArea = area; center = (a + b + c) / 3; normal = cross.normalized;
                }
            }
            if (renderer == null || maxArea <= 0) throw new Exception("No bronze triangle to capture");
            var states = all.Select(r => r.enabled).ToArray(); var previousMaterials = renderer.sharedMaterials;
            var comparisonMaterial = UnityEngine.Object.Instantiate(material);
            var lightObject = new GameObject("Bronze diagnostic light");
            var diagnosticLight = lightObject.AddComponent<Light>(); diagnosticLight.type = LightType.Directional; diagnosticLight.intensity = 2;
            var tangent = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.right : Vector3.up).normalized;
            lightObject.transform.rotation = Quaternion.LookRotation((-normal + tangent * .6f).normalized);
            var camera = Camera.main; var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var old = RenderTexture.active; var previous = camera.targetTexture;
            try
            {
                for (int i = 0; i < all.Length; i++) all[i].enabled = all[i] == renderer;
                renderer.sharedMaterials = previousMaterials.Select(m => m == material ? comparisonMaterial : m).ToArray();
                target.Create(); camera.targetTexture = target; camera.transform.position = center + normal * Mathf.Clamp(Mathf.Sqrt(maxArea) * 1.5f, .5f, 5); camera.transform.LookAt(center);
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/BronzeComparison")); Directory.CreateDirectory(output);
                foreach (bool enabled in new[] { false, true })
                {
                    if (enabled) comparisonMaterial.EnableKeyword("_NORMALMAP"); else comparisonMaterial.DisableKeyword("_NORMALMAP");
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, enabled ? "Normal-On.png" : "Normal-Off.png"), image.EncodeToPNG());
                }
                Debug.Log("SKY_BRONZE_CAPTURE renderer=" + renderer.name + " output=" + output);
            }
            finally { for (int i = 0; i < all.Length; i++) all[i].enabled = states[i]; renderer.sharedMaterials = previousMaterials; camera.targetTexture = previous; RenderTexture.active = old; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(comparisonMaterial); UnityEngine.Object.DestroyImmediate(lightObject); }
        }
        public static void VerifySurfaceDetail()
        {
            Run();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat");
            var texture = material.GetTexture("_BumpMap"); var importer = texture == null ? null : AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.NormalMap || importer.wrapMode != TextureWrapMode.Repeat || !material.IsKeywordEnabled("_NORMALMAP") || Mathf.Abs(material.GetFloat("_BumpScale") - .3f) > .0001f)
                throw new Exception("Bronze normal setup did not persist");
            PrepareSkyGiCandidate.VerifyIntegrated();
            Debug.Log("SKY_BRONZE_DETAIL_VERIFIED");
        }
        public static void ApplySurfaceDetail()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            const string materialPath = "Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat";
            const string texturePath = "Assets/RailArena/Generated/SkyGardensArt/Bronze fine normal.png";
            string backup = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/BronzeBefore"));
            if (Directory.Exists(backup) || File.Exists(texturePath)) throw new IOException("Bronze backup or texture already exists");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null || material.GetTexture("_BumpMap") != null) throw new Exception("Bronze material missing or already has a normal map");
            var meshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material))
                .Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).Where(m => m != null).Distinct().ToArray();
            if (meshes.Length == 0 || meshes.Any(m => m.uv.Length != m.vertexCount || !AssetDatabase.GetAssetPath(m).StartsWith("Assets/Validation/SkyGiCandidate/", StringComparison.Ordinal)))
                throw new Exception("Unexpected bronze meshes or missing UVs");
            Directory.CreateDirectory(backup);
            foreach (var path in meshes.Select(AssetDatabase.GetAssetPath).Append(materialPath))
            {
                var destination = Path.Combine(backup, path.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(path, destination); if (File.Exists(path + ".meta")) File.Copy(path + ".meta", destination + ".meta");
            }
            const int size = 256; var texture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            float Height(int x, int y)
            {
                float a = (x % size + size) % size * Mathf.PI * 2 / size, b = (y % size + size) % size * Mathf.PI * 2 / size;
                return .025f * Mathf.Sin(a * 31 + b * 3) + .015f * Mathf.Sin(a * 57 - b * 5) + .012f * Mathf.Cos(a * 11 + b * 17);
            }
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                var n = new Vector3(-(Height(x + 1, y) - Height(x - 1, y)) * 2, -(Height(x, y + 1) - Height(x, y - 1)) * 2, 1).normalized;
                texture.SetPixel(x, y, new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f));
            }
            File.WriteAllBytes(texturePath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); importer.textureType = TextureImporterType.NormalMap; importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.SaveAndReimport();
            foreach (var mesh in meshes) { mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh); }
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetFloat("_BumpScale", .3f); material.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets(); Debug.Log("SKY_BRONZE_DETAIL_APPLIED meshes=" + meshes.Length + " backup=" + backup);
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat");
            if (material == null) throw new Exception("Bronze material missing");
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material)).ToArray();
            var meshes = renderers.Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).Where(m => m != null).Distinct().ToArray();
            int uv = meshes.Count(m => m.uv.Length == m.vertexCount), tangents = meshes.Count(m => m.tangents.Length == m.vertexCount);
            Debug.Log("SKY_BRONZE_AUDIT renderers=" + renderers.Length + " meshes=" + meshes.Length + " uv=" + uv + " tangents=" + tangents + " shader=" + material.shader.name + " metallic=" + material.GetFloat("_Metallic") + " smoothness=" + material.GetFloat("_Smoothness"));
            if (renderers.Length == 0 || meshes.Length == 0 || uv != meshes.Length || tangents != meshes.Length) throw new Exception("Bronze meshes need UV or tangent preparation");
        }
    }
}
