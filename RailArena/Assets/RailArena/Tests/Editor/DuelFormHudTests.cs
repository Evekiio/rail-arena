using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class DuelFormHudTests
    {
        [UnityTest]
        public IEnumerator InvalidInputAndBusyPortStayOnFormAndRetryWithoutLosingDraft()
        {
            yield return new EnterPlayMode();
            if(Object.FindAnyObjectByType<ArenaSession>()==null)yield return SceneManager.LoadSceneAsync("Foundry",LoadSceneMode.Additive);
            yield return Frames(2);
            var session=Object.FindAnyObjectByType<ArenaSession>();
            UdpClient occupied=null;
            try
            {
                session.ClearConnectionError();session.MainMenu();yield return Frames(2);
                Button("ONLINE DUEL // DIRECT IP").onClick.Invoke();yield return Frames(2);
                Address.text="192.0.2.24";Port.text="0";
                Button("HOST DUEL").onClick.Invoke();yield return Frames(2);
                Assert.That(Label("Connection error"),Does.Contain("1 to 65535"));
                Assert.That(Port.text,Is.EqualTo("0"));Assert.That(Address.text,Is.EqualTo("192.0.2.24"));
                Assert.That(session.Running,Is.False);Assert.That(session.IsOnline,Is.False);
                Assert.That(GameObject.Find("Controls"),Is.Null,"Connection errors must not overlap main-menu controls.");
                Port.text="7777";
                Assert.That(session.ConnectionError,Is.Empty);Assert.That(GameObject.Find("Connection error panel"),Is.Null);
                foreach(var size in Sizes)yield return Capture(session.View,size,"Form");
                Address.text="::1";Button("JOIN HOST").onClick.Invoke();yield return Frames(2);
                Assert.That(Label("Connection error"),Does.Contain("IPv4"));Assert.That(Address.text,Is.EqualTo("::1"));
                foreach(var size in Sizes)yield return Capture(session.View,size,"InvalidAddress");
                Button("BACK").onClick.Invoke();yield return Frames(2);
                Assert.That(session.ConnectionError,Is.Empty);Assert.That(Button("HOW TO PLAY"),Is.Not.Null);
                Button("ONLINE DUEL // DIRECT IP").onClick.Invoke();yield return Frames(2);
                Assert.That(Address.text,Is.EqualTo("::1"));Assert.That(Port.text,Is.EqualTo("7777"));
                Address.text="192.0.2.24";
                occupied=new UdpClient(new IPEndPoint(IPAddress.Any,0));
                string port=((IPEndPoint)occupied.Client.LocalEndPoint).Port.ToString();Port.text=port;
                Button("HOST DUEL").onClick.Invoke();yield return Frames(2);
                Assert.That(session.Running,Is.False);Assert.That(session.IsOnline,Is.False);
                Assert.That(Label("Connection error"),Does.Contain("already in use"));
                Assert.That(Label("Connection error"),Does.Contain("Choose a different port"));Assert.That(Port.text,Is.EqualTo(port));
                foreach(var size in Sizes)yield return Capture(session.View,size,"PortBusy");
                occupied.Dispose();occupied=null;
                Button("HOST DUEL").onClick.Invoke();yield return Frames(2);
                Assert.That(session.IsOnline,Is.True);Assert.That(session.OnlineWaiting,Is.True);Assert.That(session.ConnectionError,Is.Empty);
                Assert.That(Label("Connection status"),Does.Contain("HOSTING"));
                Button("DISCONNECT").onClick.Invoke();yield return Frames(2);
                Assert.That(session.Running,Is.False);Assert.That(session.IsOnline,Is.False);Assert.That(Button("ONLINE DUEL // DIRECT IP"),Is.Not.Null);
            }
            finally {occupied?.Dispose();session.ClearConnectionError();session.MainMenu();}
            yield return new ExitPlayMode();
        }

        private static UnityEngine.UI.InputField Address=>GameObject.Find("HOST IPv4 ADDRESS entry").GetComponent<UnityEngine.UI.InputField>();
        private static UnityEngine.UI.InputField Port=>GameObject.Find("UDP PORT entry").GetComponent<UnityEngine.UI.InputField>();
        private static string Label(string name)=>GameObject.Find(name).GetComponent<UnityEngine.UI.Text>().text;
        private static UnityEngine.UI.Button Button(string text)=>Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
            .Single(button=>button.GetComponentInChildren<UnityEngine.UI.Text>().text==text);
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(1280,720)};
        private static IEnumerator Capture(Camera camera,Vector2Int size,string name)
        {
            var canvas=GameObject.Find("Arena interface").GetComponent<Canvas>();
            var target=new RenderTexture(size.x,size.y,24);var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            try
            {
                target.Create();camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.2f;
                yield return Frames(2);Canvas.ForceUpdateCanvases();HeadlessUiReview.Draw(canvas,camera,target);
                foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(text.cachedTextGenerator.characterCountVisible,Is.EqualTo(text.text.Length),text.name);
                var corners=new Vector3[4];
                foreach(var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    graphic.rectTransform.GetWorldCorners(corners);
                    foreach(var corner in corners)
                    {
                        var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                        Assert.That(point.x,Is.InRange(-1,size.x+1));Assert.That(point.y,Is.InRange(-1,size.y+1));
                    }
                }
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/DuelForm"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;camera.targetTexture=previousTarget;RenderTexture.active=previous;
                Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);
            }
        }
        private static IEnumerator Frames(int count)
        {
            int target=Time.frameCount+count;float deadline=Time.realtimeSinceStartup+5;
            while(Time.frameCount<target)
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
        }
        [UnityTearDown]
        public IEnumerator RestoreEditorMode() {if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
