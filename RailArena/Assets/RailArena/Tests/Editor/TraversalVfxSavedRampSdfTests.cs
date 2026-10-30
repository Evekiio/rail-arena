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
    public sealed class TraversalVfxSavedRampSdfTests
    {
        [System.Serializable] private sealed class Metadata { public string sourceGuid; public string sourceDependencyHash; public string generatorDependencyHash; public string unityVersion; public string vfxPackageVersion; public float padding; public int signPasses; public float threshold; public float sdfOffset; public Vector3 center; public Vector3 size; public int maxResolution; }
        [UnityTest]
        public IEnumerator SavedSdfConstrainsParticlesOnSlopedSection()
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
            var origin = floorMesh.transform.TransformPoint(localBounds.center + Vector3.right * 5f) + Vector3.up * 6;
            Assert.That(floorMesh.Raycast(new Ray(origin, Vector3.down), out var hit, 12f), Is.True);
            var floorPoint = hit.point; Debug.Log("TRAVERSAL_ARENA_RAMP " + floorMesh.name + " " + floorPoint);
            var saved = AssetDatabase.LoadAssetAtPath<Texture3D>(folder + "R1Sdf64V3.asset");
            var metaText = AssetDatabase.LoadAssetAtPath<TextAsset>(folder + "R1Sdf64V3.json"); Assert.That(saved, Is.Not.Null); Assert.That(metaText, Is.Not.Null);
            var metadata = JsonUtility.FromJson<Metadata>(metaText.text);
            Assert.That(metadata.sourceGuid, Is.EqualTo(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(floorMesh.sharedMesh))));
            Assert.That(metadata.sourceDependencyHash, Is.EqualTo(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(floorMesh.sharedMesh)).ToString()));
            Assert.That(metadata.generatorDependencyHash, Is.EqualTo(AssetDatabase.GetAssetDependencyHash("Assets/RailArena/Editor/RampSdfAssetCandidate.cs").ToString()));
            Assert.That(metadata.unityVersion, Is.EqualTo(Application.unityVersion));
            Assert.That(metadata.vfxPackageVersion, Is.EqualTo(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MeshToSDFBaker).Assembly).version));
            Assert.That(metadata.maxResolution, Is.EqualTo(64)); Assert.That(metadata.padding, Is.EqualTo(.8f));
            Assert.That(metadata.signPasses, Is.EqualTo(1)); Assert.That(metadata.threshold, Is.EqualTo(.5f)); Assert.That(metadata.sdfOffset, Is.EqualTo(0));
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
                    effect.visualEffectAsset = entry.Item2; effect.Reinit(); if (entry.Item1 == "depth") { effect.SetTexture("SdfTexture", saved); effect.SetVector3("SdfCenter", root.transform.InverseTransformPoint(floorMesh.transform.TransformPoint(metadata.center))); effect.SetVector3("SdfSize", metadata.size); } effect.Play();
                    for (int frame = 0; frame < 8; frame++)
                    {
                        effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        camera.transform.position = floorPoint + new Vector3(0, 1.66f, -4f); camera.transform.rotation = Quaternion.LookRotation(Vector3.back);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    }
                    effect.Reinit(); if (entry.Item1 == "depth") { effect.SetTexture("SdfTexture", saved); effect.SetVector3("SdfCenter", root.transform.InverseTransformPoint(floorMesh.transform.TransformPoint(metadata.center))); effect.SetVector3("SdfSize", metadata.size); } effect.Play();
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
                        Debug.Log("TRAVERSAL_SAVED_SDF_SLOPE " + entry.Item1 + " below=" + below + " above=" + above + " alive=" + effect.aliveParticleCount);
                        Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000));
                        if (entry.Item1 == "free") Assert.That(below, Is.GreaterThan(0));
                        else { Assert.That(below, Is.EqualTo(0)); Assert.That(above, Is.GreaterThan(0)); }
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                }
            }
            finally
            {
                bridge.Dispose(); camera.targetTexture = oldTarget; camera.fieldOfView = oldFov; camera.farClipPlane = oldFar; camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                target.Release(); observation.Release(); Object.Destroy(target); Object.Destroy(observation); Object.Destroy(root); Object.Destroy(observer.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}










