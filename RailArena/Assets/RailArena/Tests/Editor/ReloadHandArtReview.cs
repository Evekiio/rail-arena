using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class ReloadHandArtReview
    {
        [Serializable] sealed class Contact { public string finger; public float[] solvedTipCentreUnityMetres; }
        [Serializable] sealed class Spec { public string masterSha256; public Contact[] contacts; }
        [Serializable] sealed class Report { public string masterSha256;public int framesPerStage=121,appearances=4,poseSamples,jointSamples,thirdPersonPoseSamples;public float maximumWristError,maximumJointGap,maximumAuthoredTipError,maximumThirdPersonWristError;public bool passed;public string scope="Original held grip versus Blender-authored release/grasp/return, with the same existing wrist/magazine track, camera and frozen actor. 121 distinct runtime pose updates per stage. Silent cosmetic review; not native input/audio or complete skin collision acceptance."; }
        sealed class Ring { public int bone; public Vector3 point; }
        sealed class Seam { public Ring end,start; }
        byte[] quality;string pipelinePath;
        static Ring Centre(SkinnedMeshRenderer surface,int index,bool end)
        {
            var mesh=surface.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[index];var selected=Enumerable.Range(0,vertices.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.9999f).ToArray();
            float plane=end?selected.Max(i=>bind.MultiplyPoint3x4(vertices[i]).y):selected.Min(i=>bind.MultiplyPoint3x4(vertices[i]).y);
            var points=selected.Where(i=>Mathf.Abs(bind.MultiplyPoint3x4(vertices[i]).y-plane)<1e-7f).Select(i=>vertices[i]).Distinct().ToArray();Assert.That(points.Length,Is.GreaterThanOrEqualTo(6));return new Ring{bone=index,point=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length};
        }
        static Vector3 World(Ring ring,SkinnedMeshRenderer surface)=>surface.bones[ring.bone].localToWorldMatrix.MultiplyPoint3x4(surface.sharedMesh.bindposes[ring.bone].MultiplyPoint3x4(ring.point));
        static Seam[] Seams(SkinnedMeshRenderer surface)
        {
            var result=new List<Seam>();foreach(string side in new[]{"Left","Right"})foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Pinky"})for(int segment=1;segment<3;segment++)
            {int a=Array.FindIndex(surface.bones,b=>b.name==side+"Hand"+finger+segment),b=Array.FindIndex(surface.bones,bone=>bone.name==side+"Hand"+finger+(segment+1));result.Add(new Seam{end=Centre(surface,a,true),start=Centre(surface,b,false)});}return result.ToArray();
        }
        static void Capture(Camera camera,RenderTexture target,Texture2D pixels,string path)
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
        }
        [UnityTest] public IEnumerator AuthoredFingersFollowMagazineAndRestore()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipelinePath=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();session.enabled=false;session.View.enabled=false;foreach(var actor in session.Actors){actor.Controller.enabled=false;actor.Presentation.enabled=false;}
            var slice=new GameObject("Reload fingers native review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;var camera=session.View;var arms=camera.GetComponentInChildren<FirstPersonArms>(true);var weapon=arms.GetComponentInParent<RailgunPresentation>(true);weapon.gameObject.SetActive(true);yield return null;arms.Animator.enabled=false;arms.SetRecoil(0);
            var reload=weapon.GetComponent<RifleReloadPresentation>();reload.enabled=false;var variants=Resources.Load<ContenderArtSet>("Art/Contenders").variants;var spec=JsonUtility.FromJson<Spec>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1/ReloadHandAction.json"));Assert.That(Resources.Load<RifleReloadHandArt>("Art/RifleReloadHands").sourceSha256,Is.EqualTo(spec.masterSha256));
            string folder=Path.GetFullPath("../Validation/ReloadHandsPlayback-01");Assert.That(Directory.Exists(folder),Is.False);Directory.CreateDirectory(folder);var report=new Report{masterSha256=spec.masterSha256};var csv=new StringBuilder("stage,frame,enginePoseFrame,wristError,jointGap,authoredTipError\n");
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);var detail=new GameObject("Owned hand contact inspection camera").AddComponent<Camera>();detail.CopyFrom(camera);detail.enabled=false;detail.orthographic=true;detail.orthographicSize=.22f;detail.nearClipPlane=.01f;detail.farClipPlane=2;
            var actorRoot=session.Human.transform.position;var velocity=session.Human.Velocity;int tick=session.Match.Tick,rounds=session.Human.Magazine.Rounds,nextShot=session.Human.NextShotTick;float height=session.Human.Controller.height;int rate=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                camera.transform.SetPositionAndRotation(session.Human.Eye,Quaternion.Euler(session.Human.Command.Pitch,session.Human.Command.Yaw,0));weapon.transform.localPosition=new AimDownSights().Position(weapon.transform.InverseTransformPoint(weapon.Aim.position),Vector3.zero,0);weapon.transform.localRotation=Quaternion.identity;camera.fieldOfView=session.Aim.FieldOfView;
                for(int stage=0;stage<5;stage++)
                {
                    int appearance=Mathf.Max(0,stage-1);string name=stage==0?"before":"after-"+variants[appearance].identity;string directory=Path.Combine(folder,name);Directory.CreateDirectory(directory);arms.GetComponent<ContenderArtPresentation>().ApplyArms(arms,variants[appearance]);reload.ResetPose();var surface=arms.Surface;var seams=Seams(surface);
                    for(int frame=0;frame<=120;frame++)
                    {
                        int previous=arms.LastPoseFrame;reload.PoseAt(frame/120f);if(stage==0)arms.Hands.ClearReloadPose();float deadline=Time.realtimeSinceStartup+5;
                        do{yield return null;Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));}while(arms.LastPoseFrame<=previous);
                        float wrist=Mathf.Max(Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position,weapon.LeftGrip.position),Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position,weapon.RightGrip.position));Assert.That(wrist,Is.LessThan(.0005f));report.maximumWristError=Mathf.Max(report.maximumWristError,wrist);float gap=0,tipError=0;
                        foreach(var seam in seams){gap=Mathf.Max(gap,Vector3.Distance(World(seam.end,surface),World(seam.start,surface)));report.jointSamples++;}Assert.That(gap,Is.LessThan(.00005f));report.maximumJointGap=Mathf.Max(report.maximumJointGap,gap);
                        for(int joint=15;joint<30;joint++)Assert.That(Quaternion.Angle(arms.Hands.Joints[joint].localRotation,arms.Hands.GripRotations[joint]),Is.LessThan(.01f));
                        if(stage>0&&frame==21)
                        {
                            foreach(var contact in spec.contacts){int index=Array.FindIndex(surface.bones,b=>b.name=="LeftHand"+contact.finger+"3");var expected=new Vector3(contact.solvedTipCentreUnityMetres[0],contact.solvedTipCentreUnityMetres[1],contact.solvedTipCentreUnityMetres[2]);tipError=Mathf.Max(tipError,Vector3.Distance(weapon.transform.InverseTransformPoint(World(Centre(surface,index,true),surface)),expected));}Assert.That(tipError,Is.LessThan(.00005f));report.maximumAuthoredTipError=Mathf.Max(report.maximumAuthoredTipError,tipError);
                        }
                        report.poseSamples++;csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3:R},{4:R},{5:R}\n",name,frame,arms.LastPoseFrame,wrist,gap,tipError);
                        Assert.That(session.Human.transform.position,Is.EqualTo(actorRoot));Assert.That(session.Human.Velocity,Is.EqualTo(velocity));Assert.That(session.Match.Tick,Is.EqualTo(tick));Assert.That(session.Human.Magazine.Rounds,Is.EqualTo(rounds));Assert.That(session.Human.NextShotTick,Is.EqualTo(nextShot));Assert.That(session.Human.Controller.height,Is.EqualTo(height));
                        if(stage<2||new[]{0,15,21,65,98,120}.Contains(frame))Capture(camera,target,pixels,Path.Combine(directory,$"frame-{frame:D3}.png"));
                        if(new[]{15,21,65,98}.Contains(frame))
                        {var carrier=weapon.transform.Find("Authored magazine carrier");detail.transform.position=carrier.position+weapon.transform.TransformVector(new Vector3(.7f,.08f,.03f));detail.transform.LookAt(carrier.position,weapon.transform.up);Capture(detail,target,pixels,Path.Combine(directory,$"contact-{frame:D3}.png"));}
                    }
                    foreach(float interrupted in new[]{.15f,.50f,.85f}){reload.PoseAt(interrupted);arms.Hands.Pose(0);reload.ResetPose();for(int joint=0;joint<30;joint++)Assert.That(Quaternion.Angle(arms.Hands.Joints[joint].localRotation,arms.Hands.GripRotations[joint]),Is.LessThan(.01f));Assert.That(reload.Progress,Is.Zero);}
                }
                foreach(var actor in session.Actors)
                {
                    var presentation=actor.Presentation;presentation.Animator.enabled=false;presentation.Hands.SetRecoil(0);var bodyAction=presentation.Railgun.GetComponent<RifleReloadPresentation>();bodyAction.enabled=false;
                    foreach(int frame in new[]{0,15,21,36,65,98,120})
                    {
                        presentation.ApplyCombatPose();bodyAction.PoseAt(frame/120f);ArmContactPose.Apply(presentation.Animator,true,presentation.LeftGrip);presentation.Hands.Pose(0);
                        float error=Vector3.Distance(presentation.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position,presentation.LeftGrip.position);Assert.That(error,Is.LessThan(.025f),"Third-person reload wrist");report.maximumThirdPersonWristError=Mathf.Max(report.maximumThirdPersonWristError,error);report.thirdPersonPoseSamples++;
                    }
                    bodyAction.ResetPose();
                }
                report.passed=true;File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Path.Combine(folder,"samples.csv"),csv.ToString());TestContext.WriteLine("RELOAD_HAND_NATIVE_PASSED "+report.masterSha256+" / "+report.poseSamples+" poses / "+report.jointSamples+" joint samples");
            }
            finally{reload.ResetPose();Time.captureFramerate=rate;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(detail.gameObject);}
            Object.Destroy(slice);yield return null;yield return new ExitPlayMode();
        }
        [Test] public void ResetPreservesDisabledDeathPoseAndRestoresLiveGrip()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Characters/ViewArms"));
            try{var hands=root.GetComponent<HandArticulation>();var art=Resources.Load<RifleReloadHandArt>("Art/RifleReloadHands");hands.SetReloadPose(art,.175f);hands.Pose(0);Assert.That(Enumerable.Range(0,15).Max(i=>Quaternion.Angle(hands.Joints[i].localRotation,hands.GripRotations[i])),Is.GreaterThan(10));hands.ClearReloadPose();for(int i=0;i<30;i++)Assert.That(Quaternion.Angle(hands.Joints[i].localRotation,hands.GripRotations[i]),Is.LessThan(.01f));hands.enabled=false;var death=Quaternion.Euler(12,20,30);hands.Joints[0].localRotation=death;hands.ClearReloadPose();Assert.That(Quaternion.Angle(hands.Joints[0].localRotation,death),Is.LessThan(.01f));hands.enabled=true;root.SetActive(false);hands.ClearReloadPose();Assert.That(Quaternion.Angle(hands.Joints[0].localRotation,death),Is.LessThan(.01f));root.SetActive(true);hands.Pose(0);Assert.That(Quaternion.Angle(hands.Joints[0].localRotation,hands.GripRotations[0]),Is.LessThan(.01f));}finally{Object.DestroyImmediate(root);}
        }
        [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();if(quality!=null){QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.ImportAsset("ProjectSettings/QualitySettings.asset",ImportAssetOptions.ForceSynchronousImport);}}
    }
}
