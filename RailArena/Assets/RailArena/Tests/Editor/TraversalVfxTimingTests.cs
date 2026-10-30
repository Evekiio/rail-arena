using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class TraversalVfxTimingTests
    {
        [Serializable] private sealed class Run { public string mode; public int validGpuSamples; public double medianGpuMs; public int maximumParticles; }
        [Serializable] private sealed class Report { public string gpu, api, warning; public Run[] runs; }

        [UnityTest]
        public IEnumerator CompareGpuTimingAvailability()
        {
            bool oldTiming = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
                EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
                var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstCyanVisible.vfx");
                Assert.That(graph, Is.Not.Null);
                yield return new EnterPlayMode(); yield return null;
                var session = Object.FindAnyObjectByType<ArenaSession>();
                var presentation = session.World.Portals[0].Presentation;
                bool oldReduced = session.Comfort.ReducedFlash;
                session.Comfort.ReducedFlash = false;
                var center = presentation.transform.position + Vector3.up * 1.7f;
                var camera = new GameObject("VFX timing camera").AddComponent<Camera>();
                camera.transform.position = center + presentation.transform.forward * 5;
                camera.transform.LookAt(center);
                var target = new RenderTexture(1280, 720, 24); target.Create();
                var root = new GameObject("VFX timing candidate"); root.transform.position = center + presentation.transform.up * 1.5f;
                var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
                var bridge = new TraversalVfxPrototype(); bridge.Bind(presentation, effect);
                var readings = new FrameTiming[1]; var runs = new List<Run>();
                try
                {
                    foreach (string mode in new[] { "existing", "candidate", "existing", "candidate" })
                    {
                        bool active = mode == "candidate";
                        root.SetActive(active);
                        if (active) bridge.Bind(presentation, effect);
                        var gpu = new List<double>(); int maxParticles = 0;
                        for (int frame = 0; frame < 180; frame++)
                        {
                            if (frame % 30 == 0) { presentation.ResetPulse(); presentation.Activate(); }
                            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                            FrameTimingManager.CaptureFrameTimings();
                            EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                            if (active) maxParticles = Math.Max(maxParticles, effect.aliveParticleCount);
                            if (frame >= 30 && FrameTimingManager.GetLatestTimings(1, readings) > 0 && readings[0].gpuFrameTime > 0)
                                gpu.Add(readings[0].gpuFrameTime);
                        }
                        gpu.Sort();
                        runs.Add(new Run { mode = mode, validGpuSamples = gpu.Count, medianGpuMs = gpu.Count == 0 ? -1 : gpu[gpu.Count / 2], maximumParticles = maxParticles });
                        Debug.Log("TRAVERSAL_VFX_TIMING mode=" + mode + " valid=" + gpu.Count + " maxParticles=" + maxParticles + " medianMs=" + (gpu.Count == 0 ? "unavailable" : gpu[gpu.Count / 2].ToString("F3")));
                    }
                    Assert.That(runs.FindAll(run => run.mode == "candidate").TrueForAll(run => run.maximumParticles >= 2000), Is.True, "Candidate must be active during profiling");
                    var report = new Report { gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(), warning = "Editor/offscreen timings are diagnostic only; -1 means GPU cost is unavailable.", runs = runs.ToArray() };
                    var name = "traversal-vfx-timing-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json";
                    var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29", name));
                    if (File.Exists(output)) throw new IOException("Timing output already exists");
                    File.WriteAllText(output, JsonUtility.ToJson(report, true));
                }
                finally
                {
                    session.Comfort.ReducedFlash = oldReduced; bridge.Unbind(); target.Release();
                    Object.Destroy(target); Object.Destroy(root); Object.Destroy(camera.gameObject);
                }
                yield return new ExitPlayMode();
            }
            finally { PlayerSettings.enableFrameTimingStats = oldTiming; }
        }

        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
