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
    public sealed class TraversalVfxCollisionTests
    {
        [UnityTest]
        public IEnumerator ExposedNormalTiltsTheGpuCollisionBoundary()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV3.vfx");
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Sloped plane test camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -8); camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 640, 24); target.Create();
            var root = new GameObject("Sloped plane test effect");
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                Assert.That(effect.HasVector3("CollisionPosition") && effect.HasVector3("CollisionNormal"), Is.True);
                effect.Reinit(); effect.SetVector3("CollisionPosition", Vector3.zero);
                effect.SetVector3("CollisionNormal", new Vector3(-.5f, 1f, 0).normalized); effect.Play();
                for (int frame = 0; frame < 36; frame++)
                {
                    effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                }
                Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                    var pixels = image.GetPixels32(); int left = 640, right = 640;
                    for (int y = 0; y < 640; y++) for (int x = 180; x < 460; x++)
                    {
                        var pixel = pixels[y * 640 + x]; if (pixel.r <= 15 && pixel.g <= 15 && pixel.b <= 15) continue;
                        if (x < 280) left = System.Math.Min(left, y);
                        if (x >= 360) right = System.Math.Min(right, y);
                    }
                    Debug.Log("TRAVERSAL_VFX_SLOPED_FLOORS left=" + left + " right=" + right);
                    Assert.That(left, Is.LessThan(640)); Assert.That(right, Is.LessThan(640));
                    Assert.That(right, Is.GreaterThan(left + 30), "Tilted normal should raise the right side of the collision boundary");
                }
                finally { RenderTexture.active = previous; Object.Destroy(image); }
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(root); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ExposedPlanePositionMovesTheGpuCollisionBoundary()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV2.vfx");
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Dynamic plane test camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -8); camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 640, 24); target.Create();
            var root = new GameObject("Dynamic plane test effect");
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                Assert.That(effect.HasVector3("CollisionPosition"), Is.True, "The collision position must be exposed");
                var floors = new int[3]; int index = 0;
                foreach (var placement in new[] { (origin: 0f, plane: 1f), (origin: 0f, plane: -1f), (origin: 2f, plane: -1f) })
                {
                    root.transform.position = new Vector3(0, placement.origin, 0);
                    effect.Reinit(); effect.SetVector3("CollisionPosition", new Vector3(0, placement.plane, 0)); effect.Play();
                    for (int frame = 0; frame < 36; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var pixels = image.GetPixels32(); int minimum = 640;
                        for (int y = 0; y < 640; y++) for (int x = 0; x < 640; x++)
                        { var pixel = pixels[y * 640 + x]; if (pixel.r > 15 || pixel.g > 15 || pixel.b > 15) minimum = System.Math.Min(minimum, y); }
                        floors[index++] = minimum;
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
                Debug.Log("TRAVERSAL_VFX_DYNAMIC_FLOORS high=" + floors[0] + " low=" + floors[1] + " translated=" + floors[2]);
                Assert.That(floors[0], Is.GreaterThan(floors[1] + 150), "Changing the plane height should move the collision boundary");
                Assert.That(floors[2], Is.InRange(floors[0] - 3, floors[0] + 3), "The plane must follow the effect's local-to-world transform");
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(root); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CapturePlaneCollisionComparison()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstCyanVisible.vfx");
            var collision = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstCollision.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(collision, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Collision comparison camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -8); camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 640, 24); target.Create();
            var root = new GameObject("Collision comparison effect");
            var effect = root.AddComponent<VisualEffect>();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/TraversalVfxCollision"));
            Directory.CreateDirectory(output);
            try
            {
                int freeBelow = 0, collisionBelow = 0;
                foreach (var entry in new[] { ("free", free), ("collision", collision) })
                {
                    effect.visualEffectAsset = entry.Item2;
                    effect.initialEventName = "DisabledAutoStart";
                    effect.resetSeedOnPlay = false; effect.startSeed = 173;
                    effect.pause = true; effect.Reinit(); effect.Play();
                    for (int frame = 0; frame < 36; frame++)
                    {
                        effect.Simulate(1f / 60f, 1);
                        EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000), "Expected a live GPU burst");
                    var path = Path.Combine(output, entry.Item1 + "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var pixels = image.GetPixels32(); int below = 0;
                        // With this orthographic camera the y=0 plane is at row 320; ignore its 15-pixel border.
                        for (int y = 0; y < 305; y++) for (int x = 0; x < 640; x++)
                        { var pixel = pixels[y * 640 + x]; if (pixel.r > 15 || pixel.g > 15 || pixel.b > 15) below++; }
                        if (entry.Item1 == "free") freeBelow = below; else collisionBelow = below;
                        File.WriteAllBytes(path, image.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                    Debug.Log("TRAVERSAL_VFX_COLLISION_CAPTURE " + entry.Item1 + " " + path + " particles=" + effect.aliveParticleCount);
                }
                Debug.Log("TRAVERSAL_VFX_COLLISION_PIXELS freeBelow=" + freeBelow + " collisionBelow=" + collisionBelow);
                Assert.That(freeBelow, Is.GreaterThan(100), "Free particles should fall below the test plane");
                Assert.That(collisionBelow, Is.LessThan(10), "Collision particles should stay above the test plane");
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(root); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
