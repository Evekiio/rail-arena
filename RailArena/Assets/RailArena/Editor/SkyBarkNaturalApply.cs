using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkNaturalApply
    {
        public static void Apply()
        {
            const string folder = "Assets/RailArena/Generated/SkyGardensArt";
            const string candidate = "Assets/Validation/SkyBarkNatural";
            const string materialPath = folder + "/Material-bark.mat";
            string[] names = { "BarkNaturalAlbedo.png", "BarkNaturalNormal.png" };
            if (names.Any(name => AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name) != null)) throw new InvalidOperationException("Destination texture already exists");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath); if (material == null || material.GetTexture("_BaseMap") != null || material.GetTexture("_BumpMap") != null) throw new InvalidOperationException("Unexpected bark material state");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var meshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material)).Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).Where(m => m != null).Distinct().ToArray();
            if (meshes.Length == 0 || meshes.Any(m => m.uv.Length != m.vertexCount || m.tangents.Length != m.vertexCount)) throw new InvalidOperationException("Bark mesh UVs or tangents are missing");
            var backup = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/BarkMaterialBeforeNatural"); if (Directory.Exists(backup)) throw new IOException("Backup exists"); Directory.CreateDirectory(backup); File.Copy(materialPath, Path.Combine(backup, "Material-bark.mat"));
            if (!AssetDatabase.CopyAsset(candidate + "/BarkAlbedo.png", folder + "/" + names[0]) || !AssetDatabase.CopyAsset(candidate + "/BarkNormalSource.png", folder + "/" + names[1])) throw new IOException("Texture copy failed");
            material.SetColor("_BaseColor", new Color(.85f, .85f, .85f, 1));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + names[0]));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + names[1])); material.SetFloat("_BumpScale", .35f); material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); Debug.Log("SKY_BARK_NATURAL_APPLIED meshes=" + meshes.Length + " backup=" + backup);
        }
    }
}
