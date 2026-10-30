using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxDepthComparisonTests
    {
        [Test]
        public void ProbePreservesKnownFloatPattern()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Validation/TraversalVfxCandidate/DepthHistoryProbe.shader");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var input = new Texture2D(8, 8, TextureFormat.RFloat, false, true) { filterMode = FilterMode.Point };
            var target = new RenderTexture(8, 8, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear); target.Create();
            var result = new Texture2D(8, 8, TextureFormat.RFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                    input.SetPixel(x, y, new Color(x < 4 ? .2f : .8f, 0, 0));
                input.Apply(); Graphics.Blit(input, target, material);
                RenderTexture.active = target; result.ReadPixels(new Rect(0, 0, 8, 8), 0, 0); result.Apply();
                float left = result.GetPixel(1, 4).r, right = result.GetPixel(6, 4).r;
                Debug.Log("TRAVERSAL_DEPTH_PROBE_CONTROL left=" + left + " right=" + right);
                Assert.That(left, Is.EqualTo(.2f).Within(.01f));
                Assert.That(right, Is.EqualTo(.8f).Within(.01f));
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(input); Object.DestroyImmediate(result); Object.DestroyImmediate(material); }
        }
        [UnityTest]
        public IEnumerator CaptureSameSeedWithAndWithoutDepthCollision()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstCyanVisible.vfx");
            var depth = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDepthCollision.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(depth, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Depth comparison camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 5, -8); camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            var target = new RenderTexture(640, 640, 24); target.Create(); camera.targetTexture = target;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.transform.position = new Vector3(0, -1, 0); surface.transform.localScale = new Vector3(5, .3f, 4);
            var root = new GameObject("Depth comparison effect");
            var effect = root.AddComponent<VisualEffect>();
            effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/TraversalVfxDepthComparison"));
            Directory.CreateDirectory(output);
            try
            {
                foreach (var entry in new[] { ("free", free), ("depth", depth) })
                {
                    effect.visualEffectAsset = entry.Item2; effect.Reinit(); effect.Play();
                    for (int frame = 0; frame < 36; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                    var needed = VFXManager.IsCameraBufferNeeded(camera);
                    var cameraData = camera.GetUniversalAdditionalCameraData();
                    var history = cameraData.history.GetHistoryForRead<RawDepthHistory>();
                    var historyTexture = history?.GetCurrentTexture();
                    Debug.Log("TRAVERSAL_DEPTH_HISTORY " + entry.Item1 + " available=" + (historyTexture != null) + " texture=" + (historyTexture?.rt != null));
                    if (entry.Item1 == "depth")
                    {
                        Assert.That(historyTexture?.rt, Is.Not.Null, "The requested camera depth history must be allocated");
                        var shader = AssetDatabase.LoadAssetAtPath<Shader>(folder + "DepthHistoryProbe.shader");
                        Assert.That(shader, Is.Not.Null);
                        var probe = new Material(shader);
                        var outputDepth = new RenderTexture(640, 640, 0, RenderTextureFormat.RFloat); outputDepth.Create();
                        var readback = new Texture2D(640, 640, TextureFormat.RFloat, false);
                        var oldActive = RenderTexture.active;
                        try
                        {
                            Graphics.Blit(historyTexture.rt, outputDepth, probe);
                            RenderTexture.active = outputDepth; readback.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); readback.Apply();
                            var samples = readback.GetPixels(); float min = 1, max = 0;
                            foreach (var sample in samples) { min = Mathf.Min(min, sample.r); max = Mathf.Max(max, sample.r); }
                            Debug.Log("TRAVERSAL_DEPTH_VALUES min=" + min + " max=" + max);
                            Assert.That(max - min, Is.GreaterThan(0.001f), "The depth history should contain visible geometry and background");
                            surface.GetComponent<Renderer>().enabled = false;
                            for (int i = 0; i < 4; i++)
                            {
                                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                            }
                            history = cameraData.history.GetHistoryForRead<RawDepthHistory>();
                            Assert.That(history?.GetCurrentTexture()?.rt, Is.Not.Null);
                            Graphics.Blit(history.GetCurrentTexture().rt, outputDepth, probe);
                            RenderTexture.active = outputDepth; readback.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); readback.Apply();
                            var withoutSurface = readback.GetPixels(); int changed = 0;
                            for (int i = 0; i < samples.Length; i++) if (Mathf.Abs(samples[i].r - withoutSurface[i].r) > .001f) changed++;
                            Debug.Log("TRAVERSAL_DEPTH_SURFACE_CHANGED_PIXELS " + changed);
                            Assert.That(changed, Is.GreaterThan(100), "Removing the surface should change its camera depth history");
                            surface.GetComponent<Renderer>().enabled = true;
                            EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        }
                        finally { RenderTexture.active = oldActive; outputDepth.Release(); Object.Destroy(outputDepth); Object.Destroy(readback); Object.Destroy(probe); }
                    }
                    Debug.Log("TRAVERSAL_DEPTH_BUFFER_NEEDED " + entry.Item1 + " " + needed);
                    Assert.That(needed.ToString(), Is.EqualTo(entry.Item1 == "depth" ? "Depth" : "None"));
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var path = Path.Combine(output, entry.Item1 + "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                        File.WriteAllBytes(path, image.EncodeToPNG()); Debug.Log("TRAVERSAL_DEPTH_COMPARISON " + entry.Item1 + " " + path);
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(root); Object.Destroy(surface); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}













