using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class VerifyCarbineDetailMask
    {
        [Serializable] private sealed class Atlas { public int size; public Tile[] tiles; }
        [Serializable] private sealed class Tile { public int column,row,palette; }
        public static void Check()
        {
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--carbine-atlas");
            if(index<0||index+1>=args.Length)throw new ArgumentException("Supply --carbine-atlas");
            var atlas=JsonUtility.FromJson<Atlas>(File.ReadAllText(args[index+1]));
            const string root="Assets/RailArena/Art/PrecisionCarbine/";
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+"Surface.mat");
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"MetalDetailMask.png");
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"MetalMicroNormal.png");
            if(material.GetTexture("_DetailMask")!=mask||material.GetTexture("_DetailNormalMap")!=normal||!material.IsKeywordEnabled("_DETAIL_MULX2"))throw new Exception("Carbine material bindings are missing");
            var importer=(TextureImporter)AssetImporter.GetAtPath(root+"MetalDetailMask.png");
            if(importer.sRGBTexture||importer.alphaSource!=TextureImporterAlphaSource.FromInput)throw new Exception("Mask must retain linear alpha");
            var normalImporter=(TextureImporter)AssetImporter.GetAtPath(root+"MetalMicroNormal.png");
            if(normalImporter.textureType!=TextureImporterType.NormalMap||normalImporter.wrapMode!=TextureWrapMode.Repeat)throw new Exception("Detail normal import mismatch");
            var render=new RenderTexture(atlas.size,atlas.size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var pixels=new Texture2D(atlas.size,atlas.size,TextureFormat.RGBA32,false,true);var previous=RenderTexture.active;
            try
            {
                render.Create();Graphics.Blit(mask,render);RenderTexture.active=render;pixels.ReadPixels(new Rect(0,0,atlas.size,atlas.size),0,0);pixels.Apply();
                foreach(var tile in atlas.tiles)
                {
                    int x=Mathf.RoundToInt((tile.column+.5f)*atlas.size/9f);int y=Mathf.RoundToInt((tile.row+.5f)*atlas.size/9f);
                    float expected=tile.palette==3?120f/255f:0;
                    if(Mathf.Abs(pixels.GetPixel(x,y).a-expected)>.035f)throw new Exception("Wrong mask alpha at atlas tile "+tile.column+","+tile.row+" palette="+tile.palette);
                }
                Debug.Log("CARBINE_MASK_GPU_VERIFIED tiles="+atlas.tiles.Length);
            }
            finally {RenderTexture.active=previous;render.Release();UnityEngine.Object.DestroyImmediate(render);UnityEngine.Object.DestroyImmediate(pixels);}
        }
    }
}
