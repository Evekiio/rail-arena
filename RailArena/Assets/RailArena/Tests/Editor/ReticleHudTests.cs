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
    public sealed class ReticleHudTests
    {
        [UnityTest]
        public IEnumerator SettingsPreviewAndLiveReticleShareSavedAppearance()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            bool hadSize=PlayerPrefs.HasKey(ReticlePreferences.SizeKey), hadColor=PlayerPrefs.HasKey(ReticlePreferences.ColorKey);
            float oldSize=PlayerPrefs.GetFloat(ReticlePreferences.SizeKey);
            int oldColor=PlayerPrefs.GetInt(ReticlePreferences.ColorKey);
            try
            {
                session.Reticle.Reset(); session.MainMenu(); yield return Frames(2);
                Button("SETTINGS").onClick.Invoke(); yield return Frames(2);
                Button("RETICLE SETTINGS").onClick.Invoke(); yield return Frames(2);
                var slider=Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsSortMode.None).Single();
                slider.value=64;
                for(int i=0;i<3;i++)Button("COLOR // "+session.Reticle.ColorName).onClick.Invoke();
                yield return Frames(2);
                var saved=new ReticlePreferences(); saved.Load();
                Assert.That(saved.Size,Is.EqualTo(64)); Assert.That(saved.ColorName,Is.EqualTo("MAGENTA"));
                foreach(var name in new[]{"ReticlePreviewDark","ReticlePreviewLight"})
                    CheckReticle(GameObject.Find(name).GetComponent<RectTransform>(),saved);
                var canvas=slider.GetComponentInParent<Canvas>();
                foreach(var size in Sizes)yield return Capture(canvas,session.View,size,"Settings");
                slider.value=18; yield return Frames(2);
                CheckReticle(GameObject.Find("ReticlePreviewDark").GetComponent<RectTransform>(),session.Reticle);
                Button("RESET RETICLE").onClick.Invoke(); yield return Frames(2);
                Assert.That(slider.value,Is.EqualTo(36)); Assert.That(session.Reticle.ColorName,Is.EqualTo("CYAN"));
                slider.value=48; Button("COLOR // CYAN").onClick.Invoke();
                Button("BACK TO SETTINGS").onClick.Invoke(); yield return Frames(2);
                Assert.That(Button("RETICLE SETTINGS"),Is.Not.Null);
                Button("BACK").onClick.Invoke(); yield return Frames(2);
                session.StartSession(true,0,1,TargetScenario.Stationary);
                while(!session.Match.IsActive)session.Match.Advance();
                session.Human.NextShotTick=session.LocalSimulationTick+Simulation.CombatRules.ShotIntervalTicks;
                yield return Frames(2);
                var live=GameObject.Find("Crosshair").GetComponent<RectTransform>();
                CheckReticle(live,session.Reticle);
                var center=RectTransformUtility.WorldToScreenPoint(null,live.position);
                Assert.That(center.x,Is.EqualTo(canvas.pixelRect.center.x).Within(1));
                Assert.That(center.y,Is.EqualTo(canvas.pixelRect.center.y).Within(1));
                Assert.That(GameObject.Find("RailValue").GetComponent<UnityEngine.UI.Text>().text,Does.StartWith("CYCLE"));
                foreach(var size in Sizes)yield return Capture(canvas,session.View,size,"Live");
            }
            finally
            {
                if(hadSize)PlayerPrefs.SetFloat(ReticlePreferences.SizeKey,oldSize); else PlayerPrefs.DeleteKey(ReticlePreferences.SizeKey);
                if(hadColor)PlayerPrefs.SetInt(ReticlePreferences.ColorKey,oldColor); else PlayerPrefs.DeleteKey(ReticlePreferences.ColorKey);
                PlayerPrefs.Save(); session.Reticle.Load(); session.MainMenu();
            }
            yield return new ExitPlayMode();
        }

        private static void CheckReticle(RectTransform reticle,ReticlePreferences preferences)
        {
            Assert.That(reticle.sizeDelta,Is.EqualTo(Vector2.one*preferences.Size));
            Assert.That(reticle.GetComponentsInChildren<UnityEngine.UI.Image>().All(image=>!image.raycastTarget),Is.True);
            for(int i=0;i<5;i++)Assert.That(reticle.Find("Mark"+i).GetComponent<UnityEngine.UI.Image>().color,Is.EqualTo(preferences.Color));
            Assert.That(reticle.Find("Outline0"),Is.Not.Null);
        }

        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};
        private static UnityEngine.UI.Button Button(string label)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text==label);

        private static IEnumerator Capture(Canvas canvas,Camera camera,Vector2Int size,string name)
        {
            var target=new RenderTexture(size.x,size.y,24);
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous=RenderTexture.active; var previousTarget=camera.targetTexture;
            var mode=canvas.renderMode; var previousCamera=canvas.worldCamera; float distance=canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=.2f;
                if(name=="Live")
                {
                    var session=Object.FindAnyObjectByType<ArenaSession>();
                    session.Human.NextShotTick=session.LocalSimulationTick+Simulation.CombatRules.ShotIntervalTicks;
                }
                yield return Frames(2); Canvas.ForceUpdateCanvases();
                HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/Reticle"));
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
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
