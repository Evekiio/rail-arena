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
    public static class HealthPickupArtBuilder
    {
        private const string Folder="Assets/RailArena/Art/HealthBeacon";
        private const string Prefab="Assets/Resources/Environment/HealthBeacon.prefab";
        private const string Scene="Assets/RailArena/Generated/Foundry.unity";

        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Apply pickup art outside play mode.");
            if(EditorSceneManager.GetActiveScene().path!=Scene)EditorSceneManager.OpenScene(Scene);
            var world=Object.FindAnyObjectByType<ArenaWorld>();
            string before=FoundryArtBuilder.GameplaySignature(world);
            BuildPrefab();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            foreach(var pickup in world.Pickups)
            {
                if(pickup.Presentation!=null)Object.DestroyImmediate(pickup.Presentation.gameObject);
                foreach(var renderer in pickup.Visuals)if(renderer!=null)Object.DestroyImmediate(renderer.gameObject);
                var art=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                art.transform.SetParent(pickup.transform,false);
                pickup.Presentation=art.GetComponent<HealthPickupVisual>();
                pickup.Visuals=new[]{pickup.Presentation.Marker};
                pickup.Presentation.SetAvailable(true);
            }
            string after=FoundryArtBuilder.GameplaySignature(world);
            if(before!=after)throw new InvalidOperationException("Pickup art altered gameplay geometry.");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("../Validation/HealthBeacon");
            File.WriteAllText("../Validation/HealthBeacon/Geometry.json",JsonUtility.ToJson(new GeometryRecord {before=before,after=after,unchanged=true,sites=world.Pickups.Length},true));
            Capture(world);
        }

        [Serializable] private sealed class GeometryRecord { public string before,after; public bool unchanged; public int sites; }

        private static void BuildPrefab()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory("Assets/Resources/Environment"); AssetDatabase.Refresh();
            var graphite=Material("Graphite",new Color(.035f,.055f,.065f),.45f,.5f);
            var ceramic=Material("Ivory",new Color(.82f,.80f,.71f),.1f,.4f);
            var amber=Material("Amber",new Color(1,.51f,.06f),.1f,.45f);
            amber.EnableKeyword("_EMISSION"); amber.SetColor("_EmissionColor",new Color(1,.32f,.02f));
            var cube=FoundryArtBuilder.BeveledCube(); var cross=CrossMesh();
            var root=new GameObject("Health beacon"); root.layer=2;
            try
            {
                var dark=new List<CombineInstance>(); var light=new List<CombineInstance>(); var indicators=new List<CombineInstance>();
                void Box(List<CombineInstance> group,Vector3 center,Vector3 size,float yaw=0)
                    => group.Add(new CombineInstance {mesh=cube,transform=Matrix4x4.TRS(center,Quaternion.Euler(0,yaw,0),size)});
                Box(dark,new Vector3(0,-.92f,0),new Vector3(.8f,.12f,.8f));
                Box(dark,new Vector3(0,-.80f,0),new Vector3(.58f,.04f,.58f));
                Box(dark,new Vector3(0,-.755f,0),new Vector3(.24f,.05f,.24f));
                foreach(float side in new[]{-1f,1f})
                {
                    Box(light,new Vector3(side*.33f,-.82f,0),new Vector3(.14f,.08f,.68f));
                    Box(light,new Vector3(0,-.82f,side*.33f),new Vector3(.52f,.08f,.14f));
                }
                for(int i=0;i<4;i++)
                {
                    float angle=i*90*Mathf.Deg2Rad;
                    Box(indicators,new Vector3(Mathf.Sin(angle)*.24f,-.77f,Mathf.Cos(angle)*.24f),new Vector3(.15f,.018f,.035f),i*90);
                }
                var pedestal=Part(root,"Faceted pedestal",Combine("Pedestal",dark,light),new[]{graphite,ceramic});
                var markerDark=new List<CombineInstance>(); var markerLight=new List<CombineInstance>();
                Box(markerDark,Vector3.zero,new Vector3(.38f,.5f,.38f));
                for(int i=0;i<4;i++)
                {
                    var rotation=Quaternion.Euler(0,i*90,0);
                    markerDark.Add(new CombineInstance {mesh=cross,transform=Matrix4x4.TRS(rotation*new Vector3(0,0,.215f),rotation,new Vector3(.68f,.68f,.035f))});
                    markerLight.Add(new CombineInstance {mesh=cross,transform=Matrix4x4.TRS(rotation*new Vector3(0,0,.245f),rotation,new Vector3(.58f,.58f,.02f))});
                }
                var marker=Part(root,"Amber plus marker",Combine("Marker",markerDark,markerLight),new[]{graphite,amber});
                var status=Part(root,"Pedestal status segments",Combine("Status",indicators),new[]{amber});
                var presentation=root.AddComponent<HealthPickupVisual>();
                presentation.Marker=marker; presentation.Status=status;
                presentation.AvailableMaterial=amber; presentation.DepletedMaterial=graphite;
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cube); Object.DestroyImmediate(cross); }
        }

        private static Material Material(string name,Color color,float metallic,float smoothness)
        {
            string path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(material); return material;
        }

        private static Mesh Combine(string name,params List<CombineInstance>[] groups)
        {
            var parts=new List<Mesh>();
            try
            {
                foreach(var group in groups) {var mesh=new Mesh();mesh.CombineMeshes(group.ToArray(),true,true);parts.Add(mesh);}
                var combined=new Mesh {name="Health beacon "+name};
                combined.CombineMeshes(parts.Select(mesh=>new CombineInstance {mesh=mesh,transform=Matrix4x4.identity}).ToArray(),false,true);
                string path=Folder+"/"+name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null){AssetDatabase.CreateAsset(combined,path);return combined;}
                existing.Clear(); existing.indexFormat=combined.indexFormat;
                existing.vertices=combined.vertices; existing.normals=combined.normals; existing.uv=combined.uv;
                existing.subMeshCount=combined.subMeshCount;
                for(int i=0;i<combined.subMeshCount;i++)existing.SetTriangles(combined.GetTriangles(i),i);
                existing.bounds=combined.bounds; existing.UploadMeshData(false);
                Object.DestroyImmediate(combined); EditorUtility.SetDirty(existing); return existing;
            }
            finally {foreach(var mesh in parts)Object.DestroyImmediate(mesh);}
        }

        private static Renderer Part(GameObject root,string name,Mesh mesh,Material[] materials)
        {
            var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.layer=2;part.transform.SetParent(root.transform,false);
            part.GetComponent<MeshFilter>().sharedMesh=mesh; var renderer=part.GetComponent<Renderer>();renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;return renderer;
        }

        private static Mesh CrossMesh()
        {
            const float a=.5f,b=.15f;
            var outline=new[]{new Vector2(-a,-b),new Vector2(-b,-b),new Vector2(-b,-a),new Vector2(b,-a),new Vector2(b,-b),new Vector2(a,-b),
                new Vector2(a,b),new Vector2(b,b),new Vector2(b,a),new Vector2(-b,a),new Vector2(-b,b),new Vector2(-a,b)};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            void Triangle(Vector3 x,Vector3 y,Vector3 z)
            {
                int start=vertices.Count;vertices.AddRange(new[]{x,y,z});
                uv.AddRange(new[]{new Vector2(x.x+.5f,x.y+.5f),new Vector2(y.x+.5f,y.y+.5f),new Vector2(z.x+.5f,z.y+.5f)});
                triangles.AddRange(new[]{start,start+1,start+2});
            }
            for(int i=0;i<outline.Length;i++)
            {
                var x=outline[i];var y=outline[(i+1)%outline.Length];
                var xf=new Vector3(x.x,x.y,.5f);var yf=new Vector3(y.x,y.y,.5f);var xb=new Vector3(x.x,x.y,-.5f);var yb=new Vector3(y.x,y.y,-.5f);
                Triangle(Vector3.forward*.5f,xf,yf);Triangle(Vector3.back*.5f,yb,xb);
                Triangle(xb,yb,yf);Triangle(xb,yf,xf);
            }
            var mesh=new Mesh {name="Closed extruded health plus"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private static void Capture(ArenaWorld world)
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraRoot=new GameObject("Health review camera");
            try
            {
                root.transform.position=new Vector3(0,100,0);
                var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.10f,.12f);
                camera.transform.position=root.transform.position+new Vector3(1.7f,1.0f,-2.4f);camera.transform.LookAt(root.transform.position+Vector3.down*.3f);
                camera.orthographic=true;camera.orthographicSize=1.1f;camera.farClipPlane=10;
                var visual=root.GetComponent<HealthPickupVisual>();
                foreach(bool available in new[]{true,false})
                {
                    visual.SetAvailable(available);Write(camera,available?"Available":"Depleted");
                }
                var pickup=world.Pickups[0];camera.orthographic=false;camera.fieldOfView=65;camera.farClipPlane=200;
                camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=pickup.transform.position+new Vector3(2,1.2f,-3.2f);camera.transform.LookAt(pickup.transform.position+Vector3.down*.3f);
                Write(camera,"Arena-site");
            }
            finally {Object.DestroyImmediate(root);Object.DestroyImmediate(cameraRoot);}
        }
        private static void Write(Camera camera,string name)
        {
            var target=new RenderTexture(1440,1080,24);var image=new Texture2D(1440,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                target.Create();
                // Newly authored shared materials need a warm render before recording their state.
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1440,1080),0,0);image.Apply();File.WriteAllBytes("../Validation/HealthBeacon/"+name+".png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
