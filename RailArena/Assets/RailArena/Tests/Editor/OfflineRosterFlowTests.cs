using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class OfflineRosterFlowTests
    {
        [UnityTest]
        public IEnumerator SelectionCreatesUniqueContendersMatchingHandsAndBoundedSessions()
        {
            yield return new EnterPlayMode();
            if (Object.FindAnyObjectByType<ArenaSession>() == null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session = Object.FindAnyObjectByType<ArenaSession>();
            bool existed = PlayerPrefs.HasKey("offlineCharacter");
            int previous = PlayerPrefs.GetInt("offlineCharacter");
            try
            {
                for (int selected = 0; selected < 4; selected++)
                {
                    session.SelectOfflineCharacter(selected);
                    session.StartSession(true,99,1,TargetScenario.Stationary);
                    yield return Frames(3);
                    Assert.That(session.BotCount, Is.EqualTo(3));
                    Assert.That(session.Actors.Count, Is.EqualTo(4));
                    Assert.That(session.Human.CharacterId, Is.EqualTo(selected));
                    Assert.That(session.Actors.Select(actor => actor.CharacterId).Distinct().Count(), Is.EqualTo(4));
                    Assert.That(PlayerPrefs.GetInt("offlineCharacter"), Is.EqualTo(selected));
                    foreach (var actor in session.Actors)
                    {
                        Assert.That(actor.Controller.height, Is.EqualTo(1.8f));
                        Assert.That(actor.Controller.radius, Is.EqualTo(.36f));
                    }
                    var arms = session.View.GetComponentInChildren<FirstPersonArms>(true);
                    Assert.That(arms.Surface.sharedMaterial, Is.SameAs(session.Human.Presentation.BodyRenderers[0].sharedMaterial));
                    CaptureWorld(session.View,$"Character-{selected}");
                    session.SetPaused(true);
                    session.SelectOfflineCharacter((selected+1)%4);
                    Assert.That(session.Human.CharacterId, Is.EqualTo(selected), "Selection must not swap a live contender.");
                    session.SelectOfflineCharacter(selected);
                    session.SetPaused(false);
                }
                session.StartSession(false,99,1,TargetScenario.Stationary);
                yield return Frames(2);
                Assert.That(session.BotCount, Is.EqualTo(3));
                Assert.That(session.Scenario, Is.EqualTo(TargetScenario.Combat));
                session.StartSession(false,-5,1,TargetScenario.Moving);
                yield return Frames(2);
                Assert.That(session.BotCount, Is.EqualTo(1));
                Assert.That(session.Scenario, Is.EqualTo(TargetScenario.Combat));
                session.StartSession(true,-5,1,TargetScenario.Moving);
                yield return Frames(2);
                Assert.That(session.Actors.Count, Is.EqualTo(1));
                Assert.That(session.Scenario, Is.EqualTo(TargetScenario.Moving));
                session.MainMenu();
                yield return Frames(2);
                // The menu cycles only valid offline appearances and populations.
                int firstChoice = session.SelectedOfflineCharacter;
                for (int i = 0; i < 4; i++)
                {
                    ButtonStarting("CHARACTER //").onClick.Invoke();
                    yield return Frames(1);
                    Assert.That(session.SelectedOfflineCharacter, Is.EqualTo((firstChoice+i+1)%4));
                }
                for (int i = 0; i < 4; i++)
                {
                    ButtonStarting("BOTS  ").onClick.Invoke();
                    yield return Frames(1);
                    string label = ButtonStarting("BOTS  ").GetComponentInChildren<UnityEngine.UI.Text>().text;
                    Assert.That(label, Is.EqualTo(i == 0 ? "BOTS  0 // SOLO   +" : $"BOTS  {i}   +"));
                    if (i == 0)
                    {
                        Assert.That(ButtonStarting("BOT MATCH // SELECT").interactable, Is.False);
                        Assert.That(ButtonStarting("SOLO PRACTICE").interactable, Is.True);
                    }
                }
                foreach (var size in new[] {new Vector2Int(1920,1080),new Vector2Int(1280,720)})
                    yield return CaptureMenu(session.View,size);
                int port;
                using (var probe = new System.Net.Sockets.UdpClient(0)) port = ((System.Net.IPEndPoint)probe.Client.LocalEndPoint).Port;
                session.HostDuel(port);
                yield return Frames(2);
                Assert.That(session.IsOnline, Is.True);
                CollectionAssert.AreEqual(new[] {0,1},session.Actors.Select(actor => actor.CharacterId).ToArray());
                session.MainMenu();
            }
            finally
            {
                session.MainMenu();
                if (existed) PlayerPrefs.SetInt("offlineCharacter",previous); else PlayerPrefs.DeleteKey("offlineCharacter");
                PlayerPrefs.Save();
            }
            yield return new ExitPlayMode();
        }

        private static UnityEngine.UI.Button ButtonStarting(string prefix)
            => Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .Single(button => button.GetComponentInChildren<UnityEngine.UI.Text>()?.text.StartsWith(prefix) == true);

        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/OfflineRoster"));

        private static void CaptureWorld(Camera camera,string name)
        {
            var target = new RenderTexture(1920,1080,24);
            var image = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var old = RenderTexture.active;
            try
            {
                target.Create(); RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
                Directory.CreateDirectory(Output); File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());
            }
            finally { RenderTexture.active = old; Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target); }
        }

        private static IEnumerator CaptureMenu(Camera camera,Vector2Int size)
        {
            var canvas = ButtonStarting("CHARACTER //").GetComponentInParent<Canvas>();
            var target = new RenderTexture(size.x,size.y,24);
            var image = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var active = RenderTexture.active; var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f;
                yield return Frames(2); Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                foreach (var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    Assert.That(Vector3.Distance(corners[0],corners[2]), Is.GreaterThan(.001f));
                    foreach (var point in corners)
                    {
                        var pixel = RectTransformUtility.WorldToScreenPoint(camera,point);
                        Assert.That(pixel.x, Is.InRange(0,size.x)); Assert.That(pixel.y, Is.InRange(0,size.y));
                    }
                }
                HeadlessUiReview.Draw(canvas,camera,target);
                RenderTexture.active = target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                Directory.CreateDirectory(Output); File.WriteAllBytes(Path.Combine(Output,$"Menu-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                camera.targetTexture = oldTarget; RenderTexture.active = active;
                Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static IEnumerator Frames(int count)
        {
            int target = Time.frameCount+count; float deadline = Time.realtimeSinceStartup+5;
            while (Time.frameCount < target)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
