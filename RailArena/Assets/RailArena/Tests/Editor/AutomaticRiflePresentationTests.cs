using UnityEditor.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class AutomaticRiflePresentationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnityTest]
        public IEnumerator RecoilMovesTheInstalledWeaponPreservesAimAndHonorsSteadyWeapon()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry", LoadSceneMode.Additive);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            bool oldMotion = session.Comfort.WeaponMotion;
            try
            {
                session.StartSession(true, 0, 1, TargetScenario.Stationary);
                session.enabled = false; session.Comfort.WeaponMotion = true;
                var motion = (FirstPersonWeaponMotion)typeof(ArenaSession).GetField("weaponMotion", Private).GetValue(session);
                var gun = (Transform)typeof(ArenaSession).GetField("weapon", Private).GetValue(session);
                var late = typeof(ArenaSession).GetMethod("LateUpdate", Private);
                var fire = typeof(ArenaSession).GetMethod("ShowShot", Private);
                motion.Reset(false);
                late.Invoke(session, null);
                Quaternion aim = session.View.transform.rotation;
                Vector3 rest = gun.localPosition;
                foreach (bool ads in new[] { false, true })
                {
                    typeof(ArenaSession).GetProperty(nameof(ArenaSession.IsAiming)).SetValue(session, ads);
                    var sights = (AimDownSights)typeof(ArenaSession).GetField("sights", Private).GetValue(session);
                    sights.Step(ads, 1);
                    typeof(ArenaSession).GetMethod("ResetRecoil", Private).Invoke(session, null);
                    late.Invoke(session, null); rest = gun.localPosition;
                    Capture(session.View, ads ? "ADS-rest" : "Hip-rest");
                    yield return Frame(); yield return Frame();
                    fire.Invoke(session, new object[] { session.Human, session.Human.Eye, session.Human.Eye + session.Human.AimDirection * 50 });
                    float started = Time.realtimeSinceStartup, maxTravel = 0, maxAngle = 0;
                    bool captured = false;
                    do
                    {
                        yield return Frame(); late.Invoke(session, null);
                        maxTravel = Mathf.Max(maxTravel, Vector3.Distance(rest, gun.localPosition));
                        maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(Quaternion.identity, gun.localRotation));
                        Assert.That(Quaternion.Angle(aim, session.View.transform.rotation), Is.LessThan(.001f));
                        if (!captured && maxTravel > .015f)
                        { Capture(session.View, ads ? "ADS-kick" : "Hip-kick"); captured = true; }
                    } while (Time.realtimeSinceStartup - started < .45f);
                    Assert.That(captured, Is.True, "No visible impulse reached the installed rifle");
                    Assert.That(maxTravel, Is.InRange(.015f, .09f));
                    Assert.That(maxAngle, Is.InRange(.1f, ads ? 2 : 8));
                    Assert.That(Vector3.Distance(rest, gun.localPosition), Is.LessThan(.001f));
                    Capture(session.View, ads ? "ADS-recovered" : "Hip-recovered");
                }
                session.Comfort.WeaponMotion = false;
                fire.Invoke(session, new object[] { session.Human, session.Human.Eye, session.Human.Eye + Vector3.forward * 50 });
                late.Invoke(session, null);
                Assert.That(Quaternion.Angle(gun.localRotation, Quaternion.identity), Is.LessThan(.001f));
                Assert.That((float)typeof(ArenaSession).GetField("recoil", Private).GetValue(session), Is.Zero);
                Assert.That(session.Human.Health, Is.EqualTo(100));
                Assert.That(session.Match.Statistics[0].Shots, Is.Zero, "Controlled presentation must not invent simulation shots");
            }
            finally { session.Comfort.WeaponMotion = oldMotion; session.enabled = true; session.MainMenu(); }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ProductionMouseMappingFiresWhileHeldAndStopsOnRelease()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode();
            var session = Object.FindAnyObjectByType<ArenaSession>();
            var originalMouse = Mouse.current;
            var settings = InputSystem.settings;
            var originalEditorInput = settings.editorInputBehaviorInPlayMode;
            var originalBackground = settings.backgroundBehavior;
            Mouse mouse = null;
            try
            {
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                mouse = InputSystem.AddDevice<Mouse>("Automatic rifle review mouse"); mouse.MakeCurrent();
                session.StartSession(true, 0, 1, TargetScenario.Stationary);
                while (!session.Match.IsActive) session.Match.Advance();
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                int start = session.Match.Tick, before = session.Human.ShotSerial;
                float deadline = Time.realtimeSinceStartup + 5;
                while (session.Match.Tick < start + 60)
                { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return Frame(); }
                Assert.That(session.Human.ShotSerial - before, Is.InRange(4, 6), "Held production mouse input must repeat at roughly five shots per second");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return Frame(); yield return Frame();
                before = session.Human.ShotSerial; start = session.Match.Tick; deadline = Time.realtimeSinceStartup + 5;
                while (session.Match.Tick < start + 18)
                { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return Frame(); }
                Assert.That(session.Human.ShotSerial, Is.EqualTo(before));
            }
            finally
            {
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
                settings.editorInputBehaviorInPlayMode = originalEditorInput; settings.backgroundBehavior = originalBackground;
                session.MainMenu();
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Restore() { if (Application.isPlaying) yield return new ExitPlayMode(); }
        private static IEnumerator Frame()
        {
            int frame = Time.frameCount; double deadline = UnityEditor.EditorApplication.timeSinceStartup + 5;
            while (Time.frameCount == frame)
            {
                Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(deadline));
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
        }

        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; var oldTarget = camera.targetTexture;
            try
            {
                target.Create(); camera.targetTexture = target;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/Automatic-Rifle-2026-10-29/Captures"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, name + ".png"), pixels.EncodeToPNG());
            }
            finally { camera.targetTexture = oldTarget; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
    }
}
