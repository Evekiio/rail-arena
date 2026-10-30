using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class RetreatMotionArtReview
    {
        [Serializable] sealed class Pose { public string clip; public float phase; public Vector3 leftFoot, rightFoot, hips; }
        [Serializable] sealed class Evidence { public string scope = "Three installed original humanoid retreat clips sampled on the refined player and existing avatar, after body alignment. Normal-speed playback and all-variant review remain required."; public Pose[] poses; }
        [UnityTest]
        public IEnumerator ReviewImportedRetreatOnInstalledAvatar()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,0,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Retreat candidate visual review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;var actor=session.Human;actor.Controller.enabled=false;actor.Presentation.enabled=false;
            actor.Presentation.Weapon.gameObject.SetActive(false);actor.transform.SetPositionAndRotation(new Vector3(0,actor.Controller.skinWidth,12),Quaternion.identity);
            actor.Presentation.transform.localRotation=Quaternion.identity;
            foreach(var item in actor.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            var animator=actor.Presentation.Animator;
            var replacement=Resources.Load<AnimatorOverrideController>("Art/ContenderRetreat");
            Assert.That(replacement,Is.Not.Null);Assert.That(animator.runtimeAnimatorController,Is.EqualTo(replacement));
            Assert.That(animator.applyRootMotion,Is.False);var originalPosition=actor.transform.position;
            var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();replacement.GetOverrides(pairs);
            var names=new Dictionary<string,string>{{"Backward walk","RetreatWalk"},{"Backward jog","RetreatJog"},{"Backward run","RetreatRun"}};
            int changed=0;
            for(int index=0;index<pairs.Count;index++)if(names.TryGetValue(pairs[index].Key.name,out var name))
            {
                Assert.That(pairs[index].Value.name,Is.EqualTo(name));Assert.That(pairs[index].Value.isHumanMotion,Is.True);changed++;
            }
            Assert.That(changed,Is.EqualTo(3));animator.Rebind();animator.speed=0;
            var camera=new GameObject("Owned retreat review camera").AddComponent<Camera>();
            camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.06f,.075f).linear;
            camera.transform.position=new Vector3(3,1.2f,14);camera.transform.LookAt(new Vector3(0,.9f,12));camera.fieldOfView=38;
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            string folder=Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Animation/UnityReview");Directory.CreateDirectory(folder);var poses=new List<Pose>();
            try
            {
                foreach(var gait in new[]{("RetreatWalk",-1.5f),("RetreatJog",-4f),("RetreatRun",-9f)})
                    foreach(float phase in new[]{0,.125f,.25f,.375f,.5f,.75f})
                    {
                        animator.SetFloat("Speed",gait.Item2);animator.SetFloat("Gait",1);animator.SetBool("Grounded",true);animator.SetBool("Dead",false);
                        animator.Play("Locomotion",0,phase);animator.Update(0);ArmContactPose.AlignBody(animator);
                        poses.Add(new Pose{clip=gait.Item1,phase=phase,leftFoot=actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position),rightFoot=actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position),hips=actor.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position)});
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,gait.Item1+"-"+Mathf.RoundToInt(phase*1000)+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                        yield return null;
                    }
                File.WriteAllText(Path.Combine(folder,"retargeted-poses.json"),JsonUtility.ToJson(new Evidence{poses=poses.ToArray()},true));
                foreach(var gait in names.Values)
                {
                    var first=poses.Single(p=>p.clip==gait&&p.phase==0);var half=poses.Single(p=>p.clip==gait&&p.phase==.5f);
                    Assert.That(half.leftFoot.z-first.leftFoot.z,Is.GreaterThan(.2f),gait+" support foot must advance relative to a retreating body");
                    Assert.That(Mathf.Abs(first.leftFoot.y-half.leftFoot.y),Is.LessThan(.03f),gait+" stance endpoints must retain ankle height");
                }
                Assert.That(actor.transform.position,Is.EqualTo(originalPosition),"Animation must not move the controller root");
            }
            finally{target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);}
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
