using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CrouchCandidateArtReview
    {
        byte[] priorQuality;
        RenderPipelineAsset priorPipeline;
        internal sealed class Surface
        {
            readonly SkinnedMeshRenderer skin;
            readonly Vector3[] vertices;
            readonly BoneWeight[] weights;
            readonly Matrix4x4[] bindposes,matrices;
            public string LowestBone { get; private set; }
            public Surface(ArenaActor actor)
            {
                skin=(SkinnedMeshRenderer)actor.Presentation.BodyRenderers[0]; vertices=skin.sharedMesh.vertices;
                weights=skin.sharedMesh.boneWeights;bindposes=skin.sharedMesh.bindposes;matrices=new Matrix4x4[skin.bones.Length];
                Assert.That(vertices.Length,Is.GreaterThan(10000),"Review the complete imported body skin");
            }
            public Vector2 Heights()
            {
                for(int i=0;i<matrices.Length;i++)matrices[i]=skin.bones[i].localToWorldMatrix*bindposes[i];
                float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
                for(int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i];var w=weights[i];
                    float y=(matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3).y;
                    if(y<minimum)
                    {
                        minimum=y;int bone=w.boneIndex0;float weight=w.weight0;
                        if(w.weight1>weight){bone=w.boneIndex1;weight=w.weight1;}if(w.weight2>weight){bone=w.boneIndex2;weight=w.weight2;}if(w.weight3>weight)bone=w.boneIndex3;
                        LowestBone=skin.bones[bone].name;
                    }
                    maximum=Mathf.Max(maximum,y);
                }
                return new Vector2(minimum,maximum);
            }
        }
        [UnityTest] public IEnumerator CaptureNativeCandidatesWithoutInstallingStance()=>Capture(false);
        [UnityTest] public IEnumerator CaptureInstalledGroundCrouch()=>Capture(true);
        internal IEnumerator Capture(bool installed,string[] clipNames=null,string outputDirectory=null,bool standing=false)
        {
            priorQuality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");priorPipeline=QualitySettings.renderPipeline;
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            byte[] graphSource=File.ReadAllBytes("Assets/Resources/Art/ContenderMotion.controller");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Native crouch candidate review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;session.View.enabled=false;
            Assert.That(session.Actors.Count,Is.EqualTo(4));
            var surfaces=new Dictionary<int,Surface>();var graphs=new Dictionary<int,PlayableGraph>();
            var originals=new Dictionary<int,RuntimeAnimatorController>();
            foreach(var actor in session.Actors)
            {
                actor.Controller.enabled=false;actor.Presentation.enabled=false;actor.SetCrouched(false);actor.Command=default;actor.Slide=default;actor.Velocity=Vector3.zero;actor.GroundedOverride=true;actor.AirborneTicks=0;
                actor.transform.SetPositionAndRotation(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12),Quaternion.identity);
                foreach(var item in actor.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
                var animator=actor.Presentation.Animator;originals[actor.Id]=animator.runtimeAnimatorController;
                if(!installed)actor.Presentation.SetMotionController(null);
                animator.applyRootMotion=false;animator.enabled=!installed;animator.speed=installed?0:1;
                if(installed){animator.Rebind();actor.SetCrouched(!standing);}
                actor.Presentation.Hands.enabled=false;surfaces[actor.Id]=new Surface(actor);
            }
            var camera=new GameObject("Owned crouch family camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.18f,.22f);camera.orthographic=true;camera.orthographicSize=1.5f;camera.aspect=16f/9;
            camera.transform.position=new Vector3(3.2f,1.8f,16.8f);camera.transform.LookAt(new Vector3(0,.8f,12));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Owned crouch floor";floor.layer=30;
            floor.transform.position=new Vector3(0,-.05f,12);floor.transform.localScale=new Vector3(7,.1f,4);Object.DestroyImmediate(floor.GetComponent<Collider>());
            var material=new Material(Resources.Load<Material>("Art/StoneCover-Stone"));floor.GetComponent<Renderer>().sharedMaterial=material;
            var fill=new GameObject("Owned crouch portrait fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.shadows=LightShadows.None;fill.cullingMask=1<<30;fill.transform.rotation=Quaternion.Euler(25,200,0);
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            string folder=Path.GetFullPath(outputDirectory??Environment.GetEnvironmentVariable("RA_CROUCH_REVIEW_OUTPUT")??(installed?"../Validation/CrouchInstalledPlayback":"../Validation/CrouchCandidatePlayback"));Directory.CreateDirectory(folder);
            var csv=new StringBuilder("frame,clip,sourceFrame,characterId,minimumSkinY,maximumSkinY,headY,leftWristError,rightWristError,actorX,actorY,actorZ,controllerHeight\n");
            var failures=new List<string>();int outputFrame=0;int previousRate=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                foreach(string name in clipNames??(installed?new[]{"CrouchIdle","CrouchForward","CrouchBackward","CrouchLeft","CrouchRight"}:new[]{"CrouchIdle","CrouchForward","CrouchBackward","CrouchLeft","CrouchRight","CrouchEnter","CrouchExit"}))
                {
                    var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/RailArena/Art/ArtPass/Animation/"+name+".fbx").OfType<AnimationClip>().Single(item=>!item.name.StartsWith("__preview__",StringComparison.Ordinal));
                    var playables=new Dictionary<int,AnimationClipPlayable>();
                    foreach(var actor in session.Actors)
                    {
                        if(installed)continue;
                        if(graphs.TryGetValue(actor.Id,out var old))old.Destroy();
                        var graph=PlayableGraph.Create("Owned crouch "+actor.CharacterId);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable=AnimationClipPlayable.Create(graph,clip);playable.SetSpeed(0);playable.SetApplyFootIK(false);
                        var output=AnimationPlayableOutput.Create(graph,"Body",actor.Presentation.Animator);output.SetSourcePlayable(playable);graph.Play();
                        graphs[actor.Id]=graph;playables[actor.Id]=playable;
                    }
                    int frames=Mathf.RoundToInt(clip.length*60);
                    for(int frame=0;frame<(installed&&!standing?120:frames+1);frame++,outputFrame++)
                    {
                        foreach(var actor in session.Actors)
                        {
                            if(installed)
                            {
                                actor.Velocity=(name=="CrouchForward"?Vector3.forward:name=="CrouchBackward"?Vector3.back:name=="CrouchLeft"?Vector3.left:name=="CrouchRight"?Vector3.right:Vector3.zero)*3.6f;
                                actor.Presentation.ApplyMotionState(0);actor.Presentation.Animator.Play(standing?"Locomotion":"Crouch",0,Mathf.Repeat(frame/(float)frames,1));actor.Presentation.Animator.Update(0);
                                Assert.That(Array.Exists(actor.Presentation.Animator.GetCurrentAnimatorClipInfo(0),item=>item.clip.name==name&&item.weight>.999f),Is.True);
                            }
                            else{playables[actor.Id].SetTime(frame/60d);graphs[actor.Id].Evaluate(0);}
                            actor.Presentation.ApplyCombatPose();actor.Presentation.Hands.Pose(0);
                            var animator=actor.Presentation.Animator;var heights=surfaces[actor.Id].Heights();
                            float left=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,actor.Presentation.LeftGrip.position);
                            float right=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,actor.Presentation.RightGrip.position);
                            if(heights.x<-.005f||heights.x>.035f)failures.Add(name+" frame "+frame+" character "+actor.CharacterId+" skin floor "+heights.x);
                            if(name.StartsWith("Crouch",StringComparison.Ordinal)&&!name.EndsWith("Enter",StringComparison.Ordinal)&&!name.EndsWith("Exit",StringComparison.Ordinal)&&heights.y>1.35f)failures.Add(name+" crouch height "+heights.y);
                            if(Mathf.Max(left,right)>.025f)failures.Add(name+" grip error "+Mathf.Max(left,right));
                            var position=actor.transform.position;
                            Assert.That(position,Is.EqualTo(new Vector3((actor.Id-1.5f)*1.4f,actor.Controller.skinWidth,12)));
                            Assert.That(actor.Controller.height,Is.EqualTo(installed&&!standing?ArenaActor.CrouchingHeight:ArenaActor.StandingHeight));
                            csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F3},{10:F3},{11:F3},{12:F3}\n",outputFrame,name,frame,actor.CharacterId,heights.x,heights.y,animator.GetBoneTransform(HumanBodyBones.Head).position.y,left,right,position.x,position.y,position.z,actor.Controller.height);
                        }
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"frame-"+outputFrame.ToString("D4")+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                }
                File.WriteAllText(Path.Combine(folder,"Playback.csv"),csv.ToString());File.WriteAllLines(Path.Combine(folder,"Failures.txt"),failures);
                CollectionAssert.AreEqual(graphSource,File.ReadAllBytes("Assets/Resources/Art/ContenderMotion.controller"),"Candidate review must not edit the live graph");
                Assert.That(failures,Is.Empty,string.Join("\n",failures.Take(20)));
            }
            finally
            {
                foreach(var graph in graphs.Values)if(graph.IsValid())graph.Destroy();
                foreach(var actor in session.Actors)actor.Presentation.Animator.runtimeAnimatorController=originals[actor.Id];
                Time.captureFramerate=previousRate;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);Object.Destroy(floor);Object.Destroy(material);Object.Destroy(fill.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(priorQuality==null)yield break;
            QualitySettings.renderPipeline=priorPipeline;File.WriteAllBytes("ProjectSettings/QualitySettings.asset",priorQuality);AssetDatabase.Refresh();
        }
    }
}
