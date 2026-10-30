using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RailArena.Tests
{
    public sealed class MotionBlurSettingsTests
    {
        [UnityTest]
        public IEnumerator CaptureCameraTranslationWithAndWithoutBlur()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity"); yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 1, 1, TargetScenario.Stationary); session.SetPaused(false);
            while (!session.Match.IsActive) session.Match.Advance();
            typeof(ArenaSession).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
            var weapon = (Transform)typeof(ArenaSession).GetField("weapon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(weapon.gameObject.activeInHierarchy, Is.True, "The first-person weapon must be visible for this capture");
            session.enabled = false;
            var camera = session.View; var originalSetting = session.Graphics.MotionBlur;
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; var oldEnabled = camera.enabled;
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/MotionComparison"));
            Directory.CreateDirectory(output); target.Create(); camera.targetTexture = target; camera.enabled = false;
            try
            {
                foreach (bool enabled in new[] { false, true })
                {
                    session.Graphics.MotionBlur = enabled; session.Graphics.Apply(camera);
                    camera.transform.SetPositionAndRotation(new Vector3(-.15f, 1.6f, -11), Quaternion.identity);
                    yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    yield return null;
                    camera.transform.position = new Vector3(0, 1.6f, -11);
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, enabled ? "Translation-On.png" : "Translation-Off.png"), image.EncodeToPNG());
                }
            }
            finally
            {
                session.Graphics.MotionBlur = originalSetting; session.Graphics.Apply(camera); camera.targetTexture = oldTarget; camera.enabled = oldEnabled; RenderTexture.active = oldActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); session.enabled = true; session.MainMenu();
            }
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator ComfortTogglePersistsAndLeavesAntialiasingUnchanged()
        {
            bool hadKey = PlayerPrefs.HasKey(GraphicsPreferences.MotionBlurKey); int original = PlayerPrefs.GetInt(GraphicsPreferences.MotionBlurKey, 1);
            try
            {
                PlayerPrefs.SetInt(GraphicsPreferences.MotionBlurKey, 1);
                EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity"); yield return new EnterPlayMode(); yield return null;
                var session = Object.FindAnyObjectByType<ArenaSession>();
                Assert.That(session.Graphics.MotionBlurSupported, Is.True);
                var volume = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
                Assert.That(volume.profile.TryGet<MotionBlur>(out var blur), Is.True); Assert.That(blur.active, Is.True);
                var aa = session.View.GetUniversalAdditionalCameraData().antialiasing;
                var hud = session.GetComponent<ArenaHud>(); typeof(ArenaHud).GetMethod("ComfortSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                yield return null;
                var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.GetComponentInChildren<Text>()?.text.StartsWith("CAMERA BLUR //") == true);
                button.onClick.Invoke(); Assert.That(blur.active, Is.False); Assert.That(PlayerPrefs.GetInt(GraphicsPreferences.MotionBlurKey), Is.Zero);
                Assert.That(session.View.GetUniversalAdditionalCameraData().antialiasing, Is.EqualTo(aa));
                var reloaded = new GraphicsPreferences(); reloaded.Load(); Assert.That(reloaded.MotionBlur, Is.False);
                button.onClick.Invoke(); Assert.That(blur.active, Is.True); Assert.That(PlayerPrefs.GetInt(GraphicsPreferences.MotionBlurKey), Is.EqualTo(1));
                yield return BrandUiTests.Review(session.View, "Sky-CameraBlur-Comfort");
                yield return new ExitPlayMode();
            }
            finally { if (hadKey) PlayerPrefs.SetInt(GraphicsPreferences.MotionBlurKey, original); else PlayerPrefs.DeleteKey(GraphicsPreferences.MotionBlurKey); PlayerPrefs.Save(); }
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
