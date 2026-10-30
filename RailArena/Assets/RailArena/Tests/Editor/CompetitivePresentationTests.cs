using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CompetitivePresentationTests
    {
        [UnityTest]
        public IEnumerator InstalledSightAlignsAndCrouchPreservesFeetBodyScaleAndLifecycle()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode();
            yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 1, 1, TargetScenario.Stationary);
            yield return Frames(3);
            session.enabled = false;
            for (int i = 0; i < Simulation.CombatRules.CountdownTicks; i++) session.Match.Advance();
            var late = typeof(ArenaSession).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var ads = (AimDownSights)typeof(ArenaSession).GetField("sights", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var weapon = (Transform)typeof(ArenaSession).GetField("weapon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            var aim = weapon.GetComponent<RailgunPresentation>().Aim;
            late.Invoke(session, null);
            yield return Frames(2);
            Capture(session.View, "Hip");
            typeof(ArenaSession).GetProperty("IsAiming").SetValue(session, true);
            ads.Step(true, 1);
            late.Invoke(session, null);
            yield return Frames(2);
            var viewport = session.View.WorldToViewportPoint(aim.position);
            Assert.That(viewport.x, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(viewport.y, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(session.View.fieldOfView, Is.EqualTo(session.Aim.ViewFov(true)).Within(.001f));
            Assert.That(GameObject.Find("IlluminatedSight"), Is.Not.Null);
            Capture(session.View, "ADS");

            var actor = session.Human;
            var animator = actor.Presentation.Animator;
            actor.Presentation.enabled = false;
            animator.enabled = false;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var headBefore = head.position; var footBefore = foot.position; var scale = actor.Model.localScale;
            actor.SetCrouched(true);
            Assert.That(actor.Controller.height, Is.EqualTo(ArenaActor.CrouchingHeight));
            Assert.That(headBefore.y - head.position.y, Is.EqualTo(.6f).Within(.01f));
            Assert.That(Vector3.Distance(footBefore, foot.position), Is.LessThan(.015f));
            Assert.That(actor.Model.localScale, Is.EqualTo(scale));
            for (int i = 0; i < 20; i++) actor.Presentation.ApplyStance();
            Assert.That(headBefore.y - head.position.y, Is.EqualTo(.6f).Within(.01f), "Pose must not accumulate.");
            late.Invoke(session, null);
            yield return Frames(2);
            Capture(session.View, "Crouched-ADS");
            actor.SetCrouched(false);
            Assert.That(Vector3.Distance(headBefore, head.position), Is.LessThan(.01f));
            var snapshot = new Networking.ActorSnapshot { Position = actor.transform.position, Health = 100, Life = actor.Life, Crouched = true };
            var restore = typeof(ArenaSession).GetMethod("RestoreActor", BindingFlags.Instance | BindingFlags.NonPublic);
            restore.Invoke(session, new object[] { actor, snapshot, true, false });
            Assert.That(actor.IsCrouched, Is.True);
            Assert.That(actor.Controller.height, Is.EqualTo(ArenaActor.CrouchingHeight));
            snapshot.Crouched = false;
            restore.Invoke(session, new object[] { actor, snapshot, true, false });
            Assert.That(actor.IsCrouched, Is.False);
            actor.SetCrouched(true);
            typeof(ContenderPresentation).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actor.Presentation, null);
            var oldPosition = session.View.transform.position; var oldRotation = session.View.transform.rotation;
            int mask = session.View.cullingMask;
            weapon.gameObject.SetActive(false);
            session.View.cullingMask |= 1 << 10;
            session.View.transform.position = actor.transform.position + new Vector3(2, 1.5f, -3);
            session.View.transform.LookAt(actor.transform.position + Vector3.up * .65f);
            Capture(session.View, "Crouch-body", false);
            session.View.transform.SetPositionAndRotation(oldPosition, oldRotation); session.View.cullingMask = mask;
            weapon.gameObject.SetActive(true);
            actor.SetCrouched(false);
            session.SetPaused(true);
            Assert.That(session.IsAiming, Is.False); Assert.That(session.AimBlend, Is.Zero);
            session.SetPaused(false);
            actor.Health = 0; actor.SetAlive(false);
            late.Invoke(session, null);
            Assert.That(weapon.gameObject.activeSelf, Is.False);
            session.enabled = true;
            session.MainMenu();
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        private static IEnumerator Frames(int count)
        {
            int end = Time.frameCount + count;
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 10;
            while (Time.frameCount < end)
            {
                Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(deadline), "Player loop did not advance.");
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        private static void Capture(Camera camera, string name, bool includeHud = true)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/ADS-2026-10-29/Captures"));
            Directory.CreateDirectory(folder);
            var target = new RenderTexture(1920, 1080, 24);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            try
            {
                target.Create();
                camera.targetTexture = target;
                if (includeHud) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f; }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                if (includeHud) HeadlessUiReview.Draw(canvas, camera, target, clear: false);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            }
        }
    }
}
