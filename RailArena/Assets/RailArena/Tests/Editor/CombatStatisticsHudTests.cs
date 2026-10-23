using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RailArena.Simulation;
using RailArena.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CombatStatisticsHudTests
    {
        [UnityTest]
        public IEnumerator StandingsRankTiesAndResultsShowAuthoritativeTotals()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            var settings=InputSystem.settings; var originalKeyboard=Keyboard.current;
            var inputBehavior=settings.editorInputBehaviorInPlayMode; var background=settings.backgroundBehavior;
            Keyboard keyboard=null;
            DuelLink peer=null;
            try
            {
                settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard=InputSystem.AddDevice<Keyboard>("Statistics review"); keyboard.MakeCurrent();
                session.StartSession(true,3,1,TargetScenario.Stationary); session.SetPaused(true);
                AdvanceCountdown(session.Match); Seed(session.Match,true);
                session.SetPaused(false);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
                yield return Frames(3);
                var live=GameObject.Find("ScoreboardPanel"); Assert.That(live,Is.Not.Null);
                Assert.That(Cell(live,0,"Rank"),Is.EqualTo("1")); Assert.That(Cell(live,1,"Rank"),Is.EqualTo("1"));
                Assert.That(Cell(live,2,"Rank"),Is.EqualTo("3")); Assert.That(Cell(live,3,"Rank"),Is.EqualTo("4"));
                Assert.That(Cell(live,0,"Name"),Is.EqualTo("YOU")); Assert.That(Cell(live,0,"KillsDeaths"),Is.EqualTo("1 / 0"));
                Assert.That(Cell(live,0,"Accuracy"),Is.EqualTo("66.7%")); Assert.That(Cell(live,0,"HitsShots"),Is.EqualTo("2 / 3"));
                Assert.That(Cell(live,0,"Headshots"),Is.EqualTo("1")); Assert.That(Cell(live,3,"Points"),Is.EqualTo("-1"));
                foreach(var size in Sizes)yield return Capture(session.View,live.GetComponentInParent<Canvas>(),size,"Live");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                session.StartSession(false,3,1,TargetScenario.Combat); session.SetPaused(true);
                AdvanceCountdown(session.Match); Seed(session.Match,true); Finish(session.Match);
                session.GetComponent<ArenaHud>().RefreshMenu(); yield return Frames(2);
                var final=GameObject.Find("FinalStandings"); Assert.That(final,Is.Not.Null);
                Assert.That(Cell(final,0,"Name"),Is.EqualTo("YOU")); Assert.That(Cell(final,0,"Headshots"),Is.EqualTo("2"));
                Assert.That(Cell(final,0,"Points"),Is.EqualTo("2"));
                foreach(var size in Sizes)yield return Capture(session.View,final.GetComponentInParent<Canvas>(),size,"OfflineResults");
                int port; using(var socket=new System.Net.Sockets.UdpClient(0))port=((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
                session.HostDuel(port);
                peer=DuelLink.Join(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback,port),BuildIdentity.Token);
                float deadline=Time.realtimeSinceStartup+5;
                while(peer.State!=DuelLinkState.Connected)
                {
                    Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
                    peer.Tick(Time.realtimeSinceStartupAsDouble); yield return Frames(1);
                }
                yield return Frames(2);
                session.SetPaused(true); AdvanceCountdown(session.Match); Seed(session.Match,false); Finish(session.Match);
                session.GetComponent<ArenaHud>().RefreshMenu(); yield return Frames(2);
                final=GameObject.Find("FinalStandings"); Assert.That(final,Is.Not.Null);
                Assert.That(final.transform.Find("StandingRow2").gameObject.activeSelf,Is.False);
                foreach(var size in Sizes)yield return Capture(session.View,final.GetComponentInParent<Canvas>(),size,"DuelResults");
            }
            finally
            {
                peer?.Dispose();
                session.MainMenu();
                if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(originalKeyboard!=null && originalKeyboard.added)originalKeyboard.MakeCurrent();
                settings.editorInputBehaviorInPlayMode=inputBehavior; settings.backgroundBehavior=background;
            }
            yield return new ExitPlayMode();
        }

        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};
        private static string Cell(GameObject root,int row,string name)=>root.transform.Find($"StandingRow{row}/{name}").GetComponent<UnityEngine.UI.Text>().text;
        private static void AdvanceCountdown(MatchState match)
        { for(int i=0;i<CombatRules.CountdownTicks;i++)match.Advance(); }
        private static void Seed(MatchState match,bool four)
        {
            match.RecordShot(0,null); match.RecordShot(0,HitRegion.Body); match.RecordShot(0,HitRegion.Head);
            match.RecordKill(0,four?3:1);
            if(four){match.RecordShot(1,HitRegion.Head);match.RecordKill(1,2);match.RecordVoidDeath(2);}
        }
        private static void Finish(MatchState match)
        {
            for(int i=0;i<=CombatRules.MatchTicks;i++)
            {
                if(match.Phase==MatchPhase.Results)break;
                match.Advance();
                if(match.Phase==MatchPhase.SuddenDeath){match.RecordShot(0,HitRegion.Head);match.RecordKill(0,1);}
            }
            Assert.That(match.Phase,Is.EqualTo(MatchPhase.Results));
        }

        private static IEnumerator Capture(Camera camera,Canvas canvas,Vector2Int size,string name)
        {
            var target=new RenderTexture(size.x,size.y,24); var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var active=RenderTexture.active; var oldTarget=camera.targetTexture;
            var oldMode=canvas.renderMode; var oldCamera=canvas.worldCamera; float oldDistance=canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera; canvas.planeDistance=.2f;
                yield return Frames(2); Canvas.ForceUpdateCanvases();
                HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>().Where(text=>text.transform.parent.name.StartsWith("StandingRow")))
                {
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name+" was clipped");
                    var corners=new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                    foreach(var point in corners)
                    {
                        var pixel=RectTransformUtility.WorldToScreenPoint(camera,point);
                        Assert.That(pixel.x,Is.InRange(0,size.x)); Assert.That(pixel.y,Is.InRange(0,size.y));
                    }
                }
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/CombatStatistics"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode; canvas.worldCamera=oldCamera; canvas.planeDistance=oldDistance;
                camera.targetTexture=oldTarget; RenderTexture.active=active;
                Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count; float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;}
        }
        [UnityTearDown] public IEnumerator RestoreEditorMode(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
