using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CharacterPickerTests
    {
        [Serializable] private sealed class Portrait {public int character;public string prefab,dependencyHash,file;}
        [Serializable] private sealed class Manifest {public Portrait[] portraits;}
        [Test]
        public void PortraitsMatchInstalledPrefabDependencies()
        {
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/CharacterPicker/Portraits.json"));
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(path));Assert.That(manifest.portraits.Length,Is.EqualTo(4));
            foreach(var item in manifest.portraits)
            {
                Assert.That(AssetDatabase.GetAssetDependencyHash(item.prefab).ToString(),Is.EqualTo(item.dependencyHash));
                var texture=Resources.Load<Texture2D>("UI/ContenderPortraits/Character-"+item.character);
                Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(512));Assert.That(texture.height,Is.EqualTo(640));
            }
        }
        [UnityTest]
        public IEnumerator PickerSavesExplicitChoiceAndDefersLiveAppearanceUntilNextSession()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);var session=Object.FindAnyObjectByType<ArenaSession>();
            bool existed=PlayerPrefs.HasKey("offlineCharacter");int oldPreference=PlayerPrefs.GetInt("offlineCharacter");int oldSelection=session.SelectedOfflineCharacter;
            try
            {
                session.MainMenu();yield return Frames(2);
                Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("CHARACTER //")).onClick.Invoke();
                yield return Frames(2);
                Assert.That(Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None).Count(image=>image.name.StartsWith("Portrait ")),Is.EqualTo(4));
                for(int i=0;i<4;i++)
                {
                    GameObject.Find("Character card "+i).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject,Is.SameAs(GameObject.Find("Character card "+i)));
                    Assert.That(session.SelectedOfflineCharacter,Is.EqualTo(i));Assert.That(PlayerPrefs.GetInt("offlineCharacter"),Is.EqualTo(i));
                    Assert.That(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Count(t=>t.text.EndsWith("\nSELECTED")),Is.EqualTo(1));
                    Assert.That(GameObject.Find("Portrait "+i).GetComponent<UnityEngine.UI.RawImage>().texture,Is.Not.Null);
                }
                foreach(var size in Sizes)yield return Capture(session.View,size,"Picker");
                Button("BACK TO SETUP").onClick.Invoke();yield return Frames(2);
                session.StartSession(true,0,1,TargetScenario.Stationary);yield return Frames(2);Assert.That(session.Human.CharacterId,Is.EqualTo(3));
                session.SetPaused(true);yield return Frames(2);
                Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("CHARACTER //")).onClick.Invoke();yield return Frames(2);
                GameObject.Find("Character card 0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Assert.That(session.Human.CharacterId,Is.EqualTo(3));Assert.That(session.SelectedOfflineCharacter,Is.Zero);
                Assert.That(GameObject.Find("Character scope").GetComponent<UnityEngine.UI.Text>().text,Does.StartWith("NEXT SESSION"));
                foreach(var size in Sizes)yield return Capture(session.View,size,"NextSession");
                Button("BACK TO SETUP").onClick.Invoke();yield return Frames(2);
                session.StartSession(true,0,1,TargetScenario.Stationary);yield return Frames(2);Assert.That(session.Human.CharacterId,Is.Zero);
            }
            finally
            {
                session.MainMenu();session.SelectOfflineCharacter(oldSelection);
                if(existed)PlayerPrefs.SetInt("offlineCharacter",oldPreference);else PlayerPrefs.DeleteKey("offlineCharacter");PlayerPrefs.Save();
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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/CharacterPicker"));
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
