using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PickupHudTests
    {
        [UnityTest]
        public IEnumerator PickupNoticeUsesActualGainWithMutedAudioAndKeepsDangerReadable()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);var session=Object.FindAnyObjectByType<ArenaSession>();
            float oldEffects=session.Sound.Effects;bool oldFlash=session.Comfort.ReducedFlash;
            try
            {
                session.StartSession(true,0,1,TargetScenario.Stationary);session.enabled=false;
                while(!session.Match.IsActive)session.Match.Advance();
                session.Sound.Effects=0;session.Feedback.ApplySound();session.Comfort.ReducedFlash=true;
                foreach(var site in session.World.Pickups)site.AvailableTick=int.MaxValue;
                var pickup=session.World.Pickups[0];
                var advance=typeof(ArenaSession).GetMethod("AdvanceAuthority",Private);
                foreach(var item in new[]{(health:50,after:75,amount:25,name:"Gain25"),(health:98,after:100,amount:2,name:"Capped2"),(health:5,after:30,amount:25,name:"LowHealth")})
                {
                    session.Human.Health=item.health;session.Human.Place(pickup.transform.position-Vector3.up);pickup.AvailableTick=0;
                    advance.Invoke(session,null);yield return Frames(2);
                    Assert.That(session.Human.Health,Is.EqualTo(item.after));Assert.That(session.Feedback.LastPickupAmount,Is.EqualTo(item.amount));
                    Assert.That(GameObject.Find("HealthGain").GetComponent<UnityEngine.UI.Text>().text,Is.EqualTo("+"+item.amount));
                    Assert.That(GameObject.Find("HealthValue").GetComponent<UnityEngine.UI.Text>().text,Is.EqualTo(item.after.ToString("000")));
                    Assert.That(session.Feedback.Sound.Effects,Is.Zero);
                    if(item.name=="LowHealth")
                    {
                        var color=GameObject.Find("HealthValue").GetComponent<UnityEngine.UI.Text>().color;
                        Assert.That(color.r,Is.GreaterThan(color.g),"Positive cue must not replace low-health red");
                    }
                    foreach(var size in Sizes)yield return Capture(session.View,size,item.name);
                }
                int cues=session.Feedback.PickupCues;session.Feedback.PickedUp(0);session.Feedback.PickedUp(-1);Assert.That(session.Feedback.PickupCues,Is.EqualTo(cues));
                var notice=GameObject.Find("HealthGain");
                typeof(RailFeedback).GetField("pickupUntil",Private).SetValue(session.Feedback,Time.realtimeSinceStartupAsDouble-1);
                yield return Frames(2);Assert.That(notice.activeSelf,Is.False);Assert.That(session.Human.Health,Is.EqualTo(30));
                session.Feedback.PickedUp(2);yield return Frames(2);Assert.That(notice.activeSelf,Is.True);
                session.Feedback.Damaged();yield return Frames(2);Assert.That(notice.activeSelf,Is.False);
                session.Feedback.PickedUp(2);session.Human.Health=0;session.Human.SetAlive(false);yield return Frames(2);Assert.That(notice.activeSelf,Is.False);
                session.Feedback.ResetFeedback();Assert.That(session.Feedback.LastPickupAmount,Is.Zero);Assert.That(session.Feedback.PickupStrength,Is.Zero);
                Assert.That(session.Match.Statistics[session.Human.Id].Shots,Is.Zero);
            }
            finally
            {
                session.Sound.Effects=oldEffects;session.Feedback.ApplySound();session.Comfort.ReducedFlash=oldFlash;
                session.enabled=true;session.MainMenu();
            }
            yield return new ExitPlayMode();
        }
        private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};
        private static UnityEngine.UI.Button Button(string label)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text==label);

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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/PickupHud"));
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
