#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena
{
    /// <summary>Opt-in normal player rendering probe; never writes saved user preferences.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class ArtPerformanceValidation : MonoBehaviour
    {
        [Serializable] sealed class Reading
        {
            public string view;
            public int frames, validGpuSamples, nonBlackSamples;
            public float meanFrameMs, p95FrameMs;
            public double medianGpuMs = -1;
            public bool frameBudgetMet;
            public int portalRenders;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Version 2: observer stages after ArenaSession and before portal views; forced 1080p main rendering at EndOfFrame with synchronized one-pixel readback. High tier, 100-degree vertical FOV, one automated human and three bots. Timings include readback overhead. Earlier probes staged after portal views and are not equivalent portal workloads. Overlay HUD, native swapchain/input/audio and sustained visible-play acceptance remain open. GPU -1 means unavailable.";
            public int measurementVersion=2;
            public string gpu, api, fingerprint;
            public int width, height, exceptions;
            public Reading[] views;
            public bool passed, allViewsWithinBudget;
        }
        ArenaSession session;
        Camera view;
        Vector3 position, target;
        bool staged;
        int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--validate-art-performance") >= 0)
                new GameObject("Integrated art performance validation").AddComponent<ArtPerformanceValidation>();
        }
        void OnEnable() => Application.logMessageReceived += Log;
        void OnDisable() => Application.logMessageReceived -= Log;
        void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        void LateUpdate()
        {
            if (!staged || view == null) return;
            view.transform.position = position; view.transform.LookAt(target);
            view.focalLength=Camera.FieldOfViewToFocalLength(100,view.sensorSize.y);
        }
        IEnumerator Start()
        {
            yield return null;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--validation-output");
            string output = index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : Path.Combine(Application.persistentDataPath, "art-performance.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            session = FindFirstObjectByType<ArenaSession>(); view = session.View;
            session.Aim.FieldOfView=100;session.Graphics.Tier = GraphicsTier.High;session.Graphics.Smaa=true;session.Graphics.MotionBlur=true; session.Graphics.Apply(view);
            session.AutomatedHuman = true; session.StartSession(true, 3, 1, TargetScenario.Combat);
            yield return new WaitForSecondsRealtime(5);
            view.cullingMask = (view.cullingMask | (1 << 8)) & ~(1 << 11);
            view.enabled = false;
            var targetTexture = new RenderTexture(1920,1080,24); targetTexture.Create();
            var synchronizationPixel = new Texture2D(1,1,TextureFormat.RGB24,false);
            var capturePixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var readings = new List<Reading>(); var timing = new FrameTiming[1];
            var endOfFrame=new WaitForEndOfFrame();var request=new RenderPipeline.StandardRequest{destination=targetTexture};
            var portalViews=FindObjectsByType<PortalView>(FindObjectsSortMode.None);
            var portal = session.World.Portals[0];
            var poses = new[] {
                ("combat", new Vector3(25,3,-14), new Vector3(0,5,0)),
                ("summit", new Vector3(14,31,-16), new Vector3(0,23,0)),
                ("basement", new Vector3(43,-3,43), new Vector3(32,-9,32)),
                ("foliage-waterfall", new Vector3(-35,-6,23), new Vector3(-31,-10,16)),
                ("portal", portal.transform.position + portal.transform.forward * 3 + Vector3.up * 1.5f, portal.transform.position + Vector3.up * 1.1f) };
            foreach (var pose in poses)
            {
                position = pose.Item2; target = pose.Item3; staged = true;
                var frames = new List<float>(); var gpu = new List<double>();
                int initialPortalFrames=0;
                for (int frame = 0; frame < 240; frame++)
                {
                    if(frame==60)foreach(var portalView in portalViews)initialPortalFrames+=portalView.RenderedFrames;
                    yield return endOfFrame;
                    RenderPipeline.SubmitRenderRequest(view, request);
                    var previousTarget = RenderTexture.active;
                    RenderTexture.active = targetTexture;
                    synchronizationPixel.ReadPixels(new Rect(0,0,1,1),0,0,false);
                    RenderTexture.active = previousTarget;
                    FrameTimingManager.CaptureFrameTimings(); yield return null;
                    if (frame < 60) continue;
                    frames.Add(Time.unscaledDeltaTime * 1000);
                    if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0) gpu.Add(timing[0].gpuFrameTime);
                }
                frames.Sort(); gpu.Sort(); float total = 0; foreach (float ms in frames) total += ms;
                var reading = new Reading { view = pose.Item1, frames = frames.Count, meanFrameMs = total / frames.Count,
                    p95FrameMs = frames[(int)(frames.Count * .95f)], validGpuSamples = gpu.Count,
                    medianGpuMs = gpu.Count == 0 ? -1 : gpu[gpu.Count / 2] };
                foreach(var portalView in portalViews)reading.portalRenders+=portalView.RenderedFrames;
                reading.portalRenders-=initialPortalFrames;
                reading.frameBudgetMet = reading.p95FrameMs <= 16.67f; readings.Add(reading);
                var previousCaptureTarget = RenderTexture.active;
                RenderTexture.active = targetTexture;
                capturePixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false); capturePixels.Apply();
                RenderTexture.active = previousCaptureTarget;
                var pixels = capturePixels.GetPixels32();
                for (int pixel = 0; pixel < pixels.Length; pixel += 64)
                    if (pixels[pixel].r > 8 || pixels[pixel].g > 8 || pixels[pixel].b > 8) reading.nonBlackSamples++;
                File.WriteAllBytes(output + "." + pose.Item1 + ".png", capturePixels.EncodeToPNG());
                yield return null;
            }
            var report = new Report { gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                fingerprint = Resources.Load<TextAsset>("BuildIdentity").text.Trim(), width = Screen.width, height = Screen.height,
                exceptions = errors, views = readings.ToArray(), passed = errors == 0 && Screen.width == 1920 && Screen.height == 1080 && readings.TrueForAll(r => r.nonBlackSamples > 100),
                allViewsWithinBudget = readings.TrueForAll(r => r.frameBudgetMet) };
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            targetTexture.Release(); Destroy(targetTexture); Destroy(synchronizationPixel); Destroy(capturePixels);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
