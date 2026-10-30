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
    /// <summary>Expands the existing editable arena while preserving traversal objects and pickup state.</summary>
    public static class ExpandedPlatformsBuilder
    {
        private const string Folder="Assets/RailArena/Generated/ExpandedPlatformArt";
        private static Material deck, armor, ceramic, lane, blue, red, warm;
        public static void Apply()
        {
            EditorSceneManager.OpenScene(TwinPlatformsBuilder.ScenePath);
            var world=Object.FindAnyObjectByType<ArenaSession>().World;
            if(world.transform.Find("Expanded platform detailing")!=null) throw new System.InvalidOperationException("Expansion already applied; edit the saved scene directly.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); CreateTextures();
            deck=Mat("Panel deck",new Color(.65f,.73f,.78f),.55f,.35f,true);
            armor=Mat("Armor panels",new Color(.26f,.34f,.4f),.65f,.4f,true);
            ceramic=Mat("Ceramic trim",new Color(.7f,.76f,.74f),.15f,.4f,false);
            lane=Mat("Walkway",new Color(.26f,.33f,.36f),.3f,.22f,true);
            blue=Mat("Blue guidance",new Color(.03f,.45f,1),.2f,.3f,false,2);
            red=Mat("Red guidance",new Color(1,.1f,.045f),.2f,.3f,false,2);
            warm=Mat("Warm luminaires",new Color(1,.72f,.34f),.2f,.3f,false,2.5f);
            var detail=new GameObject("Expanded platform detailing").transform; detail.SetParent(world.transform,false);
            var nodes=world.Nodes.ToList(); var links=world.Links.ToList();
            foreach(int side in new[]{-1,1})
            {
                var root=world.transform.Find(side<0?"BLUE PLATFORM":"RED PLATFORM");
                var floor=root.Find("Floating deck"); floor.position=new Vector3(side*40,-.7f,0); floor.localScale=new Vector3(44,1.4f,56);
                var floorMat=new Material(deck); floorMat.name=side<0?"Blue deck":"Red deck";
                floorMat.mainTextureScale=new Vector2(11,14); AssetDatabase.CreateAsset(floorMat,Folder+"/"+floorMat.name+".mat");
                floor.GetComponent<Renderer>().sharedMaterial=floorMat;
                var under=root.Find("Floating understructure"); under.position=new Vector3(side*40,-2.2f,0); under.localScale=new Vector3(41,2.1f,53); under.GetComponent<Renderer>().sharedMaterial=armor;
                foreach(Transform child in root.Cast<Transform>().ToArray())
                    if(child.name=="Outer safety rail"||child.name=="Perimeter light"||child.name=="Deck panel joint"||child.name=="Inner void warning") Object.DestroyImmediate(child.gameObject);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                    if(renderer.name=="Cover") renderer.sharedMaterial=armor;
                    else if(renderer.name=="Cover cap") renderer.sharedMaterial=ceramic;
                Material accent=side<0?blue:red;
                for(int edge=-1;edge<=1;edge+=2)
                {
                    Box(detail,"Perimeter rail",new Vector3(side*40,.65f,edge*28),new Vector3(44,.15f,.18f),ceramic);
                    Box(detail,"Perimeter guidance",new Vector3(side*40,.04f,edge*27.7f),new Vector3(44,.04f,.12f),accent,false);
                    Box(detail,"Outer flanking lane",new Vector3(side*40,.012f,edge*22),new Vector3(39,.02f,3.2f),lane,false);
                    for(int i=0;i<5;i++) Box(detail,"Lane dash",new Vector3(side*(24+i*8),.03f,edge*20.3f),new Vector3(2,.025f,.09f),ceramic,false);
                    Cover(detail,new Vector3(side*34,0,edge*18),new Vector3(5,2.5f,3),accent);
                    Cover(detail,new Vector3(side*30,0,edge*24),new Vector3(4,1.1f,2),accent);
                    Pole(detail,new Vector3(side*59,0,edge*24),accent);
                }
                Box(detail,"Outer rail",new Vector3(side*62,.65f,0),new Vector3(.18f,.15f,56),ceramic);
                Box(detail,"Outer lane",new Vector3(side*56,.012f,0),new Vector3(3.2f,.02f,49),lane,false);
                Box(detail,"Inner edge warning",new Vector3(side*18.2f,.025f,0),new Vector3(.17f,.04f,55),warm,false);
                foreach(int z in new[]{-6,6}) Cover(detail,new Vector3(side*51,0,z),new Vector3(4,2.2f,4),accent);
                Cover(detail,new Vector3(side*59,0,0),new Vector3(2,1.1f,6),accent);
                var outer=new List<int>();
                foreach(int z in new[]{-22,-12,0,12,22}) outer.Add(Node(detail,nodes,new Vector3(side*56,.08f,z)));
                for(int i=0;i<4;i++) Both(links,outer[i],outer[i+1]);
                for(int i=1;i<=3;i++) Both(links,outer[i],world.NearestNode(new Vector3(side*44,.08f,(i-2)*12)));
                foreach(int edge in new[]{-1,1})
                {
                    int middle=Node(detail,nodes,new Vector3(side*44,.08f,edge*22));
                    int inner=Node(detail,nodes,new Vector3(side*24,.08f,edge*22));
                    Both(links,outer[edge<0?0:4],middle); Both(links,middle,inner);
                    Both(links,middle,world.NearestNode(new Vector3(side*44,.08f,edge*12)));
                    Both(links,inner,world.NearestNode(new Vector3(side*34,.08f,edge*12)));
                }
            }
            world.Nodes=nodes.ToArray(); world.Links=links.ToArray();
            world.MenuEye=new Vector3(78,52,-80); world.MenuLook=Quaternion.LookRotation(new Vector3(0,0,0)-world.MenuEye).eulerAngles;
            Camera.main.transform.SetPositionAndRotation(world.MenuEye,Quaternion.Euler(world.MenuLook));
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Single(light=>light.name=="Orbital sunlight");
            sun.transform.rotation=Quaternion.LookRotation(new Vector3(.45f,-.85f,-.3f)); sun.color=new Color(1,.91f,.78f); sun.intensity=1.6f; sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.6f,.73f,.84f);
            RenderSettings.ambientEquatorColor=new Color(.45f,.55f,.64f); RenderSettings.ambientGroundColor=new Color(.23f,.29f,.34f);
            var sky=new Material(RenderSettings.skybox); sky.name="Expanded platform sky";
            if(sky.HasProperty("_SunDirection")) sky.SetVector("_SunDirection",-sun.transform.forward);
            AssetDatabase.CreateAsset(sky,Folder+"/Expanded platform sky.mat"); RenderSettings.skybox=sky;
            Physics.SyncTransforms(); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("EXPANDED_PLATFORMS_BUILT decks=44x56 gap=36 nodes="+world.Nodes.Length);
        }
        private static void Cover(Transform root,Vector3 floor,Vector3 size,Material accent)
        {
            Box(root,"Added tactical cover",floor+Vector3.up*(size.y*.5f),size,armor);
            Box(root,"Cover ceramic cap",floor+Vector3.up*(size.y+.04f),new Vector3(size.x+.12f,.08f,size.z+.12f),ceramic,false);
            foreach(int side in new[]{-1,1})
                Box(root,"Cover edge inset",floor+new Vector3(side*(size.x*.5f+.01f),size.y*.6f,0),new Vector3(.025f,.08f,size.z*.7f),accent,false);
        }
        private static void Pole(Transform root,Vector3 position,Material accent)
        {
            Box(root,"Light mast",position+Vector3.up*2.7f,new Vector3(.16f,5.4f,.16f),armor);
            Box(root,"Light fixture",position+Vector3.up*5.4f,new Vector3(1,.18f,1),warm,false);
            Box(root,"Mast team strip",position+Vector3.up*2,new Vector3(.18f,2,.18f),accent,false);
            var obj=new GameObject("Warm deck light"); obj.transform.SetParent(root,false); obj.transform.position=position+Vector3.up*4.8f;
            var light=obj.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.78f,.52f); light.intensity=4; light.range=16; light.shadows=LightShadows.None;
        }
        private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name; obj.transform.SetParent(parent,false); obj.transform.position=position; obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material; if(!collision) Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }
        private static int Node(Transform parent,List<Transform> nodes,Vector3 position)
        { var obj=new GameObject("Expanded route "+nodes.Count); obj.transform.SetParent(parent,false); obj.transform.position=position; nodes.Add(obj.transform); return nodes.Count-1; }
        private static void Both(List<Vector2Int> links,int a,int b) { links.Add(new Vector2Int(a,b)); links.Add(new Vector2Int(b,a)); }
        private static Material Mat(string name,Color color,float metal,float smooth,bool textured,float emission=0)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name}; material.color=color;
            material.SetFloat("_Metallic",metal); material.SetFloat("_Smoothness",smooth);
            if(textured) { material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Panel albedo.png"); material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Panel normal.png")); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale",.65f); }
            if(emission>0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*emission); }
            AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat"); return material;
        }
        private static void CreateTextures()
        {
            const int size=512; var albedo=new Texture2D(size,size,TextureFormat.RGB24,false); var normal=new Texture2D(size,size,TextureFormat.RGB24,false);
            var rng=new System.Random(4096);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                int px=x%128,py=y%128; bool seam=px<3||py<3;
                float noise=(float)rng.NextDouble()*.045f; float value=seam?.13f:.55f+noise;
                if(py%31==0 && px>12 && px<114) value-=.035f;
                bool bolt=(px-9)*(px-9)+(py-9)*(py-9)<7; if(bolt)value=.24f;
                albedo.SetPixel(x,y,new Color(value*.94f,value*.98f,value));
                float nx=px==3?-.3f:px==127?.3f:0,ny=py==3?-.3f:py==127?.3f:0;
                Vector3 n=new Vector3(nx,ny,1).normalized; normal.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
            }
            File.WriteAllBytes(Folder+"/Panel albedo.png",albedo.EncodeToPNG()); File.WriteAllBytes(Folder+"/Panel normal.png",normal.EncodeToPNG()); Object.DestroyImmediate(albedo); Object.DestroyImmediate(normal); AssetDatabase.Refresh();
            foreach(string name in new[]{"Panel albedo.png","Panel normal.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+name); importer.wrapMode=TextureWrapMode.Repeat; importer.anisoLevel=4;
                if(name.Contains("normal")) importer.textureType=TextureImporterType.NormalMap; importer.SaveAndReimport();
            }
        }
    }
}
