using System.Collections;
using System.Linq;
using NUnit.Framework;
using RailArena.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class ShutdownTests
    {
        [UnityTest]
        public IEnumerator QuitIsAvailableAndReleasesTheHostSocket()
        {
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.MainMenu();
            yield return null;
            yield return null;
            AssertQuitButton();
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            session.SetPaused(true);
            yield return null;
            yield return null;
            AssertQuitButton();

            int port;
            using (var probe = new DuelSocket(0)) port = probe.LocalEndPoint.Port;
            session.HostDuel(port);
            yield return null;
            yield return null;
            Assert.That(session.IsOnline, Is.True);
            var quit = AssertQuitButton();
            // Application.Quit is ignored in the Editor; the real menu action still releases resources.
            quit.onClick.Invoke();
            Assert.That(session.IsOnline, Is.False);
            using (var rebound = new DuelSocket(port)) Assert.That(rebound.LocalEndPoint.Port, Is.EqualTo(port));
            session.QuitGame();
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            yield return new ExitPlayMode();
        }

        private static UnityEngine.UI.Button AssertQuitButton()
        {
            var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            var quit = buttons.SingleOrDefault(button => button.GetComponentInChildren<UnityEngine.UI.Text>()?.text == "QUIT");
            Assert.That(quit, Is.Not.Null, "The current menu must offer a direct game exit.");
            Assert.That(quit.interactable, Is.True);
            return quit;
        }
    }
}
