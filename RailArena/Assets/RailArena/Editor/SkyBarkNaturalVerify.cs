using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkNaturalVerify
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            const string folder = "Assets/RailArena/Generated/SkyGardensArt/";
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "Material-bark.mat");
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BarkNaturalAlbedo.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BarkNaturalNormal.png");
            var colorImporter = AssetImporter.GetAtPath(folder + "BarkNaturalAlbedo.png") as TextureImporter;
            var normalImporter = AssetImporter.GetAtPath(folder + "BarkNaturalNormal.png") as TextureImporter;
            if (material == null || color == null || normal == null || colorImporter == null || normalImporter == null ||
                material.GetTexture("_BaseMap") != color || material.GetTexture("_BumpMap") != normal || !material.IsKeywordEnabled("_NORMALMAP") ||
                !colorImporter.sRGBTexture || normalImporter.textureType != TextureImporterType.NormalMap || !normalImporter.convertToNormalmap ||
                colorImporter.wrapMode != TextureWrapMode.Repeat || normalImporter.wrapMode != TextureWrapMode.Repeat)
                throw new Exception("Bark material or texture import mismatch");
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material)).ToArray();
            if (renderers.Length != 8 || renderers.Any(r => r.GetComponent<MeshFilter>()?.sharedMesh == null || r.GetComponent<MeshFilter>().sharedMesh.tangents.Length != r.GetComponent<MeshFilter>().sharedMesh.vertexCount))
                throw new Exception("Bark renderer or tangent mismatch");
            Debug.Log("SKY_BARK_NATURAL_VERIFIED renderers=" + renderers.Length + " texture=" + color.width + "x" + color.height);
        }
    }
}
