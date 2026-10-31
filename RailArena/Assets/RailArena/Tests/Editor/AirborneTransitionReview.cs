using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class AirborneTransitionReview
    {
        [UnityTest]
        public IEnumerator CaptureControllerDrivenJumpAndRecovery()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,0,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Controller-driven airborne review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;session.View.enabled=false;Assert.That(session.Paused,Is.False);
            var actor=session.Human;actor.Presentation.enabled=false;
            var animator=actor.Presentation.Animator;animator.keepAnimatorStateOnDisable=true;animator.enabled=false;Assert.That(animator.applyRootMotion,Is.False);
            foreach(var item in actor.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Owned collision deck";floor.layer=0;floor.transform.position=new Vector3(300,-.05f,12);floor.transform.localScale=new Vector3(10,.1f,100);
            var material=new Material(Resources.Load<Material>("Art/StoneCover-Stone"));floor.GetComponent<Renderer>().sharedMaterial=material;Physics.SyncTransforms();
            var camera=new GameObject("Owned ballistic review camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=(1<<0)|(1<<30);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.orthographic=true;camera.orthographicSize=2;camera.aspect=16f/9;
            var fill=new GameObject("Owned ballistic fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(25,200,0);
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var update=typeof(ContenderPresentation).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
            var soles=new RetreatPlaybackReview.FootSurface(actor);
            string folder=Path.GetFullPath(System.Environment.GetEnvironmentVariable("RA_AIRBORNE_REVIEW_OUTPUT")??"../Validation/AirborneTransitions");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,scenario,rootX,rootY,rootZ,verticalVelocity,grounded,state,normalizedTime,minimumSoleY\n");
            int oldRate=Time.captureFramerate;Time.captureFramerate=60;int outputFrame=0;
            try
            {
                foreach(var scenario in new[]{("standing",0f),("forward",1f),("retreat",-1f)})
                {
                    actor.Place(new Vector3(300,.2f,12));actor.Command=default;
                    for(int tick=0;tick<30;tick++)actor.Move(session.Tuning);
                    Assert.That(actor.Grounded,Is.True);float baseY=actor.transform.position.y,maximumY=baseY;
                    animator.Rebind();animator.Play("Locomotion",0,0);animator.Update(0);
                    var observed=new HashSet<string>();bool airborne=false,landed=false;
                    for(int frame=0;frame<120;frame++,outputFrame++)
                    {
                        actor.Command=new MovementCommand{Move=new Vector2(0,scenario.Item2),Jump=frame==20};
                        actor.Move(session.Tuning);update.Invoke(actor.Presentation,null);animator.Update(1f/60);actor.Presentation.ApplyCombatPose();actor.Presentation.Hands.Pose(0);
                        maximumY=Mathf.Max(maximumY,actor.transform.position.y);airborne|=frame>20&&!actor.Grounded;landed|=airborne&&actor.Grounded;
                        var state=animator.GetCurrentAnimatorStateInfo(0);string name="Other";
                        foreach(string candidate in new[]{"Locomotion","Jump","Air","Land"})if(state.IsName(candidate)){name=candidate;observed.Add(candidate);break;}
                        float sole=soles.MinimumWorldHeight();
                        var position=actor.transform.position;
                        csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3:F6},{4:F6},{5:F6},{6},{7},{8:F6},{9:F6}\n",outputFrame,scenario.Item1,position.x,position.y,position.z,actor.Velocity.y,actor.Grounded?1:0,name,state.normalizedTime,sole);
                        Assert.That(sole,Is.GreaterThan(-.015f),"Authored gaits and airborne states must remain above the actual collision deck");
                        camera.transform.position=new Vector3(303,1.9f,position.z+4.8f);camera.transform.LookAt(new Vector3(300,1.3f,position.z));
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"frame-"+outputFrame.ToString("D4")+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                    Assert.That(landed,Is.True,scenario.Item1+" must complete the simulated jump");
                    foreach(string name in new[]{"Locomotion","Jump","Air","Land"})Assert.That(observed,Does.Contain(name),scenario.Item1+" must use the installed state graph");
                    float theoreticalHeight=session.Tuning.JumpSpeed*session.Tuning.JumpSpeed/(2*session.Tuning.Gravity);
                    Assert.That(maximumY-baseY,Is.EqualTo(theoreticalHeight).Within(.12f),"Root height must follow existing discrete ballistic physics");
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),Is.True,"Landing must recover while stationary or moving");
                }
                File.WriteAllText(Path.Combine(folder,"Playback.csv"),csv.ToString());
            }
            finally
            { File.WriteAllText(Path.Combine(folder,"Playback.csv"),csv.ToString());Time.captureFramerate=oldRate;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);Object.Destroy(floor);Object.Destroy(material);Object.Destroy(fill.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
