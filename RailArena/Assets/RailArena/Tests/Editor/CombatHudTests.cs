using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CombatHudTests
    {
        [UnityTest]
        public IEnumerator LiveHudTracksCombatAndProductionKeyboardMapping()
        {
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry", LoadSceneMode.Additive);
            yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            Assert.That(session, Is.Not.Null);
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            yield return Frames(2);
            var root = GameObject.Find("CombatHud");
            Assert.That(root, Is.Not.Null);
            var texts = root.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            UnityEngine.UI.Text Label(string name) => texts.Single(t => t.name == name);
            Assert.That(Label("ModeValue").text, Is.EqualTo("SOLO PRACTICE"));
            Assert.That(Label("RailValue").text, Is.EqualTo("STAND BY"));
            while (!session.Match.IsActive) session.Match.Advance();
            foreach (var pickup in session.World.Pickups) pickup.AvailableTick = int.MaxValue;
            session.Human.Health = 25;
            session.Human.NextShotTick = session.LocalSimulationTick + 45;
            yield return Frames(2);
            Assert.That(Label("HealthValue").text, Is.EqualTo("025"));
            var health = root.transform.Find("HealthPanel/HealthFillTrack/HealthFill").GetComponent<RectTransform>();
            var rail = root.transform.Find("RailPanel/RailFillTrack/RailFill").GetComponent<RectTransform>();
            Assert.That(health.anchorMax.x, Is.EqualTo(.25f).Within(.001));
            Assert.That(rail.anchorMax.x, Is.InRange(.45f, .65f));
            Assert.That(Label("RailValue").text, Does.StartWith("CYCLE"));
            Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g => !g.raycastTarget), Is.True);
            session.Feedback.ConfirmHit(false); session.Feedback.Damaged();
            yield return Frames(2);
            Assert.That(root.transform.Find("HitConfirmation").gameObject.activeSelf, Is.True);
            Assert.That(root.transform.Find("DamageEdge0").GetComponent<UnityEngine.UI.Image>().color.a, Is.GreaterThan(0));
            session.Feedback.ResetFeedback();
            yield return Frames(2);
            Assert.That(root.transform.Find("HitConfirmation").gameObject.activeSelf, Is.False);
            var crosshair = root.transform.Find("Crosshair");
            Canvas.ForceUpdateCanvases();
            var viewport = root.GetComponentInParent<Canvas>().pixelRect;
            var center = RectTransformUtility.WorldToScreenPoint(null, crosshair.position);
            Assert.That(center.x, Is.EqualTo(viewport.center.x).Within(1));
            Assert.That(center.y, Is.EqualTo(viewport.center.y).Within(1));
            var corners = new Vector3[4];
            foreach (var panel in new[] { "HealthPanel", "StatsPanel", "RailPanel", "MatchPanel" })
            {
                root.transform.Find(panel).GetComponent<RectTransform>().GetWorldCorners(corners);
                Assert.That(corners.All(c => c.x >= viewport.xMin && c.x <= viewport.xMax && c.y >= viewport.yMin && c.y <= viewport.yMax), Is.True, panel);
            }

            var originalKeyboard = Keyboard.current;
            var originalSettings = InputSystem.settings;
            var originalEditorInput = originalSettings.editorInputBehaviorInPlayMode;
            var originalBackground = originalSettings.backgroundBehavior;
            Keyboard keyboard = null;
            try
            {
                originalSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                originalSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = InputSystem.AddDevice<Keyboard>("HUD review keyboard"); keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
                Vector3 before = session.Human.transform.position;
                int movementTick = session.Match.Tick + 15;
                float movementDeadline = Time.realtimeSinceStartup + 5;
                while (session.Match.Tick < movementTick && Time.realtimeSinceStartup < movementDeadline) yield return null;
                Assert.That(session.Match.Tick, Is.GreaterThanOrEqualTo(movementTick));
                Vector3 displacement = session.Human.transform.position - before;
                Assert.That(new Vector2(displacement.x, displacement.z).magnitude, Is.GreaterThan(.1f),
                    $"enabled={keyboard.enabled} current={Keyboard.current == keyboard} W={keyboard.wKey.isPressed} " +
                    $"command={session.Human.Command.Move} paused={session.Paused} phase={session.Match.Phase} " +
                    $"focus={Application.isFocused} background={InputSystem.settings.backgroundBehavior} tick={session.Match.Tick}");
                Assert.That(session.Human.transform.position.y, Is.GreaterThan(before.y + .1f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
                yield return Frames(2);
                Assert.That(root.transform.Find("ScoreboardPanel").gameObject.activeSelf, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return Frames(2);
                Assert.That(root.transform.Find("ScoreboardPanel").gameObject.activeSelf, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return Frames(2);
                Assert.That(session.Paused, Is.True);
                Assert.That(root.activeSelf, Is.False);
                int pausedTick = session.Match.Tick;
                float pauseDeadline = Time.realtimeSinceStartup + .08f;
                while (Time.realtimeSinceStartup < pauseDeadline) yield return null;
                Assert.That(session.Match.Tick, Is.EqualTo(pausedTick));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return Frames(2);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return Frames(2);
                Assert.That(session.Paused, Is.False);
                Assert.That(root.activeSelf, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                session.Human.Health = 0;
                session.Human.RespawnTick = session.Match.Tick + 120;
                session.Human.SetAlive(false);
                yield return Frames(2);
                Assert.That(crosshair.gameObject.activeSelf, Is.False);
                Assert.That(Label("Notice").text, Does.StartWith("RESPAWNING"));
            }
            finally
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
                originalSettings.editorInputBehaviorInPlayMode = originalEditorInput;
                originalSettings.backgroundBehavior = originalBackground;
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }

        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount + count;
            float deadline = Time.realtimeSinceStartup + 5;
            while (Time.frameCount < target && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Time.frameCount, Is.GreaterThanOrEqualTo(target), "Player frames stopped advancing.");
        }
    }
}
