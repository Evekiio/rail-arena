using System.Collections;
using System.Linq;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxArenaBoundedBindingTests
    {
        [UnityTest]
        public IEnumerator BindsOnlySupportedArenaBoxColliders()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalBounded.vfx"); Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var gate = session.World.Portals[0];
            var origin = gate.transform.position + gate.transform.forward * 1.5f + Vector3.up * 5;
            Assert.That(Physics.Raycast(origin, Vector3.down, out var hit, 12f, 1 << 0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider, Is.InstanceOf<MeshCollider>(), "Document the actual arena floor collider type");
            var root = new GameObject("Bounded collider prototype"); root.transform.position = hit.point + Vector3.up * 2;
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var bridge = new TraversalVfxPrototype(); bridge.Bind(gate.Presentation, effect);
            try
            {
                Assert.That(bridge.TrySetAxisAlignedBox(hit.collider as BoxCollider), Is.False);
                var meshCollider = (MeshCollider)hit.collider;
                Assert.That(bridge.TrySetAxisAlignedBoxMesh(meshCollider), Is.True, "The sampled floor should be a verified box mesh");
                Assert.That(Vector3.Distance(root.transform.TransformPoint(effect.GetVector3("BoxCenter")), meshCollider.bounds.center), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(effect.GetVector3("BoxSize"), meshCollider.bounds.size), Is.LessThan(.001f));
                var originalMesh = meshCollider.sharedMesh; var alteredMesh = Object.Instantiate(originalMesh);
                try
                {
                    var vertices = alteredMesh.vertices; vertices[0] = Vector3.Lerp(vertices[0], alteredMesh.bounds.center, .2f);
                    alteredMesh.vertices = vertices; alteredMesh.RecalculateBounds(); meshCollider.sharedMesh = alteredMesh;
                    Assert.That(bridge.TrySetAxisAlignedBoxMesh(meshCollider), Is.False, "A non-box mesh must not be approximated silently");
                }
                finally { meshCollider.sharedMesh = originalMesh; Object.Destroy(alteredMesh); }
                var boxes = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var supported = boxes.FirstOrDefault(box => bridge.TrySetAxisAlignedBox(box));
                Assert.That(supported, Is.Not.Null, "Expected an axis-aligned arena box collider");
                var expectedCenter = supported.transform.TransformPoint(supported.center);
                Assert.That(Vector3.Distance(root.transform.TransformPoint(effect.GetVector3("BoxCenter")), expectedCenter), Is.LessThan(.001f));
                var size = effect.GetVector3("BoxSize");
                Assert.That(Vector3.Distance(size, supported.bounds.size), Is.LessThan(.001f));
                Debug.Log("TRAVERSAL_ARENA_BOX_BOUND " + supported.name + " center=" + expectedCenter + " size=" + size + " floor=" + hit.collider.GetType().Name);
                root.transform.rotation = Quaternion.Euler(0, 10, 0);
                Assert.That(bridge.TrySetAxisAlignedBox(supported), Is.False);
                root.transform.rotation = Quaternion.identity;
                supported.enabled = false;
                try { Assert.That(bridge.TrySetAxisAlignedBox(supported), Is.False); }
                finally { supported.enabled = true; }
            }
            finally { bridge.Dispose(); Object.Destroy(root); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}


