using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class RifleReloadArtReview
    {
        [UnityTest]
        public IEnumerator CaptureReloadMotion()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.Graphics.MotionBlur = false; session.Graphics.Apply(session.View);
            session.Comfort.WeaponMotion = true;
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Authored reload playback").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            string folder = Path.GetFullPath("../Validation/ArtReloadMotion"); Directory.CreateDirectory(folder);
            var csv = new System.Text.StringBuilder("frame,simulationTick,reloading,rounds\n");
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
                for (int frame = 0; frame < 180; frame++)
                {
                    if (frame == 30)
                    {
                        session.Human.Magazine.Rounds = 12;
                        session.Human.Magazine.Advance(session.LocalSimulationTick, true);
                    }
                    yield return null;
                    CaptureFrame(session.View, Path.Combine(folder, "frame-" + frame.ToString("0000") + ".png"));
                    csv.AppendLine(frame + "," + session.LocalSimulationTick + "," + session.Human.Magazine.Reloading + "," + session.Human.Magazine.Rounds);
                }
                Assert.That(session.Human.Magazine.Reloading, Is.False);
                Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(Simulation.RifleMagazine.Capacity));
                File.WriteAllText(Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/PlaybackTiming.csv"), csv.ToString());
            }
            finally { Time.captureFramerate = previousRate; }
            Debug.Log("AUTHORED_RELOAD_MOTION_CAPTURED 180 frames at fixed 60 Hz simulation; not a performance measurement");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AuthoredReloadContactsAndReset()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.Graphics.MotionBlur = false; session.Graphics.Apply(session.View);
            session.Comfort.WeaponMotion = true;
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            yield return null; session.enabled = false;
            var slice = new GameObject("Authored action review").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            var arms = session.View.GetComponentInChildren<FirstPersonArms>(true);
            var weapon = arms.GetComponentInParent<RailgunPresentation>();
            var action = weapon.GetComponent<RifleReloadPresentation>();
            Assert.That(action.Initialized, Is.True);
            var art = Resources.Load<RifleReloadArt>("Art/RifleReload");
            var originalGrip = weapon.LeftGrip.localPosition;
            var originalRotation = weapon.LeftGrip.localRotation;
            int rounds = session.Human.Magazine.Rounds, endTick = session.Human.Magazine.ReloadEndTick;
            action.enabled = false;
            string directory = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/in-game");
            Directory.CreateDirectory(directory);
            var camera = session.View;
            // Show the normal current first-person camera, with fixed simulation during authored pose review.
            foreach (int frame in new[] { 0, 21, 36, 65, 91, 118, 120 })
            {
                action.PoseAt(frame / 120f);
                yield return null;
                var wrist = arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
                float contactError = Vector3.Distance(wrist.position, weapon.LeftGrip.position);
                Assert.That(contactError, Is.LessThan(.025f), "Left wrist target unreachable at frame " + frame);
                Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(rounds));
                Assert.That(session.Human.Magazine.ReloadEndTick, Is.EqualTo(endTick));
                yield return Capture(camera, Path.Combine(directory, "reload-" + frame.ToString("000") + ".png"));
            }
            action.ResetPose();
            Assert.That(Vector3.Distance(originalGrip, weapon.LeftGrip.localPosition), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(originalRotation, weapon.LeftGrip.localRotation), Is.LessThan(.01f));
            // Real simulation state starts/stops the action, including an interruption at partial progress.
            action.enabled = true;
            session.Human.Magazine.Rounds--;
            session.Human.Magazine.Advance(session.LocalSimulationTick, true);
            for (int i = 0; i < 30; i++) session.Match.Advance();
            yield return null; yield return null;
            Assert.That(action.Progress, Is.EqualTo(.25f).Within(.01f));
            session.Human.Magazine = Simulation.RifleMagazine.Full;
            yield return null; yield return null;
            Assert.That(action.Progress, Is.Zero);
            Assert.That(Vector3.Distance(originalGrip, weapon.LeftGrip.localPosition), Is.LessThan(.00001f));
            Debug.Log("AUTHORED_RELOAD_REVIEW_PASSED 7 contact poses; simulation timing; interruption/reset; no ammo writes");
            Object.Destroy(slice); yield return null;
            yield return new ExitPlayMode();
        }

        static IEnumerator Capture(Camera camera, string path)
        {
            yield return null;
            CaptureFrame(camera, path);
        }

        static void CaptureFrame(Camera camera, string path)
        {
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                HeadlessUiReview.Draw(canvas, camera, target, false);
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
        }
    }
}
