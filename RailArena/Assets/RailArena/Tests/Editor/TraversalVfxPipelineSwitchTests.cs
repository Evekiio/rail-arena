using System.Collections;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class TraversalVfxPipelineSwitchTests
    {
        [UnityTest, Explicit("Investigating zero-particle results in the headless VFX environment.")]
        public IEnumerator CandidateStillEmitsAfterGraphicsTierChanges()
        {
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDepthCollision.vfx");
            Assert.That(graph, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindFirstObjectByType<ArenaSession>();
            var gate = session.World.Portals[0]; var camera = session.View;
            var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldTier = session.Graphics.Tier; bool oldReduced = session.Comfort.ReducedFlash; bool sessionEnabled = session.enabled;
            session.enabled = false; session.Comfort.ReducedFlash = false;
            var target = new RenderTexture(320, 180, 24); target.Create();
            var center = gate.transform.position + Vector3.up * 1.7f;
            camera.transform.SetPositionAndRotation(center + gate.transform.forward * 3, Quaternion.LookRotation(-gate.transform.forward));
            var root = new GameObject("VFX tier audit"); root.transform.position = center + Vector3.up;
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var bridge = new TraversalVfxPrototype(); bridge.Bind(gate.Presentation, effect);
            try
            {
                foreach (var tier in new[] { GraphicsTier.High, GraphicsTier.Balanced, GraphicsTier.Performance })
                {
                    session.Graphics.Tier = tier; session.Graphics.Apply(camera);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    effect.Reinit();
                    for (int frame = 0; frame < 3; frame++) yield return null;
                    gate.Presentation.ResetPulse();
                    Assert.That(gate.Presentation.Activate(), Is.True);
                    Assert.That(bridge.BurstsRequested, Is.GreaterThan(0), "The presentation did not request a VFX burst.");
                    int maximum = 0;
                    for (int frame = 0; frame < 35; frame++)
                    {
                        EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        maximum = Mathf.Max(maximum, effect.aliveParticleCount);
                    }
                    Debug.Log($"VFX_AFTER_TIER_SWITCH {tier} particles={maximum}");
                    Assert.That(maximum, Is.GreaterThanOrEqualTo(2000), tier.ToString());
                }
            }
            finally
            {
                bridge.Dispose(); session.Comfort.ReducedFlash = oldReduced; session.Graphics.Tier = oldTier; session.Graphics.Apply(camera);
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); session.enabled = sessionEnabled;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(root);
            }
            yield return new ExitPlayMode();
        }
    }
}
