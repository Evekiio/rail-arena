using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class BrandUiTests
    {
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720),new(2560,1080)};
        private RenderPipelineAsset originalPipeline;
        private bool artPipeline;

        [UnityTest]
        public IEnumerator BrandedFlowsKeepSetupFocusBackNavigationAndReadableHud()
            => Run(false);

        [UnityTest]
        public IEnumerator ArtPassFlowsKeepSetupFocusBackNavigationAndReadableHud()
            => Run(true);

        private IEnumerator Run(bool enhanced)
        {
            if(enhanced)
            {
                originalPipeline=QualitySettings.renderPipeline;artPipeline=true;
                QualitySettings.renderPipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            }
            EditorSceneManager.OpenScene(enhanced?"Assets/RailArena/Generated/SkyGardensBasement.unity":"Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode();yield return Frames(3);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            if(enhanced){new GameObject("Integrated art UI review").AddComponent<ArtSlicePresentation>();yield return Frames(2);}
            var hud=session.GetComponent<ArenaHud>();
            session.MainMenu();yield return Frames(2);
            Assert.That(Resources.Load<Font>("UI/Brand/Fonts/Barlow-Regular"),Is.Not.Null);
            Assert.That(Resources.Load<Font>("UI/Brand/Fonts/BarlowCondensed-SemiBold"),Is.Not.Null);
            Assert.That(GameObject.Find("Rail Arena brand").GetComponent<UnityEngine.UI.RawImage>().texture,Is.Not.Null);
            yield return Review(session.View,"Main");
            var bots=Starting("BOTS  ");EventSystem.current.SetSelectedGameObject(bots.gameObject);
            bots.onClick.Invoke();yield return Frames(2);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(Starting("BOTS  ").gameObject));
            Button("CONTENDERS").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Contenders");
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("SETTINGS").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Settings");
            Button("AUDIO MIX / SOUND TEST").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Audio");
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("RETICLE SETTINGS").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Reticle");
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("VISUAL COMFORT").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Comfort");
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("FRAME PACING").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Display");
            bool originalSmaa = session.Graphics.Smaa;
            var antialiasing = Starting("ANTIALIASING // ");
            antialiasing.onClick.Invoke(); yield return Frames(2);
            Assert.That(session.Graphics.Smaa, Is.EqualTo(!originalSmaa));
            Assert.That(antialiasing.GetComponentInChildren<UnityEngine.UI.Text>().text,
                Is.EqualTo("ANTIALIASING // " + (session.Graphics.Smaa ? "SMAA HIGH" : "OFF")));
            antialiasing.onClick.Invoke(); yield return Frames(2);
            Assert.That(session.Graphics.Smaa, Is.EqualTo(originalSmaa));
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("HOW TO PLAY").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Help");
            Assert.That(hud.TryHandleBack(),Is.True);yield return Frames(2);
            Button("ONLINE DUEL // DIRECT IP").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Duel");
            GameObject.Find("UDP PORT entry").GetComponent<UnityEngine.UI.InputField>().text="0";
            Button("HOST DUEL").onClick.Invoke();yield return Frames(2);yield return Review(session.View,"Duel-error");
            Button("BACK").onClick.Invoke();yield return Frames(2);
            int port;
            using(var socket=new System.Net.Sockets.UdpClient(0))port=((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
            session.HostDuel(port);yield return Frames(2);
            Assert.That(Button("WAITING FOR OPPONENT").interactable,Is.False);
            yield return Review(session.View,"Ready-room");
            session.MainMenu();yield return Frames(2);
            session.StartSession(true,3,1,TargetScenario.Stationary);yield return Frames(3);
            while(!session.Match.IsActive)session.Match.Advance();
            session.enabled=false;session.Human.Health=25;session.Human.NextShotTick=session.LocalSimulationTick+Simulation.CombatRules.ShotIntervalTicks/2;
            typeof(ArenaSession).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,null);
            yield return Frames(2);yield return Review(session.View,"HUD-recovery");
            Assert.That(GameObject.Find("Health icon").GetComponent<UnityEngine.UI.RawImage>().texture,Is.Not.Null);
            session.Human.Health=100;session.Human.NextShotTick=0;
            var ads=(AimDownSights)typeof(ArenaSession).GetField("sights",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
            typeof(ArenaSession).GetProperty("IsAiming").SetValue(session,true);ads.Step(true,1);
            typeof(ArenaSession).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,null);
            yield return Frames(2);yield return Review(session.View,"HUD-ADS");
            session.SetPaused(true);yield return Frames(2);yield return Review(session.View,"Pause");
            Button("SETTINGS").onClick.Invoke();yield return Frames(2);
            Assert.That(hud.TryHandleBack(),Is.True);Assert.That(session.Paused,Is.True);
            session.StartSession(false,3,1,TargetScenario.Combat);session.enabled=false;
            while(!session.Match.IsActive)session.Match.Advance();
            session.Match.RecordKill(0,1);
            for(int tick=0;tick<Simulation.CombatRules.MatchTicks;tick++)session.Match.Advance();
            hud.RefreshMenu();yield return Frames(2);yield return Review(session.View,"Results");
            session.enabled=true;session.MainMenu();
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Restore()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(artPipeline){QualitySettings.renderPipeline=originalPipeline;artPipeline=false;}
        }
        private static UnityEngine.UI.Button Button(string text)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text==text);
        private static UnityEngine.UI.Button Starting(string prefix)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith(prefix));
        private static IEnumerator Frames(int count)
        {
            int end=Time.frameCount+count;double deadline=UnityEditor.EditorApplication.timeSinceStartup+10;
            while(Time.frameCount<end)
            {
                Assert.That(UnityEditor.EditorApplication.timeSinceStartup,Is.LessThan(deadline));
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
        }
        internal static IEnumerator Review(Camera camera,string state)
        {
            foreach(var size in Sizes)
            {
                var canvas=Object.FindAnyObjectByType<Canvas>();
                var target=new RenderTexture(size.x,size.y,24);
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
                try
                {
                    target.Create();camera.targetTexture=target;
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;
                    yield return Frames(2);Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                    HeadlessUiReview.Draw(canvas,camera,target,clear:false);
                    RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                    string folder=Path.GetFullPath(Path.Combine(Application.dataPath,Object.FindAnyObjectByType<ArtSlicePresentation>()!=null?"../../ArtSource/Original/ArtPass-v1/UiReview":"../../Validation/UI-Brand-2026-10-29/Captures"));
                    Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,$"{state}-{size.x}x{size.y}.png"),pixels.EncodeToPNG());
                    foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                        Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),state+" clipped text: "+text.name+" / "+text.text);
                    var controls=canvas.GetComponentsInChildren<UnityEngine.UI.Selectable>().Where(control=>control.IsActive()).ToArray();
                    foreach(var control in controls)
                    {
                        var corners=new Vector3[4];control.GetComponent<RectTransform>().GetWorldCorners(corners);
                        foreach(var corner in corners)
                        {
                            var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                            Assert.That(point.x,Is.InRange(-1,size.x+1),state+" horizontal bounds: "+control.name);
                            Assert.That(point.y,Is.InRange(-1,size.y+1),state+" vertical bounds: "+control.name);
                        }
                    }

                }
                finally
                {
                    camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
                    RenderTexture.active=oldActive;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(pixels);
                }
            }
        }
    }
}
