using System.Collections;
using System.Collections.Generic;
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
    public sealed class TraversalVfxArenaCoverageTests
    {
        [UnityTest]
        public IEnumerator AuditColliderCompatibilityInSkyGardens()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalBounded.vfx"); Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var root = new GameObject("Bounded VFX compatibility audit");
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var bridge = new TraversalVfxPrototype(); bridge.Bind(session.World.Portals[0].Presentation, effect);
            try
            {
                var counts = new Dictionary<string, int>(); var examples = new Dictionary<string, List<string>>();
                foreach (var collider in session.World.GetComponentsInChildren<Collider>(false))
                {
                    string label;
                    if (!collider.enabled || collider.isTrigger) label = "disabled_or_trigger";
                    else if (collider is BoxCollider box) label = bridge.TrySetAxisAlignedBox(box) ? "box_supported" : "box_unsupported";
                    else if (collider is MeshCollider mesh) label = bridge.TrySetAxisAlignedBoxMesh(mesh) ? "mesh_supported" : "mesh_unsupported";
                    else label = "other_" + collider.GetType().Name;
                    if (label == "box_supported" || label == "mesh_supported")
                    {
                        Assert.That(Vector3.Distance(root.transform.TransformPoint(effect.GetVector3("BoxCenter")), collider.bounds.center), Is.LessThan(.001f), collider.name);
                        Assert.That(Vector3.Distance(effect.GetVector3("BoxSize"), collider.bounds.size), Is.LessThan(.001f), collider.name);
                    }
                    counts.TryGetValue(label, out int count); counts[label] = count + 1;
                    if (!examples.TryGetValue(label, out var names)) { names = new List<string>(); examples[label] = names; }
                    if (names.Count < 15) names.Add(collider.name);
                }
                foreach (var entry in counts.OrderBy(e => e.Key)) Debug.Log("TRAVERSAL_COLLIDER_COVERAGE " + entry.Key + "=" + entry.Value + " examples=" + string.Join(",", examples[entry.Key]));
                Assert.That(counts.TryGetValue("mesh_supported", out int supported) && supported > 0, Is.True, "Expected at least one recognized arena mesh");
                Assert.That(counts.Values.Sum(), Is.GreaterThan(0));
            }
            finally { bridge.Dispose(); Object.Destroy(root); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}


