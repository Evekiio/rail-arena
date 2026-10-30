using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class VerifySkyStoneDetail
    {
        public static void Check()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World;
            var meshes=new HashSet<Mesh>(); var materials=new HashSet<Material>();
            var stoneRoles=new HashSet<string>{"Material-structure","Material-cover","Material-ivory_trim","Material-garden_structure","Material-ramps","Material-summit"};
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.TryGetComponent<MeshRenderer>(out var renderer)) continue;
                var material=renderer.sharedMaterial;
                if (material==null || !stoneRoles.Contains(material.name) || !AssetDatabase.GetAssetPath(material).StartsWith("Assets/RailArena/Generated/SkyGardensArt/",StringComparison.Ordinal) || !material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap")==null) continue;
                materials.Add(material);var mesh=filter.sharedMesh;
                if (mesh!=null) meshes.Add(mesh);
            }
            foreach(var material in materials)
            {
                if (!material.IsKeywordEnabled("_DETAIL_MULX2") || material.GetTexture("_DetailNormalMap")==null || material.GetTextureScale("_DetailAlbedoMap")!=new Vector2(8,8)) throw new Exception("Detail setup missing: "+material.name);
                var path=AssetDatabase.GetAssetPath(material.GetTexture("_DetailNormalMap"));
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || importer.textureType!=TextureImporterType.NormalMap || importer.wrapMode!=TextureWrapMode.Repeat) throw new Exception("Detail import mismatch: "+path);
            }
            foreach(var mesh in meshes)
            {
                if (mesh.tangents.Length != mesh.vertexCount || mesh.uv.Length != mesh.vertexCount) throw new Exception("Incomplete tangent data: "+mesh.name);
                foreach(var tangent in mesh.tangents) if(float.IsNaN(tangent.x)||float.IsNaN(tangent.y)||float.IsNaN(tangent.z)) throw new Exception("Invalid tangent: "+mesh.name);
            }
            if(materials.Count!=6 || meshes.Count==0) throw new Exception("Unexpected audited asset counts");
            Debug.Log("SKY_STONE_DETAIL_REOPEN_VERIFIED meshes="+meshes.Count+" materials="+materials.Count);
        }
    }
}
