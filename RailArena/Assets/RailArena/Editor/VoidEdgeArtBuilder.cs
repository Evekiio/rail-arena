using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Surface paint at verified open drops. Never creates gameplay collision.</summary>
    public static class VoidEdgeArtBuilder
    {
        public const string RootName="Foundry void-edge markings";
        private const string Folder="Assets/RailArena/Art/VoidEdges";
        private const string ScenePath="Assets/RailArena/Generated/Foundry.unity";
        private const string Output="../Validation/VoidEdges";
        [Serializable] private sealed class Report
        {
            public string unity,before,after;
            public bool unchanged;
            public int edgeGroups,hatches,triangles;
            public string scope="Cosmetic floor paint on verified open deck drops. Existing ramps, collision, routes and safe lower landings unchanged.";
        }
        public static bool HasLanding(Vector3 edgeProbe)
        {
            return Physics.RaycastAll(edgeProbe+Vector3.up*.2f,Vector3.down,32,1<<0,QueryTriggerInteraction.Ignore)
                .Any(hit=>hit.collider.name=="Load bearing deck" || hit.collider.name=="Access ramp");
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edge authoring requires edit mode.");
            if(EditorSceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
            var world=Object.FindAnyObjectByType<ArenaWorld>();
            if(world==null)throw new InvalidOperationException("Foundry world is required.");
            string before=FoundryArtBuilder.GameplaySignature(world);
            foreach(var previous in Resources.FindObjectsOfTypeAll<GameObject>().Where(obj=>obj.scene==world.gameObject.scene && obj.name==RootName).ToArray())
                Object.DestroyImmediate(previous);
            Physics.SyncTransforms();
            var vertices=new List<Vector3>();var triangles=new List<int>();var probes=new List<Vector3>();
            foreach(var deck in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name=="Load bearing deck").OrderBy(c=>c.transform.position.x).ThenBy(c=>c.transform.position.z).ThenBy(c=>c.transform.position.y))
            {
                var t=deck.transform;
                foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    var outward=t.TransformDirection(normal).normalized;
                    var tangent=Vector3.Cross(Vector3.up,outward).normalized;
                    var middle=t.TransformPoint(normal*.5f+Vector3.up*.5f);
                    float length=Mathf.Abs(normal.x)>.5f?t.lossyScale.z:t.lossyScale.x;
                    int groups=Mathf.FloorToInt(length/1.5f);
                    for(int i=0;i<groups;i++)
                    {
                        var edge=middle+tangent*(length*((i+.5f)/groups-.5f));
                        if(new[]{-.35f,0,.35f}.Any(offset=>HasLanding(edge+tangent*offset+outward*.45f)))continue;
                        probes.Add(edge+outward*.45f);
                        foreach(float offset in new[]{-.25f,0,.25f})
                        {
                            var center=edge-outward*.32f+tangent*offset+Vector3.up*.018f;
                            var along=(tangent+outward*.7f).normalized*.23f;
                            var across=Vector3.Cross(Vector3.up,along).normalized*.055f;
                            int start=vertices.Count;
                            vertices.Add(center-along-across);vertices.Add(center+along-across);vertices.Add(center+along+across);vertices.Add(center-along+across);
                            triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                        }
                    }
                }
            }
            if(probes.Count==0)throw new InvalidOperationException("No void edges were found.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            string meshPath=Folder+"/Void hatch mesh.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null) {mesh=new Mesh {name="Void hatch mesh"};AssetDatabase.CreateAsset(mesh,meshPath);}
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            string materialPath=Folder+"/Safety paint.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.name="Safety paint";material.SetColor("_BaseColor",new Color(.72f,.54f,.24f));EditorUtility.SetDirty(material);
            var root=new GameObject(RootName) {layer=2};root.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            string after=FoundryArtBuilder.GameplaySignature(world);
            if(before!=after)throw new InvalidOperationException("Edge paint altered gameplay geometry.");
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"/geometry.json",JsonUtility.ToJson(new Report {unity=Application.unityVersion,before=before,after=after,unchanged=true,edgeGroups=probes.Count,hatches=vertices.Count/4,triangles=triangles.Count/3},true));
        }
        public static void ApplyAndReview()
        {
            EditorSceneManager.OpenScene(ScenePath);Directory.CreateDirectory(Output);
            var previous=GameObject.Find(RootName);if(previous!=null)previous.SetActive(false);
            Views("Before");Apply();Views("After");
        }
        private static void Views(string prefix)
        {
            Capture(prefix+"-Overview",new Vector3(46,35,-52),new Vector3(0,2,0),65);
            Capture(prefix+"-OuterEdge",new Vector3(-20,1.58f,-20),new Vector3(-23,-.1f,-24),85);
            Capture(prefix+"-InnerGap",new Vector3(0,1.58f,-15),new Vector3(0,-.1f,-10),85);
        }
        private static void Capture(string name, Vector3 position, Vector3 target, float fov)
        {
            var camera = Camera.main;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            float oldFov = camera.fieldOfView;
            var texture = new RenderTexture(1920,1080,24);
            var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var oldActive = RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
                camera.fieldOfView = fov;
                texture.Create();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation); camera.fieldOfView = oldFov;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(pixels); texture.Release(); Object.DestroyImmediate(texture);
            }
        }
    }
}
