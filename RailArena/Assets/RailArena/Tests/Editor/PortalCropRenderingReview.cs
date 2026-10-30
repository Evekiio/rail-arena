using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PortalCropRenderingReview
    {
        [Serializable] sealed class Case
        {
            public string portal,pose,worldOccluder;public float meanAbsoluteRgb;public int samples,chartPixels,worldChartPixels,fullWidth,fullHeight,cropWidth,cropHeight;public Rect viewport;
        }
        [Serializable] sealed class Report { public string scope="All six basement-map portal directions; normal physical lens and wide ADS lens; full-frame reference versus cropped live destination. Layer-isolated unlit colour chart proves UV alignment separately from map occlusion and LOD/art changes. worldChartPixels/worldOccluder record the original scene's visibility independently. Offscreen rendering is not native input/audio acceptance.";public Case[] cases; }
        [UnityTest] public IEnumerator CroppedDestinationsPreservePhysicalLensAndScreenAlignment()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.enabled=false;
            var views=session.World.Portals.Select(gate=>gate.GetComponent<PortalView>()).ToArray();foreach(var view in views)view.enabled=false;
            var layers=session.World.Portals.ToDictionary(gate=>gate.Presentation.Surface.gameObject,gate=>gate.Presentation.Surface.gameObject.layer);
            var camera=session.View;camera.enabled=false;camera.cullingMask=(1<<0)|(1<<9)|(1<<29);
            var chart=GameObject.CreatePrimitive(PrimitiveType.Cube);chart.name="Owned destination alignment chart";chart.layer=29;Object.DestroyImmediate(chart.GetComponent<Collider>());chart.transform.localScale=new Vector3(6,3,.04f);
            var pattern=new Texture2D(16,16,TextureFormat.RGB24,false);pattern.filterMode=FilterMode.Point;
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)pattern.SetPixel(x,y,x<8?(y<8?Color.red:Color.green):(y<8?Color.blue:Color.yellow));pattern.Apply();
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",pattern);chart.GetComponent<Renderer>().sharedMaterial=material;
            var target=new RenderTexture(840,480,24);target.Create();camera.targetTexture=target;var pixels=new Texture2D(840,480,TextureFormat.RGB24,false);
            string folder=Path.GetFullPath(Environment.GetEnvironmentVariable("RA_PORTAL_CROP_REVIEW_OUTPUT")??"../ArtSource/Original/ArtPass-v1/Performance/PortalCrop");Directory.CreateDirectory(folder);var cases=new List<Case>();
            float priorScale=Time.timeScale;Time.timeScale=0;
            try
            {
                foreach(var gate in session.World.Portals)
                {
                    var view=gate.GetComponent<PortalView>();
                    chart.transform.SetPositionAndRotation(gate.Exit.transform.position+gate.Exit.transform.forward+Vector3.up*1.7f,Quaternion.LookRotation(gate.Exit.transform.forward));
                    foreach(bool ads in new[]{false,true})
                    {
                        camera.aspect=ads?21f/9:16f/9;float fov=ads?new AimPreferences().ViewFov(true):100;
                        camera.focalLength=Camera.FieldOfViewToFocalLength(fov,camera.sensorSize.y);
                        float distance=gate.name.StartsWith("Under bridge",StringComparison.Ordinal)?2:6;
                        camera.transform.position=gate.transform.position+gate.transform.forward*distance+gate.transform.right*(ads?1.5f:0)+Vector3.up*1.7f;camera.transform.LookAt(gate.Presentation.Surface.bounds.center);yield return null;
                        Color32[] Capture(bool cropped)
                        {
                            view.CropToAperture=cropped;Assert.That(view.RenderView(camera),Is.True);
                            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;
                            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,840,480),0,0);pixels.Apply();return pixels.GetPixels32();}finally{RenderTexture.active=old;}
                        }
                        camera.cullingMask=(1<<0)|(1<<9)|(1<<29);var world=Capture(false);
                        var delta=gate.Presentation.Surface.bounds.center-camera.transform.position;
                        string occluder=Physics.Raycast(camera.transform.position,delta.normalized,out var obstruction,delta.magnitude,(1<<0)|(1<<9),QueryTriggerInteraction.Ignore)?obstruction.collider.name:"";
                        if(gate.name.StartsWith("Under bridge",StringComparison.Ordinal)&&!ads){pixels.SetPixels32(world);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,gate.name.Replace(' ','-')+"-world.png"),pixels.EncodeToPNG());}
                        gate.Presentation.Surface.gameObject.layer=30;camera.cullingMask=(1<<29)|(1<<30);
                        var full=Capture(false);int fullWidth=view.Texture.width,fullHeight=view.Texture.height;var cropped=Capture(true);
                        var rect=view.Viewport;Assert.That(rect.width*rect.height,Is.LessThan(.6f));
                        Assert.That(view.Texture.width,Is.GreaterThanOrEqualTo(Mathf.CeilToInt(fullWidth*rect.width)));Assert.That(view.Texture.height,Is.GreaterThanOrEqualTo(Mathf.CeilToInt(fullHeight*rect.height)));
                        var offset=gate.Exit.transform.position-gate.transform.position;
                        foreach(var point in new[]{gate.Presentation.Surface.bounds.center,gate.Presentation.Surface.bounds.center+Vector3.up*.5f,gate.Presentation.Surface.bounds.center+gate.transform.right*.5f})
                        {
                            var original=camera.WorldToViewportPoint(point);var destination=view.DestinationCamera.WorldToViewportPoint(point+offset);
                            Assert.That(destination.x*rect.width+rect.x,Is.EqualTo(original.x).Within(.0001f));Assert.That(destination.y*rect.height+rect.y,Is.EqualTo(original.y).Within(.0001f));
                        }
                        var center=camera.WorldToViewportPoint(gate.Presentation.Surface.bounds.center);int count=0,chartPixels=0,worldChartPixels=0;double difference=0;
                        int left=Mathf.Clamp((int)((center.x-rect.width*.18f)*840),0,839),right=Mathf.Clamp((int)((center.x+rect.width*.18f)*840),0,839),bottom=Mathf.Clamp((int)((center.y-rect.height*.18f)*480),0,479),top=Mathf.Clamp((int)((center.y+rect.height*.18f)*480),0,479);
                        for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++)
                        { int index=y*840+x;var a=full[index];var b=cropped[index];var original=world[index];difference+=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);count++;if(Mathf.Max(a.r,a.g,a.b)-Mathf.Min(a.r,a.g,a.b)>100)chartPixels++;if(Mathf.Max(original.r,original.g,original.b)-Mathf.Min(original.r,original.g,original.b)>100)worldChartPixels++; }
                        Assert.That(count,Is.GreaterThan(20));Assert.That(chartPixels,Is.GreaterThan(20),gate.name+" reference must actually expose the colour chart, not an occluding wall");float error=(float)(difference/(count*3*255));Assert.That(error,Is.LessThan(.025f),gate.name+" crop must preserve the chart's screen alignment");
                        if(gate.name.StartsWith("Under bridge",StringComparison.Ordinal))
                        {
                            Assert.That(worldChartPixels,Is.GreaterThan(20),gate.name+" must expose its destination with the original map visible");
                            Assert.That(occluder,Is.Empty,gate.name+" approach must expose the aperture");
                        }
                        cases.Add(new Case{portal=gate.name,pose=ads?"wide-ADS-strafe":"normal",meanAbsoluteRgb=error,samples=count,chartPixels=chartPixels,worldChartPixels=worldChartPixels,worldOccluder=occluder,fullWidth=fullWidth,fullHeight=fullHeight,cropWidth=view.Texture.width,cropHeight=view.Texture.height,viewport=rect});
                        if(cases.Count<=2)
                        {
                            pixels.SetPixels32(full);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,ads?"ads-full.png":"normal-full.png"),pixels.EncodeToPNG());pixels.SetPixels32(cropped);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,ads?"ads-cropped.png":"normal-cropped.png"),pixels.EncodeToPNG());
                        }
                        Assert.That(gate.Presentation.Surface.forceRenderingOff,Is.False);
                        gate.Presentation.Surface.gameObject.layer=layers[gate.Presentation.Surface.gameObject];
                    }
                }
                Assert.That(cases.Count,Is.EqualTo(12));File.WriteAllText(Path.Combine(folder,"alignment-review.json"),JsonUtility.ToJson(new Report{cases=cases.ToArray()},true));
            }
            finally{foreach(var layer in layers)layer.Key.layer=layer.Value;Time.timeScale=priorScale;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(chart);Object.Destroy(material);Object.Destroy(pattern);}
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
