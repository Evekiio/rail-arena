using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class AirborneMotionArtReview
    {
        [UnityTest]
        public IEnumerator ReviewInstalledAirborneAtNativeClipRate()
        {
            var graph=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Art/ContenderMotion.controller");
            Assert.That(graph,Is.Not.Null);
            var states=graph.layers[0].stateMachine.states.ToDictionary(child=>child.state.name,child=>child.state);
            foreach(string name in new[]{"Jump","Air","Land"})
            { Assert.That(states[name].speed,Is.EqualTo(1));Assert.That(states[name].speedParameterActive,Is.False); }
            Assert.That(states["Jump"].motion.name,Is.EqualTo("Takeoff"));Assert.That(states["Air"].motion.name,Is.EqualTo("AirborneHold"));Assert.That(states["Land"].motion.name,Is.EqualTo("LandingRecover"));
            var original=Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            Assert.That(original,Is.Not.EqualTo(graph));
            var legacy=(AnimatorController)original;
            var legacyStates=legacy.layers[0].stateMachine.states.ToDictionary(child=>child.state.name,child=>child.state);
            Assert.That(legacyStates["Jump"].speed,Is.EqualTo(3.5f));Assert.That(legacyStates["Land"].speed,Is.EqualTo(3));
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Installed airborne review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;session.View.enabled=false;
            var soles=new Dictionary<int,RetreatPlaybackReview.FootSurface>();
            var controller=Resources.Load<AnimatorOverrideController>("Art/ContenderRetreat");
            foreach(var actor in session.Actors)
            {
                actor.Controller.enabled=false;actor.Presentation.enabled=false;actor.Command=default;actor.Slide=default;
                actor.transform.SetPositionAndRotation(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12),Quaternion.identity);actor.Presentation.transform.localRotation=Quaternion.identity;
                foreach(var item in actor.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
                var animator=actor.Presentation.Animator;Assert.That(animator.runtimeAnimatorController,Is.EqualTo(controller));Assert.That(animator.applyRootMotion,Is.False);
                animator.Rebind();animator.speed=0;animator.keepAnimatorStateOnDisable=true;animator.enabled=false;
                soles.Add(actor.Id,new RetreatPlaybackReview.FootSurface(actor));
            }
            var camera=new GameObject("Owned airborne reference camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.orthographic=true;camera.orthographicSize=1.55f;camera.aspect=16f/9;
            camera.transform.position=new Vector3(3.2f,1.8f,16.8f);camera.transform.LookAt(new Vector3(0,.8f,12));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Owned landing contact reference";floor.layer=30;floor.transform.position=new Vector3(0,-.05f,12);floor.transform.localScale=new Vector3(7,.1f,4);Object.DestroyImmediate(floor.GetComponent<Collider>());
            var material=new Material(Resources.Load<Material>("Art/StoneCover-Stone"));floor.GetComponent<Renderer>().sharedMaterial=material;
            var fill=new GameObject("Owned airborne fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.shadows=LightShadows.None;fill.cullingMask=1<<30;fill.transform.rotation=Quaternion.Euler(25,200,0);
            var target=new RenderTexture(1920,1080,24);target.Create();var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            string folder=Path.GetFullPath("../Validation/AirbornePlayback");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,clip,phase,characterId,minimumSoleY,hipsY,leftWristError,rightWristError,rootX,rootY,rootZ\n");
            int previousRate=Time.captureFramerate;Time.captureFramerate=60;int outputFrame=0;
            try
            {
                foreach(var clip in new[]{("Jump","Takeoff",18,false),("Air","AirborneHold",36,true),("Land","LandingRecover",30,false)})
                    for(int frame=0;frame<180;frame++,outputFrame++)
                    {
                        int cycle=clip.Item3+60;int frameInCycle=frame%cycle;
                        float phase=clip.Item4?Mathf.Repeat(frame/(float)clip.Item3,1):Mathf.Min(frameInCycle/(float)clip.Item3,.9999f);
                        foreach(var actor in session.Actors)
                        {
                            var animator=actor.Presentation.Animator;animator.SetFloat("Speed",0);animator.SetFloat("Gait",1);animator.SetBool("Grounded",clip.Item1=="Land");animator.SetBool("Dead",false);
                            animator.Play(clip.Item1,0,phase);animator.Update(0);actor.Presentation.ApplyCombatPose();actor.Presentation.Hands.Pose(0);
                            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(phase).Within(.001f));
                            float sole=soles[actor.Id].MinimumWorldHeight();
                            Assert.That(sole,Is.GreaterThan(-.005f),clip.Item2+" boots must not penetrate the reference floor");
                            if(clip.Item1=="Land")Assert.That(sole,Is.LessThan(.035f),"Landing plants both feet while the hips absorb impact");
                            if(clip.Item1=="Air")Assert.That(sole,Is.GreaterThan(.04f),"Suspension must have visible foot clearance");
                            float left=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,actor.Presentation.LeftGrip.position),right=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,actor.Presentation.RightGrip.position);
                            Assert.That(Mathf.Max(left,right),Is.LessThan(.025f));
                            var position=actor.transform.position;Assert.That(position,Is.EqualTo(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12)));
                            csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F3},{9:F3},{10:F3}\n",outputFrame,clip.Item2,phase,actor.CharacterId,sole,animator.GetBoneTransform(HumanBodyBones.Hips).position.y,left,right,position.x,position.y,position.z);
                        }
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"frame-"+outputFrame.ToString("D4")+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                File.WriteAllText(Path.Combine(folder,"Playback.csv"),csv.ToString());
            }
            finally
            { Time.captureFramerate=previousRate;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);Object.Destroy(floor);Object.Destroy(material);Object.Destroy(fill.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
