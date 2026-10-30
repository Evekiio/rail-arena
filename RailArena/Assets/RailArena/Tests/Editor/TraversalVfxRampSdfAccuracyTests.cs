using System.Collections;
using System.Linq;
using UnityEngine.VFX.SDF;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxRampSdfAccuracyTests
    {
        [UnityTest] public IEnumerator SampleWestEdge() => Run(-7.5f);
        [UnityTest] public IEnumerator SampleWestSlope() => Run(-5f);
        [UnityTest] public IEnumerator SampleCenter() => Run(0f);
        [UnityTest] public IEnumerator SampleEastSlope() => Run(5f);
        [UnityTest] public IEnumerator SampleEastEdge() => Run(7.5f);
        private IEnumerator Run(float offset)
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalFreeV3.vfx");
            var depth = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalSdf.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(depth, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var gate = session.World.Portals[0];
            var floorMesh = session.World.GetComponentsInChildren<MeshCollider>().Single(c => c.name == "R1");
            var localBounds = floorMesh.sharedMesh.bounds;
            var origin = floorMesh.transform.TransformPoint(localBounds.center + Vector3.right * offset) + Vector3.up * 6;
            Assert.That(floorMesh.Raycast(new Ray(origin, Vector3.down), out var hit, 12f), Is.True);
            var floorPoint = hit.point; Debug.Log("TRAVERSAL_ARENA_RAMP " + floorMesh.name + " " + floorPoint);
            var baker = new MeshToSDFBaker(localBounds.size + Vector3.one * .8f, localBounds.center, 64, floorMesh.sharedMesh); baker.BakeSDF();
            var camera = session.View; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldTarget = camera.targetTexture; var oldFov = camera.fieldOfView; var oldFar = camera.farClipPlane;
            var target = new RenderTexture(640, 640, 24); target.Create(); camera.targetTexture = target;
            camera.fieldOfView = 60; camera.farClipPlane = 50; camera.GetUniversalAdditionalCameraData();
            var observer = new GameObject("Arena floor VFX observer").AddComponent<Camera>(); observer.enabled = false;
            observer.transform.position = floorPoint + new Vector3(0, 0, -12); observer.orthographic = true; observer.orthographicSize = 5;
            observer.clearFlags = CameraClearFlags.SolidColor; observer.backgroundColor = Color.black; observer.cullingMask = 1 << 8;
            var observation = new RenderTexture(640, 640, 24); observation.Create();
            var root = new GameObject("Arena floor directional VFX"); root.layer = 8; root.transform.position = floorPoint + Vector3.up * 1.85f;
            var bridge = new TraversalVfxPrototype();
            var effect = root.AddComponent<VisualEffect>(); effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                foreach (var entry in new[] { ("free", free), ("depth", depth) })
                {
                    effect.visualEffectAsset = entry.Item2; effect.Reinit(); if (entry.Item1 == "depth") { effect.SetTexture("SdfTexture", baker.SdfTexture); effect.SetVector3("SdfCenter", root.transform.InverseTransformPoint(floorMesh.transform.TransformPoint(localBounds.center))); effect.SetVector3("SdfSize", baker.GetActualBoxSize()); } effect.Play();
                    for (int frame = 0; frame < 8; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        camera.transform.position = floorPoint + new Vector3(0, 1.66f, -4f); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    }
                    effect.Reinit(); if (entry.Item1 == "depth") { effect.SetTexture("SdfTexture", baker.SdfTexture); effect.SetVector3("SdfCenter", root.transform.InverseTransformPoint(floorMesh.transform.TransformPoint(localBounds.center))); effect.SetVector3("SdfSize", baker.GetActualBoxSize()); } effect.Play();
                    for (int frame = 0; frame < 35; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        camera.transform.position = floorPoint + new Vector3(0, 1.66f, -4f); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    }
                    RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = observation; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var pixels = image.GetPixels32(); int below = 0, above = 0, sumRows = 0, lit = 0;
                        for (int y = 0; y < 640; y++) for (int x = 0; x < 640; x++)
                        {
                            var pixel = pixels[y * 640 + x]; if (pixel.r <= 10 && pixel.g <= 10 && pixel.b <= 10) continue;
                            if (y < 315) below++; else above++; sumRows += y; lit++;
                        }
                        Debug.Log("TRAVERSAL_ARENA_SDF_SLOPE " + entry.Item1 + " below=" + below + " above=" + above + " alive=" + effect.aliveParticleCount);
                        Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                        if (entry.Item1 == "free") Assert.That(below, Is.GreaterThan(0));
                        else { Assert.That(below, Is.EqualTo(0)); Assert.That(above, Is.GreaterThan(0)); float heightError = ((float)sumRows / lit - 320f) * (10f / 640f); Debug.Log("TRAVERSAL_SDF_HEIGHT offset=" + offset + " heightError=" + heightError); Assert.That(Mathf.Abs(heightError), Is.LessThan(.4f)); }
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
            }
            finally
            {
                bridge.Dispose(); baker.Dispose(); camera.targetTexture = oldTarget; camera.fieldOfView = oldFov; camera.farClipPlane = oldFar; camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                target.Release(); observation.Release(); Object.Destroy(target); Object.Destroy(observation); Object.Destroy(root); Object.Destroy(observer.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}









