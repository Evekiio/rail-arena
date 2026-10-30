using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class SkyStoneSmoothness
    {
        private const string Folder = "Assets/RailArena/Generated/SkyGardensArt";
        private static readonly string[] Roles = { "structure", "cover", "ivory_trim", "garden_structure", "ramps", "summit" };
        public static void Apply()
        {
            SkyStoneSurfaceMaps.Verify();
            var materials = Roles.Select(role => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat")).ToArray();
            string texturePath = Folder + "/Stone metallic smoothness.png";
            string backup = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/StoneSmoothnessBefore"));
            if (File.Exists(texturePath) || Directory.Exists(backup)) throw new IOException("Stone smoothness texture or backup already exists");
            if (materials.Any(m => m == null || m.GetTexture("_MetallicGlossMap") != null || Mathf.Abs(m.GetFloat("_Metallic") - .05f) > .001f || m.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A")))
                throw new Exception("Stone material differs from expected metallic setup");
            Directory.CreateDirectory(backup);
            foreach (var material in materials)
            {
                string path = AssetDatabase.GetAssetPath(material), dest = Path.Combine(backup, Path.GetFileName(path));
                File.Copy(path, dest); File.Copy(path + ".meta", dest + ".meta");
            }
            const int size = 512; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                int xx = x % 256, yy = y % 128; float distance = Mathf.Min(xx, 256 - xx, yy, 128 - yy);
                float edge = Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / 4f));
                float variation = .04f * Mathf.Sin(x * Mathf.PI * 2 / size * 7) * Mathf.Cos(y * Mathf.PI * 2 / size * 9);
                float smoothness = Mathf.Lerp(.68f, .94f + variation, edge);
                texture.SetPixel(x, y, new Color(.05f, 0, 0, smoothness));
            }
            File.WriteAllBytes(texturePath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.SaveAndReimport();
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            foreach (var material in materials) { material.SetTexture("_MetallicGlossMap", map); material.EnableKeyword("_METALLICSPECGLOSSMAP"); EditorUtility.SetDirty(material); }
            AssetDatabase.SaveAssets(); Debug.Log("SKY_STONE_SMOOTHNESS_APPLIED materials=" + materials.Length + " backup=" + backup);
        }
        public static void Verify()
        {
            SkyStoneSurfaceMaps.Verify();
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!raw.LoadImage(File.ReadAllBytes(Folder + "/Stone metallic smoothness.png"))) throw new Exception("Could not read stone map");
                var seam = raw.GetPixel(0, 32); var face = raw.GetPixel(32, 32);
                if (Mathf.Abs(face.r - .05f) > .01f || seam.a >= face.a || face.a < .85f) throw new Exception("Unexpected packed metallic or smoothness values");
            }
            finally { UnityEngine.Object.DestroyImmediate(raw); }
            foreach (string role in Roles)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat"); var texture = material.GetTexture("_MetallicGlossMap");
                var importer = texture == null ? null : AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer == null || importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Repeat || !material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") || material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A")) throw new Exception("Invalid metallic smoothness map on " + role);
            }
            Debug.Log("SKY_STONE_SMOOTHNESS_VERIFIED materials=" + Roles.Length);
        }
        public static void CaptureComparison()
        {
            Verify();
            var materials = Roles.Select(role => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Material-" + role + ".mat")).ToArray();
            var camera = Camera.main; var previous = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/StoneSmoothnessComparison")); Directory.CreateDirectory(output);
            try
            {
                target.Create(); camera.targetTexture = target; camera.transform.position = new Vector3(0, 1.6f, -9); camera.transform.LookAt(new Vector3(0, 0, -6));
                foreach (bool enabled in new[] { false, true })
                {
                    foreach (var material in materials) if (enabled) material.EnableKeyword("_METALLICSPECGLOSSMAP"); else material.DisableKeyword("_METALLICSPECGLOSSMAP");
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, enabled ? "Map-On.png" : "Map-Off.png"), image.EncodeToPNG());
                }
            }
            finally { foreach (var material in materials) material.EnableKeyword("_METALLICSPECGLOSSMAP"); camera.targetTexture = previous; RenderTexture.active = active; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
            Debug.Log("SKY_STONE_SMOOTHNESS_COMPARISON " + output);
        }
    }
}
