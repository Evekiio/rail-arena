using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailArena.Tests
{
    public sealed class GraphicsTierMenuTests
    {
        [UnityTest]
        public IEnumerator TierButtonCyclesAndRestoresTheStartingSetting()
        {
            var keys = new[] { GraphicsPreferences.TierKey, GraphicsPreferences.AntialiasingKey, GraphicsPreferences.MotionBlurKey };
            var existed = keys.Select(PlayerPrefs.HasKey).ToArray();
            var values = keys.Select(key => PlayerPrefs.GetInt(key)).ToArray();
            try
            {
                EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
                yield return new EnterPlayMode();
                yield return null;
                var session = Object.FindAnyObjectByType<ArenaSession>();
                Assert.That(session, Is.Not.Null);
                var hud = session.GetComponent<ArenaHud>();
                Assert.That(hud, Is.Not.Null);
                typeof(ArenaHud).GetMethod("FramePacingSettings", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                var button = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                    .Single(item => item.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("GRAPHICS // "));
                var initial = session.Graphics.Tier;
                for (int i = 0; i < 3; i++)
                {
                    var next = GraphicsTierProfiles.Next(session.Graphics.Tier);
                    button.onClick.Invoke();
                    Assert.That(session.Graphics.Tier, Is.EqualTo(next));
                    Assert.That(button.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("GRAPHICS // " + next.ToString().ToUpperInvariant()));
                }
                Assert.That(session.Graphics.Tier, Is.EqualTo(initial));
            }
            finally
            {
                for (int i = 0; i < keys.Length; i++) { if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]); }
                PlayerPrefs.Save();
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator StopPlayMode() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
