using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PadEnergyNodeTests
    {
        [UnityTest]
        public IEnumerator GraphReceivesRealPadPresentationControls()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Effects/PadEnergy.shadergraph");
            Assert.That(shader, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindFirstObjectByType<ArenaSession>();
            var pad = System.Array.Find(Object.FindObjectsByType<TraversalPresentation>(FindObjectsSortMode.None), effect => effect.Kind == TraversalEffectKind.SuperPad);
            Assert.That(session, Is.Not.Null); Assert.That(pad, Is.Not.Null);
            var energy = (Material)typeof(TraversalPresentation).GetField("energy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pad);
            Assert.That(energy.shader, Is.EqualTo(shader), "The real pad did not load the production graph.");
            var oldShader = energy.shader;
            bool oldReducedFlash = session.Comfort.ReducedFlash;
            try
            {
                energy.SetColor("_Tint", pad.Tint);
                session.StartSession(true, 0, 1, TargetScenario.Stationary);
                session.Comfort.ReducedFlash = false;
                yield return null;
                Assert.That(pad.Activate(), Is.True);
                Assert.That(energy.GetFloat("_Burst"), Is.GreaterThan(.9f));
                session.Comfort.ReducedFlash = true;
                yield return null;
                Assert.That(energy.GetFloat("_Calm"), Is.EqualTo(1).Within(.001f));
                Assert.That(energy.GetFloat("_Burst"), Is.LessThanOrEqualTo(.22f));
                session.SetPaused(true);
                yield return null;
                float pausedClock = energy.GetFloat("_Clock");
                yield return null;
                Assert.That(energy.GetFloat("_Clock"), Is.EqualTo(pausedClock).Within(.00001f));
            }
            finally
            {
                session.SetPaused(false);
                session.Comfort.ReducedFlash = oldReducedFlash;
                energy.shader = oldShader;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ShaderGraphPrototypeRendersSpatialEnergyVariation()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Effects/PadEnergy.shadergraph");
            Assert.That(shader, Is.Not.Null);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            var cameraObject = new GameObject("Pad graph test camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0, 3, 0), Quaternion.Euler(90, 0, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true; camera.orthographicSize = .6f;
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.localScale = Vector3.one * .1f;
            var material = new Material(shader);
            var existingShader = Resources.Load<Shader>("Effects/TraversalEnergy");
            Assert.That(existingShader, Is.Not.Null);
            var existing = new Material(existingShader);
            existing.SetFloat("_Mode", 1);
            foreach (string property in new[] { "_Tint", "_Clock", "_Burst", "_Calm", "_Charge" }) Assert.That(material.HasProperty(property), Is.True, property);
            material.SetColor("_Tint", new Color(.06f, .65f, .61f, 1));
            plane.GetComponent<Renderer>().sharedMaterial = material;
            var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(64, 64, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                for (int i = 0; i < 3; i++) yield return null;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                for (int y = 12; y < 52; y++) for (int x = 12; x < 52; x++)
                {
                    var color = pixels.GetPixel(x, y);
                    Assert.That(float.IsFinite(color.g), Is.True);
                    min = Mathf.Min(min, color.g); max = Mathf.Max(max, color.g);
                }
                Debug.Log($"PAD_GRAPH_GREEN_RANGE {min} {max}");
                Assert.That(max - min, Is.GreaterThan(.02f), "The graph did not render a spatially varying pad pattern.");
                var baseline = pixels.GetPixels();
                material.SetFloat("_Clock", 1.7f); material.SetFloat("_Burst", .8f); material.SetFloat("_Charge", .4f);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
                var active = pixels.GetPixels();
                material.SetFloat("_Calm", 1);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
                var calm = pixels.GetPixels();
                double change = 0, activeSum = 0, calmSum = 0;
                for (int y = 12; y < 52; y++) for (int x = 12; x < 52; x++)
                {
                    int i = y * 64 + x;
                    change += Mathf.Abs(active[i].g - baseline[i].g);
                    activeSum += active[i].g; calmSum += calm[i].g;
                }
                Debug.Log($"PAD_GRAPH_CONTROLS change={change / 1600} active={activeSum / 1600} calm={calmSum / 1600}");
                Assert.That(change / 1600, Is.GreaterThan(.02), "Game controls did not change the rendered pattern.");
                Assert.That(calmSum, Is.LessThan(activeSum * .8), "Reduced flash did not lower intensity.");
                foreach (var p in new[] { new Vector4(0, 0, 0, 0), new Vector4(1.7f, .8f, 0, .4f), new Vector4(3.2f, .2f, 1, .7f) })
                {
                    foreach (var candidate in new[] { material, existing })
                    {
                        candidate.SetColor("_Tint", new Color(.06f, .65f, .61f, 1));
                        candidate.SetFloat("_Clock", p.x); candidate.SetFloat("_Burst", p.y); candidate.SetFloat("_Calm", p.z); candidate.SetFloat("_Charge", p.w);
                    }
                    Color[] Capture(Material candidate)
                    {
                        plane.GetComponent<Renderer>().sharedMaterial = candidate;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
                        return pixels.GetPixels();
                    }
                    var graphPixels = Capture(material); var originalPixels = Capture(existing);
                    double difference = 0;
                    for (int y = 12; y < 52; y++) for (int x = 12; x < 52; x++)
                    {
                        int i = y * 64 + x;
                        difference += Mathf.Abs(graphPixels[i].r - originalPixels[i].r) + Mathf.Abs(graphPixels[i].g - originalPixels[i].g) + Mathf.Abs(graphPixels[i].b - originalPixels[i].b);
                    }
                    Debug.Log($"PAD_GRAPH_ORIGINAL_DIFFERENCE {difference / 1600}");
                    Assert.That(difference / 1600, Is.LessThan(.03), "Graph does not match the existing pad shader.");
                }
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); Object.DestroyImmediate(material); Object.DestroyImmediate(existing); Object.DestroyImmediate(plane); Object.DestroyImmediate(cameraObject);
            }
            yield return new ExitPlayMode();
        }

        [Test]
        public void ReflectedPadFunctionMatchesExistingEnergyFormula()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Validation/TraversalShaderGraph/PadEnergyProbe.shader");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var target = new RenderTexture(32, 32, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                foreach (var p in new[] { new Vector4(0, 0, 0, 0), new Vector4(1.7f, .8f, 0, .4f), new Vector4(3.2f, .2f, 1, .7f) })
                {
                    material.SetVector("_Params", p);
                    Graphics.Blit(Texture2D.whiteTexture, target, material);
                    RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); pixels.Apply();
                    Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
                    foreach (var point in new[] { new Vector2Int(16, 16), new Vector2Int(25, 20), new Vector2Int(2, 29) })
                    {
                        float x = ((point.x + .5f) / 32f - .5f) * 2, y = ((point.y + .5f) / 32f - .5f) * 2;
                        float r = Mathf.Sqrt(x * x + y * y);
                        float rim = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(r - .84f) / .12f), 2);
                        float pulse = Mathf.Pow(Mathf.Clamp01(.5f + .5f * Mathf.Sin(r * 25 - p.x * 5)), 5);
                        float field = .2f + pulse * .8f + rim * .9f + p.w * .25f;
                        field += p.y * (.3f + .65f * Mathf.Pow(Mathf.Clamp01(.5f + .5f * Mathf.Sin(r * 18 - p.x * 8)), 3));
                        field *= Mathf.Lerp(1, .55f, p.z);
                        var actual = pixels.GetPixel(point.x, point.y);
                        Assert.That(actual.g, Is.EqualTo(.65f * Mathf.Min(field, 2.5f)).Within(.003f));
                    }
                }
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); Object.DestroyImmediate(material);
            }
        }
    }
}
