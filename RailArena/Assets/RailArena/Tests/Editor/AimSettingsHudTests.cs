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
    public sealed class AimSettingsHudTests
    {
        [UnityTest]
        public IEnumerator SettingsApplyLiveAndFitAtBothSupportedResolutions()
        {
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry", LoadSceneMode.Additive);
            yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            string[] keys = { "sensitivity", "zoomSensitivity", "fieldOfView", "invertY" };
            bool[] existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            float[] old = keys.Take(3).Select(key => PlayerPrefs.GetFloat(key)).ToArray();
            int oldInvert = PlayerPrefs.GetInt("invertY");
            try
            {
                session.MainMenu();
                yield return Frames(2);
                Button("SETTINGS").onClick.Invoke();
                yield return Frames(2);
                var sliders = Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsSortMode.None);
                Assert.That(sliders.Length, Is.EqualTo(4));
                var zoom = sliders.Single(s => s.name == "Zoom sensitivity");
                zoom.value = .08f;
                sliders.Single(s => s.name == "Mouse sensitivity").value = .2f;
                sliders.Single(s => s.name == "Vertical FOV").value = 88;
                bool oldToggle = session.Aim.InvertY;
                Button("INVERT Y // " + (oldToggle ? "ON" : "OFF")).onClick.Invoke();
                Assert.That(session.Aim.InvertY, Is.EqualTo(!oldToggle));
                Assert.That(session.Aim.ZoomSensitivity, Is.EqualTo(.08f));
                Assert.That(session.Sensitivity, Is.EqualTo(.2f));
                yield return Frames(2);
                Assert.That(session.View.fieldOfView, Is.EqualTo(88));
                var restored = new AimPreferences(); restored.Load();
                Assert.That(restored.FieldOfView, Is.EqualTo(88));
                Assert.That(restored.ZoomSensitivity, Is.EqualTo(.08f));
                Assert.That(restored.InvertY, Is.EqualTo(!oldToggle));
                foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720) })
                    yield return CaptureAndCheck(sliders, session.View, size);
                Button("BACK").onClick.Invoke();
                yield return Frames(2);
                Assert.That(Button("SETTINGS"), Is.Not.Null);
            }
            finally
            {
                for (int i = 0; i < 3; i++)
                    if (existed[i]) PlayerPrefs.SetFloat(keys[i], old[i]); else PlayerPrefs.DeleteKey(keys[i]);
                if (existed[3]) PlayerPrefs.SetInt("invertY", oldInvert); else PlayerPrefs.DeleteKey("invertY");
                PlayerPrefs.Save(); session.Aim.Load();
            }
            yield return new ExitPlayMode();
        }

        private static UnityEngine.UI.Button Button(string label)
            => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .Single(b => b.GetComponentInChildren<UnityEngine.UI.Text>()?.text == label);

        private static IEnumerator CaptureAndCheck(UnityEngine.UI.Slider[] sliders, Camera camera, Vector2Int size)
        {
            var canvas = sliders[0].GetComponentInParent<Canvas>();
            var target = new RenderTexture(size.x,size.y,24);
            var image = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f;
                yield return Frames(2);
                Canvas.ForceUpdateCanvases();
                var viewport = camera.pixelRect;
                var corners = new Vector3[4];
                foreach (var slider in sliders)
                {
                    Assert.That(slider.fillRect, Is.Not.Null, slider.name);
                    float value = slider.value;
                    foreach (float endpoint in new[] { slider.minValue, slider.maxValue })
                    {
                        slider.SetValueWithoutNotify(endpoint); Canvas.ForceUpdateCanvases();
                        slider.handleRect.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var pixel = RectTransformUtility.WorldToScreenPoint(camera,corner);
                            Assert.That(pixel.x, Is.InRange(viewport.xMin,viewport.xMax),slider.name);
                            Assert.That(pixel.y, Is.InRange(viewport.yMin,viewport.yMax),slider.name);
                        }
                    }
                    slider.SetValueWithoutNotify(value);
                }
                Canvas.ForceUpdateCanvases();
                // Screen-space UI is omitted by URP's isolated camera requests in batch mode.
                // Draw the generated uGUI meshes/materials against a neutral review background.
                using (var commands = new CommandBuffer { name = "Settings uGUI geometry review" })
                {
                    var meshes = new System.Collections.Generic.List<Mesh>();
                    try
                    {
                        commands.SetRenderTarget(target);
                        commands.SetViewport(new Rect(0,0,size.x,size.y));
                        commands.ClearRenderTarget(true,true,new Color(.02f,.035f,.05f));
                        commands.SetViewProjectionMatrices(Matrix4x4.identity,GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0,size.x,0,size.y,-1,1),false));
                        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                        {
                            if (!graphic.isActiveAndEnabled) continue;
                            graphic.canvasRenderer.cull = false;
                            graphic.SetVerticesDirty();
                            graphic.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                            var source = graphic.canvasRenderer.GetMesh();
                            if (source == null) continue;
                            var mesh = Object.Instantiate(source); meshes.Add(mesh);
                            if (mesh.vertexCount == 0) continue;
                            mesh.vertices = mesh.vertices.Select(vertex =>
                            {
                                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera,graphic.transform.TransformPoint(vertex));
                                return new Vector3(point.x,point.y,0);
                            }).ToArray();
                            mesh.RecalculateBounds();
                            var properties = new MaterialPropertyBlock();
                            properties.SetTexture("_MainTex",graphic.mainTexture);
                            properties.SetColor("_Color",Color.white);
                            properties.SetVector("_TextureSampleAdd",graphic.mainTexture is Texture2D texture && texture.format == TextureFormat.Alpha8
                                ? new Vector4(1,1,1,0) : Vector4.zero);
                            commands.DrawMesh(mesh,Matrix4x4.identity,graphic.materialForRendering,0,0,properties);
                        }
                        Assert.That(meshes.Sum(mesh => mesh.vertexCount), Is.GreaterThan(100), "The batch UI geometry was not rebuilt.");
                        Graphics.ExecuteCommandBuffer(commands);
                    }
                    finally { foreach (var mesh in meshes) Object.DestroyImmediate(mesh); }
                }
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                Assert.That(image.GetPixel(15,15).maxColorComponent, Is.LessThan(.3f), "The settings panel was omitted from the review capture.");
                Assert.That(image.GetPixels().Count(pixel => pixel.g > .6f && pixel.b > .7f), Is.GreaterThan(100), "The review capture omitted the readable UI geometry.");
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/AimSettings"));
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,$"Settings-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount + count;
            float deadline = Time.realtimeSinceStartup + 5;
            while (Time.frameCount < target)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Player frames stopped advancing");
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
