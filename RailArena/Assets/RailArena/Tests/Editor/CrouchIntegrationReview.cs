using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CrouchIntegrationReview
    {
        [UnityTest]public IEnumerator GroundedCrouchPreservesImmediateStanceAndContacts()
        {
            var graph=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Art/ContenderMotion.controller");
            var crouch=graph.layers[0].stateMachine.states.Single(item=>item.state.name=="Crouch").state;
            Assert.That(crouch.tag,Is.EqualTo("AuthoredCrouch"));Assert.That(crouch.speed,Is.EqualTo(1));Assert.That(crouch.speedParameterActive,Is.False);
            var tree=(BlendTree)crouch.motion;Assert.That(tree.children.Length,Is.EqualTo(5));Assert.That(tree.children.All(child=>child.timeScale==1),Is.True);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session,0);
            session.StartSession(true,3,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();
            new GameObject("Installed crouch integration review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            session.enabled=false;session.View.enabled=false;
            var surfaces=session.Actors.ToDictionary(actor=>actor.Id,actor=>new CrouchCandidateArtReview.Surface(actor));
            var boxes=session.Actors.SelectMany(actor=>actor.GetComponentsInChildren<Hitbox>(true)).ToDictionary(box=>box,box=>(json:EditorJsonUtility.ToJson(box.GetComponent<Collider>()),position:box.transform.localPosition,rotation:box.transform.localRotation,scale:box.transform.localScale));
            var csv=new StringBuilder("characterId,aimYaw,directionX,directionZ,phase,minimumSkinY,maximumSkinY,leftWristError,rightWristError,controllerHeight\n");
            var failures=new List<string>();
            foreach(var actor in session.Actors)
            {
                actor.Controller.enabled=false;actor.Presentation.enabled=false;actor.GroundedOverride=true;actor.AirborneTicks=0;
                actor.transform.SetPositionAndRotation(new Vector3(300+actor.Id*2,actor.Controller.skinWidth,12),Quaternion.identity);
                actor.Presentation.Animator.Rebind();actor.Presentation.Animator.speed=0;actor.Presentation.Animator.enabled=false;
            }
            foreach(float yaw in new[]{0f,90f})
            foreach(var direction in new[]{Vector2.zero}.Concat(Enumerable.Range(0,16).Select(index=>new Vector2(Mathf.Sin(index*Mathf.PI/8),Mathf.Cos(index*Mathf.PI/8)))))
            foreach(float phase in new[]{0f,.25f,.5f,.75f})
            foreach(var actor in session.Actors)
            {
                actor.Command=new Simulation.MovementCommand{Yaw=yaw,Move=direction,Crouch=true};actor.GroundedOverride=true;
                actor.Velocity=Quaternion.Euler(0,yaw,0)*new Vector3(direction.x,0,direction.y)*3.6f;
                var root=actor.transform.position;var velocity=actor.Velocity;
                actor.Presentation.ApplyMotionState(0);actor.SetCrouched(true);
                var animator=actor.Presentation.Animator;animator.Play("Crouch",0,phase);animator.Update(0);actor.Presentation.ApplyCombatPose();actor.Presentation.Hands.Pose(0);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.True);
                var height=surfaces[actor.Id].Heights();
                float left=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position,actor.Presentation.LeftGrip.position);
                float right=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position,actor.Presentation.RightGrip.position);
                if(height.x<-.005f||height.x>.04f||height.y>1.35f)failures.Add(actor.CharacterId+" direction "+direction+" phase "+phase+" skin "+height+" lowest bone "+surfaces[actor.Id].LowestBone);
                if(Mathf.Max(left,right)>.025f)failures.Add(actor.CharacterId+" grip error "+Mathf.Max(left,right));
                Assert.That(actor.Controller.height,Is.EqualTo(ArenaActor.CrouchingHeight));Assert.That(actor.transform.position,Is.EqualTo(root));Assert.That(actor.Velocity,Is.EqualTo(velocity));
                csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F0},{2:F4},{3:F4},{4:F2},{5:F6},{6:F6},{7:F6},{8:F6},{9:F2}\n",actor.CharacterId,yaw,direction.x,direction.y,phase,height.x,height.y,left,right,actor.Controller.height);
                actor.SetCrouched(false);
                Assert.That(actor.Controller.height,Is.EqualTo(ArenaActor.StandingHeight));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.False,"Stand-up must update the pose in the same setter call");
                Assert.That(animator.GetBoneTransform(HumanBodyBones.Head).position.y,Is.GreaterThan(1.35f),"The head must not remain crouched after authoritative stand-up");
                actor.SetCrouched(true);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.True,"Crouch pose must update in the same setter call");
            }
            string output=Path.GetFullPath("../Validation/CrouchIntegration");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"Poses.csv"),csv.ToString());File.WriteAllLines(Path.Combine(output,"Failures.txt"),failures);
            Assert.That(failures,Is.Empty,string.Join("\n",failures.Take(20)));
            var lifecycle=new StringBuilder("characterId,airborneFallback,slideFallback,deathState,respawnState,legacyLowering\n");
            var legacy=Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            foreach(var actor in session.Actors)
            {
                var presentation=actor.Presentation;var animator=presentation.Animator;var artController=animator.runtimeAnimatorController;
                actor.Command=default;actor.GroundedOverride=false;actor.AirborneTicks=1;actor.Velocity=Vector3.up*5;
                presentation.ApplyMotionState(0);presentation.ApplyCombatPose();Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"),Is.True);
                Assert.That(actor.Controller.height,Is.EqualTo(ArenaActor.CrouchingHeight));
                actor.GroundedOverride=true;actor.AirborneTicks=0;actor.Velocity=Vector3.forward*9;actor.Slide=new Simulation.SlideState{RemainingTicks=12};
                presentation.ApplyMotionState(0);presentation.ApplyCombatPose();Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.False);
                Assert.That(actor.Slide.RemainingTicks,Is.EqualTo(12),"Presentation must not advance the slide simulation");
                actor.Slide=default;presentation.ApplyMotionState(0);presentation.ApplyCombatPose();Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.True);
                actor.Health=0;presentation.SetAlive(false);animator.speed=1;
                for(int frame=0;frame<8;frame++){presentation.ApplyMotionState(1f/60);animator.Update(1f/60);}
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredDeath"),Is.True);Assert.That(animator.GetLayerWeight(1),Is.Zero);Assert.That(presentation.Weapon.gameObject.activeSelf,Is.False);
                var root=actor.transform.position;actor.Place(root);actor.Controller.enabled=false;actor.Health=100;presentation.SetAlive(true);actor.GroundedOverride=true;
                presentation.ApplyMotionState(0);presentation.ApplyCombatPose();Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredCrouch"),Is.False);
                Assert.That(presentation.Weapon.gameObject.activeSelf,Is.True);Assert.That(actor.IsCrouched,Is.False);
                presentation.SetMotionController(legacy);animator.Rebind();animator.Update(0);presentation.ApplyMotionState(0);presentation.ApplyCombatPose();
                float standingHip=animator.GetBoneTransform(HumanBodyBones.Hips).position.y;actor.SetCrouched(true);
                float lowering=standingHip-animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
                Assert.That(lowering,Is.EqualTo(ArenaActor.StandingHeight-ArenaActor.CrouchingHeight).Within(.0001f));
                Assert.That(animator.parameters.Any(parameter=>parameter.name=="AuthoredCrouch"),Is.False);
                lifecycle.AppendFormat(CultureInfo.InvariantCulture,"{0},true,true,true,true,{1:F6}\n",actor.CharacterId,lowering);
                presentation.SetMotionController(artController);animator.Rebind();actor.SetCrouched(false);
            }
            foreach(var box in boxes)
            {
                Assert.That(EditorJsonUtility.ToJson(box.Key.GetComponent<Collider>()),Is.EqualTo(box.Value.json));
                Assert.That(box.Key.transform.localPosition,Is.EqualTo(box.Value.position));Assert.That(box.Key.transform.localRotation,Is.EqualTo(box.Value.rotation));Assert.That(box.Key.transform.localScale,Is.EqualTo(box.Value.scale));
            }
            File.WriteAllText(Path.Combine(output,"Lifecycle.csv"),lifecycle.ToString());
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
