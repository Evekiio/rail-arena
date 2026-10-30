using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class StoneSurfaceArtBuilder
    {
        public static void Prepare()
        {
            const string root = "Assets/RailArena/Art/ArtPass/Environment/";
            foreach (string file in new[] { "Stone-Painted.png", "Stone-Normal.png", "Stone-Cavity.png" })
            {
                var importer = AssetImporter.GetAtPath(root + file) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing stone texture " + file);
                importer.textureType = file.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = file.Contains("Painted"); importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
            var profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            profile.StoneBase = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Stone-Painted.png");
            profile.StoneNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Stone-Normal.png");
            profile.StoneCavity = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Stone-Cavity.png");
            profile.StoneSources = new[] { "structure", "garden_structure", "ivory_trim", "ramps", "summit", "cover" }
                .Select(role => AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-" + role + ".mat")).ToArray();
            if (profile.StoneSources.Any(m => m == null)) throw new InvalidOperationException("Missing stone material family");
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            Debug.Log("STONE_SURFACE_ART_READY six families; existing UVs, collision and lightmaps preserved");
        }
    }
}
