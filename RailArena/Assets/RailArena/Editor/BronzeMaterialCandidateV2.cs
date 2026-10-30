using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class BronzeMaterialCandidateV2
    {
        private const string Folder = "Assets/Validation/BronzePbrCandidateV2";
        public static void Create()
        {
            if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Candidate folder already exists");
            AssetDatabase.CreateFolder("Assets/Validation", "BronzePbrCandidateV2");
            const int side = 256;
            var albedo = new Texture2D(side, side, TextureFormat.RGBA32, false);
            var metallic = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            var occlusion = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            var height = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            try
            {
                for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
                {
                    float u = x * Mathf.PI * 2 / side, v = y * Mathf.PI * 2 / side;
                    float pattern = .55f * Mathf.Sin(u * 3 + Mathf.Sin(v * 2)) + .3f * Mathf.Sin(u * 11 + v * 7) + .15f * Mathf.Cos(u * 19 - v * 13);
                    float shade = Mathf.Clamp01(.985f + .015f * pattern);
                    albedo.SetPixel(x, y, new Color(shade, shade, shade * .99f, 1));
                    metallic.SetPixel(x, y, new Color(.65f + .015f * pattern, 0, 0, .975f + .025f * pattern));
                    occlusion.SetPixel(x, y, new Color(0, .96f + .025f * pattern, 0, 1));
                    height.SetPixel(x, y, new Color(0, .5f + .08f * pattern, 0, 1));
                }
                foreach (var item in new[] { ("Albedo.png", albedo), ("MetallicSmoothness.png", metallic), ("Occlusion.png", occlusion), ("Height.png", height) })
                { item.Item2.Apply(); File.WriteAllBytes(Folder + "/" + item.Item1, item.Item2.EncodeToPNG()); }
            }
            finally { UnityEngine.Object.DestroyImmediate(albedo); UnityEngine.Object.DestroyImmediate(metallic); UnityEngine.Object.DestroyImmediate(occlusion); UnityEngine.Object.DestroyImmediate(height); }
            AssetDatabase.Refresh();
            foreach (string name in new[] { "Albedo.png", "MetallicSmoothness.png", "Occlusion.png", "Height.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + name);
                importer.sRGBTexture = name == "Albedo.png"; importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.SaveAndReimport();
            }
            const string source = "Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat";
            if (!AssetDatabase.CopyAsset(source, Folder + "/BronzeCandidate.mat")) throw new InvalidOperationException("Material copy failed");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/BronzeCandidate.mat");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Albedo.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/MetallicSmoothness.png")); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Occlusion.png")); material.EnableKeyword("_OCCLUSIONMAP");
            material.SetTexture("_ParallaxMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Height.png")); material.SetFloat("_Parallax", .002f); material.EnableKeyword("_PARALLAXMAP");
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); Debug.Log("BRONZE_PBR_CANDIDATE_CREATED " + Folder);
        }
    }
}

