using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class MenuButtonTests
    {
        private static UnityEngine.UI.Button Starting(string prefix)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith(prefix));
        private static void InstantTransitions()
        {
            foreach(var button in Object.FindObjectsByType<MenuButton>(FindObjectsSortMode.None))
            {var colors=button.colors;colors.fadeDuration=0;button.colors=colors;}
        }
        [UnityTest]
        public IEnumerator NativeButtonStatesAreDistinctAndDisabledActionsStayBlocked()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);var session=Object.FindAnyObjectByType<ArenaSession>();
            string[] keys={FramePacingPreferences.SyncKey,FramePacingPreferences.LimitKey};
            bool[] existed=keys.Select(PlayerPrefs.HasKey).ToArray();int[] old=keys.Select(key=>PlayerPrefs.GetInt(key)).ToArray();
            int oldSync=QualitySettings.vSyncCount,oldLimit=Application.targetFrameRate;
            try
            {
                session.MainMenu();yield return Frames(2);
                for(int i=0;i<4 && !Starting("BOTS  ").GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("BOTS  0");i++)
                {Starting("BOTS  ").onClick.Invoke();yield return Frames(2);}
                InstantTransitions();EventSystem.current.SetSelectedGameObject(null);
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                var blocked=Starting("BOT MATCH // SELECT");var help=Button("HOW TO PLAY");
                Assert.That(blocked,Is.TypeOf<MenuButton>());Assert.That(blocked.interactable,Is.False);
                Assert.That(blocked.targetGraphic.canvasRenderer.GetColor(),Is.EqualTo(blocked.colors.disabledColor));
                Assert.That(blocked.GetComponentInChildren<UnityEngine.UI.Text>().color.r,Is.LessThan(help.GetComponentInChildren<UnityEngine.UI.Text>().color.r));
                blocked.OnPointerClick(pointer);Assert.That(session.Running,Is.False);
                foreach(var size in Sizes)yield return Capture(session.View,size,"Disabled");
                help.OnPointerEnter(pointer);
                Assert.That(help.targetGraphic.canvasRenderer.GetColor(),Is.EqualTo(help.colors.highlightedColor));
                Assert.That(help.colors.highlightedColor,Is.Not.EqualTo(help.colors.normalColor));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Hover");
                EventSystem.current.SetSelectedGameObject(help.gameObject);
                Assert.That(help.targetGraphic.canvasRenderer.GetColor(),Is.EqualTo(help.colors.selectedColor));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Selected");
                help.OnPointerDown(pointer);Assert.That(help.targetGraphic.canvasRenderer.GetColor(),Is.EqualTo(help.colors.pressedColor));
                help.OnPointerUp(pointer);help.OnPointerExit(pointer);EventSystem.current.SetSelectedGameObject(null);
                Button("SETTINGS").OnPointerClick(pointer);yield return Frames(2);
                session.FramePacing.VSync=false;session.FramePacing.Apply();
                Button("FRAME PACING").OnPointerClick(pointer);yield return Frames(2);InstantTransitions();
                Button("VSYNC // OFF").OnPointerClick(pointer);
                var limit=Starting("FPS LIMIT //");Assert.That(limit.interactable,Is.False);
                Assert.That(limit.targetGraphic.canvasRenderer.GetColor(),Is.EqualTo(limit.colors.disabledColor));
                Assert.That(limit.GetComponentInChildren<UnityEngine.UI.Text>().color.r,Is.LessThan(.6f));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Inactive");
                Button("VSYNC // ON").OnPointerClick(pointer);Assert.That(limit.interactable,Is.True);
                Assert.That(limit.GetComponentInChildren<UnityEngine.UI.Text>().color.r,Is.GreaterThan(.8f));
                Button("BACK TO SETTINGS").OnPointerClick(pointer);yield return Frames(2);Button("BACK").OnPointerClick(pointer);yield return Frames(2);
                Button("HOW TO PLAY").OnPointerClick(pointer);yield return Frames(2);
                Assert.That(GameObject.Find("Title").GetComponent<UnityEngine.UI.Text>().text,Is.EqualTo("HOW TO PLAY"));
            }
            finally
            {
                for(int i=0;i<keys.Length;i++)if(existed[i])PlayerPrefs.SetInt(keys[i],old[i]);else PlayerPrefs.DeleteKey(keys[i]);PlayerPrefs.Save();
                session.FramePacing.Load();QualitySettings.vSyncCount=oldSync;Application.targetFrameRate=oldLimit;session.MainMenu();
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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/ButtonStates"));
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
