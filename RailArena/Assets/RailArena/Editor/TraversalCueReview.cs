using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Editor
{
    public static class TraversalCueReview
    {
        private const string Output="../Validation/TraversalCues";
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Traversal art authoring requires edit mode.");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            Directory.CreateDirectory(Output);
            var world=Object.FindAnyObjectByType<ArenaWorld>();
            string before=FoundryArtBuilder.GameplaySignature(world);
            Capture(world,"Before");
            FoundryArtBuilder.Apply();
            string after=FoundryArtBuilder.GameplaySignature(world);
            if(before!=after)throw new InvalidOperationException("Traversal art altered gameplay geometry.");
            Capture(world,"After");
            File.WriteAllText(Output+"/Geometry.json",JsonUtility.ToJson(new Receipt {before=before,after=after,unchanged=true,pads=world.Pads.Length,
                sharedLandingZones=world.Pads.Select(pad=>pad.Destination).Distinct().Count()},true));
        }
        [Serializable] private sealed class Receipt {public string before,after;public bool unchanged;public int pads,sharedLandingZones;}
        private static void Capture(ArenaWorld world,string state)
        {
            var root=new GameObject("Traversal review camera");
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=55;camera.clearFlags=CameraClearFlags.Skybox;
                var pad=world.Pads[0];var forward=pad.Destination-pad.transform.position;forward.y=0;forward.Normalize();
                camera.transform.position=pad.transform.position+Vector3.up*5;
                camera.transform.rotation=Quaternion.LookRotation(Vector3.down,forward);
                camera.orthographic=true;camera.orthographicSize=1.8f;
                if(state=="After")Write(camera,state+"-Pad");
                camera.orthographic=false;
                camera.transform.position=pad.Destination+new Vector3(1.8f,2.2f,-2.8f);
                camera.transform.LookAt(pad.Destination);Write(camera,state+"-Landing");
            }
            finally {Object.DestroyImmediate(root);}
        }
        private static void Write(Camera camera,string name)
        {
            var target=new RenderTexture(1440,1080,24);var image=new Texture2D(1440,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                target.Create();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1440,1080),0,0);image.Apply();File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
