using System.Collections;
using NUnit.Framework;
using RailArena.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace RailArena.Tests
{
    public sealed class TraversalVfxOrientedBindingTests
    {
        [UnityTest]
        public IEnumerator MapsRotatedBoxCornersAndRejectsShear()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalOrientedBox.vfx"); Assert.That(graph, Is.Not.Null);
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var root = new GameObject("Oriented binding effect"); root.transform.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(5, 25, -8)); root.transform.localScale = Vector3.one * 2;
            var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph;
            var boxObject = new GameObject("Oriented collision test box"); boxObject.transform.SetPositionAndRotation(new Vector3(-3, 2, 7), Quaternion.Euler(20, 40, 15)); boxObject.transform.localScale = new Vector3(2, 1, 3);
            var box = boxObject.AddComponent<BoxCollider>(); box.center = new Vector3(.2f, .3f, -.1f); box.size = new Vector3(2, .7f, 1);
            var bridge = new TraversalVfxPrototype(); bridge.Bind(session.World.Portals[0].Presentation, effect);
            try
            {
                Assert.That(bridge.TrySetOrientedBox(box), Is.True);
                var center = effect.GetVector3("BoxCenter"); var size = effect.GetVector3("BoxSize"); var rotation = Quaternion.Euler(effect.GetVector3("BoxAngles"));
                float maxError = 0;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                {
                    var signs = new Vector3(x, y, z) * .5f;
                    var expected = box.transform.TransformPoint(box.center + Vector3.Scale(box.size, signs));
                    var actual = root.transform.TransformPoint(center + rotation * Vector3.Scale(size, signs));
                    maxError = Mathf.Max(maxError, Vector3.Distance(expected, actual));
                }
                Debug.Log("TRAVERSAL_ORIENTED_BINDING maxCornerError=" + maxError);
                Assert.That(maxError, Is.LessThan(.001f));
                var parent = new GameObject("Shear test parent");
                try
                {
                    parent.transform.localScale = new Vector3(2, 1, 1); boxObject.transform.SetParent(parent.transform, false);
                    boxObject.transform.localRotation = Quaternion.Euler(0, 45, 0);
                    Assert.That(bridge.TrySetOrientedBox(box), Is.False, "Sheared matrices must be rejected");
                }
                finally { boxObject.transform.SetParent(null); Object.Destroy(parent); }
                boxObject.transform.localScale = new Vector3(-1, 1, 1);
                Assert.That(bridge.TrySetOrientedBox(box), Is.False, "Reflected transforms require explicit handling");
            }
            finally { bridge.Dispose(); Object.Destroy(boxObject); Object.Destroy(root); }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
