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
    public sealed class TraversalVfxArenaHybridTests
    {
        [UnityTest]
        public IEnumerator HybridPlaneConstrainsOffscreenParticles()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            var free = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalFreeV3.vfx");
            var depth = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + "TraversalBurstDirectionalHybrid.vfx");
            Assert.That(free, Is.Not.Null); Assert.That(depth, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var gate = session.World.Portals[0]; var origin = gate.transform.position + gate.transform.forward * 1.5f + Vector3.up * 5;
            Assert.That(Physics.Raycast(origin, Vector3.down, out var hit, 12f, 1 << 0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(Vector3.Dot(hit.normal, Vector3.up), Is.GreaterThan(.99f));
            var floorPoint = hit.point; Debug.Log("TRAVERSAL_ARENA_FLOOR " + hit.collider.name + " " + floorPoint);
            var camera = session.View; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldTarget = camera.targetTexture; var oldFov = camera.fieldOfView; var oldFar = camera.farClipPlane;
            var target = new RenderTexture(640, 640, 24); target.Create(); camera.targetTexture = target;
            camera.fieldOfView = 60; camera.farClipPlane = 50; camera.GetUniversalAdditionalCameraData();
            var observer = new GameObject("Arena floor VFX observer").AddComponent<Camera>(); observer.enabled = false;
            observer.transform.position = floorPoint + new Vector3(0, 0, -12); observer.orthographic = true; observer.orthographicSize = 5;
            observer.clearFlags = CameraClearFlags.SolidColor; observer.backgroundColor = Color.black; observer.cullingMask = 1 << 8;
            var observation = new RenderTexture(640, 640, 24); observation.Create();
            var root = new GameObject("Arena floor directional VFX"); root.layer = 8; root.transform.position = floorPoint + Vector3.up * 1.85f;
            var effect = root.AddComponent<VisualEffect>(); effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                foreach (var entry in new[] { ("free", free), ("depth", depth) })
                {
                    effect.visualEffectAsset = entry.Item2; effect.Reinit(); if (entry.Item1 == "depth") { effect.SetVector3("CollisionPosition", root.transform.InverseTransformPoint(floorPoint)); effect.SetVector3("CollisionNormal", Vector3.up); } effect.Play();
                    for (int frame = 0; frame < 8; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        camera.transform.position = floorPoint + new Vector3(0, 1.66f, -4f); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    effect.Reinit(); if (entry.Item1 == "depth") { effect.SetVector3("CollisionPosition", root.transform.InverseTransformPoint(floorPoint)); effect.SetVector3("CollisionNormal", Vector3.up); } effect.Play();
                    for (int frame = 0; frame < 35; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        camera.transform.position = floorPoint + new Vector3(0, 1.66f, -4f); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    RenderPipeline.SubmitRenderRequest(observer, new RenderPipeline.StandardRequest { destination = observation });
                    var previous = RenderTexture.active; var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = observation; image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
                        var pixels = image.GetPixels32(); int below = 0, above = 0;
                        for (int y = 0; y < 640; y++) for (int x = 0; x < 640; x++)
                        {
                            var pixel = pixels[y * 640 + x]; if (pixel.r <= 10 && pixel.g <= 10 && pixel.b <= 10) continue;
                            if (y < 315) below++; else above++;
                        }
                        Debug.Log("TRAVERSAL_ARENA_HYBRID_RESULT " + entry.Item1 + " below=" + below + " above=" + above + " alive=" + effect.aliveParticleCount);
                        Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                        if (entry.Item1 == "free") Assert.That(below, Is.GreaterThan(0));
                        else { Assert.That(below, Is.EqualTo(0)); Assert.That(above, Is.GreaterThan(0)); }
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.fieldOfView = oldFov; camera.farClipPlane = oldFar; camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                target.Release(); observation.Release(); Object.Destroy(target); Object.Destroy(observation); Object.Destroy(root); Object.Destroy(observer.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}




