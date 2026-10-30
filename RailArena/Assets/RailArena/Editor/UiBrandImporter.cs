using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class UiBrandImporter
    {
        public static void Configure()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles("Assets/Resources/UI/Brand","*.png",SearchOption.AllDirectories))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;
                importer.textureShape=TextureImporterShape.Texture2D;
                importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.sRGBTexture=true;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.mipmapEnabled=false;
                importer.alphaIsTransparency=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=4096;
                importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles("Assets/Resources/UI/Brand/Fonts","*.ttf"))
            {
                var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(path);
                importer.fontTextureCase=FontTextureCase.Dynamic;
                importer.includeFontData=true;
                importer.fontSize=32;
                importer.SaveAndReimport();
            }
            Debug.Log("UI_BRAND_IMPORT_CONFIGURED");
        }
    }
}
