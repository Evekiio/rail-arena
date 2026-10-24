using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Editor
{
    public static class CloudSkyReview
    {
        private const string Output="../Validation/CloudSky";
        public static void Before() => Views("Before");
        public static void After() => Views("After");
        private static void Views(string prefix)
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");Directory.CreateDirectory(Output);
            Capture(prefix+"-Overview",new Vector3(46,35,-52),new Vector3(0,2,0),65);
            Capture(prefix+"-Edge",new Vector3(-24,1.58f,-18),new Vector3(-180,-2,-100),75);
            Capture(prefix+"-Horizon",new Vector3(0,8,0),new Vector3(160,4,80),75);
            Capture(prefix+"-UpperSky",new Vector3(0,8,0),new Vector3(80,85,80),90);
        }
        private static void Capture(string name, Vector3 position, Vector3 target, float fov)
        {
            var camera = Camera.main;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            float oldFov = camera.fieldOfView;
            var texture = new RenderTexture(1920,1080,24);
            var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var oldActive = RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
                camera.fieldOfView = fov;
                texture.Create();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation); camera.fieldOfView = oldFov;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(pixels); texture.Release(); Object.DestroyImmediate(texture);
            }
        }
    }
}
