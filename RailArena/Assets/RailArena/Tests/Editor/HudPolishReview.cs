using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class HudPolishReview
    {
        [UnityTest]
        public IEnumerator CombatStatesRemainAccurateReadableAndNonInteractive()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null) yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true,0,1,TargetScenario.Stationary);
            session.enabled=false;
            while(!session.Match.IsActive)session.Match.Advance();
            foreach(var site in session.World.Pickups)site.AvailableTick=int.MaxValue;
            bool flash=session.Comfort.ReducedFlash;
            try
            {
                foreach(var state in new[]{"Healthy","Damage","Critical","Healing","LowAmmo","Empty","Reload","Elimination","ReducedFlash","Death"})
                {
                    session.Feedback.ResetFeedback();
                    session.Human.Health=100;session.Human.SetAlive(true);
                    session.Human.Magazine.Rounds=30;session.Human.Magazine.ReloadEndTick=0;
                    if(state=="Damage" || state=="Critical" || state=="ReducedFlash") {session.Human.Health=state=="Damage"?65:25;session.Feedback.Damaged();}
                    if(state=="Healing") {session.Human.Health=75;session.Feedback.PickedUp(25);}
                    if(state=="LowAmmo")session.Human.Magazine.Rounds=3;
                    if(state=="Empty")session.Human.Magazine.Rounds=0;
                    if(state=="Reload") {session.Human.Magazine.ReloadEndTick=session.LocalSimulationTick+60;}
                    if(state=="Elimination")session.Feedback.ConfirmHit(true);
                    if(state=="ReducedFlash")session.Comfort.ReducedFlash=true;
                    if(state=="Death") {session.Human.Health=0;session.Human.SetAlive(false);}
                    yield return Frames(2);
                    var root=GameObject.Find("CombatHud");
                    var texts=root.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    Assert.That(texts.Single(t=>t.name=="HealthValue").text,Is.EqualTo(session.Human.Health.ToString("000")));
                    Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.raycastTarget),Is.True);
                    if(state=="Critical")Assert.That(texts.Single(t=>t.name=="HealthState").text,Does.StartWith("CRITICAL"));
                    if(state=="Reload")Assert.That(texts.Single(t=>t.name=="WeaponPrompt").text,Is.EqualTo("RELOADING"));
                    if(state=="Empty")Assert.That(texts.Single(t=>t.name=="WeaponPrompt").text,Does.StartWith("EMPTY"));
                    foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})yield return Capture(session.View,size,state);
                }
            }
            finally {session.Comfort.ReducedFlash=flash;session.enabled=true;session.MainMenu();}
            yield return new ExitPlayMode();
        }
        private static IEnumerator Capture(Camera camera,Vector2Int size,string name)
        {
            var canvas=GameObject.Find("Arena interface").GetComponent<Canvas>();
            var target=new RenderTexture(size.x,size.y,24); var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous=RenderTexture.active; var previousTarget=camera.targetTexture;
            var mode=canvas.renderMode; var previousCamera=canvas.worldCamera; float distance=canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=.2f;
                yield return Frames(2); Canvas.ForceUpdateCanvases();
                HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/HudPolish-29"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=mode; canvas.worldCamera=previousCamera; canvas.planeDistance=distance;
                camera.targetTexture=previousTarget; RenderTexture.active=previous;
                Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
            }
        }

        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count; float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Player frames stopped advancing");
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
        }
        [UnityTearDown]
        public IEnumerator RestoreEditorMode() { if(Application.isPlaying)yield return new ExitPlayMode(); }
    }
}

