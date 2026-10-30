using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RailArena;
using RailArena.Simulation;

namespace RailArena.Tests
{
    public sealed class MechanicsArrangementDiagnosticTests
    {
        [UnityTest]
        public IEnumerator InspectSkyShotArrangement()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(false, 1, 1, TargetScenario.Stationary); session.SetPaused(true);
            var shooter = session.Actors[0]; var target = session.Actors[1];
            foreach (var a in session.Actors) a.Bot = false;
            foreach (var x in new[] { 0f }) foreach (var z in new[] { -22f, -12f })
            {
                shooter.Place(new Vector3(x, .08f, z)); target.Place(new Vector3(x, .08f, z + 6)); Physics.SyncTransforms();
                var h = System.Array.Find(target.GetComponentsInChildren<Hitbox>(), v => v.Region == HitRegion.Head);
                if (h == null) { Debug.Log("ARRANGEMENT no head"); continue; }
                var c = h.GetComponent<Collider>(); var direction = (c.bounds.center - shooter.Eye).normalized;
                bool found = Physics.Raycast(shooter.Eye, direction, out var hit, 180, (1 << 8) | 1, QueryTriggerInteraction.Collide);
                Assert.That(found, Is.True); if (z == -22f) Assert.That(hit.collider, Is.Not.EqualTo(c), "Original lane is blocked in Sky Gardens"); else Assert.That(hit.collider, Is.EqualTo(c), "Sky Gardens validation lane must have a clear head shot");
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}

