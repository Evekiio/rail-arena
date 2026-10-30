using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class SkyStoneSurfaceMaps
    {
        private const string Folder = "Assets/RailArena/Generated/SkyGardensArt";
        private static readonly string[] Roles = { "structure", "cover", "ivory_trim", "garden_structure", "ramps", "summit" };
        public static void Apply()
        {
            VerifySkyStoneDetail.Check();
            string backup = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/StoneMapsBefore"));
            string heightPath = Folder + "/Stone height.png", aoPath = Folder + "/Stone occlusion.png";
            if (Directory.Exists(backup) || File.Exists(heightPath) || File.Exists(aoPath)) throw new IOException("Stone maps or backup already exist");
            var materials = Roles.Select(role => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat")).ToArray();
            if (materials.Any(m => m == null || m.GetTexture("_ParallaxMap") != null || m.GetTexture("_OcclusionMap") != null)) throw new Exception("Stone material missing or already has maps");
            Directory.CreateDirectory(backup);
            foreach (var material in materials)
            {
                string path = AssetDatabase.GetAssetPath(material), destination = Path.Combine(backup, Path.GetFileName(path));
                File.Copy(path, destination); File.Copy(path + ".meta", destination + ".meta");
            }
            const int size = 512; var height = new Texture2D(size, size, TextureFormat.RGB24, false, true); var ao = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int xx = x % 256, yy = y % 128; float distance = Mathf.Min(xx, 256 - xx, yy, 128 - yy);
                float edge = Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / 4f));
                float h = Mathf.Lerp(.25f, .75f, edge), occlusion = Mathf.Lerp(.65f, 1f, edge);
                height.SetPixel(x, y, new Color(h, h, h)); ao.SetPixel(x, y, new Color(occlusion, occlusion, occlusion));
            }
            File.WriteAllBytes(heightPath, height.EncodeToPNG()); File.WriteAllBytes(aoPath, ao.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(height); UnityEngine.Object.DestroyImmediate(ao); AssetDatabase.Refresh();
            foreach (string path in new[] { heightPath, aoPath })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.SaveAndReimport();
            }
            var heightMap = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath); var aoMap = AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);
            foreach (var material in materials)
            {
                material.SetTexture("_ParallaxMap", heightMap); material.SetFloat("_Parallax", .003f); material.EnableKeyword("_PARALLAXMAP");
                material.SetTexture("_OcclusionMap", aoMap); material.SetFloat("_OcclusionStrength", .4f); material.EnableKeyword("_OCCLUSIONMAP"); EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets(); Debug.Log("SKY_STONE_SURFACE_MAPS_APPLIED materials=" + materials.Length + " backup=" + backup);
        }
        public static void Verify()
        {
            VerifySkyStoneDetail.Check();
            foreach (string role in Roles)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat");
                foreach (string property in new[] { "_ParallaxMap", "_OcclusionMap" })
                {
                    var texture = material.GetTexture(property); var importer = texture == null ? null : AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                    if (importer == null || importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Repeat) throw new Exception("Invalid " + property + " on " + role);
                }
                if (!material.IsKeywordEnabled("_PARALLAXMAP") || !material.IsKeywordEnabled("_OCCLUSIONMAP") || Mathf.Abs(material.GetFloat("_Parallax") - .003f) > .0001f) throw new Exception("Stone map settings missing on " + role);
            }
            PrepareSkyGiCandidate.VerifyIntegrated(); Debug.Log("SKY_STONE_SURFACE_MAPS_VERIFIED materials=" + Roles.Length);
        }
        public static void CaptureComparison()
        {
            Verify();
            var materials = Roles.Select(role => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat")).ToArray();
            var camera = Camera.main; var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/StoneMapsComparison")); Directory.CreateDirectory(output);
            try
            {
                target.Create(); camera.targetTexture = target;
                foreach (var view in new[] { ("Crown", new Vector3(-8, 5.7f, 7), new Vector3(4, 5, 0)), ("FloorClose", new Vector3(0, 1.6f, -9), new Vector3(0, 0, -6)), ("WallClose", new Vector3(-4, 5.7f, 4), new Vector3(4, 5, 0)) })
                {
                    camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                    foreach (bool enabled in new[] { false, true })
                    {
                        foreach (var material in materials)
                            if (enabled) { material.EnableKeyword("_PARALLAXMAP"); material.EnableKeyword("_OCCLUSIONMAP"); }
                            else { material.DisableKeyword("_PARALLAXMAP"); material.DisableKeyword("_OCCLUSIONMAP"); }
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, view.Item1 + (enabled ? "-On.png" : "-Off.png")), image.EncodeToPNG());
                    }
                }
            }
            finally
            {
                foreach (var material in materials) { material.EnableKeyword("_PARALLAXMAP"); material.EnableKeyword("_OCCLUSIONMAP"); }
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            Debug.Log("SKY_STONE_MAPS_COMPARISON " + output);
        }
    }
}
