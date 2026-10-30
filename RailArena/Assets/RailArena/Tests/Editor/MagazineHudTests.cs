using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class MagazineHudTests
    {
        [UnityTest]
        public IEnumerator ReloadKeyBlocksFireCompletesAndHudFitsAllReviewSizes()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode(); yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            var oldKeyboard = Keyboard.current; var oldMouse = Mouse.current;
            bool oldMotion = session.Comfort.WeaponMotion;
            var settings = InputSystem.settings;
            var oldInput = settings.editorInputBehaviorInPlayMode; var oldBackground = settings.backgroundBehavior;
            Keyboard keyboard = null; Mouse mouse = null;
            try
            {
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = InputSystem.AddDevice<Keyboard>("Magazine review keyboard"); keyboard.MakeCurrent();
                mouse = InputSystem.AddDevice<Mouse>("Magazine review mouse"); mouse.MakeCurrent();
                session.StartSession(true, 0, 1, TargetScenario.Stationary);
                while (!session.Match.IsActive) session.Match.Advance();
                session.Human.Magazine.Rounds = 7;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                double deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (!session.Human.Magazine.Reloading)
                { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline)); yield return Frames(1); }
                int end = session.Human.Magazine.ReloadEndTick, shots = session.Human.ShotSerial;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                int tick = session.Match.Tick + 5;
                while (session.Match.Tick < tick)
                { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline)); yield return Frames(1); }
                Assert.That(session.Human.ShotSerial, Is.EqualTo(shots));
                Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(7));
                Assert.That(session.Human.Magazine.ReloadEndTick, Is.EqualTo(end));
                InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Frames(2);
                session.enabled = false;
                var reloading = session.Human.Magazine;
                yield return Frames(2);
                Assert.That(Label("AmmoValue").text, Is.EqualTo("07 / 30"));
                Assert.That(Label("RailValue").text, Does.StartWith("RELOADING"));
                yield return Review(session.View, "Reloading");
                session.Comfort.WeaponMotion = false;
                typeof(ArenaSession).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(session, null);
                Assert.That((float)typeof(ArenaSession).GetField("reloadBlend", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(session), Is.Zero);
                session.Comfort.WeaponMotion = oldMotion;
                session.Human.Magazine = RifleMagazine.Full; session.Human.Magazine.Rounds = 0;
                yield return Frames(2); Assert.That(Label("RailValue").text, Does.StartWith("EMPTY"));
                yield return Review(session.View, "Empty");
                session.Human.Magazine = RifleMagazine.Full;
                yield return Frames(2); Assert.That(Label("AmmoValue").text, Is.EqualTo("30 / 30"));
                yield return Review(session.View, "Full");
                session.Human.Magazine.Rounds = 3;
                yield return Frames(2); yield return Review(session.View, "Low");
                session.Human.Magazine = reloading; session.enabled = true;
                deadline = Time.realtimeSinceStartupAsDouble + 5;
                while (session.Human.Magazine.Reloading)
                { Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline)); yield return Frames(1); }
                Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(30));
                Assert.That(session.Match.Tick, Is.GreaterThanOrEqualTo(end));
            }
            finally
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
                if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
                settings.editorInputBehaviorInPlayMode = oldInput; settings.backgroundBehavior = oldBackground;
                session.Comfort.WeaponMotion = oldMotion; session.enabled = true; session.MainMenu();
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Restore() { if (Application.isPlaying) yield return new ExitPlayMode(); }
        private static UnityEngine.UI.Text Label(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Text>();
        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount + count; double deadline = UnityEditor.EditorApplication.timeSinceStartup + 5;
            while (Time.frameCount < target)
            { Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(deadline)); UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
        }

        private static IEnumerator Review(Camera camera, string state)
        {
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1080) })
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                var target = new RenderTexture(size.x, size.y, 24); var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
                var mode = canvas.renderMode; var oldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
                try
                {
                    target.Create(); camera.targetTexture = target;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f;
                    yield return Frames(2); Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    HeadlessUiReview.Draw(canvas, camera, target, clear: false);
                    foreach (var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                        Assert.That(text.cachedTextGenerator.characterCountVisible, Is.EqualTo(text.text.Length), state + ": " + text.name);
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
                    string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/Magazines-250RPM-2026-10-29/Captures"));
                    Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, $"{state}-{size.x}x{size.y}.png"), image.EncodeToPNG());
                }
                finally
                {
                    canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.planeDistance = distance;
                    camera.targetTexture = oldTarget; RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                }
            }
        }
    }
}
