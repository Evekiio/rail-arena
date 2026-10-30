using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyStoneDetailBuilder
    {
        private const string Folder = "Assets/RailArena/Generated/SkyGardensArt";
        public static void Apply()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false, true);
            float Height(int x,int y)
            {
                float a = (x % size + size) % size * Mathf.PI * 2 / size;
                float b = (y % size + size) % size * Mathf.PI * 2 / size;
                return .035f * Mathf.Sin(a*13+b*7) + .025f * Mathf.Sin(a*29-b*19) + .02f * Mathf.Cos(a*7+b*31);
            }
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float dx = Height(x+1,y)-Height(x-1,y), dy = Height(x,y+1)-Height(x,y-1);
                var n = new Vector3(-dx*2,-dy*2,1).normalized;
                texture.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
            }
            string texturePath = Folder + "/Stone micro normal.png";
            if (File.Exists(texturePath)) throw new IOException("Detail texture already exists; preserve it and edit explicitly");
            File.WriteAllBytes(texturePath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath); importer.textureType=TextureImporterType.NormalMap; importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=4; importer.SaveAndReimport();
            var detail=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World;
            var meshes=new HashSet<Mesh>(); var materials=new HashSet<Material>();
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.TryGetComponent<MeshRenderer>(out var renderer)) continue;
                var material=renderer.sharedMaterial;
                if (material == null || !material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap") == null) continue;
                if (!AssetDatabase.GetAssetPath(material).StartsWith(Folder+"/",StringComparison.Ordinal)) continue;
                materials.Add(material); var mesh=filter.sharedMesh;
                if (mesh != null && AssetDatabase.GetAssetPath(mesh).StartsWith(Folder+"/",StringComparison.Ordinal)) meshes.Add(mesh);
            }
            foreach(var mesh in meshes) { if(mesh.uv.Length!=mesh.vertexCount) throw new Exception("Missing UVs: "+mesh.name); mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh); }
            foreach(var material in materials)
            {
                material.SetTexture("_DetailNormalMap",detail); material.SetTextureScale("_DetailAlbedoMap",new Vector2(8,8));
                material.SetFloat("_DetailNormalMapScale",.2f); material.EnableKeyword("_DETAIL_MULX2"); EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            if (meshes.Count==0 || materials.Count==0) throw new Exception("No stone assets were updated");
            Debug.Log("SKY_STONE_DETAIL_APPLIED meshes="+meshes.Count+" materials="+materials.Count);
        }
    }
}
