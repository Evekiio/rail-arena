using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class FoundryLightingTests
    {
        [UnityTest]
        public IEnumerator DaylightPersistsThroughPracticePauseAndMenu()
        {
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry", LoadSceneMode.Additive);
            yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            var sky = RenderSettings.skybox;
            Assert.That(sky, Is.Not.Null);
            Assert.That(sky.shader.name, Is.EqualTo("Rail Arena/Foundry Cloud Sky"));
            Assert.That(sky.shader.isSupported, Is.True);
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(sky.shader), Is.False);
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Trilight));
            var stars = Resources.FindObjectsOfTypeAll<GameObject>().Single(obj => obj.scene == session.gameObject.scene && obj.name == "Orbital starfield");
            Assert.That(stars.activeSelf, Is.False);
            Assert.That(stars.GetComponentsInChildren<Collider>(true), Is.Empty);
            var towardSun = (Vector3)sky.GetVector("_SunDirection");
            Assert.That(Vector3.Dot(towardSun.normalized,-RenderSettings.sun.transform.forward), Is.GreaterThan(.9999f));
            session.StartSession(true,0,1,TargetScenario.Stationary);
            yield return Frames(3);
            Assert.That(session.View.clearFlags, Is.EqualTo(CameraClearFlags.Skybox));
            Capture(session.View);
            session.SetPaused(true);
            yield return Frames(2);
            Assert.That(RenderSettings.skybox, Is.SameAs(sky));
            session.MainMenu();
            yield return Frames(2);
            Assert.That(session.View.clearFlags, Is.EqualTo(CameraClearFlags.Skybox));
            Assert.That(RenderSettings.skybox, Is.SameAs(sky));
            yield return new ExitPlayMode();
        }

        private static void Capture(Camera camera)
        {
            var target = new RenderTexture(1920,1080,24);
            var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                Assert.That(pixels.GetPixels().Count(pixel => pixel.maxColorComponent > .3f), Is.GreaterThan(1920*1080/4),
                    "The daylight view rendered black or with an unsupported sky shader.");
                string output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/FoundryLighting"));
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output,"Player-Ready.png"),pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount + count;
            float deadline = Time.realtimeSinceStartup + 5;
            while (Time.frameCount < target)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
