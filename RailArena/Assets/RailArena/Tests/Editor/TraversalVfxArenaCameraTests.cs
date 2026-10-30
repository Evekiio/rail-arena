using System.Collections;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxArenaCameraTests
    {
        [UnityTest]
        public IEnumerator ArenaMainCameraRetainsDepthRequestWhenPortalViewRenders()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDepthCollision.vfx");
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var gate = session.World.Portals[0]; var portal = gate.GetComponent<PortalView>(); Assert.That(portal, Is.Not.Null);
            var camera = session.View; var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            Assert.That(cameraData, Is.Not.Null); Assert.That(camera, Is.EqualTo(Camera.main));
            var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation; var oldTarget = camera.targetTexture;
            var reduced = session.Comfort.ReducedFlash;
            var target = new RenderTexture(960, 540, 24); target.Create(); camera.targetTexture = target;
            var center = gate.Presentation.transform.position + Vector3.up * 1.7f;
            camera.transform.position = center + gate.Presentation.transform.forward * 2.5f; camera.transform.LookAt(center);
            var root = new GameObject("Arena depth VFX candidate"); root.transform.position = center + gate.transform.up * 1.5f;
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var bridge = new TraversalVfxPrototype(); bridge.Bind(gate.Presentation, effect);
            try
            {
                session.Comfort.ReducedFlash = false; gate.Presentation.ResetPulse();
                Assert.That(gate.Presentation.Activate(), Is.True); Assert.That(bridge.BurstsRequested, Is.EqualTo(1));
                int maximum = 0; bool portalRendered = false;
                for (int frame = 0; frame < 40; frame++)
                {
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    portalRendered |= portal.RenderView(camera);
                    maximum = Mathf.Max(maximum, effect.aliveParticleCount);
                }
                Debug.Log("TRAVERSAL_ARENA_DEPTH main=" + VFXManager.IsCameraBufferNeeded(camera) + " portal=" + VFXManager.IsCameraBufferNeeded(portal.DestinationCamera) + " particles=" + maximum + " portalRendered=" + portalRendered);
                Assert.That(maximum, Is.GreaterThanOrEqualTo(2000)); Assert.That(portalRendered, Is.True);
                Assert.That(VFXManager.IsCameraBufferNeeded(camera).ToString(), Is.EqualTo("Depth"));
                Assert.That(VFXManager.IsCameraBufferNeeded(portal.DestinationCamera).ToString(), Is.EqualTo("None"));
                Assert.That(cameraData.history.GetHistoryForRead<RawDepthHistory>()?.GetCurrentTexture()?.rt, Is.Not.Null);
            }
            finally
            {
                bridge.Dispose(); session.Comfort.ReducedFlash = reduced; camera.targetTexture = oldTarget;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                target.Release(); Object.Destroy(target); Object.Destroy(root);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
