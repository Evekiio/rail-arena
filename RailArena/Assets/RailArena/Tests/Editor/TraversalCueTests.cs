using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class TraversalCueTests
    {
        [Test]
        public void LandingMaterialIsMutedAndNonEmissive()
        {
            var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Art/Foundry/Landing guide.mat");
            Assert.That(material,Is.Not.Null);
            Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.False);
            var color=material.GetColor("_BaseColor");
            Assert.That(Mathf.Abs(color.r-color.b),Is.LessThan(.05f));
            Assert.That(color.maxColorComponent,Is.LessThan(.8f));
        }

        [UnityTest]
        public IEnumerator AuthoredChevronsPointTowardActualDestinationsAndLandingsSitOnDeck()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaWorld>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var world=Object.FindAnyObjectByType<ArenaWorld>();
            Assert.That(world.Pads.Length,Is.EqualTo(4));
            var energy=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RailArena/Art/Foundry/Launch field mesh.asset");
            foreach(var pad in world.Pads)
            {
                var direction=pad.Destination-pad.transform.position;direction.y=0;
                var inverse=Quaternion.Inverse(Quaternion.LookRotation(direction));
                var tips=energy.vertices.Select(vertex=>inverse*(vertex-pad.transform.position-Vector3.up*.17f))
                    .Where(point=>point.z>.34f&&point.z<.45f&&new Vector2(point.x,point.z).sqrMagnitude<.36f).ToArray();
                Assert.That(tips.Length,Is.GreaterThan(0));
                // The forwardmost ends must meet at the arrow spine, rather than opening away from it.
                Assert.That(tips.Max(point=>Mathf.Abs(point.x)),Is.LessThan(.1f),pad.name);
                Assert.That(pad.FlightSeconds,Is.EqualTo(1.25f));
                var origin=pad.transform.position+Vector3.up*.08f;
                var end=origin+pad.Launch(origin,25)*pad.FlightSeconds-Vector3.up*(.5f*25*pad.FlightSeconds*pad.FlightSeconds);
                Assert.That(Vector3.Distance(end,pad.Destination),Is.LessThan(.001f));
            }
            var destinations=world.Pads.Select(pad=>pad.Destination).Distinct().ToArray();
            Assert.That(destinations.Length,Is.EqualTo(2));
            var guide=GameObject.Find("Foundry surface detailing/Landing guide");
            Assert.That(guide,Is.Not.Null);Assert.That(guide.GetComponentsInChildren<Collider>(),Is.Empty);
            var vertices=guide.GetComponent<MeshFilter>().sharedMesh.vertices;
            foreach(var vertex in vertices)
            {
                Assert.That(destinations.Any(destination=>new Vector2(vertex.x-destination.x,vertex.z-destination.z).magnitude<1.05f),Is.True);
                Assert.That(Physics.Raycast(vertex+Vector3.up*.1f,Vector3.down,out var hit,.3f,1<<0,QueryTriggerInteraction.Ignore),Is.True);
                Assert.That(vertex.y-hit.point.y,Is.InRange(.005f,.02f));
            }
            foreach(var destination in destinations)
                Assert.That(vertices.Count(vertex=>Mathf.Abs(vertex.x-destination.x)<1&&Mathf.Abs(vertex.z-destination.z)<1),Is.GreaterThan(100));
            yield return new ExitPlayMode();
        }

        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count;float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
        }
        [UnityTearDown]
        public IEnumerator RestoreEditorMode() {if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
