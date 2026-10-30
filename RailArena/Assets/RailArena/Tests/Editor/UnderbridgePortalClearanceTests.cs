using System.Collections;
using System.Linq;
using System.IO;
using System;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class UnderbridgePortalClearanceTests
    {
        [Test] public void WallMountIsIdempotent()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            var builder = Type.GetType("RailArena.Editor.UnderbridgePortalArtBuilder, Assembly-CSharp-Editor", true);
            var mount = builder.GetMethod("Mount"); Assert.That(mount, Is.Not.Null);
            foreach (var gate in Object.FindObjectsByType<PortalGate>(FindObjectsSortMode.None).Where(gate => gate.name.StartsWith("Under bridge")))
            {
                var original = gate.transform.position;
                mount.Invoke(null, new object[] { gate });
                mount.Invoke(null, new object[] { gate });
                Assert.That(Vector3.Distance(original, gate.transform.position), Is.LessThan(.00001f));
            }
        }

        [UnityTest] public IEnumerator CaptureUnobstructedDestinationsWithoutDiagnosticCharts()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.enabled = false;
            foreach (var gate in session.World.Portals) gate.GetComponent<PortalView>().enabled = false;
            var camera = session.View; camera.enabled = false; camera.aspect = 16f / 9;
            var target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted"); Directory.CreateDirectory(folder);
            try
            {
                foreach (var gate in session.World.Portals.Where(gate => gate.name.StartsWith("Under bridge")))
                foreach (bool ads in new[] { false, true })
                {
                    camera.focalLength = Camera.FieldOfViewToFocalLength(ads ? new AimPreferences().ViewFov(true) : 100, camera.sensorSize.y);
                    camera.transform.position = gate.transform.position + gate.transform.forward * 2 + Vector3.up * ArenaActor.StandingEyeHeight;
                    camera.transform.LookAt(gate.Presentation.Surface.bounds.center);
                    yield return null;
                    Assert.That(gate.GetComponent<PortalView>().RenderView(camera), Is.True);
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    var previous = RenderTexture.active;
                    try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); }
                    finally { RenderTexture.active = previous; }
                    File.WriteAllBytes(Path.Combine(folder, gate.name.Replace(' ', '-') + (ads ? "-ADS-destination.png" : "-destination.png")), pixels.EncodeToPNG());
                }
            }
            finally { camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(pixels); }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator BothBasementPairsExposeTheirAperturesAndTraverseBothWays()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 0, 1, TargetScenario.Stationary); session.SetPaused(true);
            var actor = session.Human;
            var gates = session.World.Portals.Where(gate => gate.name == "Basement return portal" || gate.name.StartsWith("Under bridge")).ToArray();
            Assert.That(gates.Length, Is.EqualTo(4));
            foreach (var gate in gates)
            {
                foreach (bool crouch in new[] { false, true })
                foreach (float lateral in new[] { -.15f, 0, .15f })
                {
                    actor.Place(gate.transform.position + gate.transform.forward * 2 + gate.transform.right * lateral + Vector3.up * .08f);
                    actor.Command = new MovementCommand { Move = Vector2.up, Crouch = crouch, Yaw = Quaternion.LookRotation(-gate.transform.forward).eulerAngles.y };
                    int serial = actor.TeleportSerial;
                    var result = PortalResult.None;
                    for (int tick = 0; tick < 120; tick++)
                    {
                        var previous = actor.transform.position; actor.Move(session.Tuning); Physics.SyncTransforms();
                        result = gate.TryTraverse(actor, previous, 100 + tick, session.Actors);
                        if (result != PortalResult.None) break;
                    }
                    Assert.That(result, Is.EqualTo(PortalResult.Teleported), $"{gate.name}: crouch={crouch} lateral={lateral} stopped at {actor.transform.position}");
                    Assert.That(actor.TeleportSerial, Is.EqualTo(serial + 1));
                    Assert.That(Vector3.Dot(actor.transform.position - gate.Exit.transform.position, gate.Exit.transform.forward), Is.GreaterThan(actor.Controller.radius));
                    Assert.That(Physics.Raycast(actor.transform.position + Vector3.up * .25f, Vector3.down, .6f, 1 << 0), Is.True, "Exit needs floor support");
                    var clearance = Physics.OverlapCapsule(actor.transform.position + Vector3.up * actor.Controller.radius,
                        actor.transform.position + Vector3.up * (actor.Controller.height - actor.Controller.radius), actor.Controller.radius - .01f,
                        (1 << 0) | (1 << 9), QueryTriggerInteraction.Ignore);
                    Assert.That(clearance.All(collider => collider == actor.Controller || collider.transform.IsChildOf(actor.transform)), Is.True, "Exit capsule intersects map collision");
                }
                if (!gate.name.StartsWith("Under bridge")) continue;
                foreach (float eye in new[] { ArenaActor.CrouchingEyeHeight, ArenaActor.StandingEyeHeight })
                foreach (float strafe in new[] { -.6f, 0, .6f })
                foreach (float horizontal in new[] { -.45f, 0, .45f })
                {
                    var origin = gate.transform.position + gate.transform.forward * 2 + gate.transform.right * strafe + Vector3.up * eye;
                    var point = gate.transform.position + gate.transform.right * horizontal + Vector3.up * 1.1f;
                    var delta = point - origin;
                    Assert.That(Physics.Raycast(origin, delta.normalized, out var hit, delta.magnitude - .01f, (1 << 0) | (1 << 9), QueryTriggerInteraction.Ignore),
                        Is.False, $"{gate.name} aperture is blocked by {hit.collider?.name}, eye={eye} strafe={strafe} sample={horizontal}");
                }
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
