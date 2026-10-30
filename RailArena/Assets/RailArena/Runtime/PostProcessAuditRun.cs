#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailArena
{
    public sealed class PostProcessAuditRun : MonoBehaviour
    {
        [Serializable] private sealed class ShaderStatus { public string name; public bool found, supported; }
        [Serializable] private sealed class Report { public string pipeline, graphicsApi, antialiasing, toneMapping; public bool postProcessing, bloomActive, motionBlurActive; public ShaderStatus[] shaders; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--audit-postprocessing") >= 0)
                new GameObject("Post processing audit").AddComponent<PostProcessAuditRun>();
        }
        private IEnumerator Start()
        {
            for (int i = 0; i < 5; i++) yield return null;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--validation-output");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("--audit-postprocessing requires --validation-output");
            var camera = Camera.main;
            var data = camera.GetUniversalAdditionalCameraData();
            var stack = VolumeManager.instance.stack;
            string[] names = { "Bloom", "CameraMotionBlur", "LutBuilderHdr", "UberPost", "FinalPost", "SubpixelMorphologicalAntialiasing", "GaussianDepthOfField", "BokehDepthOfField", "PaniniProjection" };
            var shaders = new ShaderStatus[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var shader = Shader.Find("Hidden/Universal Render Pipeline/" + names[i]);
                shaders[i] = new ShaderStatus { name = names[i], found = shader != null, supported = shader != null && shader.isSupported };
            }
            var bloom = stack.GetComponent<Bloom>();
            var blur = stack.GetComponent<MotionBlur>();
            var tone = stack.GetComponent<Tonemapping>();
            var pipeline = QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            var report = new Report { pipeline = pipeline != null ? pipeline.name : "none", graphicsApi = SystemInfo.graphicsDeviceType.ToString(), antialiasing = data.antialiasing.ToString(), postProcessing = data.renderPostProcessing, bloomActive = bloom != null && bloom.active && bloom.IsActive(), motionBlurActive = blur != null && blur.active && blur.IsActive(), toneMapping = tone != null && tone.active ? tone.mode.value.ToString() : "inactive", shaders = shaders };
            string output = Path.GetFullPath(args[index + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("POST_PROCESS_AUDIT_COMPLETE " + output);
            Application.Quit();
        }
    }
}
#endif
