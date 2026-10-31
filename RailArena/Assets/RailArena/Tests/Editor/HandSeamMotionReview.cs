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
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class HandSeamMotionReview
    {
        sealed class Anchor { public int bone; public Vector3 point; }
        sealed class Joint { public string name; public Anchor end,start; }
        [Serializable] sealed class Report { public int framesPerStage=180,jointSamples; public float beforeMaximumGap,afterMaximumGap,maximumWristError; public bool passed; public string scope="Player appearance; same frozen actor/Animator state and camera; cosmetic equip, ADS, recoil articulation sweep and authored two-second reload. Before uses the previous legacy-bind calibration on an owned clone. After uses installed corrected skin. Not new action authoring, native input/audio or artistic acceptance."; }
        byte[] quality;string pipelinePath;
        static Anchor Ring(Mesh mesh,int index,bool end)
        {
            var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[index];
            var selected=Enumerable.Range(0,vertices.Length).Where(i=>weights[i].boneIndex0==index&&weights[i].weight0>.9999f).ToArray();
            Assert.That(selected.Length,Is.GreaterThan(10));
            float plane=end?selected.Max(i=>bind.MultiplyPoint3x4(vertices[i]).y):selected.Min(i=>bind.MultiplyPoint3x4(vertices[i]).y);
            var points=selected.Where(i=>Mathf.Abs(bind.MultiplyPoint3x4(vertices[i]).y-plane)<1e-7f).Select(i=>vertices[i]).Distinct().ToArray();
            Assert.That(points.Length,Is.GreaterThanOrEqualTo(6));
            return new Anchor{bone=index,point=points.Aggregate(Vector3.zero,(sum,value)=>sum+value)/points.Length};
        }
        static Joint[] Joints(SkinnedMeshRenderer surface)
        {
            var result=new List<Joint>();
            foreach(string side in new[]{"Left","Right"}) foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Pinky"}) for(int segment=1;segment<3;segment++)
            {
                string name=side+"Hand"+finger;int a=Array.FindIndex(surface.bones,bone=>bone.name==name+segment),b=Array.FindIndex(surface.bones,bone=>bone.name==name+(segment+1));
                Assert.That(Mathf.Min(a,b),Is.GreaterThanOrEqualTo(0));result.Add(new Joint{name=name+segment,end=Ring(surface.sharedMesh,a,true),start=Ring(surface.sharedMesh,b,false)});
            }
            return result.ToArray();
        }
        static Vector3 World(Anchor anchor,SkinnedMeshRenderer surface,Matrix4x4[] binds)=>surface.bones[anchor.bone].localToWorldMatrix.MultiplyPoint3x4(binds[anchor.bone].MultiplyPoint3x4(anchor.point));
        [UnityTest] public IEnumerator ContactsRemainCoherentThroughVisualActions()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipelinePath=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();session.enabled=false;session.View.enabled=false;
            foreach(var actor in session.Actors){actor.Controller.enabled=false;actor.Presentation.enabled=false;}
            var slice=new GameObject("Hand seam action review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            var camera=session.View;var arms=camera.GetComponentInChildren<FirstPersonArms>(true);var weapon=arms.GetComponentInParent<RailgunPresentation>(true);weapon.gameObject.SetActive(true);yield return null;
            var characters=Resources.Load<ContenderArtSet>("Art/Contenders");arms.GetComponent<ContenderArtPresentation>().ApplyArms(arms,characters.variants[0]);yield return null;
            arms.Animator.enabled=false;var surface=arms.Surface;var corrected=surface.sharedMesh;var joints=Joints(surface);
            var legacy=(SkinnedMeshRenderer)AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/Contender.prefab").GetComponent<ContenderPresentation>().BodyRenderers[0];
            var before=Object.Instantiate(corrected);before.name="Owned before-calibration hand review";before.bindposes=surface.bones.Select(bone=>legacy.sharedMesh.bindposes[Array.FindIndex(legacy.bones,item=>item.name==bone.name)]).ToArray();
            var reload=weapon.GetComponent<RifleReloadPresentation>();Assert.That(reload.Initialized,Is.True);reload.enabled=false;
            var positions=surface.bones.Select(bone=>bone.localPosition).ToArray();var rotations=surface.bones.Select(bone=>bone.localRotation).ToArray();var scales=surface.bones.Select(bone=>bone.localScale).ToArray();
            Vector3 actorRoot=session.Human.transform.position,velocity=session.Human.Velocity;float height=session.Human.Controller.height;int tick=session.Match.Tick,rounds=session.Human.Magazine.Rounds,nextShot=session.Human.NextShotTick;
            string folder=Path.GetFullPath("../Validation/HandBindingMotion-01");Assert.That(Directory.Exists(folder),Is.False);Directory.CreateDirectory(folder);
            var report=new Report();var csv=new StringBuilder("stage,frame,phase,joint,gapMetres,leftWristError,rightWristError\n");
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);int rate=Time.captureFramerate;float fov=camera.fieldOfView;Time.captureFramerate=60;
            try
            {
                camera.transform.SetPositionAndRotation(session.Human.Eye,Quaternion.Euler(session.Human.Command.Pitch,session.Human.Command.Yaw,0));
                for(int stage=0;stage<2;stage++)
                {
                    string label=stage==0?"before":"after";Directory.CreateDirectory(Path.Combine(folder,label));surface.sharedMesh=stage==0?before:corrected;var binds=surface.sharedMesh.bindposes;
                    for(int index=0;index<surface.bones.Length;index++){surface.bones[index].localPosition=positions[index];surface.bones[index].localRotation=rotations[index];surface.bones[index].localScale=scales[index];}
                    reload.ResetPose();var sight=new AimDownSights();var motion=new FirstPersonWeaponMotion{EquipArt=Resources.Load<FirstPersonEquipArt>("Art/FirstPersonEquip")};motion.Reset(true);
                    for(int frame=0;frame<180;frame++)
                    {
                        string phase=frame<18?"equip":frame<48?"ADS-recoil-sweep":frame<168?"reload":"settled";
                        float recoil=frame>=24&&frame<48?Mathf.Sin((frame-24)/23f*Mathf.PI):0;
                        sight.Step(frame>=18&&frame<48,1f/60);motion.Step(1f/60,Vector3.zero,true,Vector2.zero,recoil);
                        weapon.transform.localPosition=sight.Position(weapon.transform.InverseTransformPoint(weapon.Aim.position),motion.Offset,recoil);weapon.transform.localRotation=sight.Rotation(motion.Rotation);
                        camera.fieldOfView=Mathf.Lerp(session.Aim.FieldOfView,session.Aim.ViewFov(true),sight.Weight);weapon.SetCharge(recoil);arms.SetRecoil(recoil);
                        if(frame>=48&&frame<168)reload.PoseAt((frame-48)/119f);else reload.ResetPose();
                        yield return null;
                        float left=Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position,weapon.LeftGrip.position),right=Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position,weapon.RightGrip.position);
                        Assert.That(Mathf.Max(left,right),Is.LessThan(.025f));report.maximumWristError=Mathf.Max(report.maximumWristError,left,right);
                        foreach(var joint in joints)
                        {
                            float gap=Vector3.Distance(World(joint.end,surface,binds),World(joint.start,surface,binds));report.jointSamples++;
                            if(stage==0)report.beforeMaximumGap=Mathf.Max(report.beforeMaximumGap,gap);else{report.afterMaximumGap=Mathf.Max(report.afterMaximumGap,gap);Assert.That(gap,Is.LessThan(.0005f),joint.name+" "+phase+" frame "+frame);}
                            csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F8},{5:F8},{6:F8}\n",label,frame,phase,joint.name,gap,left,right);
                        }
                        Assert.That(session.Human.transform.position,Is.EqualTo(actorRoot));Assert.That(session.Human.Velocity,Is.EqualTo(velocity));Assert.That(session.Human.Controller.height,Is.EqualTo(height));Assert.That(session.Match.Tick,Is.EqualTo(tick));Assert.That(session.Human.Magazine.Rounds,Is.EqualTo(rounds));Assert.That(session.Human.NextShotTick,Is.EqualTo(nextShot));
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,label,$"frame-{frame:D4}.png"),pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
                    }
                }
                Assert.That(report.beforeMaximumGap,Is.GreaterThan(.01f),"The old calibration must reproduce visible separation");report.passed=true;File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Path.Combine(folder,"contacts.csv"),csv.ToString());
            }
            finally{surface.sharedMesh=corrected;reload.ResetPose();arms.SetRecoil(0);weapon.SetCharge(0);Time.captureFramerate=rate;camera.fieldOfView=fov;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(before);}
            Object.Destroy(slice);yield return null;yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(quality!=null){QualitySettings.renderPipeline=string.IsNullOrEmpty(pipelinePath)?null:AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.Refresh();}
        }
    }
}
