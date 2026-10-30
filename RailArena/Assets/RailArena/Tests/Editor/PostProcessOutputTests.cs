using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PostProcessOutputTests
    {
        [UnityTest]
        public IEnumerator SkyGardensPostProcessingAndBloomChangeRenderedPixels()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            for (int i = 0; i < 4; i++) yield return null;
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var session = Object.FindFirstObjectByType<ArenaSession>();
            bool sessionWasEnabled = session != null && session.enabled;
            if (session != null) session.enabled = false;
            var data = camera.GetUniversalAdditionalCameraData();
            var volume = Object.FindFirstObjectByType<Volume>();
            Assert.That(volume, Is.Not.Null);
            Assert.That(volume.profile.TryGet<Bloom>(out var bloom), Is.True);
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var oldPost = data.renderPostProcessing;
            var oldIntensity = bloom.intensity.value;
            var oldOverride = bloom.intensity.overrideState;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(256, 144, 24, RenderTextureFormat.ARGBHalf);
            var pixels = new Texture2D(256, 144, TextureFormat.RGBAFloat, false, true);
            var lightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(shader, Is.Not.Null);
            var emissive = new Material(shader);
            emissive.EnableKeyword("_EMISSION");
            emissive.SetColor("_EmissionColor", new Color(12, 6, 2, 1));
            lightObject.GetComponent<Renderer>().sharedMaterial = emissive;
            try
            {
                camera.transform.SetPositionAndRotation(new Vector3(6, 24, -5), Quaternion.LookRotation(new Vector3(0, 23, 0) - new Vector3(6, 24, -5)));
                lightObject.transform.position = camera.transform.position + camera.transform.forward * 3;
                lightObject.transform.localScale = Vector3.one * .2f;
                target.Create();
                Color[] Capture()
                {
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    pixels.Apply();
                    return pixels.GetPixels();
                }
                double Difference(Color[] a, Color[] b)
                {
                    double difference = 0;
                    for (int i = 0; i < a.Length; i++)
                        difference += Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
                    return difference / a.Length;
                }
                data.renderPostProcessing = false;
                yield return null;
                var noPost = Capture();
                data.renderPostProcessing = true;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0;
                yield return null;
                var noBloom = Capture();
                bloom.intensity.value = 2;
                yield return null;
                var withBloom = Capture();
                double postDifference = Difference(noPost, noBloom);
                double bloomDifference = Difference(noBloom, withBloom);
                Debug.Log($"POST_PROCESS_PIXEL_DIFFERENCE {postDifference} BLOOM_PIXEL_DIFFERENCE {bloomDifference}");
                Assert.That(postDifference, Is.GreaterThan(.001), "Post-processing did not change captured pixels.");
                Assert.That(bloomDifference, Is.GreaterThan(.0001), "Bloom did not change captured pixels around the emissive object.");
            }
            finally
            {
                bloom.intensity.value = oldIntensity;
                bloom.intensity.overrideState = oldOverride;
                data.renderPostProcessing = oldPost;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                if (session != null) session.enabled = sessionWasEnabled;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(emissive);
                Object.DestroyImmediate(pixels);
                target.Release();
                Object.DestroyImmediate(target);
            }
            yield return new ExitPlayMode();
        }
    }
}
