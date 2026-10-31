using System.Collections;
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
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class DirectionalMotionArtReview
    {
        [UnityTest] public IEnumerator DirectionalBlendsPreserveAimStanceAndAuthority()
        {
            var graph = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Art/ContenderMotion.controller");
            var state = graph.layers[0].stateMachine.states.Single(child => child.state.name == "Locomotion").state;
            var tree = (BlendTree)state.motion;
            Assert.That(tree.blendType, Is.EqualTo(BlendTreeType.FreeformDirectional2D));
            Assert.That(tree.children.Length, Is.EqualTo(13));
            Assert.That(tree.children.All(child => child.timeScale == 1), Is.True);
            Assert.That(state.speed, Is.EqualTo(1)); Assert.That(state.speedParameterActive, Is.False);
            var original = (AnimatorController)Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            var legacy = original.layers[0].stateMachine.states.Single(child => child.state.name == "Locomotion").state;
            Assert.That(((BlendTree)legacy.motion).children.Length, Is.EqualTo(7)); Assert.That(legacy.speedParameterActive, Is.True);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary); while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Installed directional review").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            session.enabled = false; session.View.enabled = false;
            var soles = session.Actors.ToDictionary(actor => actor.Id, actor => new RetreatPlaybackReview.FootSurface(actor));
            foreach (var actor in session.Actors)
            {
                actor.Controller.enabled = false; actor.Presentation.enabled = false;
                actor.transform.SetPositionAndRotation(new Vector3(300 + actor.Id * 2, actor.Controller.skinWidth, 12), Quaternion.identity);
                var animator = actor.Presentation.Animator; animator.Rebind(); animator.speed = 0; animator.keepAnimatorStateOnDisable = true; animator.enabled = false;
                Assert.That(animator.applyRootMotion, Is.False);
            }
            var csv = new StringBuilder("characterId,crouched,aimYaw,directionX,directionZ,phase,soleY,leftWristError,rightWristError\n");
            foreach (bool crouched in new[] { false, true })
            foreach (float yaw in new[] { 0f, 90f })
            foreach (var direction in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
            foreach (float phase in new[] { 0f, .25f, .5f, .75f })
            foreach (var actor in session.Actors)
            {
                actor.SetCrouched(crouched); actor.GroundedOverride = true; actor.AirborneTicks = 0;
                actor.Command = new Simulation.MovementCommand { Yaw = yaw, Move = direction.normalized, Crouch = crouched };
                var local = new Vector3(direction.normalized.x, 0, direction.normalized.y) * (crouched ? 3.6f : 8);
                actor.Velocity = Quaternion.Euler(0, yaw, 0) * local;
                var position = actor.transform.position; var velocity = actor.Velocity;
                actor.Presentation.ApplyMotionState(0);
                var animator = actor.Presentation.Animator; animator.ResetTrigger("Land"); animator.Play("Locomotion", 0, phase); animator.Update(0);
                actor.Presentation.ApplyCombatPose(); actor.Presentation.Hands.Pose(0);
                Assert.That(animator.GetFloat("MotionX"), Is.EqualTo(local.x).Within(.001f)); Assert.That(animator.GetFloat("MotionZ"), Is.EqualTo(local.z).Within(.001f));
                Assert.That(Vector3.Dot(actor.Presentation.transform.forward, Quaternion.Euler(0, yaw, 0) * Vector3.forward), Is.GreaterThan(.999f));
                float sole = soles[actor.Id].MinimumWorldHeight();
                Assert.That(sole, Is.GreaterThan(-.005f), $"{actor.CharacterId} crouch={crouched} direction={direction} phase={phase}");
                float left = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position, actor.Presentation.LeftGrip.position);
                float right = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, actor.Presentation.RightGrip.position);
                Assert.That(Mathf.Max(left, right), Is.LessThan(.025f));
                Assert.That(actor.transform.position, Is.EqualTo(position)); Assert.That(actor.Velocity, Is.EqualTo(velocity));
                Assert.That(actor.Controller.height, Is.EqualTo(crouched ? ArenaActor.CrouchingHeight : ArenaActor.StandingHeight));
                csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6:F6},{7:F6},{8:F6}\n", actor.CharacterId, crouched, yaw, direction.x, direction.y, phase, sole, left, right);
            }
            foreach (var actor in session.Actors)
            foreach (float speed in new[] { -4f, 4f })
            {
                actor.SetCrouched(false); actor.Command = default; actor.Velocity = Vector3.right * speed; actor.GroundedOverride = true;
                float start = 0;
                foreach (float phase in new[] { 0f, .25f })
                {
                    actor.Presentation.ApplyMotionState(0); var animator = actor.Presentation.Animator; animator.ResetTrigger("Land");
                    animator.Play("Locomotion", 0, phase); animator.Update(0); actor.Presentation.ApplyCombatPose();
                    float x = actor.Presentation.transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).x;
                    if (phase == 0) start = x;
                    else Assert.That((x - start) * -Mathf.Sign(speed), Is.GreaterThan(.05f), "The planted foot must move against travel in body space; left/right may not be reversed");
                }
            }
            string folder = Path.GetFullPath("../Validation/StrafePlayback"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "BlendChecks.csv"), csv.ToString());
            foreach (var actor in session.Actors)
            {
                actor.Presentation.SetMotionController(original); actor.Presentation.ApplyMotionState(0);
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(actor.Presentation.transform.localEulerAngles.y, 65)), Is.LessThan(.01f), "Removal restores legacy travel-facing presentation");
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
