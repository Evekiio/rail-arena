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
    public sealed class FramePacingHudTests
    {
        [UnityTest]
        public IEnumerator FrameControlsPersistApplyAndExplainSyncPriority()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            bool hadSync=PlayerPrefs.HasKey(FramePacingPreferences.SyncKey), hadLimit=PlayerPrefs.HasKey(FramePacingPreferences.LimitKey);
            int oldSync=PlayerPrefs.GetInt(FramePacingPreferences.SyncKey), oldLimit=PlayerPrefs.GetInt(FramePacingPreferences.LimitKey);
            int runtimeSync=QualitySettings.vSyncCount, runtimeLimit=Application.targetFrameRate;
            try
            {
                PlayerPrefs.DeleteKey(FramePacingPreferences.SyncKey);PlayerPrefs.DeleteKey(FramePacingPreferences.LimitKey);
                var preferences=session.FramePacing;preferences.Load();preferences.Apply();
                Assert.That(preferences.VSync,Is.False);Assert.That(preferences.Limit,Is.EqualTo(144));
                Assert.That(QualitySettings.vSyncCount,Is.Zero);Assert.That(Application.targetFrameRate,Is.EqualTo(144));
                session.MainMenu();yield return Frames(2);Button("SETTINGS").onClick.Invoke();yield return Frames(2);
                foreach(var size in Sizes)yield return Capture(session.View,size,"Entry");
                Button("FRAME PACING").onClick.Invoke();yield return Frames(2);
                foreach(int expected in new[]{240,-1,60,120,144})
                {
                    Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                        .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("FPS LIMIT //")).onClick.Invoke();
                    Assert.That(preferences.Limit,Is.EqualTo(expected));Assert.That(Application.targetFrameRate,Is.EqualTo(expected));
                }
                foreach(var size in Sizes)yield return Capture(session.View,size,"Limit");
                Button("VSYNC // OFF").onClick.Invoke();
                Assert.That(QualitySettings.vSyncCount,Is.EqualTo(1));Assert.That(Application.targetFrameRate,Is.EqualTo(-1));
                Assert.That(Button("FPS LIMIT // 144 FPS").interactable,Is.False);
                var loaded=new FramePacingPreferences();loaded.Load();Assert.That(loaded.VSync,Is.True);Assert.That(loaded.Limit,Is.EqualTo(144));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Sync");
                Button("BACK TO SETTINGS").onClick.Invoke();yield return Frames(2);Button("FRAME PACING").onClick.Invoke();yield return Frames(2);
                Assert.That(Button("FPS LIMIT // 144 FPS").interactable,Is.False);
                Button("VSYNC // ON").onClick.Invoke();Assert.That(Application.targetFrameRate,Is.EqualTo(144));
                Assert.That(Button("FPS LIMIT // 144 FPS").interactable,Is.True);
                PlayerPrefs.SetInt(FramePacingPreferences.LimitKey,999);loaded.Load();Assert.That(loaded.Limit,Is.EqualTo(144));
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(1f/Simulation.CombatRules.TickRate));
            }
            finally
            {
                if(hadSync)PlayerPrefs.SetInt(FramePacingPreferences.SyncKey,oldSync);else PlayerPrefs.DeleteKey(FramePacingPreferences.SyncKey);
                if(hadLimit)PlayerPrefs.SetInt(FramePacingPreferences.LimitKey,oldLimit);else PlayerPrefs.DeleteKey(FramePacingPreferences.LimitKey);
                PlayerPrefs.Save();session.FramePacing.Load();QualitySettings.vSyncCount=runtimeSync;Application.targetFrameRate=runtimeLimit;session.MainMenu();
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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/FramePacing"));
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
