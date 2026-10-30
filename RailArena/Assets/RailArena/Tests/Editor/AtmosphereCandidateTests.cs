using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class AtmosphereCandidateTests
    {
        [UnityTest]
        public IEnumerator CandidateRespondsToAnOccludingShadowCaster()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/AtmosphereCandidate/AtmosphereMaterial.mat");
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var oldPipeline = QualitySettings.renderPipeline;
            yield return new EnterPlayMode();
            QualitySettings.renderPipeline = pipeline;
            var cameraObject = new GameObject("Atmosphere test camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 2, -10);
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.farClipPlane = 80;
            var sunObject = new GameObject("Atmosphere test sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Hard;
            sun.transform.rotation = Quaternion.Euler(55, -30, 0);
            RenderSettings.sun = sun;
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.transform.position = new Vector3(0, 2, 20);
            backdrop.transform.localScale = new Vector3(30, 20, 1);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(0, 7, 7);
            blocker.transform.localScale = new Vector3(7, 1, 7);
            var blockerRenderer = blocker.GetComponent<Renderer>();
            blockerRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            var oldParameters = material.GetVector("_AtmosphereParams");
            var oldShadowStrength = material.GetFloat("_AtmosphereShadowStrength");
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(256, 144, 24, RenderTextureFormat.ARGBFloat);
            var pixels = new Texture2D(256, 144, TextureFormat.RGBAFloat, false, true);
            for (int frame = 0; frame < 4; frame++) yield return null;
            try
            {
                target.Create();
                Color[] Capture(bool blocked, float shadowStrength)
                {
                    blockerRenderer.enabled = blocked;
                    material.SetVector("_AtmosphereParams", new Vector4(.025f, 0, 60, 1));
                    material.SetFloat("_AtmosphereShadowStrength", shadowStrength);
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    return pixels.GetPixels();
                }
                var clearNoShadow = Capture(false, 0);
                var clearShadow = Capture(false, 1);
                var blockedNoShadow = Capture(true, 0);
                var blockedShadow = Capture(true, 1);
                double difference = 0;
                for (int i = 0; i < clearShadow.Length; i++)
                {
                    var clearContribution = clearShadow[i] - clearNoShadow[i];
                    var blockedContribution = blockedShadow[i] - blockedNoShadow[i];
                    difference += Mathf.Abs(clearContribution.r - blockedContribution.r) + Mathf.Abs(clearContribution.g - blockedContribution.g) + Mathf.Abs(clearContribution.b - blockedContribution.b);
                }
                Debug.Log($"ATMOSPHERE_SHADOW_MEAN_DIFFERENCE {difference / clearShadow.Length}");
                Assert.That(difference / clearShadow.Length, Is.GreaterThan(.001), "The atmospheric contribution did not respond to the shadow caster.");
            }
            finally
            {
                material.SetVector("_AtmosphereParams", oldParameters);
                material.SetFloat("_AtmosphereShadowStrength", oldShadowStrength);
                QualitySettings.renderPipeline = oldPipeline;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(pixels);
                target.Release();
                Object.DestroyImmediate(target);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CandidateProducesFiniteDepthDependentOutput()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/AtmosphereCandidate/AtmosphereMaterial.mat");
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var oldPipeline = QualitySettings.renderPipeline;
            yield return new EnterPlayMode();
            QualitySettings.renderPipeline = pipeline;
            for (int frame = 0; frame < 4; frame++) yield return null;
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var oldParameters = material.GetVector("_AtmosphereParams");
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(256, 144, 24, RenderTextureFormat.ARGBFloat);
            var pixels = new Texture2D(256, 144, TextureFormat.RGBAFloat, false, true);
            try
            {
                camera.transform.SetPositionAndRotation(new Vector3(0, 6, -20), Quaternion.LookRotation(new Vector3(0, -0.15f, 1)));
                target.Create();
                Color[] Capture(float density)
                {
                    material.SetVector("_AtmosphereParams", new Vector4(density, oldParameters.y, oldParameters.z, oldParameters.w));
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    return pixels.GetPixels();
                }
                var baseline = Capture(0);
                var atmosphere = Capture(oldParameters.x);
                var diagnostics = ShaderUtil.GetShaderMessages(material.shader);
                var shaderMessages = string.Join("; ", System.Array.ConvertAll(diagnostics, message => $"{message.severity}: {message.message} ({message.file}:{message.line})"));
                Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, $"Shader {material.shader.name}: {shaderMessages}");
                double difference = 0;
                for (int i = 0; i < atmosphere.Length; i++)
                {
                    var c = atmosphere[i];
                    Assert.That(float.IsFinite(c.r) && float.IsFinite(c.g) && float.IsFinite(c.b), Is.True);
                    difference += Mathf.Abs(c.r - baseline[i].r) + Mathf.Abs(c.g - baseline[i].g) + Mathf.Abs(c.b - baseline[i].b);
                }
                Debug.Log($"ATMOSPHERE_CANDIDATE_MEAN_DIFFERENCE {difference / atmosphere.Length}");
                Assert.That(difference / atmosphere.Length, Is.GreaterThan(0.001), "The candidate pass did not affect the scene.");
                camera.transform.SetPositionAndRotation(new Vector3(0, 30, 0), Quaternion.LookRotation(Vector3.up, Vector3.forward));
                var skyBaseline = Capture(0);
                var skyAtmosphere = Capture(oldParameters.x);
                double skyDifference = 0;
                for (int i = 0; i < skyAtmosphere.Length; i++)
                    skyDifference += Mathf.Abs(skyAtmosphere[i].r - skyBaseline[i].r) + Mathf.Abs(skyAtmosphere[i].g - skyBaseline[i].g) + Mathf.Abs(skyAtmosphere[i].b - skyBaseline[i].b);
                Debug.Log($"ATMOSPHERE_SKY_MEAN_DIFFERENCE {skyDifference / skyAtmosphere.Length}");
                Assert.That(skyDifference / skyAtmosphere.Length, Is.GreaterThan(.001), "The candidate pass did not affect the sky.");
            }
            finally
            {
                material.SetVector("_AtmosphereParams", oldParameters);
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                QualitySettings.renderPipeline = oldPipeline;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(pixels);
                target.Release();
                Object.DestroyImmediate(target);
            }
            yield return new ExitPlayMode();
        }
    }
}
