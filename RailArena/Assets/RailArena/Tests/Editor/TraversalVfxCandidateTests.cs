using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using RailArena.Validation;

namespace RailArena.Tests
{
    public sealed class TraversalVfxCandidateTests
    {
        [UnityTest]
        public IEnumerator BurstGraphCompilesAndSimulatesOnTheGpu() => RunBurst("TraversalBurst.vfx", 128);

        [UnityTest]
        public IEnumerator LargeBurstSimulatesThousandsOfParticles() => RunBurst("TraversalBurstLarge.vfx", 2000);

        [UnityTest]
        public IEnumerator CollisionCandidateSimulatesThousandsOfParticles() => RunBurst("TraversalBurstCollision.vfx", 2000);

        [UnityTest]
        public IEnumerator RealPortalActivationTriggersCandidateAndHonorsReducedFlash()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV3.vfx");
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.World.Portals.Length, Is.GreaterThan(0));
            var presentation = session.World.Portals[0].Presentation;
            Assert.That(presentation, Is.Not.Null);
            bool previous = session.Comfort.ReducedFlash;
            var camera = new GameObject("VFX portal test camera").AddComponent<Camera>();
            var center = presentation.transform.position + Vector3.up * 1.7f;
            camera.transform.position = center + presentation.transform.forward * 2.5f;
            camera.transform.LookAt(center);
            var target = new RenderTexture(1280, 720, 24); target.Create();
            var root = new GameObject("VFX portal prototype"); root.transform.position = center + presentation.transform.up * 1.5f;
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var bridge = new TraversalVfxPrototype(); bridge.Bind(presentation, effect);
            try
            {
                var rayOrigin = presentation.transform.position + presentation.transform.forward * 1.5f + Vector3.up * 5f;
                Assert.That(Physics.Raycast(rayOrigin, Vector3.down, out var hit, 12f, 1 << 0, QueryTriggerInteraction.Ignore), Is.True, "Expected a surface near the portal");
                Assert.That(bridge.TrySetSurface(hit.point, Vector3.zero), Is.False, "A zero normal must be rejected");
                Assert.That(bridge.TrySetSurface(hit.point, hit.normal), Is.True, "Expected a usable arena surface");
                Assert.That(Vector3.Distance(root.transform.TransformPoint(effect.GetVector3("CollisionPosition")), hit.point), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(root.transform.TransformDirection(effect.GetVector3("CollisionNormal")).normalized, hit.normal.normalized), Is.LessThan(.001f));
                Debug.Log("TRAVERSAL_VFX_SURFACE_BOUND point=" + hit.point + " collider=" + hit.collider.name);
                session.Comfort.ReducedFlash = false;
                presentation.ResetPulse();
                var captureId = System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                SaveCapture(target, "portal-before-" + captureId + ".png");
                Assert.That(presentation.Activate(), Is.True);
                float activationTime = Time.realtimeSinceStartup;
                Assert.That(bridge.BurstsRequested, Is.EqualTo(1));
                Assert.That(presentation.Activate(), Is.False, "Activation cooldown must remain intact");
                Assert.That(bridge.BurstsRequested, Is.EqualTo(1));
                int maximum = 0; double deadline = EditorApplication.timeSinceStartup + 15;
                while (EditorApplication.timeSinceStartup < deadline && maximum < 2000)
                {
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    maximum = System.Math.Max(maximum, effect.aliveParticleCount);
                }
                Assert.That(maximum, Is.GreaterThanOrEqualTo(2000));
                SaveCapture(target, "portal-burst-" + captureId + ".png");
                foreach (var sampleTime in new[] { .15f, .35f, .65f })
                {
                    while (Time.realtimeSinceStartup - activationTime < sampleTime)
                    {
                        EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    }
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    float actual = Time.realtimeSinceStartup - activationTime;
                    SaveCapture(target, "portal-t" + Mathf.RoundToInt(sampleTime * 1000) + "-" + captureId + ".png");
                    Debug.Log("TRAVERSAL_VFX_CAPTURE_TIME requested=" + sampleTime + " actual=" + actual + " count=" + effect.aliveParticleCount);
                }
                session.Comfort.ReducedFlash = true;
                presentation.ResetPulse();
                Assert.That(presentation.Activate(), Is.True);
                Assert.That(bridge.BurstsRequested, Is.EqualTo(1), "Reduced flash must suppress the prototype burst");
                bridge.Unbind();
                session.Comfort.ReducedFlash = false;
                presentation.ResetPulse();
                effect.gameObject.SetActive(false);
                Assert.That(presentation.Activate(), Is.True);
                Assert.That(bridge.BurstsRequested, Is.EqualTo(1), "Unbound prototype must not respond");
                float baselineTime = Time.realtimeSinceStartup;
                foreach (var sampleTime in new[] { .15f, .35f, .65f })
                {
                    while (Time.realtimeSinceStartup - baselineTime < sampleTime)
                    {
                        EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    }
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    float actual = Time.realtimeSinceStartup - baselineTime;
                    SaveCapture(target, "portal-existing-t" + Mathf.RoundToInt(sampleTime * 1000) + "-" + captureId + ".png");
                    Debug.Log("TRAVERSAL_VFX_BASELINE_TIME requested=" + sampleTime + " actual=" + actual);
                }
                Debug.Log("TRAVERSAL_VFX_PORTAL_EVENT_VERIFIED particles=" + maximum);
            }
            finally
            {
                session.Comfort.ReducedFlash = previous;
                bridge.Unbind(); target.Release();
                Object.Destroy(target); Object.Destroy(root); Object.Destroy(camera.gameObject);
            }
            yield return new ExitPlayMode();
        }

        private static void SaveCapture(RenderTexture target, string name)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/TraversalVfxCaptures"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, name);
            if (File.Exists(path)) throw new IOException("Capture already exists: " + path);
            var previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("TRAVERSAL_VFX_CAPTURE " + path);
            }
            finally { RenderTexture.active = previous; Object.Destroy(image); }
        }

        private static IEnumerator RunBurst(string filename, int minimum)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/" + filename);
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("VFX test camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            var target = new RenderTexture(256, 256, 24);
            target.Create();
            var root = new GameObject("VFX candidate test");
            var effect = root.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            var systems = new List<string>();
            effect.GetParticleSystemNames(systems);
            Assert.That(systems, Does.Contain("Simple Burst"));
            effect.initialEventName = "DisabledAutoStart";
            effect.Reinit();
            for (int i = 0; i < 8; i++)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            }
            Assert.That(effect.aliveParticleCount, Is.EqualTo(0), "Particles started before the explicit event");
            effect.Play();
            int maximum = 0;
            double deadline = EditorApplication.timeSinceStartup + 15;
            while (EditorApplication.timeSinceStartup < deadline && maximum < minimum)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                maximum = System.Math.Max(maximum, effect.aliveParticleCount);
            }
            Debug.Log("TRAVERSAL_VFX_SIMULATION count=" + maximum + " culled=" + effect.culled + " frames=" + Time.frameCount);
            Assert.That(maximum, Is.GreaterThanOrEqualTo(minimum), "The GPU graph did not emit the expected particle count");
            target.Release();
            Object.Destroy(target);
            Object.Destroy(root);
            Object.Destroy(camera.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator StopPlayMode() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
