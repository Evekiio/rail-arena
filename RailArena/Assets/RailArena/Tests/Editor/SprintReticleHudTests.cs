using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailArena.Tests
{
    public sealed class SprintReticleHudTests
    {
        [UnityTest]
        public IEnumerator SprintCircleTracksShotConeAndHidesWhenSpreadStops()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode(); yield return null; yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            yield return null;
            var hud = Object.FindAnyObjectByType<ArenaHud>();
            var update = typeof(ArenaHud).GetMethod("UpdateCombatHud", BindingFlags.Instance | BindingFlags.NonPublic);
            while (!session.Match.IsActive) session.Match.Advance();
            var actor = session.Human;
            actor.GroundedOverride = true; actor.Command = new MovementCommand { Sprint = true, Move = Vector2.up };
            var animate = typeof(ArenaHud).GetMethod("UpdateSprintSpread", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(hud, null);
            var circle = GameObject.Find("Sprint spread"); Assert.That(circle, Is.Not.Null);
            var rect = circle.GetComponent<RectTransform>();
            var canvas = rect.GetComponentInParent<Canvas>();
            float radius = SprintFireSpread.ScreenRadius(session.View.pixelHeight, session.View.fieldOfView, canvas.scaleFactor);
            Assert.That(rect.childCount, Is.EqualTo(48));
            var boundary = GameObject.Find("Sprint spread boundary"); Assert.That(boundary, Is.Not.Null);
            var boundaryRect = boundary.GetComponent<RectTransform>();
            Assert.That(boundaryRect.sizeDelta.x, Is.EqualTo(radius * 2).Within(.01f));
            Assert.That(boundaryRect.GetChild(0).GetComponent<UnityEngine.UI.Image>().color.a, Is.GreaterThan(.5f));
            animate.Invoke(hud, new object[] { 0f, .2f });
            Assert.That(boundary.activeSelf, Is.False);
            animate.Invoke(hud, new object[] { SprintFireSpread.HalfAngleDegrees, 0f });
            Assert.That(boundary.activeSelf, Is.True, "Full boundary must appear on the first sprint frame");
            Assert.That(boundaryRect.sizeDelta.x, Is.EqualTo(radius * 2).Within(.01f));
            animate.Invoke(hud, new object[] { SprintFireSpread.HalfAngleDegrees, .04f }); float first = rect.sizeDelta.x;
            animate.Invoke(hud, new object[] { SprintFireSpread.HalfAngleDegrees, .04f }); float second = rect.sizeDelta.x;
            Assert.That(second, Is.GreaterThan(first)); Assert.That(second, Is.LessThan(radius * 2));
            Assert.That(boundary.activeSelf, Is.True); Assert.That(boundaryRect.sizeDelta.x, Is.EqualTo(radius * 2).Within(.01f));
            animate.Invoke(hud, new object[] { SprintFireSpread.HalfAngleDegrees, .2f });
            Assert.That(rect.sizeDelta.x, Is.EqualTo(radius * 2).Within(.01f));
            animate.Invoke(hud, new object[] { 0f, .04f });
            Assert.That(circle.activeSelf, Is.True); Assert.That(rect.sizeDelta.x, Is.LessThan(radius * 2)); Assert.That(boundary.activeSelf, Is.False);
            animate.Invoke(hud, new object[] { 0f, .2f }); Assert.That(circle.activeSelf, Is.False);
            actor.Command = new MovementCommand { Move = Vector2.up }; actor.Velocity = Vector3.forward * 2;
            update.Invoke(hud, null);
            float walking = SprintFireSpread.ScreenRadius(session.View.pixelHeight, session.View.fieldOfView, canvas.scaleFactor, SprintFireSpread.MovingHalfAngleDegrees);
            Assert.That(boundary.activeSelf, Is.True); Assert.That(boundaryRect.sizeDelta.x, Is.EqualTo(walking * 2).Within(.01f));
            actor.SetCrouched(true); update.Invoke(hud, null);
            float crouch = SprintFireSpread.ScreenRadius(session.View.pixelHeight, session.View.fieldOfView, canvas.scaleFactor, SprintFireSpread.CrouchMovingHalfAngleDegrees);
            Assert.That(boundaryRect.sizeDelta.x, Is.EqualTo(crouch * 2).Within(.01f));
            actor.Velocity = Vector3.zero; update.Invoke(hud, null); Assert.That(boundary.activeSelf, Is.False);
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}





