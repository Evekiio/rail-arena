using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
namespace RailArena.Tests
{
    public sealed class BasementBotTests
    {
        [UnityTest]
        public IEnumerator BotCompletesAllFourBasementRoutesThroughAuthority()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 1, 1, TargetScenario.Moving); session.SetPaused(true);
            var bot = session.Actors[1]; bot.Place(new Vector3(3, 22.08f, 3));
            while (!session.Match.IsActive) session.Match.Advance();
            var advance = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), session, typeof(ArenaSession).GetMethod("AdvanceValidationTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            int previousTrips = 0;
            for (int tick = 0; tick < 24000; tick++)
            {
                advance();
                if (bot.Brain.Basement.TripsCompleted != previousTrips)
                {
                    previousTrips = bot.Brain.Basement.TripsCompleted;
                    Debug.Log($"BASEMENT_BOT trip={previousTrips} tick={session.Match.Tick} position={bot.transform.position:F3}");
                }
                if (tick % 3000 == 0) Debug.Log($"BASEMENT_BOT_PROGRESS tick={tick} position={bot.transform.position:F3} {bot.Brain.Basement.Status} trips={previousTrips} life={bot.Life}");
                if (System.Array.TrueForAll(bot.Brain.Basement.CompletedByRoute, count => count > 0)) break;
            }
            Debug.Log("BASEMENT_BOT_COUNTS " + string.Join(",", bot.Brain.Basement.CompletedByRoute));
            Assert.That(System.Array.TrueForAll(bot.Brain.Basement.CompletedByRoute, count => count > 0), Is.True,
                $"Incomplete routes at {bot.transform.position}: {bot.Brain.Basement.Status}, counts={string.Join(",", bot.Brain.Basement.CompletedByRoute)}");
            Assert.That(bot.Life, Is.EqualTo(1), "Bot must complete without void deaths or respawn recovery");
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator ThreeBotsShareAllRoutesWithoutVoidRecovery()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 3, 1, TargetScenario.Moving); session.SetPaused(true);
            session.Actors[1].Place(new Vector3(3, 22.08f, 3));
            session.Actors[2].Place(new Vector3(-3, 22.08f, 3));
            session.Actors[3].Place(new Vector3(-3, 22.08f, -3));
            while (!session.Match.IsActive) session.Match.Advance();
            var advance = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), session, typeof(ArenaSession).GetMethod("AdvanceValidationTick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
            bool complete = false;
            for (int tick = 0; tick < 18000; tick++)
            {
                advance(); complete = true;
                for (int i = 1; i < session.Actors.Count; i++)
                    complete &= System.Array.TrueForAll(session.Actors[i].Brain.Basement.CompletedByRoute, count => count > 0);
                if (complete) break;
            }
            for (int i = 1; i < session.Actors.Count; i++)
            {
                var bot = session.Actors[i];
                Debug.Log($"BASEMENT_MULTI_BOT id={i} routes={string.Join(",", bot.Brain.Basement.CompletedByRoute)} life={bot.Life} position={bot.transform.position:F3} {bot.Brain.Basement.Status}");
                Assert.That(bot.Life, Is.EqualTo(1), "Route sharing must not require void recovery");
            }
            Assert.That(complete, Is.True, "Every bot must complete every route");
            yield return new ExitPlayMode();
        }        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}


