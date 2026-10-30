using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxDirectionalPerspectiveTests
    {
        [UnityTest]
        public IEnumerator CaptureMatchedBurstsAboveVisibleFloor()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalFreeV3.vfx");
            var depth = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalDepthV3.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(depth, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Depth crossing main camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 8, -0.1f); camera.transform.LookAt(Vector3.zero);
            camera.orthographic = false; camera.fieldOfView = 60; camera.farClipPlane = 50; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 640, 24); target.Create(); camera.targetTexture = target;
            var observer = new GameObject("Depth crossing observer").AddComponent<Camera>(); observer.enabled = false; observer.transform.position = new Vector3(0, 0, -12); observer.orthographic = true; observer.orthographicSize = 5; observer.clearFlags = CameraClearFlags.SolidColor; observer.backgroundColor = Color.black; observer.cullingMask &= ~(1 << 8);
            var observerTarget = new RenderTexture(640, 640, 24); observerTarget.Create();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.layer = 8; floor.transform.position = new Vector3(0, -1, 0); floor.transform.localScale = new Vector3(10, .3f, 10);
            var root = new GameObject("Crossing burst"); root.transform.position = new Vector3(0, 1, 0);
            var effect = root.AddComponent<VisualEffect>(); effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/TraversalVfxDirectionalPerspective")); Directory.CreateDirectory(output);
            try
            {
                foreach (var entry in new[] { ("free", free), ("depth", depth), ("depth-no-floor", depth) })
                {
                    floor.GetComponent<Renderer>().enabled = entry.Item1 != "depth-no-floor"; effect.visualEffectAsset = entry.Item2; effect.Reinit();
                    for (int warmup = 0; warmup < 4; warmup++) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); }
                    effect.Play();
                    for (int frame = 1; frame <= 45; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        if (frame != 10 && frame != 15 && frame != 20 && frame != 25 && frame != 35 && frame != 45) continue;
                        RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observerTarget });
                        var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                        try
                        {
                            RenderTexture.active = observerTarget; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                            if (frame == 35)
                            {
                                var pixels = image.GetPixels32(); int below = 0;
                                // Camera spans y=-5..5; rows below 260 are beneath the floor top at y=-0.85.
                                for (int y = 0; y < 260; y++) for (int x = 0; x < 640; x++)
                                { var p = pixels[y * 640 + x]; if (p.r > 10 || p.g > 10 || p.b > 10) below++; }
                                Debug.Log("TRAVERSAL_DEPTH_CROSSING_BELOW " + entry.Item1 + " " + below);
                                if (entry.Item1 == "free") Assert.That(below, Is.GreaterThan(0), "Free particles must pass below floor height");
                            }
                            var path = Path.Combine(output, entry.Item1 + "-" + frame + "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                            File.WriteAllBytes(path, image.EncodeToPNG()); Debug.Log("TRAVERSAL_DEPTH_CROSSING " + entry.Item1 + " frame=" + frame + " particles=" + effect.aliveParticleCount + " " + path);
                        }
                        finally { RenderTexture.active = previous; Object.Destroy(image); }
                    }
                    Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                }
            }
            finally { target.Release(); observerTarget.Release(); Object.Destroy(target); Object.Destroy(observerTarget); Object.Destroy(observer.gameObject); Object.Destroy(root); Object.Destroy(floor); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}






