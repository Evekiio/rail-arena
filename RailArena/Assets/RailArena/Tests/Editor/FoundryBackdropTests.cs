using System.Linq;
using NUnit.Framework;
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Tests
{
    public sealed class FoundryBackdropTests
    {
        private static object InvokeBuilder(string type,string method,params object[] arguments)
        {
            var builder=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RailArena.Editor."+type)).Single(t=>t!=null);
            return builder.GetMethod(method).Invoke(null,arguments);
        }
        [Test]
        public void BackdropRebuildIsDeterministicDistantAndCannotAffectCollision()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaWorld>();
            string before=(string)InvokeBuilder("FoundryArtBuilder","GameplaySignature",world);
            var original=GameObject.Find("Foundry distant islands");
            var expected=original.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices).ToArray();
            original.SetActive(false);
            InvokeBuilder("FoundryBackdropBuilder","Apply");
            Assert.That((string)InvokeBuilder("FoundryArtBuilder","GameplaySignature",world),Is.EqualTo(before));
            var root=GameObject.Find("Foundry distant islands");
            Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Count(obj=>obj.scene==world.gameObject.scene && obj.name=="Foundry distant islands"),Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Light>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Transform>(true).All(t=>t.gameObject.layer==2),Is.True);
            var filters=root.GetComponentsInChildren<MeshFilter>();Assert.That(filters.Length,Is.EqualTo(3));
            Assert.That(filters.SelectMany(f=>f.sharedMesh.vertices).ToArray(),Is.EqualTo(expected));
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;
                Assert.That(mesh.vertexCount,Is.GreaterThan(0));
                Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
                foreach(var vertex in mesh.vertices)
                {
                    Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),Is.True);
                    Assert.That(new Vector2(vertex.x,vertex.z).magnitude,Is.GreaterThan(140));
                    Assert.That(vertex.magnitude,Is.LessThan(300));
                }
                var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                    Assert.That(Vector3.Cross(b-a,c-a).sqrMagnitude,Is.GreaterThan(1e-10f));
                }
                var renderer=filter.GetComponent<MeshRenderer>();
                Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
                Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));Assert.That(renderer.receiveShadows,Is.False);
            }
        }
    }
}
