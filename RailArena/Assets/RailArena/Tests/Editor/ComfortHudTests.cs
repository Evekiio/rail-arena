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
    public sealed class ComfortHudTests
    {
        [UnityTest]
        public IEnumerator ControlsPersistAndDisableViewMotionWithoutChangingAimOrCombat()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            bool hadFlash=PlayerPrefs.HasKey(ComfortPreferences.FlashKey),hadMotion=PlayerPrefs.HasKey(ComfortPreferences.MotionKey);
            int oldFlash=PlayerPrefs.GetInt(ComfortPreferences.FlashKey),oldMotion=PlayerPrefs.GetInt(ComfortPreferences.MotionKey);
            try
            {
                session.Comfort.ReducedFlash=false; session.Comfort.WeaponMotion=true;
                session.MainMenu(); yield return Frames(2);
                Button("SETTINGS").onClick.Invoke(); yield return Frames(2);
                Button("VISUAL COMFORT").onClick.Invoke(); yield return Frames(2);
                Button("REDUCED FLASH // OFF").onClick.Invoke(); Button("WEAPON MOTION // ON").onClick.Invoke();
                var saved=new ComfortPreferences(); saved.Load();
                Assert.That(saved.ReducedFlash,Is.True); Assert.That(saved.WeaponMotion,Is.False);
                Assert.That(session.Feedback.Preferences,Is.SameAs(session.Comfort));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Settings");
                Button("BACK TO SETTINGS").onClick.Invoke(); yield return Frames(2);
                Button("BACK").onClick.Invoke(); yield return Frames(2);
                session.StartSession(true,0,1,TargetScenario.Stationary);
                while(!session.Match.IsActive)session.Match.Advance();
                yield return Frames(2);
                var weapon=(Transform)typeof(ArenaSession).GetField("weapon",Private).GetValue(session);
                var recoil=typeof(ArenaSession).GetField("recoil",Private);
                var hands=weapon.GetComponentInChildren<FirstPersonArms>().Hands;
                var handRecoil=typeof(HandArticulation).GetField("recoil",Private);
                Quaternion aim=session.View.transform.rotation;
                session.Human.Velocity=new Vector3(10,0,0); ((WeaponRecoil)typeof(ArenaSession).GetField("weaponRecoil",Private).GetValue(session)).Fire();
                yield return Frames(2);
                Assert.That(weapon.localPosition,Is.EqualTo(FirstPersonWeaponMotion.RestPosition));
                Assert.That(Quaternion.Angle(weapon.localRotation,Quaternion.identity),Is.LessThan(.001f));
                Assert.That((float)handRecoil.GetValue(hands),Is.Zero);
                Assert.That(Quaternion.Angle(aim,session.View.transform.rotation),Is.LessThan(.001f));
                Assert.That(session.Human.Health,Is.EqualTo(100)); Assert.That(session.Match.Statistics[0].Shots,Is.Zero);
                session.Feedback.ConfirmHit(false);
                foreach(var size in Sizes)yield return Capture(session.View,size,"Live");
                session.Comfort.WeaponMotion=true; ((WeaponRecoil)typeof(ArenaSession).GetField("weaponRecoil",Private).GetValue(session)).Fire(); yield return Frames(2);
                Assert.That(Vector3.Distance(weapon.localPosition,FirstPersonWeaponMotion.RestPosition),Is.GreaterThan(.001f));
                Assert.That((float)handRecoil.GetValue(hands),Is.GreaterThan(0));
                Assert.That(Quaternion.Angle(aim,session.View.transform.rotation),Is.LessThan(.001f));
            }
            finally
            {
                if(hadFlash)PlayerPrefs.SetInt(ComfortPreferences.FlashKey,oldFlash); else PlayerPrefs.DeleteKey(ComfortPreferences.FlashKey);
                if(hadMotion)PlayerPrefs.SetInt(ComfortPreferences.MotionKey,oldMotion); else PlayerPrefs.DeleteKey(ComfortPreferences.MotionKey);
                PlayerPrefs.Save(); session.Comfort.Load(); session.MainMenu();
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
                if(name=="Live")Object.FindAnyObjectByType<ArenaSession>().Feedback.Damaged();
                yield return Frames(2); Canvas.ForceUpdateCanvases();
                HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                if(name=="Live")
                    for(int i=0;i<4;i++)Assert.That(GameObject.Find("DamageEdge"+i).GetComponent<UnityEngine.UI.Image>().color.a,Is.InRange(0,.175f));
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/VisualComfort"));
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
