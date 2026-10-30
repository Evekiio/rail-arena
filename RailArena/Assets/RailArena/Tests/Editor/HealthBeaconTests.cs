using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class HealthBeaconTests
    {
        [Test]
        public void SharedArtHasFiniteGeometryAmberIdentityAndNoCollision()
        {
            var prefab=Resources.Load<GameObject>("Environment/HealthBeacon");
            Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            var visual=prefab.GetComponent<HealthPickupVisual>();
            Color amber=visual.AvailableMaterial.GetColor("_BaseColor");
            Assert.That(amber.r,Is.GreaterThan(amber.g)); Assert.That(amber.g,Is.GreaterThan(amber.b*3));
            int triangles=0;
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount)); Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),Is.True);
                Assert.That(mesh.uv.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)),Is.True);
                Assert.That(mesh.normals.All(v=>v.sqrMagnitude>.99f&&v.sqrMagnitude<1.01f),Is.True);
                var vertices=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                    Assert.That(Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).sqrMagnitude,Is.GreaterThan(1e-16f));
                triangles+=indices.Length/3;
                Assert.That(filter.GetComponent<Renderer>().sharedMaterials.Length,Is.EqualTo(mesh.subMeshCount));
            }
            Assert.That(triangles,Is.LessThanOrEqualTo(1000));
            Assert.That(visual.Marker.GetComponent<MeshFilter>().sharedMesh.bounds.size.y,Is.EqualTo(.68f).Within(.001f));
        }

        [Test]
        public void RepeatedAvailabilityChangesKeepPedestalAndSharedMaterials()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Environment/HealthBeacon"));
            try
            {
                var visual=root.GetComponent<HealthPickupVisual>();
                var pedestal=root.transform.Find("Faceted pedestal").GetComponent<Renderer>();
                for(int i=0;i<100;i++)
                {
                    visual.SetAvailable(false);visual.SetAvailable(false);
                    Assert.That(visual.Marker.enabled,Is.False);Assert.That(pedestal.enabled,Is.True);
                    Assert.That(visual.Status.sharedMaterial,Is.SameAs(visual.DepletedMaterial));
                    visual.SetAvailable(true);
                    Assert.That(visual.Marker.enabled,Is.True);Assert.That(visual.Status.sharedMaterial,Is.SameAs(visual.AvailableMaterial));
                }
                Assert.That(root.GetComponentsInChildren<Renderer>().Length,Is.EqualTo(3));
            }
            finally {Object.DestroyImmediate(root);}
        }

        [UnityTest]
        public IEnumerator ScenePickupsCollectAndReturnThroughNormalAuthority()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true,0,1,TargetScenario.Stationary);session.SetPaused(true);
            var step=typeof(ArenaSession).GetMethod("AdvanceValidationTick",BindingFlags.Instance|BindingFlags.NonPublic);
            void Advance(int ticks) {for(int i=0;i<ticks;i++)step.Invoke(session,null);}
            Assert.That(session.World.Pickups.Length,Is.EqualTo(3));
            foreach(var site in session.World.Pickups)
            {
                Assert.That(site.Presentation,Is.Not.Null);Assert.That(site.Visuals.Single(),Is.SameAs(site.Presentation.Marker));
                Assert.That(site.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            Advance(CombatRules.CountdownTicks);
            var pickup=session.World.Pickups[0];
            session.Human.Place(pickup.transform.position-Vector3.up*.92f);session.Human.Health=60;Physics.SyncTransforms();
            Advance(1);
            Assert.That(session.Human.Health,Is.EqualTo(85)); Assert.That(pickup.Presentation.Marker.enabled,Is.False);
            Assert.That(pickup.Presentation.Status.sharedMaterial,Is.SameAs(pickup.Presentation.DepletedMaterial));
            int collected=session.Match.Tick;
            Assert.That(pickup.AvailableTick,Is.EqualTo(collected+1200));
            session.Human.Place(session.World.Spawns[0].position);Physics.SyncTransforms();
            Advance(1199);Assert.That(pickup.Presentation.Marker.enabled,Is.False);
            Advance(1);Assert.That(pickup.Presentation.Marker.enabled,Is.True);
            Assert.That(pickup.Presentation.Status.sharedMaterial,Is.SameAs(pickup.Presentation.AvailableMaterial));
            session.MainMenu();
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
