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
    /// <summary>Isolated candidate import and static camera/pose review. Never updates live resources.</summary>
    public static class CarbineReferenceReview
    {
        const string Folder="Assets/RailArena/Art/CarbineReferenceReview";
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtSource/Original/PrecisionCarbine-References-v3"));
        [Serializable] class PoseTargets
        {
            public Vector3 Muzzle,RightGrip,LeftSupport,AimLine,StockContact;
            public Vector3 RightEuler,LeftEuler,FirstPersonMount,FirstPersonBody,ThirdPersonMount;
            public Vector3 RightWristOffset,LeftWristOffset;
        }
        [Serializable] class Result
        {
            public string unityVersion;
            public int captures,referenceTriangles;
            public float baseVerticalFov=100,zoomVerticalFov;
            public float maximumWristErrorMetres;
            public bool livePrefabChanged=false;
            public string status="Static design-review poses; final finger/shoulder fitting and gameplay acceptance pending";
            public List<string> poseWarnings=new List<string>();
        }
        static Result report;
        static PoseTargets targets;
        static Material surface,cyan;

        public static void Capture()
        {
            if (Application.unityVersion!="6000.6.7f1")throw new InvalidOperationException("Use Unity 6000.6.7f1");
            targets=JsonUtility.FromJson<PoseTargets>(File.ReadAllText(Path.Combine(Output,"ReviewPose.json")));
            report=new Result{unityVersion=Application.unityVersion,zoomVerticalFov=2*Mathf.Atan(Mathf.Tan(50*Mathf.Deg2Rad)/1.5f)*Mathf.Rad2Deg};
            Directory.CreateDirectory(Path.Combine(Output,"Unity"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string path=Folder+"/Carbine-Reference.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=false;importer.importCameras=importer.importLights=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.isReadable=true;importer.SaveAndReimport();
            var texImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/FoundryPalette.png");
            texImporter.filterMode=FilterMode.Point;texImporter.mipmapEnabled=false;
            texImporter.textureCompression=TextureImporterCompression.Uncompressed;texImporter.SaveAndReimport();
            surface=CreateMaterial("ReferencePalette",Color.white,.22f,.57f);
            surface.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/FoundryPalette.png"));
            cyan=CreateMaterial("ReferenceCyan",new Color(.015f,.52f,.58f),.1f,.7f);
            cyan.EnableKeyword("_EMISSION");cyan.SetColor("_EmissionColor",new Color(.015f,.65f,.72f)*.65f);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.52f,.56f);
            var key=new GameObject("Reference key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-30,0);
            var fill=new GameObject("Reference fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.6f;fill.transform.rotation=Quaternion.Euler(20,150,0);
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.nearClipPlane=.025f;camera.farClipPlane=30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.16f,.19f);
            string[] resources={"Characters/Contender","Characters/Stylized/Bot01","Characters/Stylized/Bot02","Characters/Stylized/Bot03"};
            for (int i=0;i<resources.Length;i++)
            {
                var character=Object.Instantiate(Resources.Load<GameObject>(resources[i]));
                var presentation=character.GetComponent<ContenderPresentation>();
                presentation.Weapon.gameObject.SetActive(false);presentation.enabled=false;
                character.GetComponent<LODGroup>().ForceLOD(0);
                var palette=presentation.BodyRenderers[0].sharedMaterial;
                character.SetActive(false);
                var gun=Weapon(true);gun.transform.position=targets.FirstPersonMount;
                var armRoot=Object.Instantiate(Resources.Load<GameObject>("Characters/ViewArms"));
                armRoot.transform.SetPositionAndRotation(targets.FirstPersonBody,Quaternion.identity);
                var view=armRoot.GetComponent<FirstPersonArms>();view.enabled=false;view.Hands.enabled=false;
                ContenderPresentation.ApplyPalette(view.Surface,palette);
                Pose(view.Animator,gun,"FP-"+i);
                camera.orthographic=false;camera.transform.SetPositionAndRotation(new Vector3(0,1.66f,0),Quaternion.identity);
                camera.fieldOfView=100;Write(camera,"FP-"+i+"-Normal");
                camera.fieldOfView=report.zoomVerticalFov;Write(camera,"FP-"+i+"-Zoom");
                Object.DestroyImmediate(armRoot);Object.DestroyImmediate(gun);
                character.SetActive(true);character.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var hands=character.GetComponent<HandArticulation>();if(hands!=null)hands.enabled=false;
                gun=Weapon();gun.transform.position=targets.ThirdPersonMount;
                Pose(presentation.Animator,gun,"TP-"+i);
                camera.fieldOfView=40;camera.transform.position=new Vector3(2.1f,1.65f,2.4f);camera.transform.LookAt(new Vector3(0,.95f,.2f));
                Write(camera,"TP-"+i+"-ThreeQuarter");
                camera.fieldOfView=44;camera.transform.position=new Vector3(2.8f,1.15f,.25f);camera.transform.LookAt(new Vector3(0,1,.25f));
                Write(camera,"TP-"+i+"-Side");
                Object.DestroyImmediate(gun);Object.DestroyImmediate(character);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(Output,"Unity-Review.json"),JsonUtility.ToJson(report,true));
            Debug.Log("CARBINE_REFERENCE_REVIEW_COMPLETE "+report.captures);
        }

        static GameObject Weapon(bool firstPerson=false)
        {
            var root=new GameObject("Carbine reference only");
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Carbine-Reference.fbx"),root.transform);
            int count=0;
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m.name.StartsWith("CyanInlay")?cyan:surface).ToArray();
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                if(mesh.uv.Length!=mesh.vertexCount)throw new InvalidOperationException("Missing candidate UVs");
                count+=mesh.triangles.Length/3;
                if(firstPerson && new[]{"RF_01_","RF_02_","RF_03_"}.Any(prefix=>renderer.name.StartsWith(prefix)))
                    renderer.enabled=false;
            }
            report.referenceTriangles=count;
            foreach(var item in new[]{("Muzzle",targets.Muzzle), ("RightGrip",targets.RightGrip), ("LeftSupport",targets.LeftSupport), ("AimLine",targets.AimLine), ("StockContact",targets.StockContact)})
            {
                var socket=new GameObject(item.Item1).transform;socket.SetParent(root.transform,false);socket.localPosition=item.Item2;
            }
            root.transform.Find("RightGrip").localRotation=Quaternion.Euler(targets.RightEuler);
            root.transform.Find("LeftSupport").localRotation=Quaternion.Euler(targets.LeftEuler);
            return root;
        }

        static void Pose(Animator animator,GameObject weapon,string label)
        {
            animator.enabled=true;animator.Rebind();animator.Update(0);
            using(var handler=new HumanPoseHandler(animator.avatar,animator.transform))
            {
                var pose=new HumanPose();handler.GetHumanPose(ref pose);
                for(int m=0;m<HumanTrait.MuscleCount;m++)
                {
                    string name=HumanTrait.MuscleName[m];
                    if(name.Contains("Stretched")&&(name.StartsWith("Left")||name.StartsWith("Right")))
                        pose.muscles[m]=name.StartsWith("Right")&&name.Contains("Index")?-.15f:name.Contains("Thumb")?-.10f:-.45f;
                    if(name.Contains("Spread")&&(name.StartsWith("Left")||name.StartsWith("Right")))pose.muscles[m]=.05f;
                }
                handler.SetHumanPose(ref pose);
            }
            animator.enabled=false;
            var chest=animator.GetBoneTransform(HumanBodyBones.Chest);
            if(chest!=null)chest.rotation=Quaternion.AngleAxis(25,Vector3.up)*chest.rotation;
            Arm(animator,weapon.transform.Find("RightGrip"),targets.RightWristOffset,false,label);
            Arm(animator,weapon.transform.Find("LeftSupport"),targets.LeftWristOffset,true,label);
        }

        static void Arm(Animator animator,Transform target,Vector3 wristOffset,bool left,string label)
        {
            var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            Vector3 goal=target.position+target.parent.TransformVector(wristOffset);
            Vector3 origin=upper.position,delta=goal-origin;
            float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
            var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(new Vector3(left?-.6f:.6f,-1,.15f),direction).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            var elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,origin+direction*distance-lower.position)*lower.rotation;
            hand.rotation=target.rotation;
            var middle=animator.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
            var index=animator.GetBoneTransform(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
            var little=animator.GetBoneTransform(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
            var palmDirection=middle!=null?(middle.position-hand.position).normalized:hand.TransformDirection(Vector3.up);
            var palmNormal=index!=null && little!=null?Vector3.Cross(palmDirection,index.position-little.position).normalized:hand.TransformDirection(Vector3.forward);
            var currentFrame=Quaternion.LookRotation(palmNormal,palmDirection);
            var desiredFrame=Quaternion.LookRotation(left?target.parent.up:-target.parent.right,
                left?target.parent.right:(-target.parent.up+target.parent.forward*.15f).normalized);
            hand.rotation=desiredFrame*Quaternion.Inverse(currentFrame)*hand.rotation;
            float error=Vector3.Distance(hand.position,goal);
            report.maximumWristErrorMetres=Mathf.Max(report.maximumWristErrorMetres,error);
            if(error>.015f)report.poseWarnings.Add(label+(left?" left":" right")+" wrist reach error "+error.ToString("F3")+"m");
        }

        static Material CreateMaterial(string name,Color color,float metallic,float smoothness)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(material);return material;
        }

        static void Write(Camera camera,string name)
        {
            var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try {
                target.Create();camera.targetTexture=target;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Output,"Unity/"+name+".png"),image.EncodeToPNG());report.captures++;
            }finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
