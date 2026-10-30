#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailArena
{
    public sealed class AtmosphereProfilingRun : MonoBehaviour
    {
        [Serializable] private sealed class Run { public string mode; public int validSamples; public double medianGpuMs; }
        [Serializable] private sealed class Report { public string gpu, graphicsApi; public int width, height; public Run[] runs; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--profile-atmosphere") >= 0)
                new GameObject("Atmosphere profiling run").AddComponent<AtmosphereProfilingRun>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "--validation-output");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length) throw new ArgumentException("--profile-atmosphere requires --validation-output");
            string output = Path.GetFullPath(args[outputIndex + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new Exception("Active quality is not URP");
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var renderers = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", fields).GetValue(pipeline);
            int index = (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", fields).GetValue(pipeline);
            var feature = renderers[index].rendererFeatures.Single(f => f != null && f.GetType().Name == "AtmosphereCandidateFeature");
            bool original = feature.isActive;
            var session = FindFirstObjectByType<ArenaSession>();
            if (session != null) session.enabled = false;
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(6, 24, -5), Quaternion.LookRotation(new Vector3(0, 23, 0) - new Vector3(6, 24, -5)));
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var timing = new FrameTiming[1];
            var runs = new List<Run>();
            try
            {
                foreach (bool active in new[] { false, true, false, true })
                {
                    feature.SetActive(active);
                    var gpu = new List<double>();
                    for (int frame = 0; frame < 300; frame++)
                    {
                        FrameTimingManager.CaptureFrameTimings();
                        yield return null;
                        if (frame >= 60 && FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0) gpu.Add(timing[0].gpuFrameTime);
                    }
                    gpu.Sort();
                    runs.Add(new Run { mode = active ? "On" : "Off", validSamples = gpu.Count, medianGpuMs = gpu.Count == 0 ? 0 : gpu[gpu.Count / 2] });
                }
                File.WriteAllText(output, JsonUtility.ToJson(new Report { gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(), width = Screen.width, height = Screen.height, runs = runs.ToArray() }, true));
                Debug.Log("ATMOSPHERE_PROFILE_COMPLETE " + output);
            }
            finally { feature.SetActive(original); }
            Application.Quit(runs.All(r => r.validSamples > 0) ? 0 : 2);
        }
    }
}
#endif
