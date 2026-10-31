using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RailArena.Editor
{
    public static class GardenNatureArtBuilder
    {
        public const string Folder="../ArtSource/Original/ArtPass-v1/Nature/GardenNature";
        [Serializable] public sealed class Part
        {
            public string sourceName,family,meshAsset,meshGuid,vertexSha256,indexSha256;
            public Vector3 center,size,frameCenter,frameSize;
            public int vertices,triangles,colliders;
            public bool wind;
        }
        [Serializable] public sealed class Source
        {
            public string scene="Assets/RailArena/Generated/SkyGardensBasement.unity",sceneSha256;
            public string scope="Read-only installed planting authority. Bounds are world-space metres. Canonical unit families map into these original visual envelopes; existing collision/navigation/traversal remain authoritative.";
            public string axes="Blender (x,y,z) -> Unity (x,z,-y). Unit visual family pivots use bounds centre.";
            public Part[] parts;
        }
        static string Digest(byte[] data)
        {
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(data)).Replace("-","").ToLowerInvariant();
        }
        static string Family(string name)
        {
            if(name.StartsWith("TREE-",StringComparison.Ordinal))return name.EndsWith("-BARK",StringComparison.Ordinal)?"Trunk":"Crown";
            if(name.StartsWith("SHRUB-",StringComparison.Ordinal))return "Shrub";
            if(name.StartsWith("GRASS-",StringComparison.Ordinal))return "Grass";
            if(name.StartsWith("IVY-",StringComparison.Ordinal))return name.EndsWith("-STEMS",StringComparison.Ordinal)?"VineStem":"VineLeaf";
            return null;
        }
        public static void Audit()
        {
            const string scene="Assets/RailArena/Generated/SkyGardensBasement.unity";byte[] before=File.ReadAllBytes(scene);
            EditorSceneManager.OpenScene(scene);var world=Object.FindAnyObjectByType<ArenaSession>().World;
            var parts=world.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>Family(renderer.name)!=null).OrderBy(renderer=>renderer.name,StringComparer.Ordinal).Select(renderer=>
            {
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);
                var vertices=mesh.vertices;var indices=mesh.triangles;
                byte[] vertexBytes=new byte[vertices.Length*12];for(int i=0;i<vertices.Length;i++){Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].x),0,vertexBytes,i*12,4);Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].y),0,vertexBytes,i*12+4,4);Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].z),0,vertexBytes,i*12+8,4);}
                byte[] indexBytes=new byte[indices.Length*4];Buffer.BlockCopy(indices,0,indexBytes,0,indexBytes.Length);
                return new Part{sourceName=renderer.name,family=Family(renderer.name),center=renderer.bounds.center,size=renderer.bounds.size,frameCenter=renderer.bounds.center,frameSize=renderer.bounds.size,meshAsset=path,meshGuid=AssetDatabase.AssetPathToGUID(path),vertexSha256=Digest(vertexBytes),indexSha256=Digest(indexBytes),vertices=vertices.Length,triangles=indices.Length/3,colliders=renderer.GetComponents<Collider>().Length,wind=renderer.GetComponent<SkyGardenMotion>()!=null};
            }).ToArray();
            foreach(var pair in parts.Where(part=>part.sourceName.StartsWith("IVY-",StringComparison.Ordinal)).GroupBy(part=>part.sourceName.Split('-')[1]))
            {
                var bounds=new Bounds(pair.First().center,pair.First().size);
                foreach(var part in pair)bounds.Encapsulate(new Bounds(part.center,part.size));
                foreach(var part in pair){part.frameCenter=bounds.center;part.frameSize=bounds.size;}
            }
            if(parts.Length!=24||parts.GroupBy(part=>part.family).Any(group=>group.Count()!=4))throw new InvalidOperationException("Expected four installed plantings with six visual families");
            if(!before.SequenceEqual(File.ReadAllBytes(scene)))throw new InvalidOperationException("Read-only nature audit changed saved scene bytes");
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/nature-source.json",JsonUtility.ToJson(new Source{sceneSha256=Digest(before),parts=parts},true));
            Debug.Log("GARDEN_NATURE_SOURCE_AUDITED 24 renderings / six families / four installed sites; original scene unchanged");
        }
    }
}
