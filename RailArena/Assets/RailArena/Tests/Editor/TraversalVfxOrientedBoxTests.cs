using System.Collections;
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
    public sealed class TraversalVfxOrientedBoxTests
    {
        [UnityTest]
        public IEnumerator BoxRotationChangesCollisionFootprint()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalFreeV3.vfx");
            var hybrid = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalOrientedBox.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(hybrid, Is.Not.Null);
            yield return new EnterPlayMode();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(0, -.15f, 0); floor.transform.localScale = new Vector3(2, .3f, 2); floor.layer = 8;
            Assert.That(Physics.Raycast(new Vector3(3, 3, 0), Vector3.down, 6f), Is.False, "The test particle path must be beyond the platform");
            var camera = new GameObject("Edge main camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(3, 1.66f, -4); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
            camera.GetUniversalAdditionalCameraData();
            var target = new RenderTexture(640, 640, 24); target.Create(); camera.targetTexture = target;
            var observer = new GameObject("Edge observer").AddComponent<Camera>(); observer.enabled = false;
            observer.transform.position = new Vector3(3, 0, -12); observer.orthographic = true; observer.orthographicSize = 5;
            observer.clearFlags = CameraClearFlags.SolidColor; observer.backgroundColor = Color.black; observer.cullingMask &= ~(1 << 8);
            var observation = new RenderTexture(640, 640, 24); observation.Create();
            var root = new GameObject("Edge burst"); root.transform.position = new Vector3(3, 1.85f, 0);
            var effect = root.AddComponent<VisualEffect>(); effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                foreach (var entry in new[] { ("free", free), ("rotated", hybrid), ("inside", hybrid) })
                {
                    effect.visualEffectAsset = entry.Item2; effect.Reinit();
                    if (entry.Item1 != "free") { effect.SetVector3("BoxCenter", new Vector3(-1.5f, -2f, 0)); effect.SetVector3("BoxSize", new Vector3(4, .3f, 1)); effect.SetVector3("BoxAngles", new Vector3(0, entry.Item1 == "rotated" ? 90 : 0, 0)); }
                    effect.Play();
                    for (int frame = 0; frame < 35; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    }
                    RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = observation; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var pixels = image.GetPixels32(); int below = 0, above = 0;
                        for (int y = 0; y < 640; y++) for (int x = 0; x < 640; x++)
                        { var pixel = pixels[y * 640 + x]; if (pixel.r <= 10 && pixel.g <= 10 && pixel.b <= 10) continue; if (y < 315) below++; else above++; }
                        Debug.Log("TRAVERSAL_ORIENTED_BOX " + entry.Item1 + " below=" + below + " above=" + above);
                        Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                        if (entry.Item1 != "inside") Assert.That(below, Is.GreaterThan(0));
                        else { Assert.That(below, Is.EqualTo(0)); Assert.That(above, Is.GreaterThan(0)); }
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
            }
            finally { target.Release(); observation.Release(); Object.Destroy(target); Object.Destroy(observation); Object.Destroy(root); Object.Destroy(floor); Object.Destroy(observer.gameObject); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}



