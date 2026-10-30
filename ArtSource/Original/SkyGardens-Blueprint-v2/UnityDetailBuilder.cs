using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Blueprints
{
    /// <summary>Exact construction import only. Does not replace gameplay scenes or build settings.</summary>
    public static class UnityDetailBuilder
    {
        [Serializable] private class Manifest { public Part[] objects; }
        [Serializable] private class Part { public string id,group; public bool collision; public float[] center,vertices; public int[] triangles; }
        public static void Build()
        {
            string[] args=Environment.GetCommandLineArgs(); int option=Array.IndexOf(args,"--sky-blueprint-root");
            if(option<0) throw new ArgumentException("Supply --sky-blueprint-root with the measured package directory");
            string source=Path.GetFullPath(args[option+1]); var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source,"MeshData.json")));
            const string folder="Assets/Validation/SkyGardensDetailV2Final";
            if(AssetDatabase.IsValidFolder(folder)) throw new IOException("Construction output already exists; use a fresh isolated project copy");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Sky Gardens / measured construction"); int triangles=0;
            var colors = new System.Collections.Generic.Dictionary<string,Color> {
                {"structure",new Color(.65f,.61f,.47f)}, {"cover",new Color(.5f,.49f,.39f)}, {"ivory_trim",new Color(.83f,.78f,.62f)},
                {"bronze",new Color(.32f,.19f,.065f)}, {"teal",new Color(.04f,.55f,.5f)}, {"bark",new Color(.18f,.105f,.045f)},
                {"foliage",new Color(.12f,.3f,.055f)}, {"grass",new Color(.25f,.4f,.075f)}, {"water",new Color(.08f,.52f,.65f,.7f)}, {"foam",new Color(.74f,.92f,.94f)},
                {"summit",new Color(.65f,.61f,.47f)}, {"ramps",new Color(.67f,.63f,.5f)} };
            var materials = new System.Collections.Generic.Dictionary<string,Material>();
            foreach(var part in manifest.objects)
            {
                var obj=new GameObject(part.id); obj.transform.SetParent(root.transform,false);
                obj.transform.position=Convert(part.center[0],part.center[1],part.center[2]);
                var vertices=new Vector3[part.vertices.Length/3];
                for(int i=0;i<vertices.Length;i++) vertices[i]=Convert(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]);
                int[] indices=(int[])part.triangles.Clone();
                for(int i=0;i<indices.Length;i+=3) (indices[i+1],indices[i+2])=(indices[i+2],indices[i+1]);
                var mesh=new Mesh{name=part.id}; mesh.vertices=vertices; mesh.triangles=indices; mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,folder+"/"+part.id+".asset");obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                if(!materials.TryGetValue(part.group,out var material))
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name=part.group;
                    material.color=colors.TryGetValue(part.group,out var color)?color:colors["structure"];
                    material.SetFloat("_Smoothness",part.group=="water"||part.group=="bronze"?.55f:.2f);
                    material.SetFloat("_Metallic",part.group=="bronze"?.65f:0);
                    if(part.group=="water") { material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000; }
                    if(part.group=="water"||part.group=="foam") { material.SetFloat("_Cull",0);material.SetShaderPassEnabled("ShadowCaster",false); }
                    AssetDatabase.CreateAsset(material,folder+"/Material-"+part.group+".mat");materials[part.group]=material;
                }
                obj.AddComponent<MeshRenderer>().sharedMaterial=material;
                if(part.group.EndsWith("envelopes")||part.group.EndsWith("reservations"))obj.GetComponent<MeshRenderer>().enabled=false;
                if(part.collision)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
                Bounds expected=new Bounds(vertices[0]+obj.transform.position,Vector3.zero);
                foreach(var vertex in vertices)expected.Encapsulate(vertex+obj.transform.position);
                var actual=obj.GetComponent<Renderer>().bounds;
                if((actual.min-expected.min).magnitude>.001f || (actual.max-expected.max).magnitude>.001f)throw new Exception("Bounds mismatch "+part.id);
                triangles+=indices.Length/3;
            }
            EditorSceneManager.SaveScene(scene,folder+"/SkyGardensConstruction.unity");AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(source,"Unity-Validation.json"),"{\"status\":\"PASS: direct construction mesh import and bounds, not gameplay validation\",\"objects\":"+manifest.objects.Length+",\"triangles\":"+triangles+",\"toleranceMetres\":0.001}");
            Debug.Log("SKY_UNITY_CONSTRUCTION_VERIFIED objects="+manifest.objects.Length);
        }
        private static Vector3 Convert(float x,float north,float height)=>new Vector3(x,height,-north);
    }
}
