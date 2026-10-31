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
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class SlideIntegrationReview
    {
        [UnityTest]
        public IEnumerator ClockedSlidePosesPreserveAuthorityAndInterruptions()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Installed slide integration review").AddComponent<ArtSlicePresentation>();
            yield return null;
            yield return null;
            session.enabled = false;
            session.View.enabled = false;
            var surfaces = session.Actors.ToDictionary(actor => actor.Id, actor => new CrouchCandidateArtReview.Surface(actor));
            var colliders = session.Actors.SelectMany(actor => actor.GetComponentsInChildren<Hitbox>(true))
                .ToDictionary(box => box, box => (json: EditorJsonUtility.ToJson(box.GetComponent<Collider>()), position: box.transform.localPosition, rotation: box.transform.localRotation));
            var failures = new List<string>();
            var csv = new StringBuilder("characterId,duration,elapsed,yaw,pitch,state,phase,minimumSkinY,maximumSkinY,leftWristError,rightWristError\n");
            int originalDuration = session.Tuning.SlideDurationTicks;
            foreach (var actor in session.Actors)
            {
                actor.Controller.enabled = false;
                actor.Presentation.enabled = false;
                actor.Presentation.Animator.enabled = false;
                actor.Presentation.Animator.speed = 0;
                actor.Presentation.Animator.Rebind();
                actor.GroundedOverride = true;
                actor.AirborneTicks = 0;
                actor.transform.position = new Vector3(300 + actor.Id*2, actor.Controller.skinWidth, 12);
            }
            try
            {
                foreach (int duration in new[] { 2, 8, 16, 36, 120 })
                {
                    session.Tuning.SlideDurationTicks = duration;
                    var elapsedSamples = duration == 36 ? Enumerable.Range(0, duration - 1) :
                        new[] { 0, Mathf.Min(5, duration-2), Mathf.Min(6, duration-2), Mathf.Max(0, duration-9), duration-2 }.Distinct();
                    foreach (int elapsed in elapsedSamples)
                    foreach (float yaw in new[] { 0f, 90f })
                    foreach (float pitch in new[] { -30f, 0f, 30f })
                    foreach (var actor in session.Actors)
                    {
                        actor.Slide = new Simulation.SlideState { RemainingTicks = duration-elapsed-1, CooldownTicks = 48 };
                        actor.Command = new Simulation.MovementCommand { Yaw = yaw, Pitch = pitch, Sprint = true, Move = Vector2.up };
                        actor.Velocity = Quaternion.Euler(0, yaw, 0)*Vector3.forward*9;
                        var root = actor.transform.position;
                        var velocity = actor.Velocity;
                        var slide = actor.Slide;
                        actor.Presentation.ApplyMotionState(0);
                        actor.SetCrouched(true);
                        actor.Presentation.ApplyCombatPose();
                        actor.Presentation.Hands.Pose(0);
                        var animator = actor.Presentation.Animator;
                        string expected = duration < 16 ? "SlideHold" : elapsed < 6 ? "SlideEnter" : elapsed >= duration-9 ? "SlideRecover" : "SlideHold";
                        var state = animator.GetCurrentAnimatorStateInfo(0);
                        Assert.That(state.IsName(expected), Is.True, duration+" ticks / elapsed "+elapsed);
                        Assert.That(state.IsTag("AuthoredSlide"), Is.True);
                        Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(item => item.clip.name == expected && item.weight > .999f), Is.True);
                        Assert.That(actor.transform.position, Is.EqualTo(root));
                        Assert.That(actor.Velocity, Is.EqualTo(velocity));
                        Assert.That(actor.Slide.RemainingTicks, Is.EqualTo(slide.RemainingTicks));
                        Assert.That(actor.Slide.CooldownTicks, Is.EqualTo(slide.CooldownTicks));
                        Assert.That(actor.Controller.height, Is.EqualTo(ArenaActor.CrouchingHeight));
                        var heights = surfaces[actor.Id].Heights();
                        float left = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position, actor.Presentation.LeftGrip.position);
                        float right = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, actor.Presentation.RightGrip.position);
                        if (heights.x < -.005f || heights.x > .04f || heights.y > 1.35f)
                            failures.Add(actor.CharacterId+" "+expected+" "+elapsed+" pitch "+pitch+" skin "+heights+" lowest "+surfaces[actor.Id].LowestBone);
                        if (Mathf.Max(left, right) > .025f) failures.Add("Rifle wrist contact "+Mathf.Max(left, right));
                        csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F0},{4:F0},{5},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6}\n",
                            actor.CharacterId, duration, elapsed, yaw, pitch, expected, state.normalizedTime, heights.x, heights.y, left, right);
                    }
                }
                string output = Path.GetFullPath("../Validation/SlideIntegration");
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "Poses.csv"), csv.ToString());
                File.WriteAllLines(Path.Combine(output, "Failures.txt"), failures);
                Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
                session.Tuning.SlideDurationTicks = 36;
                var lifecycle = new StringBuilder("characterId,phase,standing,crouchHeld,jump,warp,death,respawn,legacy\n");
                var legacy = Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
                foreach (var actor in session.Actors)
                foreach (int elapsed in new[] { 0, 12, 30 })
                {
                    var presentation = actor.Presentation;
                    var animator = presentation.Animator;
                    var art = animator.runtimeAnimatorController;
                    actor.Command = default;
                    actor.GroundedOverride = true;
                    actor.AirborneTicks = 0;
                    actor.Slide = new Simulation.SlideState { RemainingTicks = 35-elapsed };
                    actor.SetCrouched(true);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredSlide"), Is.True);
                    actor.Slide = default;
                    actor.SetCrouched(false);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
                    Assert.That(animator.GetBoneTransform(HumanBodyBones.Head).position.y, Is.GreaterThan(1.35f));
                    actor.Slide = new Simulation.SlideState { RemainingTicks = 35-elapsed };
                    actor.SetCrouched(true);
                    actor.Slide = default;
                    actor.SetCrouched(true);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"), Is.True, "Held crouch recovers during the same setter");
                    actor.Slide = new Simulation.SlideState { RemainingTicks = 35-elapsed };
                    actor.SetCrouched(true);
                    actor.GroundedOverride = false;
                    actor.AirborneTicks = 1;
                    actor.Velocity = Vector3.up*5;
                    presentation.ApplyMotionState(0);
                    presentation.ApplyCombatPose();
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), Is.True);
                    actor.GroundedOverride = true;
                    actor.AirborneTicks = 0;
                    var destination = actor.transform.position+new Vector3(0, 2, 3);
                    actor.Warp(destination, Vector3.forward*9, true);
                    actor.SetCrouched(true);
                    Assert.That(actor.transform.position, Is.EqualTo(destination));
                    Assert.That(actor.Slide.RemainingTicks, Is.EqualTo(35-elapsed));
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredSlide"), Is.True);
                    actor.Health = 0;
                    presentation.SetAlive(false);
                    animator.speed = 1;
                    for (int frame = 0; frame < 8; frame++) { presentation.ApplyMotionState(1f/60); animator.Update(1f/60); }
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredDeath"), Is.True);
                    Assert.That(presentation.Weapon.gameObject.activeSelf, Is.False);
                    actor.Place(new Vector3(300+actor.Id*2, actor.Controller.skinWidth, 12));
                    actor.Controller.enabled = false;
                    actor.Health = 100;
                    presentation.SetAlive(true);
                    actor.GroundedOverride = true;
                    presentation.ApplyMotionState(0);
                    presentation.ApplyCombatPose();
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredSlide"), Is.False);
                    Assert.That(presentation.Weapon.gameObject.activeSelf, Is.True);
                    presentation.SetMotionController(legacy);
                    animator.Rebind(); animator.Update(0);
                    float hip = animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
                    actor.Slide = new Simulation.SlideState { RemainingTicks = 12 };
                    actor.SetCrouched(true);
                    Assert.That(hip-animator.GetBoneTransform(HumanBodyBones.Hips).position.y, Is.EqualTo(.6f).Within(.0001f));
                    presentation.SetMotionController(art); animator.Rebind();
                    actor.Slide = default; actor.SetCrouched(false);
                    lifecycle.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},true,true,true,true,true,true,true\n", actor.CharacterId, elapsed);
                }
                foreach (var entry in colliders)
                {
                    Assert.That(EditorJsonUtility.ToJson(entry.Key.GetComponent<Collider>()), Is.EqualTo(entry.Value.json));
                    Assert.That(entry.Key.transform.localPosition, Is.EqualTo(entry.Value.position));
                    Assert.That(entry.Key.transform.localRotation, Is.EqualTo(entry.Value.rotation));
                }
                File.WriteAllText(Path.Combine(output, "Lifecycle.csv"), lifecycle.ToString());
            }
            finally { session.Tuning.SlideDurationTicks = originalDuration; }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
