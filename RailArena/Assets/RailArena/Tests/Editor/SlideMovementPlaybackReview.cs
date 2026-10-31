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
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class SlideMovementPlaybackReview
    {
        byte[] priorQuality;
        RenderPipelineAsset priorPipeline;

        [UnityTest]
        public IEnumerator CompareControllerDrivenSlidesWithoutChangingTrajectories()
        {
            priorQuality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            priorPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Controller driven slide review").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            session.enabled = false; session.View.enabled = false;
            session.Tuning.SlideDurationTicks = 36;
            var originals = session.Actors.ToDictionary(actor => actor.Id, actor => actor.Presentation.Animator.runtimeAnimatorController);
            var surfaces = session.Actors.ToDictionary(actor => actor.Id, actor => new CrouchCandidateArtReview.Surface(actor));
            var legacy = Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            foreach (var actor in session.Actors)
            {
                actor.Presentation.enabled = false;
                actor.Presentation.Hands.enabled = false;
                actor.Presentation.Animator.enabled = false;
                foreach (var child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Owned slide review runway"; floor.layer = 30;
            floor.transform.position = new Vector3(300, -.05f, 19);
            floor.transform.localScale = new Vector3(8, .1f, 24);
            var material = new Material(Resources.Load<Material>("Art/StoneCover-Stone"));
            floor.GetComponent<Renderer>().sharedMaterial = material;
            var camera = new GameObject("Owned moving slide camera").AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .18f, .22f);
            camera.orthographic = true; camera.orthographicSize = 1.5f; camera.aspect = 16f/9;
            var fill = new GameObject("Owned slide fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .5f; fill.shadows = LightShadows.None; fill.cullingMask = 1 << 30;
            fill.transform.rotation = Quaternion.Euler(25, 200, 0);
            var target = new RenderTexture(1280, 720, 24); target.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string output = Path.GetFullPath("../Validation/SlideMovementPlayback"); Directory.CreateDirectory(output);
            var reference = new Dictionary<(int frame, int actor), (Vector3 root, Vector3 velocity, int ticks, float height)>();
            var csv = new StringBuilder("pass,frame,characterId,rootX,rootY,rootZ,velocityX,velocityY,velocityZ,remainingTicks,controllerHeight,state,minimumSkinY,maximumSkinY,leftWristError,rightWristError\n");
            var failures = new List<string>();
            int previousRate = Time.captureFramerate; Time.captureFramerate = 60;
            try
            {
                foreach (bool installed in new[] { false, true })
                {
                    string pass = installed ? "authored" : "legacy";
                    string frames = Path.Combine(output, pass); Directory.CreateDirectory(frames);
                    foreach (var actor in session.Actors)
                    {
                        actor.Presentation.SetMotionController(installed ? originals[actor.Id] : legacy);
                        actor.Presentation.Animator.Rebind(); actor.Presentation.Animator.speed = 0;
                    }
                    for (int frame = 0; frame < 360; frame++)
                    {
                        int tick = frame % 60;
                        foreach (var actor in session.Actors)
                        {
                            if (tick == 0)
                            {
                                actor.Place(new Vector3(300+(actor.Id-1.5f)*1.4f, actor.Controller.skinWidth, 12));
                                actor.Presentation.Animator.Rebind();
                                actor.Velocity = Vector3.forward*12.8f;
                                actor.GroundedOverride = true;
                            }
                            actor.Command = new Simulation.MovementCommand
                            {
                                Move = new Vector2(tick >= 12 && tick < 28 ? .35f : 0, 1),
                                Sprint = true, Slide = tick == 0, Pitch = (frame/60 % 2 == 0 ? -15 : 15)
                            };
                            Physics.SyncTransforms();
                            actor.Move(session.Tuning);
                            actor.Presentation.ApplyMotionState(0);
                            actor.Presentation.Animator.Update(1f/60);
                            actor.Presentation.ApplyCombatPose(); actor.Presentation.Hands.Pose(0);
                            var root = actor.transform.position;
                            if (installed)
                            {
                                var prior = reference[(frame, actor.Id)];
                                Assert.That(Vector3.Distance(root, prior.root), Is.LessThan(.0001f), "Art must not change the moving controller trajectory");
                                Assert.That(Vector3.Distance(actor.Velocity, prior.velocity), Is.LessThan(.0001f));
                                Assert.That(actor.Slide.RemainingTicks, Is.EqualTo(prior.ticks));
                                Assert.That(actor.Controller.height, Is.EqualTo(prior.height));
                            }
                            else reference[(frame, actor.Id)] = (root, actor.Velocity, actor.Slide.RemainingTicks, actor.Controller.height);
                            var animator = actor.Presentation.Animator;
                            if (installed && actor.IsSliding) Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredSlide"), Is.True);
                            if (installed && !actor.IsSliding) Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsTag("AuthoredSlide"), Is.False);
                            var heights = surfaces[actor.Id].Heights();
                            float left = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position, actor.Presentation.LeftGrip.position);
                            float right = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, actor.Presentation.RightGrip.position);
                            if (installed && actor.IsSliding && (heights.x < -.005f || heights.x > .04f || Mathf.Max(left, right) > .025f))
                                failures.Add(frame+" / "+actor.CharacterId+" skin "+heights+" wrist "+Mathf.Max(left, right));
                            csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9},{10:F3},{11},{12:F6},{13:F6},{14:F6},{15:F6}\n",
                                pass, frame, actor.CharacterId, root.x, root.y, root.z, actor.Velocity.x, actor.Velocity.y, actor.Velocity.z,
                                actor.Slide.RemainingTicks, actor.Controller.height, animator.GetCurrentAnimatorClipInfo(0)[0].clip.name, heights.x, heights.y, left, right);
                        }
                        Vector3 center = session.Actors.Aggregate(Vector3.zero, (sum, actor) => sum+actor.transform.position)/session.Actors.Count;
                        camera.transform.position = center+new Vector3(3.2f, 1.72f, 4.8f);
                        camera.transform.LookAt(center+Vector3.up*.72f);
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        var previous = RenderTexture.active;
                        try
                        {
                            RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                            File.WriteAllBytes(Path.Combine(frames, "frame-"+frame.ToString("D4")+".png"), pixels.EncodeToPNG());
                        }
                        finally { RenderTexture.active = previous; }
                    }
                }
                File.WriteAllText(Path.Combine(output, "Playback.csv"), csv.ToString());
                File.WriteAllLines(Path.Combine(output, "Failures.txt"), failures);
                Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
            }
            finally
            {
                Time.captureFramerate = previousRate;
                target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(camera.gameObject);
                Object.Destroy(floor); Object.Destroy(material); Object.Destroy(fill.gameObject);
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (priorQuality == null) yield break;
            QualitySettings.renderPipeline = priorPipeline;
            File.WriteAllBytes("ProjectSettings/QualitySettings.asset", priorQuality); AssetDatabase.Refresh();
        }
    }
}
