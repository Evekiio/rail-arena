using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class ReloadHandSourceExport
    {
        [Serializable] sealed class MatrixRecord { public float[] values; public MatrixRecord(Matrix4x4 m){values=Enumerable.Range(0,16).Select(i=>m[i/4,i%4]).ToArray();} }
        [Serializable] sealed class Frame { public int frame,enginePoseFrame; public MatrixRecord[] bonesToRoot; public MatrixRecord wrist,magazine,cameraToRoot; public float leftWristError,rightWristError,freshSolveError,normalizedSolveError; }
        [Serializable] sealed class Source { public string scope="Frozen native wrist-IK calibration for the existing Blender-authored two-second reload. Player appearance; no simulation advances or gameplay writes."; public string[] bones; public Frame[] frames; public float verticalFov,maximumWristError; public bool passed; }
        [Serializable] sealed class Candidate { public Vector3 magazinePivot; public RifleReloadArt.Pose[] samples; }
        static float Probe(FirstPersonArms arms,Transform target,bool normalized)
        {
            var animator=arms.Animator;var upper=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);var lower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);var wrist=animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var ur=upper.localRotation;var lr=lower.localRotation;var wr=wrist.localRotation;
            try
            {
                if(!normalized)ArmContactPose.Apply(animator,true,target);
                else
                {
                    Vector3 origin=upper.position,delta=target.position-origin;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,wrist.position);float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);var direction=delta.normalized;var hint=animator.transform.TransformDirection(new Vector3(-.45f,-1,-.1f));var bend=Vector3.ProjectOnPlane(hint,direction).normalized;float along=(a*a-b*b+length*length)/(2*length);var elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
                    var from=upper.parent.InverseTransformVector(lower.position-origin).normalized;var to=upper.parent.InverseTransformVector(elbow-origin).normalized;upper.localRotation=Quaternion.FromToRotation(from,to)*upper.localRotation;
                    from=lower.parent.InverseTransformVector(wrist.position-lower.position).normalized;to=lower.parent.InverseTransformVector(origin+direction*length-lower.position).normalized;lower.localRotation=Quaternion.FromToRotation(from,to)*lower.localRotation;wrist.rotation=target.rotation;
                }
                return Vector3.Distance(wrist.position,target.position);
            }
            finally{upper.localRotation=ur;lower.localRotation=lr;wrist.localRotation=wr;}
        }
        byte[] quality;string pipelinePath;
        [UnityTest] public IEnumerator CaptureExistingReloadRig()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipelinePath=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();session.enabled=false;session.View.enabled=false;
            foreach(var actor in session.Actors){actor.Controller.enabled=false;actor.Presentation.enabled=false;}
            var slice=new GameObject("Reload hand calibration").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            var camera=session.View;var arms=camera.GetComponentInChildren<FirstPersonArms>(true);var weapon=arms.GetComponentInParent<RailgunPresentation>(true);weapon.gameObject.SetActive(true);yield return null;
            arms.GetComponent<ContenderArtPresentation>().ApplyArms(arms,Resources.Load<ContenderArtSet>("Art/Contenders").variants[0]);yield return null;
            arms.Animator.enabled=false;arms.SetRecoil(0);var reload=weapon.GetComponent<RifleReloadPresentation>();Assert.That(reload.Initialized,Is.True);reload.enabled=false;
            var artField=typeof(RifleReloadPresentation).GetField("art",BindingFlags.Instance|BindingFlags.NonPublic);var installed=(RifleReloadArt)artField.GetValue(reload);RifleReloadArt candidate=null;
            string candidatePath=Environment.GetEnvironmentVariable("RA_RELOAD_ART_SOURCE");
            if(!string.IsNullOrEmpty(candidatePath)){var record=JsonUtility.FromJson<Candidate>(File.ReadAllText(candidatePath));Assert.That(record.samples.Length,Is.EqualTo(121));candidate=ScriptableObject.CreateInstance<RifleReloadArt>();candidate.magazinePivot=record.magazinePivot;candidate.samples=record.samples;artField.SetValue(reload,candidate);}
            string folder=Path.GetFullPath(Environment.GetEnvironmentVariable("RA_RELOAD_HAND_EXPORT")??"../Validation/ReloadContacts-01/NativeBaseline");Assert.That(Directory.Exists(folder),Is.False,"Preserve frozen native calibration");Directory.CreateDirectory(folder);
            var source=new Source{bones=arms.Surface.bones.Select(b=>b.name).ToArray(),frames=new Frame[121],verticalFov=camera.fieldOfView};
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);int rate=Time.captureFramerate;Time.captureFramerate=60;
            var actorRoot=session.Human.transform.position;var velocity=session.Human.Velocity;int tick=session.Match.Tick,rounds=session.Human.Magazine.Rounds,nextShot=session.Human.NextShotTick;float height=session.Human.Controller.height;
            try
            {
                camera.transform.SetPositionAndRotation(session.Human.Eye,Quaternion.Euler(session.Human.Command.Pitch,session.Human.Command.Yaw,0));
                weapon.transform.localPosition=new AimDownSights().Position(weapon.transform.InverseTransformPoint(weapon.Aim.position),Vector3.zero,0);weapon.transform.localRotation=Quaternion.identity;camera.fieldOfView=session.Aim.FieldOfView;source.verticalFov=camera.fieldOfView;
                for(int frame=0;frame<=120;frame++)
                {
                    int previousPose=arms.LastPoseFrame;reload.PoseAt(frame/120f);arms.Hands.ClearReloadPose();float deadline=Time.realtimeSinceStartup+5;
                    do{yield return null;Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Runtime pose did not advance");}while(arms.LastPoseFrame<=previousPose);
                    var toRoot=weapon.transform.worldToLocalMatrix;
                    float left=Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position,weapon.LeftGrip.position),right=Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position,weapon.RightGrip.position);
                    source.maximumWristError=Mathf.Max(source.maximumWristError,left,right);
                    source.frames[frame]=new Frame{frame=frame,enginePoseFrame=arms.LastPoseFrame,bonesToRoot=arms.Surface.bones.Select(b=>new MatrixRecord(toRoot*b.localToWorldMatrix)).ToArray(),wrist=new MatrixRecord(toRoot*weapon.LeftGrip.localToWorldMatrix),magazine=new MatrixRecord(toRoot*weapon.transform.Find("Authored magazine carrier").localToWorldMatrix),cameraToRoot=new MatrixRecord(toRoot*camera.transform.localToWorldMatrix),leftWristError=left,rightWristError=right};
                    source.frames[frame].freshSolveError=Probe(arms,weapon.LeftGrip,false);source.frames[frame].normalizedSolveError=Probe(arms,weapon.LeftGrip,true);
                    Assert.That(session.Human.transform.position,Is.EqualTo(actorRoot));Assert.That(session.Human.Velocity,Is.EqualTo(velocity));Assert.That(session.Match.Tick,Is.EqualTo(tick));Assert.That(session.Human.Magazine.Rounds,Is.EqualTo(rounds));Assert.That(session.Human.NextShotTick,Is.EqualTo(nextShot));Assert.That(session.Human.Controller.height,Is.EqualTo(height));
                    if(new[]{0,12,21,36,65,91,105,118,120}.Contains(frame))
                    {
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,$"frame-{frame:D3}.png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                }
                source.passed=source.maximumWristError<.025f;File.WriteAllText(Path.Combine(folder,"reload-rig.json"),JsonUtility.ToJson(source,true));TestContext.WriteLine("RELOAD_HAND_SOURCE_CAPTURED "+folder+" / 121 frozen native frames");Assert.That(source.maximumWristError,Is.LessThan(.025f),"Frozen source wrist contact envelope");
            }
            finally{reload.ResetPose();artField.SetValue(reload,installed);if(candidate!=null)Object.Destroy(candidate);Time.captureFramerate=rate;target.Release();Object.Destroy(target);Object.Destroy(pixels);}
            Object.Destroy(slice);yield return null;yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(quality!=null){QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.ImportAsset("ProjectSettings/QualitySettings.asset",ImportAssetOptions.ForceSynchronousImport);}
        }
    }
}
