using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class ComfortFeedbackTests
    {
        [Test]
        public void ReducedShotsKeepBoundedCoreAndImpactWithoutMuzzleOrHalo()
        {
            var root=new GameObject("Comfort effect fixture");
            var cameraRoot=new GameObject("Comfort effect review camera");
            try
            {
                var feedback=root.AddComponent<RailFeedback>();
                typeof(RailFeedback).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(feedback,null);
                foreach(var part in root.GetComponentsInChildren<Transform>(true))part.gameObject.layer=30;
                var camera=cameraRoot.AddComponent<Camera>(); camera.enabled=false;
                camera.transform.position=new Vector3(0,1,-8); camera.transform.LookAt(new Vector3(0,0,1));
                camera.orthographic=true; camera.orthographicSize=2; camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.02f,.035f,.05f);
                feedback.Shot(new Vector3(-3,-.3f,0),new Vector3(3,.3f,2),Vector3.zero,false,true);
                float normalEnergy=Capture(camera,"Normal-Effect");
                feedback.ResetFeedback(); feedback.Preferences.ReducedFlash=true;
                feedback.Shot(new Vector3(-3,-.3f,0),new Vector3(3,.3f,2),Vector3.zero,false,true);
                var lines=root.GetComponentsInChildren<LineRenderer>();
                Assert.That(lines.Single(line=>line.name=="Core").enabled,Is.True);
                Assert.That(lines.Single(line=>line.name=="Core").startColor.maxColorComponent,Is.LessThanOrEqualTo(.75f));
                Assert.That(lines.Single(line=>line.name=="Halo").enabled,Is.False);
                Assert.That(lines.Single(line=>line.name=="Muzzle").enabled,Is.False);
                Assert.That(lines.Single(line=>line.name=="Impact").enabled,Is.True);
                float reducedEnergy=Capture(camera,"Reduced-Effect");
                Assert.That(normalEnergy,Is.GreaterThan(reducedEnergy*1.5f),"Reduced presentation should emit less visible light in the same controlled scene.");
                Assert.That(reducedEnergy,Is.GreaterThan(1),"The reduced shot must remain visible.");
                feedback.ConfirmHit(false); feedback.Damaged();
                Assert.That(feedback.ConfirmedHits,Is.EqualTo(1)); Assert.That(feedback.DamageCues,Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(root.GetComponentsInChildren<LineRenderer>(true).Length,Is.EqualTo(RailFeedback.Capacity*4));
                feedback.ResetFeedback(); feedback.Preferences.ReducedFlash=false;
                feedback.Shot(Vector3.zero,Vector3.forward*10,Vector3.zero,false,true);
                Assert.That(root.GetComponentsInChildren<LineRenderer>().Single(line=>line.name=="Halo").enabled,Is.True);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cameraRoot); }
        }

        private static float Capture(Camera camera,string name)
        {
            var target=new RenderTexture(1440,810,24); var image=new Texture2D(1440,810,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                target.Create(); RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
                RenderTexture.active=target; image.ReadPixels(new Rect(0,0,1440,810),0,0); image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/VisualComfort"));
                Directory.CreateDirectory(folder); File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
                return image.GetPixels().Sum(pixel=>Mathf.Max(0,pixel.maxColorComponent-.3f));
            }
            finally { RenderTexture.active=previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
