using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class AtmosphereTimingProbeTests
    {
        [UnityTest]
        public IEnumerator CompareSynchronizedOffscreenRenderTimes()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Validation/AtmosphereCandidate/AtmosphereRenderer.asset");
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);
            var feature = renderer.rendererFeatures.Single(f => f != null && f.GetType().Name == "AtmosphereCandidateFeature");
            bool featureWasActive = feature.isActive;
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var oldPipeline = QualitySettings.renderPipeline;
            yield return new EnterPlayMode();
            QualitySettings.renderPipeline = pipeline;
            for (int i = 0; i < 4; i++) yield return null;
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var session = Object.FindFirstObjectByType<ArenaSession>();
            bool sessionWasEnabled = session != null && session.enabled;
            if (session != null) session.enabled = false;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            try
            {
                target.Create();
                camera.transform.SetPositionAndRotation(new Vector3(6, 24, -5), Quaternion.LookRotation(new Vector3(0, 23, 0) - new Vector3(6, 24, -5)));
                foreach (bool active in new[] { false, true, false, true })
                {
                    feature.SetActive(active);
                    var samples = new List<double>();
                    for (int i = 0; i < 12; i++)
                    {
                        var watch = Stopwatch.StartNew();
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        var request = AsyncGPUReadback.Request(target);
                        request.WaitForCompletion();
                        watch.Stop();
                        Assert.That(request.hasError, Is.False);
                        if (i >= 4) samples.Add(watch.Elapsed.TotalMilliseconds);
                    }
                    samples.Sort();
                    Debug.Log($"ATMOSPHERE_SYNC_WALL {SystemInfo.graphicsDeviceName} {SystemInfo.graphicsDeviceType} 960x540 {(active ? "on" : "off")} median_ms={samples[samples.Count / 2]:F3}");
                }
            }
            finally
            {
                feature.SetActive(featureWasActive);
                QualitySettings.renderPipeline = oldPipeline;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                if (session != null) session.enabled = sessionWasEnabled;
                target.Release();
                Object.DestroyImmediate(target);
            }
            yield return new ExitPlayMode();
        }
    }
}
