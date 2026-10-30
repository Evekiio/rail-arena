using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Tests
{
    public sealed class VoidEdgeTests
    {
        private static object Invoke(string type,string method,params object[] arguments)
        {
            var builder=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RailArena.Editor."+type)).Single(t=>t!=null);
            return builder.GetMethod(method).Invoke(null,arguments);
        }
        [Test]
        public void VoidPaintIsRepeatableFloorMountedAndSkipsKnownSafeLandings()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaWorld>();
            var before=(string)Invoke("FoundryArtBuilder","GameplaySignature",world);
            var original=GameObject.Find("Foundry void-edge markings");
            var expected=original.GetComponent<MeshFilter>().sharedMesh.vertices;
            original.SetActive(false);Invoke("VoidEdgeArtBuilder","Apply");
            var root=GameObject.Find("Foundry void-edge markings");
            Assert.That(Resources.FindObjectsOfTypeAll<GameObject>().Count(obj=>obj.scene==world.gameObject.scene && obj.name==root.name),Is.EqualTo(1));
            Assert.That((string)Invoke("FoundryArtBuilder","GameplaySignature",world),Is.EqualTo(before));
            Assert.That(root.layer,Is.EqualTo(2));Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
            Assert.That(root.GetComponentsInChildren<Light>(),Is.Empty);Assert.That(root.GetComponentsInChildren<MonoBehaviour>(),Is.Empty);
            var mesh=root.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
            Assert.That(vertices,Is.EqualTo(expected));Assert.That(vertices.Length,Is.GreaterThan(0));
            Assert.That(mesh.normals.All(n=>Vector3.Dot(n,Vector3.up)>.999f),Is.True);
            foreach(var vertex in vertices)Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),Is.True);
            for(int i=0;i<vertices.Length;i+=4)
            {
                var center=(vertices[i]+vertices[i+1]+vertices[i+2]+vertices[i+3])/4;
                Assert.That(Physics.RaycastAll(center+Vector3.up*.15f,Vector3.down,.3f,1<<0,QueryTriggerInteraction.Ignore)
                    .Any(hit=>hit.collider.name=="Load bearing deck" && Mathf.Abs(hit.point.y-center.y)<.03f),Is.True,"Paint floats off deck: "+center);
            }
            Assert.That((bool)Invoke("VoidEdgeArtBuilder","HasLanding",new Vector3(0,0,-11.55f)),Is.False,"Central gap is open");
            Assert.That((bool)Invoke("VoidEdgeArtBuilder","HasLanding",new Vector3(-24.45f,0,-18)),Is.False,"Outer rim is open");
            Assert.That((bool)Invoke("VoidEdgeArtBuilder","HasLanding",new Vector3(18,0,-11.55f)),Is.True,"Deck seam is connected");
            Assert.That((bool)Invoke("VoidEdgeArtBuilder","HasLanding",new Vector3(18,6,5.45f)),Is.True,"Upper wing has a lower landing");
            var renderer=root.GetComponent<MeshRenderer>();Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
            Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));Assert.That(renderer.receiveShadows,Is.False);
        }
    }
}
