using System.Collections;
using System.IO;
using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class TwinPlatformsTests
    {
        [Test]
        public void PortalWirePreservesDiscontinuityAndTwoPickups()
        {
            var state = new DuelSnapshot { PickupTicks = new[] { 20, 40 } };
            state.Actors[0] = new ActorSnapshot { Health = 100, Life = 1, TeleportSerial = 3, PortalReadyTick = 130, LastTeleportTick = 100 };
            state.Actors[1] = new ActorSnapshot { Health = 100, Life = 1 };
            byte[] bytes = DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state });
            Assert.That(DuelProtocol.TryDecode(bytes, out var decoded), Is.True);
            Assert.That(decoded.Snapshot.Actors[0].TeleportSerial, Is.EqualTo(3));
            Assert.That(decoded.Snapshot.Actors[0].PortalReadyTick, Is.EqualTo(130));
            Assert.That(decoded.Snapshot.PickupTicks, Is.EqualTo(new[] { 20, 40 }));
            bytes[4] = 8; Assert.That(DuelProtocol.TryDecode(bytes, out _), Is.False);
        }

        [UnityTest]
        public IEnumerator RealControllersCrossBothRoutesAndBlockedExitsStaySafe()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity");
            yield return new EnterPlayMode(); yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 1, 1, TargetScenario.Stationary); session.SetPaused(true);
            foreach (var contender in session.Actors) contender.Bot = false;
            while (!session.Match.IsActive) session.Match.Advance();
            var world = session.World; var actor = session.Human;
            Assert.That(world.Portals.Length, Is.EqualTo(2)); Assert.That(world.Pads.Length, Is.EqualTo(2));
            Assert.That(world.Pickups.Length, Is.EqualTo(2));
            Assert.That(Physics.Raycast(new Vector3(0, 10, 0), Vector3.down, 40, 1 << 0, QueryTriggerInteraction.Ignore), Is.False, "The gap must contain no leftover walkable bridge");
            for (int start = 0; start < world.Nodes.Length; start++) for (int goal = 0; goal < world.Nodes.Length; goal++)
                if (start != goal) Assert.That(world.NextNode(start, goal), Is.Not.EqualTo(start), $"Disconnected route {start}->{goal}");
            session.Actors[1].Place(new Vector3(44, .08f, 14));
            var advance = typeof(ArenaSession).GetMethod("AdvanceValidationTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(advance, Is.Not.Null);
            void Advance(int ticks) { for (int i = 0; i < ticks; i++) advance.Invoke(session, null); }
            foreach (int side in new[] { -1, 1 })
            {
                actor.Place(new Vector3(side * 26, .08f, -10)); actor.Magazine.Rounds = 7;
                actor.Command = new MovementCommand { Move = Vector2.up, Yaw = side < 0 ? 90 : 270 };
                int serial = actor.TeleportSerial; Advance(35);
                Assert.That(actor.TeleportSerial, Is.EqualTo(serial + 1));
                Assert.That(actor.transform.position.x * -side, Is.GreaterThan(18));
                Assert.That(actor.Magazine.Rounds, Is.EqualTo(7));
                Assert.That(actor.Command.Yaw, Is.EqualTo(side < 0 ? 90 : 270));
            }
            foreach (int side in new[] { -1, 1 })
            {
                actor.Place(new Vector3(side * 22, .08f, 10)); actor.PadReadyTick = 0; actor.Command = default;
                int launches = session.PadLaunches; Advance(210);
                Assert.That(session.PadLaunches, Is.EqualTo(launches + 1));
                Assert.That(actor.transform.position.x * -side, Is.InRange(20, 46));
                Assert.That(actor.transform.position.y, Is.InRange(-.05f, .2f));
                Assert.That(actor.Grounded, Is.True);
            }
            // The opposite player blocks the full exit capsule, including client-side disabled-controller representations.
            actor.Place(new Vector3(-26, .08f, -10));
            session.Actors[1].Place(new Vector3(24.6f, .08f, -10));
            actor.Command = new MovementCommand { Move = Vector2.up, Yaw = 90 };
            int blockedSerial = actor.TeleportSerial; Advance(40);
            Assert.That(actor.TeleportSerial, Is.EqualTo(blockedSerial)); Assert.That(actor.transform.position.x, Is.LessThan(-24));
            session.Actors[1].Place(new Vector3(44, .08f, 14)); Advance(25);
            Assert.That(actor.TeleportSerial, Is.EqualTo(blockedSerial + 1));
            actor.Place(new Vector3(-24.1f, .08f, -10));
            session.Actors[1].Place(new Vector3(24.6f, .08f, -10)); session.Actors[1].Controller.enabled = false;
            Vector3 beforeCrossing = actor.transform.position;
            actor.Warp(new Vector3(-23.9f, .08f, -10), Vector3.right * 13, true);
            Assert.That(world.Portals[0].TryTraverse(actor, beforeCrossing, session.Match.Tick, session.Actors), Is.EqualTo(PortalResult.Blocked));
            session.Actors[1].Controller.enabled = true; session.Actors[1].Place(new Vector3(44, .08f, 14));
            actor.Place(beforeCrossing); actor.SetCrouched(true); actor.Slide = new SlideState { RemainingTicks = 8 };
            actor.Warp(new Vector3(-23.9f, .08f, -10), Vector3.right * 13, true);
            Assert.That(world.Portals[0].TryTraverse(actor, beforeCrossing, session.Match.Tick, session.Actors), Is.EqualTo(PortalResult.Teleported));
            Assert.That(actor.Slide.RemainingTicks, Is.EqualTo(8)); Assert.That(actor.IsCrouched, Is.True);
            Assert.That(actor.Velocity.x, Is.EqualTo(13));
            // Opaque gate surfaces block shots while allowing the controller crossing resolver to run.
            Assert.That(Physics.Raycast(new Vector3(-27, 1.58f, -10), Vector3.right, out var wall, 8, 1 << 0, QueryTriggerInteraction.Collide), Is.True);
            Assert.That(wall.collider.isTrigger, Is.True); Assert.That(wall.distance, Is.LessThan(4));
            // Teleporting must not bounce back during the shared guard interval.
            actor.Place(new Vector3(-26, .08f, -10)); actor.PortalReadyTick = 0;
            actor.Command = new MovementCommand { Move = Vector2.up, Yaw = 90 }; Advance(20);
            int afterSerial = actor.TeleportSerial;
            actor.Command = new MovementCommand { Move = Vector2.up, Yaw = 270 }; Advance(18);
            Assert.That(actor.TeleportSerial, Is.EqualTo(afterSerial));
            Assert.That(actor.transform.position.x, Is.GreaterThanOrEqualTo(24));
            session.MainMenu(); yield return Frames(3);
            Capture(session.View, "Overview");
            session.StartSession(true, 0, 1, TargetScenario.Stationary); session.SetPaused(true);
            var camera = session.View;
            foreach (var view in new[] { ("Blue approach", new Vector3(-41, 2.2f, -1), new Vector3(-24, 1.5f, -10)),
                ("Gap and super pads", new Vector3(-32, 3, 14), new Vector3(28, 5, 10)),
                ("Red approach", new Vector3(41, 2.2f, -1), new Vector3(24, 1.5f, -10)) })
            {
                session.enabled = false; camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                Capture(camera, view.Item1);
            }
            session.enabled = true; session.MainMenu(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator BotsUseBothTraversalTypesWithoutFallingIntoTheGap()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity");
            yield return new EnterPlayMode(); yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.AutomatedHuman = true;
            session.StartSession(true, 3, 1, TargetScenario.Moving); session.SetPaused(true);
            var advance = typeof(ArenaSession).GetMethod("AdvanceValidationTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (int tick = 0; tick < 3780; tick++) advance.Invoke(session, null);
            int portals = 0;
            foreach (var actor in session.Actors) { portals += actor.TeleportSerial; Assert.That(actor.Brain.NodesReached, Is.GreaterThan(3)); }
            Assert.That(portals, Is.GreaterThan(0)); Assert.That(session.PadLaunches, Is.GreaterThan(0)); Assert.That(session.VoidDeaths, Is.Zero);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/TwinPlatforms-2026-10-29"));
            Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "bot-traversal.txt"), $"ticks=3780 portals={portals} pads={session.PadLaunches} voids={session.VoidDeaths}");
            session.MainMenu(); yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator NewLayoutMenuHelpAndHudFitReferenceScreens()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity");
            yield return new EnterPlayMode(); yield return Frames(3);
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.MainMenu(); yield return Frames(2);
            yield return BrandUiTests.Review(session.View, "Twin-Main");
            foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if (button.GetComponentInChildren<UnityEngine.UI.Text>().text == "HOW TO PLAY") { button.onClick.Invoke(); break; }
            yield return Frames(2); yield return BrandUiTests.Review(session.View, "Twin-Help");
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            session.enabled = false;
            typeof(ArenaSession).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(session, null);
            yield return Frames(2); yield return BrandUiTests.Review(session.View, "Twin-HUD");
            session.enabled = true; session.MainMenu(); yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Restore() { if (Application.isPlaying) yield return new ExitPlayMode(); }
        private static IEnumerator Frames(int count)
        {
            int end = Time.frameCount + count; double deadline = UnityEditor.EditorApplication.timeSinceStartup + 5;
            while (Time.frameCount < end) { Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(deadline)); UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
        }
        private static void Capture(Camera camera, string name)
        {
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var old = RenderTexture.active; var previous = camera.targetTexture;
            try
            {
                target.Create(); camera.targetTexture = target;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/TwinPlatforms-2026-10-29/Captures"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = old; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
