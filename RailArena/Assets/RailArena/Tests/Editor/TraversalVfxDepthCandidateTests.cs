using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxDepthCandidateTests
    {
        [UnityTest]
        public IEnumerator DepthCandidateRunsWithMainCameraAndOpaqueGeometry()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDepthCollision.vfx");
            Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode();
            var camera = new GameObject("Depth test camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -8);
            camera.orthographic = true; camera.orthographicSize = 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.depthTextureMode |= DepthTextureMode.Depth;
            var target = new RenderTexture(640, 640, 24); target.Create();
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.transform.position = new Vector3(0, -1, 0); surface.transform.localScale = new Vector3(5, .3f, 4);
            var root = new GameObject("Depth test effect");
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            effect.initialEventName = "DisabledAutoStart"; effect.resetSeedOnPlay = false; effect.startSeed = 173; effect.pause = true;
            try
            {
                effect.Reinit(); effect.Play();
                for (int frame = 0; frame < 36; frame++)
                {
                    effect.Simulate(1f / 60f, 1); EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                }
                Debug.Log("TRAVERSAL_VFX_DEPTH_PARTICLES " + effect.aliveParticleCount);
                Assert.That(effect.aliveParticleCount, Is.GreaterThan(1000), "The depth candidate must simulate a GPU burst");
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(root); Object.Destroy(surface); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
