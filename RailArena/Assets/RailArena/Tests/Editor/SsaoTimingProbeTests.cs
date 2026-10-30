using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class SsaoTimingProbeTests
    {
        [UnityTest]
        public IEnumerator ReportEditorGpuTimingAvailability()
        {
            bool originalTimingStats = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity"); yield return new EnterPlayMode(); yield return null;
            var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            var serialized = new SerializedObject(pipeline);
            var renderer = (ScriptableRendererData)serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(serialized.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue;
            var feature = renderer.rendererFeatures.Single(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
            var settings = feature.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feature);
            var samples = settings.GetType().GetField("Samples", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var original = samples.GetValue(settings); var camera = Camera.main; var previousTarget = camera.targetTexture; var previousEnabled = camera.enabled;
            var session = Object.FindAnyObjectByType<ArenaSession>(); bool sessionEnabled = session.enabled; session.enabled = false;
            var target = new RenderTexture(1920, 1080, 24); target.Create(); camera.targetTexture = target; camera.enabled = false;
            camera.transform.SetPositionAndRotation(new Vector3(6, 24, -5), Quaternion.LookRotation(new Vector3(0, 23, 0) - new Vector3(6, 24, -5)));
            var readings = new FrameTiming[1];
            try
            {
                foreach (string mode in new[] { "Medium", "High", "Medium", "High" })
                {
                    samples.SetValue(settings, Enum.Parse(samples.FieldType, mode));
                    var gpu = new List<double>();
                    for (int frame = 0; frame < 100; frame++)
                    {
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        FrameTimingManager.CaptureFrameTimings(); yield return null;
                        if (frame >= 20 && FrameTimingManager.GetLatestTimings(1, readings) > 0 && readings[0].gpuFrameTime > 0) gpu.Add(readings[0].gpuFrameTime);
                    }
                    gpu.Sort();
                    Debug.Log("SKY_SSAO_EDITOR_TIMING mode=" + mode + " valid=" + gpu.Count + " medianGpuMs=" + (gpu.Count > 0 ? gpu[gpu.Count / 2].ToString("F3") : "unavailable"));
                }
            }
            finally { samples.SetValue(settings, original); camera.targetTexture = previousTarget; camera.enabled = previousEnabled; session.enabled = sessionEnabled; target.Release(); Object.DestroyImmediate(target); }
            yield return new ExitPlayMode();
            }
            finally { PlayerSettings.enableFrameTimingStats = originalTimingStats; }
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
