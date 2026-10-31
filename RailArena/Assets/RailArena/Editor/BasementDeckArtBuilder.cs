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
    public static class BasementDeckArtBuilder
    {
        [Serializable] public sealed class Geometry { public Vector3[] verticesUnityMetres; public int[] triangles; }
        [Serializable] public sealed class DeckSource
        {
            public string name,geometrySha256;
            public float radiusMetres,thicknessMetres;
            public Geometry collision;
            public string[] instances;
        }
        [Serializable] sealed class AuditRecord
        {
            public string scene=BasementRouteBuilder.ScenePath;
            public string axes="Blender metres (x,y,z) -> Unity (x,z,-y); pivot at the original deck top center";
            public string scope="Exact source collision mesh scaled into metres. Geometry excludes translation. Original scene and collider assets are read without saving.";
            public DeckSource[] profiles;
        }
        public static void Prepare()
        {
            const string folder="Assets/RailArena/Art/ArtPass/Environment/BasementDecks/";
            var source=JsonUtility.FromJson<AuditRecord>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/deck-source.json"));
            foreach(string file in new[]{"Paving-Painted.png","Paving-Normal.png","Paving-Cavity.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+file);
                importer.textureType=file.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=file.Contains("Painted");importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            }
            var profile=Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            string[] names={"DeckIvory","DeckStone","DeckTeal","DeckBronze","DeckShade"};
            Color[] colors={Color.white,new(.86f,.88f,.84f),new(.065f,.255f,.24f),new(.58f,.37f,.16f),new(.22f,.30f,.28f)};
            var materials=new Material[names.Length];var lit=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                for(int index=0;index<names.Length;index++)
                {
                    string path=folder+names[index]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null){material=new Material(profile.SurfaceShader);AssetDatabase.CreateAsset(material,path);}
                    lit.SetFloat("_Metallic",index==3?.5f:0);profile.Configure(material,lit);
                    material.SetColor("_BaseColor",colors[index].linear);material.SetFloat("_Metallic",index==3?.5f:0);material.SetFloat("_Smoothness",index==3?.3f:.12f);
                    if(index<2)
                    {
                        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Paving-Painted.png"));
                        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Paving-Normal.png"));material.SetFloat("_BumpScale",.5f);material.EnableKeyword("_NORMALMAP");
                        material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Paving-Cavity.png"));material.SetFloat("_OcclusionStrength",.25f);
                    }
                    material.enableInstancing=true;EditorUtility.SetDirty(material);materials[index]=material;
                }
                foreach(var deck in source.profiles)
                {
                    var root=new GameObject(deck.name+" / Authored stone deck");var lods=new LOD[3];
                    try
                    {
                        int previousTriangles=int.MaxValue;
                        for(int level=0;level<3;level++)
                        {
                            string path=folder+deck.name+"-Deck-LOD"+level+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                            importer.importAnimation=false;importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
                            var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform,false);instance.name="LOD"+level;
                            var renderers=instance.GetComponentsInChildren<MeshRenderer>();if(renderers.Length!=1)throw new InvalidOperationException("Deck LOD must contain one joined visual mesh");
                            var renderer=renderers[0];renderer.sharedMaterials=renderer.sharedMaterials.Select(material=>materials[Array.IndexOf(names,material.name)]).ToArray();
                            var expected=new Bounds(Vector3.down*deck.thicknessMetres*.5f,new Vector3(deck.radiusMetres*2,deck.thicknessMetres,deck.radiusMetres*2));
                            if((renderer.bounds.center-expected.center).sqrMagnitude>.00001f||(renderer.bounds.size-expected.size).sqrMagnitude>.00001f)
                                throw new InvalidOperationException("Deck export metre scale/top pivot mismatch: "+renderer.bounds);
                            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;int triangles=mesh.triangles.Length/3;
                            if(triangles>=previousTriangles)throw new InvalidOperationException("Each deck LOD must reduce triangles");previousTriangles=triangles;
                            lods[level]=new LOD(new[]{.18f,.06f,.001f}[level],new Renderer[]{renderer});
                        }
                        if(root.GetComponentsInChildren<Collider>().Length!=0)throw new InvalidOperationException("Deck presentation must not add colliders");
                        var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Art/"+deck.name+"StoneDeck.prefab");
                    }
                    finally{Object.DestroyImmediate(root);}
                }
            }
            finally{Object.DestroyImmediate(lit);}
            AssetDatabase.SaveAssets();Debug.Log("BASEMENT_DECK_ART_READY two measured profiles / three LODs / five cel materials / no colliders");
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            var routes=Object.FindObjectsByType<BasementRoute>(FindObjectsSortMode.None).OrderBy(route=>route.name).ToArray();
            if(routes.Length!=4)throw new InvalidOperationException("Exactly four basement routes are required");
            var profiles=new System.Collections.Generic.List<DeckSource>();
            foreach(bool landing in new[]{false,true})
            {
                var decks=routes.Select(route=>landing?route.Landing:route.Staging).ToArray();
                var source=Measure(decks[0],landing?"Basement":"Staging");
                if(decks.Any(deck=>Measure(deck,source.name).geometrySha256!=source.geometrySha256))
                    throw new InvalidOperationException("All route decks must share the same metre-scale collision geometry");
                source.instances=decks.Select(deck=>deck.parent.name+"/"+deck.name).ToArray();profiles.Add(source);
            }
            string folder="../ArtSource/Original/ArtPass-v1/Environment/BasementDecks";Directory.CreateDirectory(folder);
            File.WriteAllText(folder+"/deck-source.json",JsonUtility.ToJson(new AuditRecord{profiles=profiles.ToArray()},true));
            Debug.Log("BASEMENT_DECK_SOURCE_AUDITED two profiles / eight exact collision meshes; scene unchanged");
        }
        public static DeckSource Measure(Transform deck,string name,bool requireSharedVisualMesh=true)
        {
            var collider=deck.GetComponent<MeshCollider>();var filter=deck.GetComponent<MeshFilter>();
            if(collider==null||filter==null||collider.sharedMesh==null||(requireSharedVisualMesh&&collider.sharedMesh!=filter.sharedMesh)||Quaternion.Angle(deck.rotation,Quaternion.identity)>.001f)
                throw new InvalidOperationException("Deck source must retain its original upright collision mesh");
            var mesh=collider.sharedMesh;var size=Vector3.Scale(mesh.bounds.size,deck.lossyScale);
            var top=new Vector3(mesh.bounds.center.x,mesh.bounds.max.y,mesh.bounds.center.z);
            var geometry=new Geometry{verticesUnityMetres=mesh.vertices.Select(vertex=>Vector3.Scale(vertex-top,deck.lossyScale)).ToArray(),triangles=mesh.triangles};
            string digest;
            using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(geometry)))).Replace("-","").ToLowerInvariant();
            return new DeckSource{name=name,radiusMetres=size.x*.5f,thicknessMetres=size.y,collision=geometry,geometrySha256=digest};
        }
    }
}
