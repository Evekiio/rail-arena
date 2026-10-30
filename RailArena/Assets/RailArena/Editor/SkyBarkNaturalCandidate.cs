using System;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkNaturalCandidate
    {
        public static void Create()
        {
            const string folder = "Assets/Validation/SkyBarkNatural";
            const string target = folder + "/BarkNatural.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(target) != null) throw new InvalidOperationException("Candidate exists");
            AssetDatabase.Refresh();
            foreach (string name in new[] { "BarkAlbedo.png", "BarkNormalSource.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "/" + name);
                if (importer == null) throw new InvalidOperationException("Source texture missing: " + name);
                importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4;
                if (name == "BarkNormalSource.png") { importer.textureType = TextureImporterType.NormalMap; importer.convertToNormalmap = true; importer.heightmapScale = .18f; }
                else importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
            if (!AssetDatabase.CopyAsset("Assets/RailArena/Generated/SkyGardensArt/Material-bark.mat", target)) throw new InvalidOperationException("Material copy failed");
            var material = AssetDatabase.LoadAssetAtPath<Material>(target);
            material.SetColor("_BaseColor", new Color(.85f, .85f, .85f, 1));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BarkAlbedo.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BarkNormalSource.png")); material.SetFloat("_BumpScale", .35f); material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); Debug.Log("SKY_BARK_NATURAL_CREATED " + target);
        }
    }
}
