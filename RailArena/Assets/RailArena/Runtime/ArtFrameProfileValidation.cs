#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena
{
    /// <summary>Opt-in frame attribution. Stages the observer after the player view and before portal views.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class ArtFrameProfileValidation : MonoBehaviour
    {
        const int WarmupFrames=120,SampleFrames=360;
        [Serializable] sealed class Metric
        {
            public string column,name,category,unit;
            public bool available;
            public int samples;
            public double mean,p95,maximum;
        }
        sealed class Counter : IDisposable
        {
            public string name,category,unit;
            public ProfilerRecorder recorder;
            public void Dispose(){if(recorder.Valid)recorder.Dispose();}
        }
        [Serializable] sealed class Reading
        {
            public string view;
            public int frames,portalRenders,gc0,gc1,gc2,nonBlackSamples,validGpuSamples;
            public double meanWallMs,p95WallMs,meanSubmitMs,p95SubmitMs,meanReadbackMs,p95ReadbackMs,medianGpuMs=-1;
            public Metric[] counters;
            public PortalTarget[] portalTargets;
        }
        [Serializable] sealed class PortalTarget { public int id,renders,width,height;public string name;public Rect viewport; }
        [Serializable] sealed class Report
        {
            public string schema="rail-arena-art-frame-profile/1";
            public string scope="Hidden Windows player; 1080p High; one automated human and three bots; 100-degree vertical observer FOV; fixed 60 Hz simulation with uncapped wall-timed render throughput. Observer stages after ArenaSession and before automatic portal renders. Main rendering runs after LateUpdate at EndOfFrame with synchronous one-pixel readback. Phase wall times include scheduling/driver work; readback is not isolated GPU time. Profiler counters are inclusive, last-completed-frame values and are not additive. Unity delta is fixed by captureFramerate and is not a pacing measurement. No HUD/native input/audio/sustained visible-play acceptance.";
            public string fingerprint,gpu,api;
            public int warmupFrames=WarmupFrames,sampleFrames=SampleFrames,exceptions;
            public int width,height,simulationHz=60,randomSeed=2147;
            public bool gpuRecorderSupported,passed;
            public bool apertureCropping;
            public int stonePaletteRenderers, stonePaletteSourceSlots;
            public bool frameTimingFeatureEnabled;
            public Reading[] views;
        }
        struct Sample
        {
            public int frame,portals,gc0,gc1,gc2;
            public double wall,submit,readback,unityFrame;
        }
        static readonly HashSet<string> Wanted=new(StringComparer.Ordinal)
        {
            "Main Thread","Render Thread","PlayerLoop","WaitForTargetFPS","Gfx.WaitForPresentOnGfxThread","Gfx.PresentFrame","Gfx.WaitForRenderThread","Semaphore.WaitForSignal",
            "Gfx.ProcessCommands","GC.Alloc","GC.Collect","GC Allocated In Frame","GC Used Memory","Draw Calls Count","SetPass Calls Count","Triangles Count","Vertices Count","Batches Count",
            "Update.ScriptRunBehaviourUpdate","PreLateUpdate.ScriptRunBehaviourLateUpdate","FixedUpdate.ScriptRunBehaviourFixedUpdate","Animator.Update","Physics.Simulate",
            "RenderPipelineManager.DoRenderLoop_Internal","UniversalRenderPipeline.Render","RenderGraph.Compile","RenderGraph.Execute","RailArena.PortalView"
        };
        ArenaSession session;Camera observer;Vector3 position,target;bool staged;int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--profile-art-frame")>=0)new GameObject("Art frame attribution profile").AddComponent<ArtFrameProfileValidation>();
        }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void LateUpdate()
        {
            if(!staged||observer==null)return;
            observer.transform.position=position;observer.transform.LookAt(target);
            observer.focalLength=Camera.FieldOfViewToFocalLength(100,observer.sensorSize.y);
        }
        static List<Counter> OpenCounters(string output)
        {
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var available=new List<string>();var counters=new List<Counter>();
            foreach(var handle in handles)
            {
                var description=ProfilerRecorderHandle.GetDescription(handle);
                available.Add(description.Category+"\t"+description.UnitType+"\t"+description.Name);
                if(!Wanted.Contains(description.Name))continue;
                counters.Add(new Counter{name=description.Name,category=description.Category.ToString(),unit=description.UnitType.ToString(),
                    recorder=ProfilerRecorder.StartNew(description.Category,description.Name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame)});
            }
            available.Sort(StringComparer.Ordinal);File.WriteAllLines(output+".available-counters.tsv",available);
            return counters;
        }
        static double Mean(IEnumerable<double> values)=>values.Average();
        static double Percentile(IEnumerable<double> values,double fraction)
        {
            var sorted=values.OrderBy(value=>value).ToArray();return sorted[Math.Min(sorted.Length-1,(int)(sorted.Length*fraction))];
        }
        static int PortalFrames(PortalView[] portals){int count=0;foreach(var portal in portals)count+=portal.RenderedFrames;return count;}
        IEnumerator Start()
        {
            yield return null;var args=Environment.GetCommandLineArgs();int argument=Array.IndexOf(args,"--validation-output");
            string output=argument>=0&&argument+1<args.Length?Path.GetFullPath(args[argument+1]):Path.Combine(Application.persistentDataPath,"art-frame-profile.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));Screen.SetResolution(1920,1080,FullScreenMode.Windowed);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            session=FindFirstObjectByType<ArenaSession>();observer=session.View;session.Aim.FieldOfView=100;session.Graphics.Tier=GraphicsTier.High;session.Graphics.Smaa=true;session.Graphics.MotionBlur=true;session.Graphics.Apply(observer);
            session.AutomatedHuman=true;session.StartSession(true,3,1,TargetScenario.Combat);yield return new WaitForSecondsRealtime(5);
            int priorCaptureRate=Time.captureFramerate;Time.captureFramerate=60;UnityEngine.Random.InitState(2147);session.StartSession(true,3,1,TargetScenario.Combat);yield return null;yield return null;
            var portals=FindObjectsByType<PortalView>(FindObjectsSortMode.None);observer.cullingMask=(observer.cullingMask|(1<<8))&~(1<<11);observer.enabled=false;
            bool cropped=Array.IndexOf(args,"--profile-portal-crop-off")<0;foreach(var portal in portals)portal.CropToAperture=cropped;
            var texture=new RenderTexture(1920,1080,24);texture.Create();var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var renderRequest=new RenderPipeline.StandardRequest{destination=texture};
            var counters=OpenCounters(output);var readings=new List<Reading>();var endOfFrame=new WaitForEndOfFrame();var timing=new FrameTiming[1];
            var gate=session.World.Portals[0];
            var poses=new[]{("combat",new Vector3(25,3,-14),new Vector3(0,5,0)),("summit",new Vector3(14,31,-16),new Vector3(0,23,0)),("basement",new Vector3(43,-3,43),new Vector3(32,-9,32)),("foliage-waterfall",new Vector3(-35,-6,23),new Vector3(-31,-10,16)),("portal",gate.transform.position+gate.transform.forward*3+Vector3.up*1.5f,gate.transform.position+Vector3.up*1.1f)};
            try
            {
                foreach(var pose in poses)
                {
                    position=pose.Item2;target=pose.Item3;staged=true;
                    var samples=new Sample[SampleFrames];var raw=new long[SampleFrames,counters.Count];var gpu=new List<double>();
                    var initialPortalFrames=new int[portals.Length];
                    long previousEnd=0;int previousPortals=PortalFrames(portals),g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);
                    for(int frame=0;frame<WarmupFrames+SampleFrames;frame++)
                    {
                        if(frame==WarmupFrames)for(int portal=0;portal<portals.Length;portal++)initialPortalFrames[portal]=portals[portal].RenderedFrames;
                        yield return endOfFrame;
                        long start=Stopwatch.GetTimestamp();
                        RenderPipeline.SubmitRenderRequest(observer,renderRequest);long submitted=Stopwatch.GetTimestamp();
                        var previous=RenderTexture.active;
                        try{RenderTexture.active=texture;pixel.ReadPixels(new Rect(0,0,1,1),0,0,false);}finally{RenderTexture.active=previous;}
                        long finished=Stopwatch.GetTimestamp();int rendered=PortalFrames(portals),c0=GC.CollectionCount(0),c1=GC.CollectionCount(1),c2=GC.CollectionCount(2);
                        var sample=new Sample{frame=Time.frameCount,wall=previousEnd==0?0:(finished-previousEnd)*1000d/Stopwatch.Frequency,submit=(submitted-start)*1000d/Stopwatch.Frequency,readback=(finished-submitted)*1000d/Stopwatch.Frequency,portals=rendered-previousPortals,gc0=c0-g0,gc1=c1-g1,gc2=c2-g2};
                        previousEnd=finished;previousPortals=rendered;g0=c0;g1=c1;g2=c2;
                        FrameTimingManager.CaptureFrameTimings();yield return null;
                        if(frame<WarmupFrames)continue;
                        int index=frame-WarmupFrames;sample.unityFrame=Time.unscaledDeltaTime*1000;samples[index]=sample;
                        for(int counter=0;counter<counters.Count;counter++)raw[index,counter]=counters[counter].recorder.Valid&&counters[counter].recorder.Count>0?counters[counter].recorder.LastValue:-1;
                        if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);
                    }
                    var metrics=new List<Metric>();var csv=new StringBuilder("frame,wallMs,submitMs,readbackMs,unityFrameMs,portalRenders,gc0,gc1,gc2");
                    for(int counter=0;counter<counters.Count;counter++)
                    {
                        var data=new List<double>();for(int frame=0;frame<SampleFrames;frame++)if(raw[frame,counter]>=0)data.Add(raw[frame,counter]);
                        string column="counter"+counter;csv.Append(',').Append(column);var source=counters[counter];
                        metrics.Add(new Metric{column=column,name=source.name,category=source.category,unit=source.unit,available=source.recorder.Valid&&data.Count>0,samples=data.Count,mean=data.Count>0?Mean(data):-1,p95=data.Count>0?Percentile(data,.95):-1,maximum=data.Count>0?data.Max():-1});
                    }
                    csv.Append('\n');for(int frame=0;frame<SampleFrames;frame++)
                    {
                        var sample=samples[frame];csv.AppendFormat(CultureInfo.InvariantCulture,"{0},{1:F6},{2:F6},{3:F6},{4:F6},{5},{6},{7},{8}",sample.frame,sample.wall,sample.submit,sample.readback,sample.unityFrame,sample.portals,sample.gc0,sample.gc1,sample.gc2);
                        for(int counter=0;counter<counters.Count;counter++)csv.Append(',').Append(raw[frame,counter]);csv.Append('\n');
                    }
                    File.WriteAllText(output+"."+pose.Item1+".csv",csv.ToString());
                    var reading=new Reading{view=pose.Item1,frames=SampleFrames,meanWallMs=Mean(samples.Select(sample=>sample.wall)),p95WallMs=Percentile(samples.Select(sample=>sample.wall),.95),meanSubmitMs=Mean(samples.Select(sample=>sample.submit)),p95SubmitMs=Percentile(samples.Select(sample=>sample.submit),.95),meanReadbackMs=Mean(samples.Select(sample=>sample.readback)),p95ReadbackMs=Percentile(samples.Select(sample=>sample.readback),.95),portalRenders=samples.Sum(sample=>sample.portals),gc0=samples.Sum(sample=>sample.gc0),gc1=samples.Sum(sample=>sample.gc1),gc2=samples.Sum(sample=>sample.gc2),counters=metrics.ToArray(),validGpuSamples=gpu.Count,medianGpuMs=gpu.Count>0?Percentile(gpu,.5):-1};
                    reading.portalTargets=portals.Select((portal,index)=>new PortalTarget{id=Array.IndexOf(session.World.Portals,portal.Gate),name=portal.name,renders=portal.RenderedFrames-initialPortalFrames[index],width=portal.Texture!=null?portal.Texture.width:0,height=portal.Texture!=null?portal.Texture.height:0,viewport=portal.Viewport}).ToArray();
                    var old=RenderTexture.active;
                    try{RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1920,1080),0,0,false);image.Apply();}finally{RenderTexture.active=old;}
                    var pixels=image.GetPixels32();for(int index=0;index<pixels.Length;index+=64)if(pixels[index].r>8||pixels[index].g>8||pixels[index].b>8)reading.nonBlackSamples++;
                    File.WriteAllBytes(output+"."+pose.Item1+".png",image.EncodeToPNG());readings.Add(reading);
                }
                var report=new Report{fingerprint=Resources.Load<TextAsset>("BuildIdentity").text.Trim(),gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),gpuRecorderSupported=SystemInfo.supportsGpuRecorder,exceptions=errors,width=texture.width,height=texture.height,apertureCropping=cropped,views=readings.ToArray(),passed=errors==0&&readings.Count==5&&readings.All(reading=>reading.nonBlackSamples>100)};
                var stone = FindObjectsByType<StoneSubmissionPresentation>(FindObjectsSortMode.None);
                report.stonePaletteRenderers = stone.Sum(item => item.ReplacedRenderers);
                report.stonePaletteSourceSlots = stone.Sum(item => item.OriginalMaterialSlots);
                report.frameTimingFeatureEnabled = FrameTimingManager.IsFeatureEnabled();
                File.WriteAllText(output,JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
            }
            finally
            {
                Time.captureFramerate=priorCaptureRate;foreach(var counter in counters)counter.Dispose();texture.Release();Destroy(texture);Destroy(pixel);Destroy(image);
            }
        }
    }
}
#endif
