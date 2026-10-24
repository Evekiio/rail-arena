using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Static distant silhouettes. No gameplay collision, routes, lights or moving parts.</summary>
    public static class FoundryBackdropBuilder
    {
        public const string RootName = "Foundry distant islands";
        private const string Folder = "Assets/RailArena/Art/FoundryBackdrop";
        private const string ScenePath = "Assets/RailArena/Generated/Foundry.unity";
        private const string Output = "../Validation/FoundryBackdrop";

        [MenuItem("Rail Arena/Apply Foundry distant islands")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Backdrop authoring requires edit mode.");
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var world = Object.FindFirstObjectByType<ArenaWorld>();
            if (world == null) throw new InvalidOperationException("Foundry world is required.");
            string before = FoundryArtBuilder.GameplaySignature(world);
            foreach(var previous in Resources.FindObjectsOfTypeAll<GameObject>().Where(obj=>obj.scene==world.gameObject.scene && obj.name==RootName).ToArray())
                Object.DestroyImmediate(previous);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var materials = new[]
            {
                Material("Faceted island", new Color(.48f,.57f,.60f)),
                Material("Distant ceramic", new Color(.71f,.75f,.73f)),
                Material("Distant teal", new Color(.43f,.54f,.57f))
            };
            var groups = materials.Select(_ => new List<CombineInstance>()).ToArray();
            var sourceCube = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RailArena/Art/Foundry/Beveled module.asset");
            if(sourceCube==null)throw new InvalidOperationException("Apply Foundry surface art before backdrop generation.");
            var cube = Object.Instantiate(sourceCube);
            var rock = Rock();
            void Add(int group, Mesh mesh, Vector3 position, Vector3 scale, float angle = 0) =>
                groups[group].Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position,Quaternion.Euler(0,angle,0),scale) });
            for (int i=0;i<6;i++)
            {
                float angle = (i*60+20)*Mathf.Deg2Rad;
                float distance = i%2==0 ? 180 : 225;
                var center = new Vector3(Mathf.Cos(angle)*distance,-16-i%3*7,Mathf.Sin(angle)*distance);
                float radius = i%2==0 ? 25 : 18;
                Add(0,rock,center,new Vector3(radius,32+i%3*5,radius*.82f),i*37);
                Add(1,cube,center+Vector3.up*.6f,new Vector3(radius*1.25f,1.2f,radius*.9f),i*20);
                float height = i%2==0 ? 32 : 23;
                Add(2,cube,center+Vector3.up*(height/2+1),new Vector3(6,height,7));
                Add(1,cube,center+new Vector3(-2.5f,height/2+1,-3.7f),new Vector3(1,height-2,.6f));
                Add(1,cube,center+new Vector3(2.5f,height/2+1,-3.7f),new Vector3(1,height-2,.6f));
                Add(1,cube,center+new Vector3(0,height+1,0),new Vector3(7,1.4f,8));
                Add(2,cube,center+new Vector3(-10,6,2),new Vector3(5,10,6),25);
                Add(1,cube,center+new Vector3(-10,11.5f,2),new Vector3(6,1,7),25);
                Add(2,cube,center+new Vector3(9,4,-2),new Vector3(4,6,5),-15);
            }
            var root = new GameObject(RootName) { layer=2 };
            int triangles=0;
            for (int i=0;i<materials.Length;i++)
            {
                var mesh = new Mesh { name=materials[i].name };
                mesh.CombineMeshes(groups[i].ToArray(),true,true);
                string path=Folder+"/"+materials[i].name+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved==null) { AssetDatabase.CreateAsset(mesh,path); saved=mesh; }
                else
                {
                    saved.Clear(); saved.vertices=mesh.vertices; saved.normals=mesh.normals;
                    saved.uv=mesh.uv; saved.triangles=mesh.triangles; saved.RecalculateBounds();
                    Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved);
                }
                triangles+=saved.triangles.Length/3;
                var obj=new GameObject(materials[i].name) { layer=2 };
                obj.transform.SetParent(root.transform,false);
                obj.AddComponent<MeshFilter>().sharedMesh=saved;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[i];
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            }
            Object.DestroyImmediate(cube); Object.DestroyImmediate(rock);
            string after=FoundryArtBuilder.GameplaySignature(world);
            if(before!=after)throw new InvalidOperationException("Backdrop changed gameplay geometry.");
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"/geometry.json",JsonUtility.ToJson(new Review
            { unity=Application.unityVersion,before=before,after=after,unchanged=true,triangles=triangles },true));
        }

        [Serializable] private sealed class Review
        {
            public string unity,before,after;
            public bool unchanged;
            public int islands=6,renderers=3,triangles;
            public string scope="Static distant world art on current Foundry. No new playable geometry, collision, lighting or Foundry v2 migration. Not a performance qualification.";
        }
        private static Material Material(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.name=name;material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.12f);
            EditorUtility.SetDirty(material);return material;
        }
        private static Mesh Rock()
        {
            const int sides=12;
            var vertices=new List<Vector3>();var indices=new List<int>();
            Vector3 Point(int index,float radius,float y)
            {
                float angle=index*2*Mathf.PI/sides;
                float irregular=1+.07f*Mathf.Sin(index*2.3f);
                return new Vector3(Mathf.Cos(angle)*radius*irregular,y,Mathf.Sin(angle)*radius*irregular);
            }
            void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(start);indices.Add(start+1);indices.Add(start+2);}
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;
                Triangle(Vector3.zero,Point(next,1,0),Point(i,1,0));
                foreach(var band in new[]{new Vector4(1,0,.88f,-.18f),new Vector4(.88f,-.18f,.43f,-.72f)})
                {
                    var a=Point(i,band.x,band.y);var b=Point(next,band.x,band.y);var c=Point(next,band.z,band.w);var d=Point(i,band.z,band.w);
                    Triangle(a,b,c);Triangle(a,c,d);
                }
                Triangle(Point(i,.43f,-.72f),Point(next,.43f,-.72f),new Vector3(.06f,-1,0));
            }
            var mesh=new Mesh {name="Faceted island source"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
            mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
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
            Capture(prefix+"-LowerRoute",new Vector3(-18,1.58f,-18),new Vector3(12,3,4),90);
            Capture(prefix+"-Edge",new Vector3(-24,1.58f,-18),new Vector3(-180,-2,-100),75);
            Capture(prefix+"-Horizon",new Vector3(0,8,0),new Vector3(160,4,80),75);
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
