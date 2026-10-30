using System;
using System.Collections;
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
    public sealed class AtmospherePortalTierTests
    {
        [UnityTest]
        public IEnumerator MainCameraRespondsWhileDestinationCameraStaysUnchangedAcrossTiers()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Validation/AtmosphereCandidate/AtmospherePipeline.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/AtmosphereCandidate/AtmosphereMaterial.mat");
            Assert.That(pipeline, Is.Not.Null); Assert.That(material, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var oldPipeline = QualitySettings.renderPipeline;
            yield return new EnterPlayMode();
            QualitySettings.renderPipeline = pipeline;
            for (int i = 0; i < 4; i++) yield return null;
            var session = Object.FindFirstObjectByType<ArenaSession>();
            Assert.That(session, Is.Not.Null);
            var gate = session.World.Portals[0]; var portal = gate.GetComponent<PortalView>();
            Assert.That(portal, Is.Not.Null);
            var camera = session.View; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldParameters = material.GetVector("_AtmosphereParams"); var oldTier = session.Graphics.Tier;
            var oldActive = RenderTexture.active; var target = new RenderTexture(256, 144, 24, RenderTextureFormat.ARGBFloat);
            bool sessionEnabled = session.enabled; session.enabled = false;
            try
            {
                target.Create();
                camera.transform.SetPositionAndRotation(gate.transform.position + gate.transform.forward * 4 + Vector3.up * 1.7f,
                    Quaternion.LookRotation(-gate.transform.forward));
                Color[] Read(RenderTexture source)
                {
                    var image = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
                    try { RenderTexture.active = source; image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); image.Apply(); return image.GetPixels(); }
                    finally { Object.DestroyImmediate(image); }
                }
                double Difference(Color[] a, Color[] b)
                {
                    double difference = 0;
                    for (int i = 0; i < a.Length; i++) difference += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                    return difference / a.Length;
                }
                foreach (var tier in new[] { GraphicsTier.High, GraphicsTier.Balanced, GraphicsTier.Performance })
                {
                    session.Graphics.Tier = tier; session.Graphics.Apply(camera);
                    for (int i = 0; i < 4; i++) yield return null;
                    Color[] Portal(float density)
                    {
                        material.SetVector("_AtmosphereParams", new Vector4(density, oldParameters.y, oldParameters.z, oldParameters.w));
                        Assert.That(portal.RenderView(camera), Is.True);
                        return Read(portal.Texture);
                    }
                    Color[] Main(float density)
                    {
                        material.SetVector("_AtmosphereParams", new Vector4(density, oldParameters.y, oldParameters.z, oldParameters.w));
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        return Read(target);
                    }
                    var portalClear = Portal(0); var portalFog = Portal(oldParameters.x);
                    var mainClear = Main(0); var mainFog = Main(oldParameters.x);
                    double portalDelta = Difference(portalClear, portalFog), mainDelta = Difference(mainClear, mainFog);
                    Debug.Log($"ATMOSPHERE_PORTAL_TIER {tier} portalDelta={portalDelta} mainDelta={mainDelta}");
                    Assert.That(portalDelta, Is.LessThan(.001), "The portal camera should not run the main-camera atmosphere pass.");
                    Assert.That(mainDelta, Is.GreaterThan(.001), "The main-camera atmosphere pass did not render.");
                }
            }
            finally
            {
                material.SetVector("_AtmosphereParams", oldParameters);
                session.Graphics.Tier = oldTier; session.Graphics.Apply(camera); QualitySettings.renderPipeline = oldPipeline;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); session.enabled = sessionEnabled;
                RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target);
            }
            yield return new ExitPlayMode();
        }
    }
}
