using System;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class CarbineMicroDetailBuilder
    {
        public static void Apply()
        {
            const string root="Assets/RailArena/Art/PrecisionCarbine/";
            AssetDatabase.Refresh();
            var normalImporter=(TextureImporter)AssetImporter.GetAtPath(root+"MetalMicroNormal.png");
            normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.wrapMode=TextureWrapMode.Repeat;normalImporter.SaveAndReimport();
            var maskImporter=(TextureImporter)AssetImporter.GetAtPath(root+"MetalDetailMask.png");
            maskImporter.sRGBTexture=false;maskImporter.alphaSource=TextureImporterAlphaSource.FromInput;maskImporter.SaveAndReimport();
            var surface=AssetDatabase.LoadAssetAtPath<Material>(root+"Surface.mat");
            if(surface==null || surface.shader.name!="Universal Render Pipeline/Lit")throw new InvalidOperationException("Expected carbine surface material");
            surface.SetTexture("_DetailNormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"MetalMicroNormal.png"));
            surface.SetTexture("_DetailMask",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"MetalDetailMask.png"));
            surface.SetTextureScale("_DetailAlbedoMap",new Vector2(16,16));surface.SetFloat("_DetailNormalMapScale",.2f);
            surface.EnableKeyword("_DETAIL_MULX2");EditorUtility.SetDirty(surface);AssetDatabase.SaveAssets();
            if(surface.GetTexture("_DetailNormalMap")==null||surface.GetTexture("_DetailMask")==null||!surface.IsKeywordEnabled("_DETAIL_MULX2"))throw new InvalidOperationException("Detail assignment failed");
            Debug.Log("CARBINE_METAL_DETAIL_APPLIED");
        }
    }
}
