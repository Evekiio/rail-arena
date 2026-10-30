using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class RetreatPlaybackReview
    {
        sealed class FootSurface
        {
            readonly SkinnedMeshRenderer skin;
            readonly Vector3[] vertices;
            readonly BoneWeight[] weights;
            readonly Matrix4x4[] bindposes, matrices;
            readonly int[] points;
            public FootSurface(ArenaActor actor)
            {
                skin=(SkinnedMeshRenderer)actor.Presentation.BodyRenderers[0];vertices=skin.sharedMesh.vertices;weights=skin.sharedMesh.boneWeights;bindposes=skin.sharedMesh.bindposes;matrices=new Matrix4x4[skin.bones.Length];
                var footBones=new System.Collections.Generic.HashSet<int>();
                for(int index=0;index<skin.bones.Length;index++)if(skin.bones[index].name.Contains("Foot")||skin.bones[index].name.Contains("Toe"))footBones.Add(index);
                var selected=new System.Collections.Generic.List<int>();
                for(int index=0;index<weights.Length;index++)
                {
                    var w=weights[index];float total=(footBones.Contains(w.boneIndex0)?w.weight0:0)+(footBones.Contains(w.boneIndex1)?w.weight1:0)+(footBones.Contains(w.boneIndex2)?w.weight2:0)+(footBones.Contains(w.boneIndex3)?w.weight3:0);
                    if(total>.9f)selected.Add(index);
                }
                points=selected.ToArray();Assert.That(points.Length,Is.GreaterThan(20),"Actual weighted boot geometry is required for contact review");
            }
            public float MinimumWorldHeight()
            {
                for(int index=0;index<matrices.Length;index++)matrices[index]=skin.bones[index].localToWorldMatrix*bindposes[index];
                float minimum=float.PositiveInfinity;
                foreach(int index in points)
                {
                    var v=vertices[index];var w=weights[index];
                    var world=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                    minimum=Mathf.Min(minimum,world.y);
                }
                return minimum;
            }
        }
        [UnityTest]
        public IEnumerator CaptureInstalledRetreatForAllFourIdentities()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Installed retreat motion review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;session.View.enabled=false;
            var controller=Resources.Load<RuntimeAnimatorController>("Art/ContenderRetreat");
            Assert.That(controller,Is.Not.Null);Assert.That(session.Actors.Count,Is.EqualTo(4));
            var soles=new System.Collections.Generic.Dictionary<int,FootSurface>();
            foreach(var actor in session.Actors)
            {
                actor.Controller.enabled=false;actor.Presentation.enabled=false;actor.Presentation.Weapon.gameObject.SetActive(true);
                actor.Command=default;actor.Slide=default;
                actor.transform.SetPositionAndRotation(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12),Quaternion.identity);actor.Presentation.transform.localRotation=Quaternion.identity;
                foreach(var item in actor.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
                var animator=actor.Presentation.Animator;Assert.That(animator.runtimeAnimatorController,Is.EqualTo(controller));Assert.That(animator.applyRootMotion,Is.False);
                animator.Rebind();animator.speed=0;animator.keepAnimatorStateOnDisable=true;animator.enabled=false;
                soles.Add(actor.Id,new FootSurface(actor));
            }
            var camera=new GameObject("Owned retreat family camera").AddComponent<Camera>();camera.enabled=false;
            camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);
            camera.orthographic=true;camera.orthographicSize=1.55f;camera.aspect=16f/9;
            camera.transform.position=new Vector3(3.2f,1.8f,16.8f);camera.transform.LookAt(new Vector3(0,.8f,12));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Owned visual contact floor";floor.layer=30;
            floor.transform.position=new Vector3(0,-.05f,12);floor.transform.localScale=new Vector3(7,.1f,4);Object.DestroyImmediate(floor.GetComponent<Collider>());
            var material=new Material(Resources.Load<Material>("Art/StoneCover-Stone"));floor.GetComponent<Renderer>().sharedMaterial=material;
            var fill=new GameObject("Owned family portrait fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.shadows=LightShadows.None;fill.cullingMask=1<<30;
            fill.transform.rotation=Quaternion.Euler(25,200,0);
            var target=new RenderTexture(1920,1080,24);target.Create();var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            string folder=Path.GetFullPath("../Validation/RetreatPlayback");Directory.CreateDirectory(folder);
            var csv=new System.Text.StringBuilder("frame,clip,phase,characterId,leftAnkleY,rightAnkleY,rootX,rootY,rootZ,minimumSoleY,leftWristError,rightWristError\n");
            int previousRate=Time.captureFramerate;Time.captureFramerate=60;int outputFrame=0;
            try
            {
                foreach(var gait in new[]{("RetreatWalk",-1.5f,.6f),("RetreatJog",-4f,20f/60),("RetreatRun",-9f,.3f)})
                    for(int frame=0;frame<180;frame++,outputFrame++)
                    {
                        float phase=Mathf.Repeat(frame/(60*gait.Item3),1);
                        foreach(var actor in session.Actors)
                        {
                            var animator=actor.Presentation.Animator;animator.SetFloat("Speed",gait.Item2);animator.SetFloat("Gait",1);animator.SetBool("Grounded",true);animator.SetBool("Dead",false);
                            animator.Play("Locomotion",0,phase);animator.Update(0);actor.Presentation.ApplyCombatPose();actor.Presentation.Hands.Pose(0);
                            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(phase).Within(.001f),"Manual pose time must remain exact while automatic Animator updates are disabled");
                            float left=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,right=animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y;
                            Assert.That(Mathf.Min(left,right),Is.GreaterThan(.04f),gait.Item1+" retargeted ankles must remain above the deck");
                            float sole=soles[actor.Id].MinimumWorldHeight();Assert.That(sole,Is.InRange(-.005f,.035f),gait.Item1+" actual boot sole must stay at the deck");
                            float leftWrist=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,actor.Presentation.LeftGrip.position),rightWrist=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,actor.Presentation.RightGrip.position);
                            Assert.That(Mathf.Max(leftWrist,rightWrist),Is.LessThan(.025f),"Live wrist solve must reach the rifle grips");
                            var position=actor.transform.position;Assert.That(position,Is.EqualTo(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12)),"Clip must not move the gameplay root");
                            csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F6},{3},{4:F6},{5:F6},{6:F3},{7:F3},{8:F3},{9:F6},{10:F6},{11:F6}\n",outputFrame,gait.Item1,phase,actor.CharacterId,left,right,position.x,position.y,position.z,sole,leftWrist,rightWrist);
                        }
                        // Allow native skinning to consume the post-Animator wrist/finger transforms before capture.
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"frame-"+outputFrame.ToString("D4")+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                File.WriteAllText(Path.Combine(folder,"Playback.csv"),csv.ToString());
            }
            finally
            {
                Time.captureFramerate=previousRate;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);Object.Destroy(floor);Object.Destroy(material);Object.Destroy(fill.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
