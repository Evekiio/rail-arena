using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class StandingBlendArtReview
    {
        [UnityTest]
        public IEnumerator LowSpeedAndDirectionalBlendsPreserveContactsAndAuthority()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Installed standing blend review").AddComponent<ArtSlicePresentation>();
            yield return null;
            yield return null;
            session.enabled = false;
            session.View.enabled = false;
            var skins = session.Actors.ToDictionary(actor => actor.Id, actor => new CrouchCandidateArtReview.Surface(actor));
            foreach (var actor in session.Actors)
            {
                actor.Controller.enabled = false;
                actor.Presentation.enabled = false;
                actor.SetCrouched(false);
                actor.GroundedOverride = true;
                actor.AirborneTicks = 0;
                actor.transform.SetPositionAndRotation(new Vector3(300 + actor.Id * 2, actor.Controller.skinWidth, 12), Quaternion.identity);
                actor.Presentation.Animator.Rebind();
                actor.Presentation.Animator.speed = 0;
                actor.Presentation.Animator.enabled = false;
                Assert.That(actor.Presentation.Animator.applyRootMotion, Is.False);
            }
            var csv = new StringBuilder("characterId,yaw,direction,speed,phase,skinFloorY,leftWristError,rightWristError,idleWeight\n");
            var failures = new List<string>();
            foreach (float yaw in new[] { 0f, 90f })
            for (int direction = 0; direction < 8; direction++)
            foreach (float speed in new[] { 0f, .1f, .25f, .5f, .75f, 1f, 1.5f, 4f, 9f })
            for (int sample = 0; sample <= 8; sample++)
            foreach (var actor in session.Actors)
            {
                float phase = sample / 8f;
                var local = Quaternion.Euler(0, direction * 45, 0) * Vector3.forward * speed;
                actor.Command = new Simulation.MovementCommand { Yaw = yaw, Move = new Vector2(local.x, local.z).normalized };
                actor.Velocity = Quaternion.Euler(0, yaw, 0) * local;
                var root = actor.transform.position;
                var velocity = actor.Velocity;
                float height = actor.Controller.height;
                var animator = actor.Presentation.Animator;
                actor.Presentation.ApplyMotionState(0);
                animator.ResetTrigger("Land");
                animator.Play("Locomotion", 0, phase);
                animator.Update(0);
                actor.Presentation.ApplyCombatPose();
                actor.Presentation.Hands.Pose(0);
                float floor = skins[actor.Id].Heights().x;
                float left = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position, actor.Presentation.LeftGrip.position);
                float right = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, actor.Presentation.RightGrip.position);
                float idle = animator.GetCurrentAnimatorClipInfo(0).Where(item => item.clip.name == "StandingReady").Sum(item => item.weight);
                if (floor < -.005f || floor > .035f || Mathf.Max(left, right) > .025f)
                    failures.Add($"character {actor.CharacterId} yaw {yaw} direction {direction} speed {speed} phase {phase}: floor={floor}, wrists={left}/{right}");
                if (speed == 0) Assert.That(idle, Is.GreaterThan(.999f));
                Assert.That(actor.transform.position, Is.EqualTo(root));
                Assert.That(actor.Velocity, Is.EqualTo(velocity));
                Assert.That(actor.Controller.height, Is.EqualTo(height));
                csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5:F6},{6:F6},{7:F6},{8:F6}\n", actor.CharacterId, yaw, direction, speed, phase, floor, left, right, idle);
            }
            string folder = Path.GetFullPath("../Validation/StandingBlendReview");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "BlendChecks.csv"), csv.ToString());
            File.WriteAllLines(Path.Combine(folder, "Failures.txt"), failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(24)));
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
