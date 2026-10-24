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
    public sealed class AudioMixHudTests
    {
        [UnityTest]
        public IEnumerator SoundLevelsApplyToLocalAndRemoteCuesAndPreviewDoesNotFire()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)
                yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            string[] keys={SoundPreferences.MasterKey,SoundPreferences.EffectsKey};
            bool[] existed=keys.Select(PlayerPrefs.HasKey).ToArray();float[] old=keys.Select(key=>PlayerPrefs.GetFloat(key)).ToArray();
            float oldListener=AudioListener.volume;
            try
            {
                PlayerPrefs.SetFloat(SoundPreferences.MasterKey,.65f);PlayerPrefs.DeleteKey(SoundPreferences.EffectsKey);
                session.Sound.Load();Assert.That(session.Sound.Effects,Is.EqualTo(1));Assert.That(session.Sound.Master,Is.EqualTo(.65f));
                session.Sound.ApplyMaster();session.Feedback.ApplySound();session.MainMenu();yield return Frames(2);
                Button("SETTINGS").onClick.Invoke();yield return Frames(2);
                Button("AUDIO MIX / SOUND TEST").onClick.Invoke();yield return Frames(2);
                var sliders=Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsSortMode.None);
                var master=sliders.Single(s=>s.name=="Master volume");var effects=sliders.Single(s=>s.name=="Effects volume");
                master.value=.7f;effects.value=.25f;
                Assert.That(AudioListener.volume,Is.EqualTo(.7f).Within(.001));
                Assert.That(session.GetComponent<AudioSource>().volume,Is.EqualTo(.25f));
                Assert.That(session.GetComponentsInChildren<AudioSource>(true).Where(s=>s.spatialBlend==1).All(s=>Mathf.Abs(s.volume-.125f)<.001f),Is.True);
                session.Feedback.Shot(Vector3.zero,Vector3.forward*5,Vector3.zero,false,true);
                Assert.That(session.GetComponentsInChildren<AudioSource>(true).Where(s=>s.spatialBlend==1).All(s=>Mathf.Abs(s.volume-.125f)<.001f),Is.True);
                session.Feedback.ResetFeedback();
                int shots=session.ShotsFired,hits=session.Feedback.ConfirmedHits,damage=session.Feedback.DamageCues;
                Button("TEST RIFLE SOUND").onClick.Invoke();
                Assert.That(session.Running,Is.False);Assert.That(session.ShotsFired,Is.EqualTo(shots));
                Assert.That(session.Feedback.ConfirmedHits,Is.EqualTo(hits));Assert.That(session.Feedback.DamageCues,Is.EqualTo(damage));
                Assert.That(session.GetComponentsInChildren<LineRenderer>(),Is.Empty);
                var loaded=new SoundPreferences();loaded.Load();Assert.That(loaded.Master,Is.EqualTo(.7f).Within(.001));Assert.That(loaded.Effects,Is.EqualTo(.25f));
                foreach(var size in Sizes)yield return Capture(session.View,size,"Levels");
                effects.value=0;Assert.That(Button("TEST RIFLE SOUND").interactable,Is.False);
                Assert.That(session.GetComponentsInChildren<AudioSource>(true).All(s=>s.volume==0),Is.True);
                foreach(var size in Sizes)yield return Capture(session.View,size,"Muted");
                effects.value=.4f;master.value=0;Assert.That(Button("TEST RIFLE SOUND").interactable,Is.False);Assert.That(AudioListener.volume,Is.Zero);
                master.value=.7f;Assert.That(Button("TEST RIFLE SOUND").interactable,Is.True);
                Button("BACK TO SETTINGS").onClick.Invoke();yield return Frames(2);Button("AUDIO MIX / SOUND TEST").onClick.Invoke();yield return Frames(2);
                Assert.That(GameObject.Find("Effects volume").GetComponent<UnityEngine.UI.Slider>().value,Is.EqualTo(.4f).Within(.001));
                loaded.Master=float.NaN;loaded.Effects=float.PositiveInfinity;Assert.That(loaded.Master,Is.EqualTo(.65f));Assert.That(loaded.Effects,Is.EqualTo(1));
                loaded.Master=-1;loaded.Effects=2;Assert.That(loaded.Master,Is.Zero);Assert.That(loaded.Effects,Is.EqualTo(1));
            }
            finally
            {
                for(int i=0;i<keys.Length;i++)if(existed[i])PlayerPrefs.SetFloat(keys[i],old[i]);else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();session.Sound.Load();session.Feedback.ApplySound();session.Feedback.ResetFeedback();AudioListener.volume=oldListener;session.MainMenu();
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
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/AudioMix"));
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
