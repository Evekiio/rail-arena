using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailArena.Tests
{
    public sealed class TraversalVfxRampGeometryTests
    {
        [UnityTest]
        public IEnumerator RampSegmentsAreShearedRatherThanRectangularBoxes()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode(); yield return null;
            var world = Object.FindAnyObjectByType<ArenaSession>().World;
            foreach (var name in new[] { "R1", "R2", "R3", "R4" })
            {
                var mesh = world.GetComponentsInChildren<MeshCollider>().Single(c => c.name == name).sharedMesh;
                Assert.That(mesh.isReadable, Is.True); var vertices = mesh.vertices;
                Assert.That(vertices.Length, Is.EqualTo(108), "Expected 36 triangles expanded into independent vertices");
                var slopeEdge = vertices[5] - vertices[0]; var verticalEdge = vertices[6] - vertices[0];
                float dot = Mathf.Abs(Vector3.Dot(slopeEdge.normalized, verticalEdge.normalized));
                Debug.Log("TRAVERSAL_RAMP_SHAPE " + name + " slope=" + slopeEdge + " vertical=" + verticalEdge + " normalizedDot=" + dot);
                Assert.That(slopeEdge.magnitude, Is.GreaterThan(.01f)); Assert.That(verticalEdge.magnitude, Is.GreaterThan(.01f));
                Assert.That(dot, Is.GreaterThan(.1f), "A rectangular box requires perpendicular adjacent edges");
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
