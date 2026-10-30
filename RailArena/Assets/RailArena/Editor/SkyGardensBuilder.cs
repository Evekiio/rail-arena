using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace RailArena.Editor
{
    public static class SkyGardensBuilder
    {
        public const string ScenePath="Assets/RailArena/Generated/SkyGardens.unity";
        private const string Folder="Assets/RailArena/Generated/SkyGardensArt";
        [Serializable] private class MeshManifest{public Part[] objects;}
        [Serializable] private class Part{public string id,group;public bool collision;public float[] center,vertices;public int[] triangles;}
        [Serializable] private class Layout{public Spawn[] spawns;public Pickup[] health;public Pad[] jumpPads;public Gate[] portals;}
        [Serializable] private class Spawn{public string id;public float[] position;}
        [Serializable] private class Pickup{public string id;public float[] position;}
        [Serializable] private class Pad{public string id;public float[] start,destination;public float flightSeconds;}
        [Serializable] private class Gate{public string id;public float[] center,frontNormal;}
        private static readonly Dictionary<string,Material> materials=new();
        private static readonly HashSet<Collider> walkable=new();
        private static Vector3 Convert(float[] p)=>new Vector3(p[0],p[2],-p[1]);
        public static void Build()
        {
            if(AssetDatabase.IsValidFolder(Folder))throw new IOException("Sky Gardens art already exists; preserve and edit the saved scene.");
            string[] args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--sky-blueprint-root");
            if(index<0)throw new ArgumentException("Supply --sky-blueprint-root");string source=args[index+1];
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Path.Combine(source,"Scene.json")));
            var data=JsonUtility.FromJson<MeshManifest>(File.ReadAllText(Path.Combine(source,"MeshData.json")));
            EditorSceneManager.OpenScene(TwinPlatformsBuilder.ScenePath);
            var session=Object.FindAnyObjectByType<ArenaSession>();var previous=session.World;
            var root=new GameObject("SKY GARDENS // Foundry v2");var world=root.AddComponent<ArenaWorld>();world.LayoutName="SKY GARDENS // FOUNDRY V2";world.CarefulNavigation=true;world.RouteArrivalRadius=.45f;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();CreateTextures();materials.Clear();walkable.Clear();
            foreach(var part in data.objects)
            {
                if(part.group.EndsWith("reservations")||part.group.EndsWith("envelopes")||part.group=="portal_fields"||part.group=="devices")continue;
                var obj=new GameObject(part.id);obj.transform.SetParent(root.transform,false);obj.transform.position=Convert(part.center);
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int t=0;t<part.triangles.Length;t+=3)
                {
                    var points=new Vector3[3];int[] order={part.triangles[t],part.triangles[t+1],part.triangles[t+2]};
                    for(int j=0;j<3;j++){int i=order[j]*3;points[j]=new Vector3(part.vertices[i],part.vertices[i+2],-part.vertices[i+1]);}
                    Vector3 normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]);Vector3 abs=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));
                    foreach(var point in points){var p=point+obj.transform.position;triangles.Add(vertices.Count);vertices.Add(point);uv.Add(abs.y>=abs.x&&abs.y>=abs.z?new Vector2(p.x,p.z)*.5f:abs.x>=abs.z?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f);}
                }
                var mesh=new Mesh{name=part.id,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,Folder+"/"+part.id+".asset");obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=MaterialFor(part.group);
                if(part.collision)
                {
                    var collider=obj.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
                    if(part.group=="structure"||part.group=="ramps"||part.id=="SUMMIT-DECK")walkable.Add(collider);
                }
                if(part.group=="water"||part.group=="foam"||part.group=="foliage"||part.group=="grass")
                {var motion=obj.AddComponent<SkyGardenMotion>();motion.Water=part.group=="water"||part.group=="foam";}
            }
            var pickups=new List<HealthPickup>();
            foreach(var point in layout.health)
            {var clone=Object.Instantiate(previous.Pickups[0],root.transform);clone.name=point.id+" // GARDEN HEALTH";clone.transform.position=Convert(point.position)+Vector3.up*.92f;clone.AvailableTick=0;pickups.Add(clone);}
            world.Pickups=pickups.ToArray();
            var pads=new List<JumpPad>();
            foreach(var point in layout.jumpPads)
            {
                var pad=Object.Instantiate(previous.Pads[0],root.transform);pad.name=point.id+" // SUMMIT LAUNCH";pad.transform.position=Convert(point.start);pad.Destination=Convert(point.destination);pad.FlightSeconds=point.flightSeconds;
                foreach(var text in pad.GetComponentsInChildren<TextMesh>()){text.text="SUMMIT +22m";text.transform.localPosition=new Vector3(0,1.8f,1.8f);text.transform.localRotation=Quaternion.identity;}
                pads.Add(pad);
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name=point.id+" // SUMMIT LANDING";marker.transform.SetParent(root.transform);marker.transform.position=pad.Destination+Vector3.up*.018f;marker.transform.localScale=new Vector3(3,.015f,3);Object.DestroyImmediate(marker.GetComponent<Collider>());marker.GetComponent<Renderer>().sharedMaterial=MaterialFor("teal");
            }
            world.Pads=pads.ToArray();
            var gates=new List<PortalGate>();
            for(int i=0;i<layout.portals.Length;i++)
            {
                var point=layout.portals[i];var gate=Object.Instantiate(previous.Portals[i],root.transform);gate.name=point.id+" // GARDEN PORTAL";
                foreach(Transform child in gate.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                Vector3 position=Convert(point.center);position.y=0;gate.transform.SetPositionAndRotation(position,Quaternion.LookRotation(Convert(point.frontNormal)));
                gate.HalfWidth=.7f;gate.Height=2.2f;gate.EntryOffset=.4f;gate.DestinationName=i==0?"NORTH GARDEN":"SOUTH GARDEN";
                var face=GameObject.CreatePrimitive(PrimitiveType.Cube);face.name="Live destination field";face.transform.SetParent(gate.transform,false);face.transform.localPosition=Vector3.up*1.1f;face.transform.localScale=new Vector3(1.4f,2.2f,.06f);face.GetComponent<BoxCollider>().isTrigger=true;
                gate.Presentation.Surface=face.GetComponent<Renderer>();gate.GetComponent<PortalView>().Gate=gate;
                BoxCollider(gate.transform,"Left jamb collision",new Vector3(-.95f,1.2f,0),new Vector3(.5f,2.4f,.8f));BoxCollider(gate.transform,"Right jamb collision",new Vector3(.95f,1.2f,0),new Vector3(.5f,2.4f,.8f));
                BoxCollider(gate.transform,"Header collision",new Vector3(0,2.45f,0),new Vector3(2.4f,.5f,.8f));BoxCollider(gate.transform,"Back collision",new Vector3(0,1.3f,-.35f),new Vector3(2.4f,2.8f,.1f));gates.Add(gate);
            }
            gates[0].Exit=gates[1];gates[1].Exit=gates[0];world.Portals=gates.ToArray();
            world.Spawns=layout.spawns.Select(point=>Point(root.transform,point.id,Convert(point.position)+Vector3.up*.08f)).ToArray();
            Object.DestroyImmediate(previous.gameObject);session.World=world;
            // Remove only preserved template backdrop props, not the camera/session/UI.
            foreach(var old in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if(old!=null&&old.transform.parent==null&&old!=root&&(old.name=="Foundry backdrop"||old.name=="Foundry distant islands"))Object.DestroyImmediate(old);
            Physics.SyncTransforms();BuildRoutes(world,root.transform);Lighting(world);DistantGardens(root.transform);
            world.MenuEye=new Vector3(56,47,-60);world.MenuLook=Quaternion.LookRotation(new Vector3(0,9,0)-world.MenuEye).eulerAngles;world.MenuFieldOfView=50;
            Camera.main.transform.SetPositionAndRotation(world.MenuEye,Quaternion.Euler(world.MenuLook));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(TwinPlatformsBuilder.ScenePath,false),new EditorBuildSettingsScene("Assets/RailArena/Generated/Foundry.unity",false)};
            AssetDatabase.SaveAssets();Debug.Log("SKY_GARDENS_PLAYABLE_BUILT nodes="+world.Nodes.Length+" links="+world.Links.Length);
        }
        private static void BuildRoutes(ArenaWorld world,Transform root)
        {
            var nodes=new List<Transform>();var links=new List<Vector2Int>();var careful=new List<int>();
            for(int x=-27;x<=27;x+=2)for(int z=-19;z<=19;z+=2)
            {
                var hits=Physics.RaycastAll(new Vector3(x,30,z),Vector3.down,34,1<<0,QueryTriggerInteraction.Ignore);
                foreach(var hit in hits)
                {
                    if(!walkable.Contains(hit.collider)||hit.normal.y<.8f)continue;Vector3 p=hit.point+Vector3.up*.08f;
                    if(Physics.CheckCapsule(p+Vector3.up*.42f,p+Vector3.up*1.38f,.34f,1<<0,QueryTriggerInteraction.Ignore))continue;
                    nodes.Add(Point(root,"Walk node "+nodes.Count,p));
                }
            }
            for(int a=0;a<nodes.Count;a++)for(int b=a+1;b<nodes.Count;b++)
            {
                Vector3 p=nodes[a].position,q=nodes[b].position,d=q-p;
                if(d.magnitude>3.2f||Mathf.Abs(d.y)>1.5f)continue;
                if(Physics.CapsuleCast(p+Vector3.up*.42f,p+Vector3.up*1.38f,.34f,d.normalized,d.magnitude,1<<0,QueryTriggerInteraction.Ignore))continue;
                bool supported=true;
                for(int i=1;i<5;i++){Vector3 sample=Vector3.Lerp(p,q,i/5f);if(!Physics.Raycast(sample+Vector3.up*.25f,Vector3.down,out var hit,.6f,1<<0,QueryTriggerInteraction.Ignore)||!walkable.Contains(hit.collider)){supported=false;break;}}
                if(supported)Both(links,a,b);
            }
            world.Nodes=nodes.ToArray();
            foreach(var pad in world.Pads)links.Add(new Vector2Int(world.NearestNode(pad.transform.position),world.NearestNode(pad.Destination)));
            foreach(var gate in world.Portals)
            {
                int approach=world.NearestNode(gate.transform.position+gate.transform.forward*2);
                int entry=nodes.Count;nodes.Add(Point(root,"Portal crossing route",gate.transform.position-gate.transform.forward*.1f+Vector3.up*.08f));
                links.Add(new Vector2Int(approach,entry));links.Add(new Vector2Int(entry,world.NearestNode(gate.Exit.transform.position+gate.Exit.transform.forward*1.2f)));
            }
            foreach(int side in new[]{-1,1})
            {
                // Ordinary wing walk-offs lead to the existing lower loop.
                foreach(int end in new[]{-1,1})links.Add(new Vector2Int(world.NearestNode(new Vector3(side*19.5f,2,end*13)),world.NearestNode(new Vector3(side*19.5f,0,end*16))));
                foreach(bool eastWest in new[]{true,false})
                {
                    Vector3 edge=eastWest?new Vector3(side*8,22,0):new Vector3(0,22,side*8);
                    int start=world.NearestNode(edge),drop=nodes.Count;careful.Add(drop);nodes.Add(Point(root,"Summit return drop",edge));
                    Vector3 target=eastWest?new Vector3(side*8,4,0):new Vector3(0,0,side*8);
                    links.Add(new Vector2Int(start,drop));links.Add(new Vector2Int(drop,world.NearestNode(target)));
                }
            }
            world.Nodes=nodes.ToArray();world.Links=links.ToArray();world.CarefulDropNodes=careful.ToArray();
        }
        private static Transform Point(Transform parent,string name,Vector3 position){var obj=new GameObject(name);obj.transform.SetParent(parent);obj.transform.position=position;return obj.transform;}
        private static void Both(List<Vector2Int> links,int a,int b){if(a==b)return;links.Add(new Vector2Int(a,b));links.Add(new Vector2Int(b,a));}
        private static void BoxCollider(Transform parent,string name,Vector3 position,Vector3 size){var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.AddComponent<BoxCollider>().size=size;}
        private static Material MaterialFor(string role)
        {
            if(materials.TryGetValue(role,out var existing))return existing;
            var colors=new Dictionary<string,Color>{{"structure",new Color(.78f,.75f,.66f)},{"cover",new Color(.55f,.59f,.56f)},{"ivory_trim",new Color(.9f,.85f,.72f)},{"bronze",new Color(.38f,.25f,.1f)},{"teal",new Color(.06f,.65f,.61f)},{"bark",new Color(.22f,.13f,.07f)},{"foliage",new Color(.19f,.38f,.09f)},{"grass",new Color(.3f,.48f,.12f)},{"water",new Color(.13f,.59f,.68f,.72f)},{"foam",new Color(.8f,.95f,.95f,.75f)}};
            var material=new Material(Shader.Find(role=="water"||role=="foam"?"RailArena/SkyWater":"Universal Render Pipeline/Lit")){name=role};material.color=colors.TryGetValue(role,out var color)?color:colors["structure"];
            material.SetFloat("_Smoothness",role=="bronze"?.6f:role=="water"?.75f:.25f);material.SetFloat("_Metallic",role=="bronze"?.65f:.05f);
            if(role=="water"||role=="foam"){material.SetFloat("_Foam",role=="foam"?1:0);material.SetFloat("_Cull",0);material.SetShaderPassEnabled("ShadowCaster",false);}
            else if(role=="foliage"||role=="grass")material.SetFloat("_Cull",0);
            else if(role!="bark"&&role!="bronze"&&role!="teal")
            {material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Stone albedo.png");material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Stone normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.45f);}
            if(role=="teal"){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",material.color*.9f);}
            AssetDatabase.CreateAsset(material,Folder+"/Material-"+role+".mat");materials[role]=material;return material;
        }
        private static void CreateTextures()
        {
            const int size=512;var albedo=new Texture2D(size,size,TextureFormat.RGB24,false);var normal=new Texture2D(size,size,TextureFormat.RGB24,false);var random=new System.Random(8812);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int xx=x%256,yy=y%128;bool seam=xx<2||yy<2;float cloud=Mathf.Sin(x*.07f)*Mathf.Sin(y*.043f)*.018f;
                float shade=seam?.45f:.88f+cloud+(float)random.NextDouble()*.04f;
                albedo.SetPixel(x,y,new Color(shade,shade*.985f,shade*.94f));
                Vector3 n=new Vector3(xx==2?-.2f:xx==255?.2f:0,yy==2?-.2f:yy==127?.2f:0,1).normalized;normal.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
            }
            File.WriteAllBytes(Folder+"/Stone albedo.png",albedo.EncodeToPNG());File.WriteAllBytes(Folder+"/Stone normal.png",normal.EncodeToPNG());Object.DestroyImmediate(albedo);Object.DestroyImmediate(normal);AssetDatabase.Refresh();
            foreach(string file in new[]{"Stone albedo.png","Stone normal.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+file);importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;if(file.Contains("normal"))importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
        }
        private static void Lighting(ArenaWorld world)
        {
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(light=>light.type==LightType.Directional);sun.transform.rotation=Quaternion.LookRotation(new Vector3(.4f,-.85f,-.3f));sun.color=new Color(1,.94f,.83f);sun.intensity=1.45f;sun.shadows=LightShadows.Soft;sun.shadowBias=.03f;sun.shadowNormalBias=.15f;
            var sky=new Material(Shader.Find("Rail Arena/Foundry Cloud Sky")){name="Sky Gardens cloud sea"};sky.SetColor("_ZenithColor",new Color(.22f,.48f,.75f));sky.SetColor("_HorizonColor",new Color(.76f,.88f,.94f));sky.SetColor("_CloudColor",new Color(.99f,.98f,.93f));sky.SetColor("_CloudShadow",new Color(.57f,.72f,.82f));sky.SetVector("_SunDirection",-sun.transform.forward);AssetDatabase.CreateAsset(sky,Folder+"/Cloud sea.mat");RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.73f,.83f);RenderSettings.ambientEquatorColor=new Color(.49f,.56f,.6f);RenderSettings.ambientGroundColor=new Color(.27f,.31f,.33f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.77f,.87f,.92f);RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=280;
        }
        private static void DistantGardens(Transform root)
        {
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*.25f;Vector3 centre=new Vector3(Mathf.Cos(a)*135,-8+i%3*9,Mathf.Sin(a)*135);var island=GameObject.CreatePrimitive(PrimitiveType.Cylinder);island.name="Distant garden ruin";island.transform.SetParent(root);island.transform.position=centre;island.transform.localScale=new Vector3(18,2,18);Object.DestroyImmediate(island.GetComponent<Collider>());island.GetComponent<Renderer>().sharedMaterial=MaterialFor("structure");
                for(int c=0;c<4;c++){var column=GameObject.CreatePrimitive(PrimitiveType.Cylinder);column.name="Distant ruin column";column.transform.SetParent(root);column.transform.position=centre+new Vector3(Mathf.Cos(c*Mathf.PI*.5f)*6,8,Mathf.Sin(c*Mathf.PI*.5f)*6);column.transform.localScale=new Vector3(1.4f,7,1.4f);Object.DestroyImmediate(column.GetComponent<Collider>());column.GetComponent<Renderer>().sharedMaterial=MaterialFor("ivory_trim");}
            }
        }
    }
}
