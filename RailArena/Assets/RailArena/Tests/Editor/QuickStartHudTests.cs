using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class QuickStartHudTests
    {
        [UnityTest]
        public IEnumerator HelpPreservesSetupAndLaunchesSoloWithoutForcingATutorial()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();session.MainMenu();yield return Frames(2);
            var hud=session.GetComponent<ArenaHud>();
            Assert.That(GameObject.Find("Quick start subtitle"),Is.Null,"Help must be optional.");
            var bots=typeof(ArenaHud).GetField("bots",BindingFlags.Instance|BindingFlags.NonPublic);
            var difficulty=typeof(ArenaHud).GetField("difficulty",BindingFlags.Instance|BindingFlags.NonPublic);
            var scenario=typeof(ArenaHud).GetField("scenario",BindingFlags.Instance|BindingFlags.NonPublic);
            ButtonStarting("BOTS ").onClick.Invoke();yield return Frames(2);
            ButtonStarting("DIFFICULTY ").onClick.Invoke();yield return Frames(2);
            ButtonStarting("PRACTICE TARGETS //").onClick.Invoke();yield return Frames(2);
            int configuredDifficulty=(int)difficulty.GetValue(hud);
            var configuredScenario=(TargetScenario)scenario.GetValue(hud);
            int character=session.SelectedOfflineCharacter;
            Assert.That(Label("Weapon help"),Does.Contain("HEAD "+CombatRules.Damage(HitRegion.Head)));
            foreach(var size in Sizes)yield return Capture(session.View,size,"Menu");
            Assert.That(ButtonStarting("BOT MATCH").interactable,Is.False);
            for(int i=0;i<2;i++) {ButtonStarting("BOTS ").onClick.Invoke();yield return Frames(2);}
            int configuredBots=(int)bots.GetValue(hud);
            Assert.That(configuredBots,Is.EqualTo(2),"Preservation must be checked with a nonzero bot setup.");
            Button("HOW TO PLAY").onClick.Invoke();yield return Frames(2);
            Assert.That(Label("Modes body"),Does.Contain((CombatRules.MatchTicks/(CombatRules.TickRate*60))+" minutes"));
            Assert.That(Label("Controls body"),Does.Contain("One shot per click"));
            Assert.That(Label("Combat basics"),Does.Contain("holding fire will not repeat"));
            foreach(var size in Sizes)yield return Capture(session.View,size,"Help");
            Button("BACK").onClick.Invoke();yield return Frames(2);
            Assert.That((int)bots.GetValue(hud),Is.EqualTo(configuredBots));
            Assert.That((int)difficulty.GetValue(hud),Is.EqualTo(configuredDifficulty));
            Assert.That((TargetScenario)scenario.GetValue(hud),Is.EqualTo(configuredScenario));
            Button("HOW TO PLAY").onClick.Invoke();yield return Frames(2);
            Button("START SOLO PRACTICE").onClick.Invoke();yield return Frames(2);
            Assert.That(session.Practice,Is.True);Assert.That(session.BotCount,Is.Zero);Assert.That(session.Actors.Count,Is.EqualTo(1));
            Assert.That(session.Human.CharacterId,Is.EqualTo(character));Assert.That(session.Paused,Is.False);
            Assert.That((int)bots.GetValue(hud),Is.EqualTo(configuredBots));
            session.SetPaused(true);Button("HOW TO PLAY").onClick.Invoke();yield return Frames(2);
            int tick=session.Match.Tick;
            foreach(var size in Sizes)yield return Capture(session.View,size,"PausedHelp");
            Assert.That(session.Match.Tick,Is.EqualTo(tick));
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .Any(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text=="START SOLO PRACTICE"),Is.False);
            Button("RESUME").onClick.Invoke();yield return Frames(2);Assert.That(session.Paused,Is.False);
            session.MainMenu();yield return new ExitPlayMode();
        }

        private static string Label(string name)=>GameObject.Find(name).GetComponent<UnityEngine.UI.Text>().text;
        private static UnityEngine.UI.Button Button(string text)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text==text);
        private static UnityEngine.UI.Button ButtonStarting(string text)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith(text));
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};

        private static IEnumerator Capture(Camera camera,Vector2Int size,string name)
        {
            var canvas=GameObject.Find("Arena interface").GetComponent<Canvas>();
            var target=new RenderTexture(size.x,size.y,24);var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            try
            {
                target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;
                yield return Frames(2);Canvas.ForceUpdateCanvases();HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                var corners=new Vector3[4];
                foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>())
                {
                    button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                        Assert.That(point.x,Is.InRange(0,size.x));Assert.That(point.y,Is.InRange(0,size.y));
                    }
                }
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/QuickStart"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;camera.targetTexture=previousTarget;RenderTexture.active=previous;
                Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);
            }
        }
        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count;float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
        }
        [UnityTearDown]
        public IEnumerator RestoreEditorMode() {if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
