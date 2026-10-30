using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private GameObject physicsPanel;
        private UnityEngine.UI.Text configStatus;
        public void HidePhysicsConfig(){if(physicsPanel!=null){physicsPanel.SetActive(false);Destroy(physicsPanel);physicsPanel=null;}}
        public void ShowPhysicsConfig()
        {
            if(canvas==null)return;
            HidePhysicsConfig();if(menu!=null)menu.SetActive(false);physicsPanel=new GameObject("Physics Config",typeof(RectTransform),typeof(UnityEngine.UI.Image));physicsPanel.transform.SetParent(canvas,false);
            Rect(physicsPanel,Vector2.zero,Vector2.one);physicsPanel.GetComponent<UnityEngine.UI.Image>().color=new Color(.018f,.035f,.05f,1);
            Text(physicsPanel.transform,"Config title","RAIL ARENA // PHYSICS LAB",42,new Vector2(.05f,.89f),new Vector2(.9f,.96f)).color=cyan;
            Text(physicsPanel.transform,"Config scope",Session.IsOnline?"ONLINE // READ ONLY / STANDARD VALUES":"OFFLINE PLAYTEST // LIVE VALUES / G OR ESC TO CLOSE",21,new Vector2(.05f,.84f),new Vector2(.95f,.89f));
            var t=Session.Tuning;int index=0;
            void Row(string label,Func<float> read,Action<float> set,float min,float max,bool whole=false)
            {
                int column=index/7,row=index%7;index++;float x=.05f+column*.315f,y=.75f-row*.085f;
                Text(physicsPanel.transform,label+" label",label,21,new Vector2(x,y+.025f),new Vector2(x+.195f,y+.065f));
                var inputRoot=new GameObject(label+" input",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.InputField));inputRoot.transform.SetParent(physicsPanel.transform,false);
                Rect(inputRoot,new Vector2(x+.2f,y+.025f),new Vector2(x+.275f,y+.065f));inputRoot.GetComponent<UnityEngine.UI.Image>().color=new Color(.1f,.17f,.2f);
                var value=Text(inputRoot.transform,label+" text",read().ToString("0.####",CultureInfo.InvariantCulture),22,new Vector2(.06f,.02f),new Vector2(.96f,.98f));
                var input=inputRoot.GetComponent<UnityEngine.UI.InputField>();input.textComponent=value;input.contentType=UnityEngine.UI.InputField.ContentType.DecimalNumber;input.text=value.text;input.interactable=!Session.IsOnline;
                var sliderRoot=new GameObject(label+" slider",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Slider));sliderRoot.transform.SetParent(physicsPanel.transform,false);
                Rect(sliderRoot,new Vector2(x,y),new Vector2(x+.275f,y+.015f));sliderRoot.GetComponent<UnityEngine.UI.Image>().color=new Color(.12f,.2f,.24f);
                var handle=new GameObject("Handle",typeof(RectTransform),typeof(UnityEngine.UI.Image));handle.transform.SetParent(sliderRoot.transform,false);Rect(handle,Vector2.zero,Vector2.one);var hr=handle.GetComponent<RectTransform>();hr.sizeDelta=new Vector2(15,8);handle.GetComponent<UnityEngine.UI.Image>().color=cyan;
                var slider=sliderRoot.GetComponent<UnityEngine.UI.Slider>();slider.handleRect=hr;slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=whole;slider.SetValueWithoutNotify(read());slider.interactable=!Session.IsOnline;
                void Apply(float v){if(Session.IsOnline)return;v=Mathf.Clamp(whole?Mathf.Round(v):Mathf.Round(v*1000)/1000,min,max);set(v);slider.SetValueWithoutNotify(v);input.SetTextWithoutNotify(v.ToString("0.####",CultureInfo.InvariantCulture));configStatus.text="APPLIED // "+label+" = "+v.ToString("0.####",CultureInfo.InvariantCulture);}
                slider.onValueChanged.AddListener(Apply);input.onEndEdit.AddListener(text=>{if(float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out float v)&&!float.IsNaN(v)&&!float.IsInfinity(v))Apply(v);else input.SetTextWithoutNotify(read().ToString("0.####",CultureInfo.InvariantCulture));});
            }
            Row("GROUND SPEED m/s",()=>t.GroundSpeed,v=>t.GroundSpeed=v,2,25);
            Row("GROUND ACCELERATION",()=>t.GroundAcceleration,v=>t.GroundAcceleration=v,1,30);
            Row("AIR ACCELERATION",()=>t.AirAcceleration,v=>t.AirAcceleration=v,0,10);
            Row("GROUND FRICTION",()=>t.Friction,v=>t.Friction=v,0,15);
            Row("STOP SPEED m/s",()=>t.StopSpeed,v=>t.StopSpeed=v,0,10);
            Row("JUMP SPEED m/s",()=>t.JumpSpeed,v=>t.JumpSpeed=v,2,18);
            Row("GRAVITY m/s2",()=>t.Gravity,v=>t.Gravity=v,10,45);
            Row("SPRINT MULTIPLIER",()=>t.SprintSpeedMultiplier,v=>t.SprintSpeedMultiplier=v,1,2);
            Row("WALK MULTIPLIER",()=>t.WalkSpeedMultiplier,v=>t.WalkSpeedMultiplier=v,.1f,1);
            Row("CROUCH MULTIPLIER",()=>t.CrouchSpeedMultiplier,v=>t.CrouchSpeedMultiplier=v,.1f,1);
            Row("SLIDE DURATION ticks",()=>t.SlideDurationTicks,v=>t.SlideDurationTicks=(int)v,1,120,true);
            Row("SLIDE COOLDOWN ticks",()=>t.SlideCooldownTicks,v=>t.SlideCooldownTicks=(int)v,0,180,true);
            Row("SLIDE ENTRY SPEED m/s",()=>t.SlideMinimumSpeed,v=>t.SlideMinimumSpeed=v,1,30);
            Row("SLIDE DECELERATION",()=>t.SlideDeceleration,v=>t.SlideDeceleration=v,0,40);
            Row("SLIDE STEERING deg/s",()=>t.SlideSteeringDegrees,v=>t.SlideSteeringDegrees=v,0,180);
            Row("STEP HEIGHT m",()=>t.StepOffset,v=>t.StepOffset=v,0,.5f);
            Row("SLOPE LIMIT degrees",()=>t.SlopeLimit,v=>t.SlopeLimit=v,20,60);
            Row("CONTROLLER SKIN m",()=>t.SkinWidth,v=>t.SkinWidth=v,.02f,.12f);
            Row("PAD FLIGHT MULTIPLIER",()=>t.PadFlightMultiplier,v=>t.PadFlightMultiplier=v,.75f,1.4f);
            Text(physicsPanel.transform,"Config constants","FIXED // 60 Hz | capsule radius 0.36m | stand/crouch 1.8/1.2m | eye 1.58/0.98m\n"+Session.World.LayoutName+" | BUILD "+Networking.BuildIdentity.Fingerprint.Substring(0,12),19,new Vector2(.05f,.12f),new Vector2(.95f,.2f));
            configStatus=Text(physicsPanel.transform,"Config status","Changes are session-only. Save a PNG + JSON to share the exact configuration.",19,new Vector2(.05f,.075f),new Vector2(.95f,.12f));
            ConfigButton("RESET STANDARD",.05f,()=>{Session.ResetPhysicsConfig();ShowPhysicsConfig();},!Session.IsOnline);
            ConfigButton("COPY JSON",.29f,()=>{GUIUtility.systemCopyBuffer=Session.PhysicsConfigJson();configStatus.text="Configuration JSON copied.";});
            ConfigButton("SAVE PNG + JSON",.53f,()=>StartCoroutine(SavePhysicsConfig()));
            ConfigButton("CLOSE / G",.77f,Session.ClosePhysicsConfig);
        }
        private void ConfigButton(string title,float x,UnityEngine.Events.UnityAction action,bool enabled=true)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));obj.transform.SetParent(physicsPanel.transform,false);
            Rect(obj,new Vector2(x,.025f),new Vector2(x+.19f,.072f));obj.GetComponent<UnityEngine.UI.Image>().color=new Color(.08f,.19f,.23f);
            var button=obj.GetComponent<UnityEngine.UI.Button>();button.interactable=enabled;button.onClick.AddListener(action);
            Text(obj.transform,title+" label",title,21,new Vector2(.04f,.05f),new Vector2(.96f,.95f)).alignment=TextAnchor.MiddleCenter;
        }
        private IEnumerator SavePhysicsConfig()
        {
            string folder=Path.Combine(Application.persistentDataPath,"TuningSnapshots"),stem=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            try{Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,stem+".json"),Session.PhysicsConfigJson());configStatus.text="SAVED // TuningSnapshots/"+stem+".png + .json";}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){configStatus.text="SAVE FAILED // "+e.Message;yield break;}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,stem+".png"));
        }
    }
}
