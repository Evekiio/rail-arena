using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PadGraphEnhancedTests
    {
        [UnityTest]
        public IEnumerator CaptureCandidateOnArenaPad()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Validation/TraversalShaderGraph/PadEnergyEnhanced.shadergraph");
            Assert.That(shader, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            for (int i = 0; i < 3; i++) yield return null;
            var session = Object.FindFirstObjectByType<ArenaSession>();
            var pad = System.Array.Find(Object.FindObjectsByType<TraversalPresentation>(FindObjectsSortMode.None), p => p.Kind == TraversalEffectKind.SuperPad);
            Assert.That(session, Is.Not.Null); Assert.That(pad, Is.Not.Null);
            bool sessionEnabled = session.enabled; session.enabled = false;
            var camera = Camera.main; var position = camera.transform.position; var rotation = camera.transform.rotation;
            var original = pad.Surface.sharedMaterial; var candidate = new Material(shader);
            var oldTint = original.GetColor("_Tint");
            float oldClock = original.GetFloat("_Clock"), oldBurst = original.GetFloat("_Burst"), oldCalm = original.GetFloat("_Calm"), oldCharge = original.GetFloat("_Charge");
            var target = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/PadGraphEnhancedComparison-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                Vector3 center = pad.transform.position, eye = center + new Vector3(-3, 2.2f, 3);
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(center + Vector3.up * .2f - eye));
                foreach (var state in new[] { ("idle", 0f, 0f), ("burst", 1f, 0f), ("reduced", .22f, 1f) })
                {
                    foreach (var option in new[] { ("original", original), ("enhanced", candidate) })
                    {
                        pad.Surface.sharedMaterial = option.Item2;
                        option.Item2.SetColor("_Tint", pad.Tint); option.Item2.SetFloat("_Clock", 1.7f);
                        option.Item2.SetFloat("_Burst", state.Item2); option.Item2.SetFloat("_Calm", state.Item3); option.Item2.SetFloat("_Charge", .4f);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, state.Item1 + "-" + option.Item1 + ".png"), image.EncodeToPNG());
                    }
                }
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
                Debug.Log("PAD_GRAPH_ENHANCED_COMPARISON " + output);
            }
            finally
            {
                original.SetColor("_Tint", oldTint); original.SetFloat("_Clock", oldClock); original.SetFloat("_Burst", oldBurst); original.SetFloat("_Calm", oldCalm); original.SetFloat("_Charge", oldCharge);
                pad.Surface.sharedMaterial = original; session.enabled = sessionEnabled;
                camera.transform.SetPositionAndRotation(position, rotation); RenderTexture.active = oldActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(candidate);
            }
            yield return new ExitPlayMode();
        }
    }
}
