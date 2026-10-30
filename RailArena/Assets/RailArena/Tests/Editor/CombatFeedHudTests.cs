using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CombatFeedHudTests
    {
        [UnityTest]
        public IEnumerator FeedStaysOutOfPhaseNoticeAndRespawnWinsOverSuddenDeath()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            var originalKeyboard=Keyboard.current;var settings=InputSystem.settings;
            var originalInput=settings.editorInputBehaviorInPlayMode;var originalBackground=settings.backgroundBehavior;
            Keyboard keyboard=null;
            try
            {
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard=InputSystem.AddDevice<Keyboard>("Combat feed review");keyboard.MakeCurrent();
                session.StartSession(false,3,1,TargetScenario.Combat);session.enabled=false;
                while(session.Match.Phase!=MatchPhase.SuddenDeath)session.Match.Advance();
                string repeated=$"YOU  >  {session.Actors[1].DisplayName}   HEAD";
                session.RecentCombat.Add(repeated,Time.realtimeSinceStartupAsDouble);
                session.RecentCombat.Add($"{session.Actors[2].DisplayName}  >  {session.Actors[3].DisplayName}   BODY",Time.realtimeSinceStartupAsDouble);
                session.RecentCombat.Add(repeated,Time.realtimeSinceStartupAsDouble);
                yield return Frames(2);
                Assert.That(Text("Notice"),Is.EqualTo("SUDDEN DEATH // NEXT KILL WINS"));
                Assert.That(Text("Feed row 0"),Is.EqualTo(repeated));
                Assert.That(Text("Feed row 2"),Is.EqualTo(repeated));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Recent");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return Frames(2);
                Assert.That(GameObject.Find("CombatFeedPanel"),Is.Null);Assert.That(GameObject.Find("ScoreboardPanel"),Is.Not.Null);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames(2);
                Assert.That(GameObject.Find("CombatFeedPanel"),Is.Not.Null);
                session.Match.RecordVoidDeath(0);session.Human.Health=0;session.Human.RespawnTick=session.Match.Tick+120;session.Human.SetAlive(false);
                session.RecentCombat.Add("YOU fell into the void",Time.realtimeSinceStartupAsDouble);yield return Frames(2);
                Assert.That(Text("Notice"),Does.StartWith("RESPAWNING // 2.0s"));
                Assert.That(Text("Feed row 0"),Is.EqualTo("YOU fell into the void"));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Respawn");
                session.RecentCombat.Clear();yield return Frames(2);Assert.That(GameObject.Find("CombatFeedPanel"),Is.Null);
                session.RecentCombat.Add("Reset on new session",Time.realtimeSinceStartupAsDouble);
                session.StartSession(true,0,1,TargetScenario.Stationary);Assert.That(session.RecentCombat.Entries,Is.Empty);
            }
            finally
            {
                if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(originalKeyboard!=null && originalKeyboard.added)originalKeyboard.MakeCurrent();
                settings.editorInputBehaviorInPlayMode=originalInput;settings.backgroundBehavior=originalBackground;
                session.enabled=true;session.MainMenu();
            }
            yield return new ExitPlayMode();
        }
        private static string Text(string name)=>GameObject.Find(name).GetComponent<UnityEngine.UI.Text>().text;
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};
        private static IEnumerator Capture(Camera camera,Vector2Int size,string name)
        {
            var canvas=GameObject.Find("Arena interface").GetComponent<Canvas>();
            var target=new RenderTexture(size.x,size.y,24);var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            try
            {
                target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;
                yield return Frames(2);Canvas.ForceUpdateCanvases();HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                var feed=GameObject.Find("CombatFeedPanel").GetComponent<RectTransform>();var corners=new Vector3[4];feed.GetWorldCorners(corners);
                Assert.That(corners.Select(point=>RectTransformUtility.WorldToScreenPoint(camera,point).x).Min(),Is.GreaterThan(size.x*.65f));
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/CombatFeed"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally {canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;camera.targetTexture=previousTarget;RenderTexture.active=previous;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);}
        }
        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count;float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target) {Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;}
        }
        [UnityTearDown]
        public IEnumerator RestoreEditorMode() {if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
