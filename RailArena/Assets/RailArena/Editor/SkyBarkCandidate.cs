using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkCandidate
    {
        private const string Folder = "Assets/Validation/SkyBarkCandidate";
        private static float Height(float u, float v)
        {
            float grain = Mathf.Sin(u * 16 + .45f * Mathf.Sin(v * 2)) + .35f * Mathf.Sin(u * 31 + v * 3) + .18f * Mathf.Cos(u * 47 - v * 5);
            return .5f + grain * .13f;
        }
        public static void Create()
        {
            if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Candidate already exists");
            AssetDatabase.CreateFolder("Assets/Validation", "SkyBarkCandidate"); const int side = 256;
            var color = new Texture2D(side, side, TextureFormat.RGBA32, false);
            var normal = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            var height = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            try
            {
                float delta = 2 * Mathf.PI / side;
                for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
                {
                    float u = x * delta, v = y * delta; float h = Height(u, v);
                    float shade = Mathf.Clamp01(.83f + .17f * h);
                    color.SetPixel(x, y, new Color(shade, shade * .975f, shade * .94f, 1));
                    float dx = (Height(u + delta, v) - Height(u - delta, v)) * 2.2f;
                    float dy = (Height(u, v + delta) - Height(u, v - delta)) * 2.2f;
                    var n = new Vector3(-dx, -dy, 1).normalized;
                    normal.SetPixel(x, y, new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1));
                    height.SetPixel(x, y, new Color(0, h, 0, 1));
                }
                foreach (var entry in new[] { ("BarkAlbedo.png", color), ("BarkNormal.png", normal), ("BarkHeight.png", height) })
                { entry.Item2.Apply(); File.WriteAllBytes(Folder + "/" + entry.Item1, entry.Item2.EncodeToPNG()); }
            }
            finally { UnityEngine.Object.DestroyImmediate(color); UnityEngine.Object.DestroyImmediate(normal); UnityEngine.Object.DestroyImmediate(height); }
            AssetDatabase.Refresh();
            foreach (string name in new[] { "BarkAlbedo.png", "BarkNormal.png", "BarkHeight.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + name);
                importer.sRGBTexture = name == "BarkAlbedo.png"; importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4;
                if (name == "BarkNormal.png") importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            if (!AssetDatabase.CopyAsset("Assets/RailArena/Generated/SkyGardensArt/Material-bark.mat", Folder + "/BarkCandidate.mat")) throw new InvalidOperationException("Material copy failed");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/BarkCandidate.mat");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BarkAlbedo.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BarkNormal.png")); material.SetFloat("_BumpScale", .5f); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_ParallaxMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BarkHeight.png")); material.SetFloat("_Parallax", .003f); material.EnableKeyword("_PARALLAXMAP");
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); Debug.Log("SKY_BARK_CANDIDATE_CREATED " + Folder);
        }
    }
}
