using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class PortalRefreshRenderingReview
    {
        [Serializable] sealed class Case {public string portal;public int referenceRenders,adaptiveRenders,skipped,interval,closeRenders,scopedRenders,width,height,nonBlackPixels;public float meanAbsoluteRgb;public int changedPixels;}
        [Serializable] sealed class Report {public string scope="Frozen map, all six portal directions. Original full-rate destination versus bounded distant cache; nearby/scoped/newly visible/changed-view refresh gates. Cached shader UV and viewport remain paired. Native input/audio and moving-scene temporal acceptance remain open.";public Case[] cases;public bool passed;}
        byte[] quality;string pipeline;float timeScale=1;
        static Color32[] Pixels(RenderTexture texture)
        {
            var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();return image.GetPixels32();}finally{RenderTexture.active=old;Object.DestroyImmediate(image);}
        }
        static void Save(Color32[] pixels,int width,int height,string path)
        {var image=new Texture2D(width,height,TextureFormat.RGB24,false);try{image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{Object.DestroyImmediate(image);}}
        static IEnumerator NextFrame()
        {int frame=Time.frameCount;float deadline=Time.realtimeSinceStartup+5;do{yield return null;Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Runtime frame did not advance");}while(Time.frameCount<=frame);}
        [UnityTest] public IEnumerator DistantCacheRetainsPixelsAndCloseScopedViewsStayLive()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipeline=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,3,1,TargetScenario.Stationary);var art=new GameObject("Portal refresh inspection art").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(behaviour.GetType().Namespace=="RailArena")behaviour.enabled=false;
            var camera=session.View;camera.enabled=false;session.Aim.FieldOfView=100;camera.usePhysicalProperties=true;camera.focalLength=Camera.FieldOfViewToFocalLength(100,camera.sensorSize.y);timeScale=Time.timeScale;Time.timeScale=0;
            string folder=Path.GetFullPath("../Validation/PortalRefreshNative-04");Assert.That(Directory.Exists(folder),Is.False);Directory.CreateDirectory(folder);var cases=new List<Case>();var root=session.Human.transform.position;var velocity=session.Human.Velocity;int rounds=session.Human.Magazine.Rounds,tick=session.Match.Tick;float height=session.Human.Controller.height;
            foreach(var gate in session.World.Portals)
            {
                var view=gate.GetComponent<PortalView>();Assert.That(view.AdaptiveRefresh,Is.True,"Sky Gardens art profile enables the measured distant cache");view.AdaptiveRefresh=false;camera.transform.SetPositionAndRotation(gate.transform.position+gate.transform.forward*60+Vector3.up*1.6f,Quaternion.LookRotation(-gate.transform.forward));camera.usePhysicalProperties=true;camera.focalLength=Camera.FieldOfViewToFocalLength(100,camera.sensorSize.y);
                Assert.That(view.RenderView(camera),Is.True);var before=Pixels(view.Texture);int nonBlack=System.Array.FindAll(before,p=>p.r>8||p.g>8||p.b>8).Length;Assert.That(nonBlack,Is.GreaterThan(100),"Destination image must contain rendered content");int initial=view.RenderedFrames;view.AdaptiveRefresh=true;Assert.That(view.RenderView(camera),Is.True);int adaptiveStart=view.RenderedFrames,skipped=view.SkippedRefreshes;var stored=view.Viewport;var materialViewport=gate.Presentation.Surface.sharedMaterial.GetVector("_PortalViewport");var texture=view.Texture;
                for(int frame=0;frame<16;frame++){yield return NextFrame();view.RenderView(camera);}
                Assert.That(view.SkippedRefreshes,Is.GreaterThan(skipped),"Distant cache did not reuse a view");Assert.That(view.Texture,Is.SameAs(texture));Assert.That(view.Viewport,Is.EqualTo(stored));Assert.That(gate.Presentation.Surface.sharedMaterial.GetVector("_PortalViewport"),Is.EqualTo(materialViewport));
                var after=Pixels(view.Texture);Assert.That(after.Length,Is.EqualTo(before.Length));long difference=0;int changed=0;
                for(int i=0;i<before.Length;i++){int delta=Math.Max(Math.Abs(before[i].r-after[i].r),Math.Max(Math.Abs(before[i].g-after[i].g),Math.Abs(before[i].b-after[i].b)));if(delta>8)changed++;difference+=Math.Abs(before[i].r-after[i].r)+Math.Abs(before[i].g-after[i].g)+Math.Abs(before[i].b-after[i].b);}
                var record=new Case{portal=gate.name,referenceRenders=1,width=view.Texture.width,height=view.Texture.height,nonBlackPixels=nonBlack,adaptiveRenders=view.RenderedFrames-adaptiveStart,skipped=view.SkippedRefreshes-skipped,interval=view.RefreshInterval,meanAbsoluteRgb=difference/(float)(before.Length*3),changedPixels=changed};Assert.That(record.meanAbsoluteRgb,Is.LessThan(.05f));Assert.That(changed,Is.LessThanOrEqualTo(before.Length/1000));
                int id=System.Array.IndexOf(session.World.Portals,gate);Save(before,record.width,record.height,Path.Combine(folder,$"portal-{id}-reference.png"));Save(after,record.width,record.height,Path.Combine(folder,$"portal-{id}-cached.png"));
                // Establish a real cache-hit precondition before testing changes that fit
                // inside the old crop but still require a fresh camera projection/mask.
                bool cached=false;for(int attempt=0;attempt<8&&!cached;attempt++){yield return NextFrame();cached=!view.RenderView(camera);}Assert.That(cached,Is.True);
                var projection=camera.projectionMatrix;projection.m02+=.00001f;camera.projectionMatrix=projection;Assert.That(view.RenderView(camera),Is.True,"An altered projection must refresh even inside cached bounds");camera.ResetProjectionMatrix();Assert.That(view.RenderView(camera),Is.True);
                int mask=camera.cullingMask;camera.cullingMask^=1<<28;Assert.That(view.RenderView(camera),Is.True,"Changed layers must refresh immediately");camera.cullingMask=mask;Assert.That(view.RenderView(camera),Is.True);
                camera.transform.position+=gate.transform.right*.5f;Assert.That(view.RenderView(camera),Is.True,"A changed view must refresh immediately");camera.transform.Rotate(0,1,0);Assert.That(view.RenderView(camera),Is.True,"A turned view must refresh immediately");camera.transform.rotation=Quaternion.LookRotation(-gate.transform.forward);
                // Leaving the aperture and re-entering must never expose an old cached pose.
                camera.transform.rotation=Quaternion.LookRotation(gate.transform.forward);Assert.That(view.RenderView(camera),Is.False);yield return NextFrame();yield return NextFrame();camera.transform.rotation=Quaternion.LookRotation(-gate.transform.forward);Assert.That(view.RenderView(camera),Is.True);
                camera.transform.position=gate.transform.position+gate.transform.forward*3+Vector3.up*1.6f;initial=view.RenderedFrames;
                for(int frame=0;frame<8;frame++){yield return NextFrame();Assert.That(view.RenderView(camera),Is.True);Assert.That(view.RefreshInterval,Is.EqualTo(1));}record.closeRenders=view.RenderedFrames-initial;Assert.That(record.closeRenders,Is.EqualTo(8));
                camera.transform.position=gate.transform.position+gate.transform.forward*60+Vector3.up*1.6f;camera.usePhysicalProperties=true;camera.focalLength=Camera.FieldOfViewToFocalLength(77,camera.sensorSize.y);Assert.That(camera.fieldOfView,Is.EqualTo(77).Within(.01f));initial=view.RenderedFrames;
                for(int frame=0;frame<8;frame++){yield return NextFrame();Assert.That(view.RenderView(camera),Is.True);Assert.That(view.RefreshInterval,Is.EqualTo(1));}record.scopedRenders=view.RenderedFrames-initial;Assert.That(record.scopedRenders,Is.EqualTo(8));view.AdaptiveRefresh=false;cases.Add(record);
                Assert.That(session.Human.transform.position,Is.EqualTo(root));Assert.That(session.Human.Velocity,Is.EqualTo(velocity));Assert.That(session.Match.Tick,Is.EqualTo(tick));Assert.That(session.Human.Magazine.Rounds,Is.EqualTo(rounds));Assert.That(session.Human.Controller.height,Is.EqualTo(height));
            }
            File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(new Report{cases=cases.ToArray(),passed=true},true));TestContext.WriteLine("PORTAL_REFRESH_NATIVE_PASSED six destination directions / cached pixels / nearby and scoped full refresh / actor invariants");Time.timeScale=timeScale;Object.Destroy(art);yield return null;foreach(var gate in session.World.Portals)Assert.That(gate.GetComponent<PortalView>().AdaptiveRefresh,Is.False,"Removing the art profile restores original refresh defaults");yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator OlderMapRetainsFullRefreshDefaults()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipeline=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();foreach(var gate in session.World.Portals)Assert.That(gate.GetComponent<PortalView>().AdaptiveRefresh,Is.False);
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=timeScale;if(Application.isPlaying)yield return new ExitPlayMode();if(quality!=null){QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipeline);File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.ImportAsset("ProjectSettings/QualitySettings.asset",ImportAssetOptions.ForceSynchronousImport);}}
    }
}
