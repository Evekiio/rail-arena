using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RailArena.Simulation;

namespace RailArena.Tests
{
    public sealed class BasementRouteTests
    {
        [UnityTest]
        public IEnumerator FourSprintRoutesReachStagingAndBasement()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.StartSession(true, 0, 1, TargetScenario.Stationary); session.SetPaused(true);
            var actor = session.Human; var routes = Object.FindObjectsByType<BasementRoute>(FindObjectsSortMode.None);
            Assert.That(routes.Length, Is.EqualTo(4));
            foreach (var route in routes)
            {
                foreach (bool lower in new[] { false, true })
                foreach (float lateral in new[] { -.5f, 0f, .5f })
                {
                    var direction = route.Direction;
                    actor.Place(direction * (lower ? 23.4f : 6.4f) + Vector3.Cross(Vector3.up, direction) * lateral + Vector3.up * (lower ? 14.08f : 22.08f));
                    actor.Velocity = direction * 10.8f; actor.GroundedOverride = true;
                    var command = new MovementCommand { Move = Vector2.up, Sprint = true, Jump = true, Yaw = Quaternion.LookRotation(direction).eulerAngles.y };
                    float height = lower ? -10 : 14; bool landed = false;
                    for (int tick = 0; tick < 240; tick++)
                    {
                        actor.Command = command; actor.Move(session.Tuning); command.Jump = false;
                        if (tick > 5 && actor.Grounded && Mathf.Abs(actor.transform.position.y - height) < .15f) { landed = true; Debug.Log($"BASEMENT_LANDING {route.name} lower={lower} tick={tick} position={actor.transform.position:F3}"); break; }
                        if (actor.transform.position.y < height - 2) break;
                    }
                    Assert.That(landed, Is.True, $"{route.name} lower={lower} ended {actor.transform.position}");
                }
            }
            foreach (var route in routes)
            {
                foreach (bool lower in new[] { false, true })
                foreach (float lateral in new[] { -.5f, 0f, .5f })
                {
                    var direction = route.Direction;
                    actor.Place(direction * (lower ? 23.4f : 6.4f) + Vector3.Cross(Vector3.up, direction) * lateral + Vector3.up * (lower ? 14.08f : 22.08f));
                    actor.Velocity = direction * 8; actor.GroundedOverride = true;
                    var command = new MovementCommand { Move = Vector2.up, Jump = true, Yaw = Quaternion.LookRotation(direction).eulerAngles.y };
                    float height = lower ? -10 : 14; bool landed = false;
                    for (int tick = 0; tick < 240; tick++)
                    {
                        actor.Command = command; actor.Move(session.Tuning); command.Jump = false;
                        if (tick > 5 && actor.Grounded && Mathf.Abs(actor.transform.position.y - height) < .15f) { landed = true; break; }
                        if (actor.transform.position.y < height - 2) break;
                    }
                    Assert.That(landed, Is.False, "Normal speed unexpectedly cleared " + route.name + " lower=" + lower);
                }
            }
            foreach (var pad in session.World.Pads)
            {
                if (pad.name != "Basement return bounce") continue;
                actor.Place(pad.transform.position + Vector3.up * .08f); actor.Command = default;
                actor.Velocity = pad.Launch(actor.transform.position, session.Tuning.Gravity); actor.GroundedOverride = false; actor.AirborneTicks = 2;
                bool landed = false;
                for (int tick = 0; tick < 200; tick++)
                {
                    actor.Move(session.Tuning);
                    if (tick > 5 && actor.Grounded) { landed = true; break; }
                }
                Assert.That(landed, Is.True, "Return bounce failed");
                Assert.That(Vector3.Distance(actor.transform.position, pad.Destination), Is.LessThan(3), "Return bounce hit an obstruction at " + actor.transform.position);
            }
            foreach (var gate in session.World.Portals)
            {
                if (gate.name != "Basement return portal") continue;
                actor.Place(gate.transform.position + gate.transform.forward * 2 + Vector3.up * .08f);
                actor.Command = new MovementCommand { Move = Vector2.up, Yaw = Quaternion.LookRotation(-gate.transform.forward).eulerAngles.y };
                PortalResult result = PortalResult.None;
                for (int tick = 0; tick < 60; tick++)
                {
                    var before = actor.transform.position; actor.Move(session.Tuning); Physics.SyncTransforms();
                    result = gate.TryTraverse(actor, before, 100 + tick);
                    if (result != PortalResult.None) break;
                }
                Assert.That(result, Is.EqualTo(PortalResult.Teleported), "Under-bridge exit blocked: " + gate.Exit.transform.position);
            }            Assert.That(session.World.Pads.Length, Is.EqualTo(6)); Assert.That(session.World.Portals.Length, Is.EqualTo(6));
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}


