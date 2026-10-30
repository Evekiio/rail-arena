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
    /// <summary>Opt-in development-player comparison of SSAO frame timings.</summary>
    public sealed class SsaoProfilingRun : MonoBehaviour
    {
        [Serializable] private sealed class Run { public string quality; public int validSamples; public double medianGpuMs; }
        [Serializable] private sealed class Report { public string gpu, graphicsApi; public int width, height; public Run[] runs; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--profile-ssao") >= 0)
                new GameObject("SSAO profiling run").AddComponent<SsaoProfilingRun>();
        }
        private IEnumerator Start()
        {
            yield return null;
            string[] args = Environment.GetCommandLineArgs(); int outputIndex = Array.IndexOf(args, "--validation-output");
            if (outputIndex < 0 || outputIndex + 1 >= args.Length) throw new ArgumentException("--profile-ssao requires --validation-output");
            string output = Path.GetFullPath(args[outputIndex + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output));
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new Exception("Active quality is not URP");
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var renderers = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", fields).GetValue(pipeline);
            var index = (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", fields).GetValue(pipeline);
            var feature = renderers[index].rendererFeatures.Single(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            if (!feature.isActive) throw new Exception("SSAO is disabled");
            var settings = feature.GetType().GetField("m_Settings", fields).GetValue(feature);
            var samples = settings.GetType().GetField("Samples", fields | BindingFlags.Public);
            var original = samples.GetValue(settings);
            var session = FindFirstObjectByType<ArenaSession>(); session.enabled = false;
            var camera = Camera.main; camera.transform.SetPositionAndRotation(new Vector3(6, 24, -5), Quaternion.LookRotation(new Vector3(0, 23, 0) - new Vector3(6, 24, -5)));
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            var timing = new FrameTiming[1]; var runs = new List<Run>();
            try
            {
                foreach (string mode in new[] { "Medium", "High", "Medium", "High" })
                {
                    samples.SetValue(settings, Enum.Parse(samples.FieldType, mode)); var gpu = new List<double>();
                    for (int frame = 0; frame < 360; frame++)
                    {
                        FrameTimingManager.CaptureFrameTimings(); yield return null;
                        if (frame >= 60 && FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0) gpu.Add(timing[0].gpuFrameTime);
                    }
                    gpu.Sort(); runs.Add(new Run { quality = mode, validSamples = gpu.Count, medianGpuMs = gpu.Count == 0 ? 0 : gpu[gpu.Count / 2] });
                }
                File.WriteAllText(output, JsonUtility.ToJson(new Report { gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(), width = Screen.width, height = Screen.height, runs = runs.ToArray() }, true));
                Debug.Log("SSAO_PROFILE_COMPLETE " + output);
            }
            finally { samples.SetValue(settings, original); }
            Application.Quit(runs.All(r => r.validSamples > 0) ? 0 : 2);
        }
    }
}
#endif
