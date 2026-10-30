using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private static readonly Color Ivory = new Color(.937f, .914f, .851f).linear;
        private static readonly Color Muted = new Color(.55f, .64f, .66f).linear;
        private static readonly Color Surface = new Color(.085f, .135f, .165f, .94f).linear;
        private Font displayFont;
        private Font mediumFont;
        private Action backAction;
        private string retainedFocus;
        private AudioSource menuAudio;
        private AudioClip menuClick;

        private void InterfaceClick()
        {
            if(Session.Sound.Master<=0||Session.Sound.Effects<=0)return;
            if(menuClick==null)
            {
                const int rate=44100,count=2205;
                var samples=new float[count];
                for(int i=0;i<count;i++)
                {
                    float t=i/(float)rate;
                    samples[i]=Mathf.Sin(2*Mathf.PI*(850*t+1600*t*t))*Mathf.Exp(-t*100)*Mathf.Min(1,t*1000)*.4f;
                }
                menuClick=AudioClip.Create("Rail Arena interface click",count,1,rate,false);menuClick.SetData(samples,0);
                menuAudio=gameObject.AddComponent<AudioSource>();menuAudio.playOnAwake=false;menuAudio.spatialBlend=0;menuAudio.ignoreListenerPause=true;
            }
            ApplyInterfaceSound();
            menuAudio.PlayOneShot(menuClick);
        }

        private void ApplyInterfaceSound()
        {
            if(menuAudio!=null)menuAudio.volume=Session.Sound.Effects*.12f;
        }

        private void OnDestroy()
        {
            if(menuClick!=null) { if(Application.isPlaying)Destroy(menuClick);else DestroyImmediate(menuClick); }
        }

        private static string FocusKey(string name)
        {
            foreach (string key in new[] { "BOTS  ", "DIFFICULTY  ", "PRACTICE TARGETS //", "CHARACTER //" })
                if (name.StartsWith(key,StringComparison.Ordinal)) return key;
            return name;
        }

        private void BeginPage(Action back = null)
        {
            foreach (Transform child in menu.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            backAction = back;
            retainedFocus = null;
            EventSystem.current?.SetSelectedGameObject(null);
            BrandChrome();
        }

        public bool TryHandleBack()
        {
            if (menu == null || !menu.activeSelf || backAction == null) return false;
            var action = backAction;
            backAction = null;
            action();
            return true;
        }

        private void BrandChrome()
        {
            var background = menu.GetComponent<UnityEngine.UI.Image>();
            if (background != null) background.color = new Color(.071f, .102f, .133f).linear;
            var logo=Raw(menu.transform, "Rail Arena brand", Resources.Load<Texture2D>("UI/Brand/Logo"),
                new Vector2(.04f, .909f), new Vector2(.245f, .978f), 3);
            var logoFrame=(RectTransform)logo.transform.parent;
            logoFrame.anchorMin=logoFrame.anchorMax=new Vector2(.04f,.944f);logoFrame.pivot=new Vector2(0,.5f);
            logoFrame.sizeDelta=new Vector2(340,340f/3);logoFrame.anchoredPosition=Vector2.zero;
            Graphic(menu.transform, "Header rule", new Vector2(.04f, .887f), new Vector2(.96f, .888f), new Color(.20f,.30f,.33f).linear);
            Text(menu.transform, "Brand strapline", "ONE RIFLE. EVERY SHOT MATTERS.", 17,
                new Vector2(.62f,.925f),new Vector2(.96f,.959f)).alignment = TextAnchor.MiddleRight;
            Text(menu.transform,"Build status","LOCAL PLAYTEST",15,new Vector2(.76f,.897f),new Vector2(.96f,.928f)).color=Muted;
            Graphic(menu.transform,"Footer rule",new Vector2(.04f,.066f),new Vector2(.96f,.067f),new Color(.16f,.24f,.27f).linear);
            Text(menu.transform,"Brand footer","RAIL ARENA  /  " + Session.World.LayoutName,15,new Vector2(.04f,.022f),new Vector2(.46f,.054f)).color=Muted;
            string escape=backAction!=null?"     ESC  Back":Session.Paused?"     ESC  Resume":"";
            var hint=Text(menu.transform,"Navigation hint","ARROWS  Navigate     ENTER  Select"+escape,15,
                new Vector2(.53f,.022f),new Vector2(.96f,.054f));
            hint.color=Muted; hint.alignment=TextAnchor.MiddleRight;
        }

        private UnityEngine.UI.RawImage Raw(Transform parent,string name,Texture texture,Vector2 min,Vector2 max,float aspect)
        {
            var frame=new GameObject(name+" frame",typeof(RectTransform));
            frame.transform.SetParent(parent,false);Rect(frame,min,max);
            var obj=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));
            obj.transform.SetParent(frame.transform,false);Rect(obj,Vector2.zero,Vector2.one);
            var image=obj.GetComponent<UnityEngine.UI.RawImage>();image.texture=texture;image.raycastTarget=false;
            var fit=obj.GetComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=aspect;
            return image;
        }

        private UnityEngine.UI.RawImage Icon(Transform parent,string name,string icon,Vector2 min,Vector2 max,Color? color=null)
        {
            var image=Raw(parent,name,Resources.Load<Texture2D>("UI/Brand/Icons/"+icon),min,max,1);
            image.color=color??Ivory;
            return image;
        }

        private static string ActionIcon(string title)
        {
            if(title.Contains("CHARACTER")||title.Contains("CONTENDERS"))return "contender";
            if(title.Contains("BOTS"))return "target";
            if(title.Contains("DIFFICULTY"))return "tuning";
            if(title.Contains("PRACTICE TARGETS"))return "aim";
            if(title.Contains("RETICLE"))return "aim";
            if(title.Contains("INVERT"))return "aim";
            if(title=="MAIN MENU")return "back";
            if(title.Contains("AUDIO")||title.Contains("SOUND"))return "audio";
            if(title.Contains("FRAME")||title.Contains("VSYNC")||title.Contains("FPS")||title.Contains("FULLSCREEN")||title.Contains(" x "))return "display";
            if(title.Contains("COMFORT")||title.Contains("FLASH")||title.Contains("MOTION"))return "comfort";
            if(title.Contains("SETTINGS"))return title.StartsWith("BACK")?"back":"settings";
            if(title.Contains("BACK"))return "back";
            if(title.Contains("QUIT")||title.Contains("DISCONNECT"))return "quit";
            if(title.Contains("HOW TO"))return "help";
            if(title.Contains("READY"))return "check";
            if(title.Contains("RESTART")||title.Contains("REMATCH"))return "restart";
            if(title.Contains("HOST")||title.Contains("JOIN")||title.Contains("ONLINE"))return "network";
            return "play";
        }

        private UnityEngine.UI.Button ActionCard(string title,string detail,string icon,float y,bool enabled=true)
        {
            var obj=new GameObject(title,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(MenuButton));
            obj.transform.SetParent(menu.transform,false);Rect(obj,new Vector2(.29f,y),new Vector2(.665f,y+.145f));
            Graphic(obj.transform,"Focus Rail",Vector2.zero,new Vector2(.008f,1),cyan);
            var caption=Text(obj.transform,"Label",title,29,new Vector2(.18f,.47f),new Vector2(.91f,.88f));
            caption.font=displayFont;
            var description=Text(obj.transform,"Mode description",detail,19,new Vector2(.18f,.12f),new Vector2(.92f,.47f));description.color=Muted;
            Icon(obj.transform,"Mode icon",icon,new Vector2(.045f,.32f),new Vector2(.13f,.72f),cyan);
            Icon(obj.transform,"Mode arrow","next",new Vector2(.93f,.38f),new Vector2(.975f,.63f),Muted);
            var button=obj.GetComponent<MenuButton>();button.interactable=enabled;button.ConfigureCaption(caption);
            button.onClick.AddListener(InterfaceClick);
            return button;
        }

        private void MainMenuLayout()
        {
            // One persistent configuration panel accompanies every launch choice.
            BrandChrome();
            Text(menu.transform,"Title","ENTER THE FOUNDRY",62,new Vector2(.29f,.745f),new Vector2(.94f,.85f)).font=displayFont;
            Text(menu.transform,"Subtitle","Find your angle. Keep your momentum. Make the shot count.",21,
                new Vector2(.29f,.70f),new Vector2(.94f,.755f)).color=Muted;
            float navigation=.76f;
            Button("PLAY",RefreshMenu,ref navigation,minX:.04f,maxX:.245f);
            Button("CONTENDERS",CharacterPicker,ref navigation,minX:.04f,maxX:.245f);
            Button("SETTINGS",Settings,ref navigation,minX:.04f,maxX:.245f);
            Button("HOW TO PLAY",QuickStart,ref navigation,minX:.04f,maxX:.245f);
            navigation=.16f;Button("QUIT",Session.QuitGame,ref navigation,minX:.04f,maxX:.245f);
            Text(menu.transform,"Weapon help",WeaponSummary,18,new Vector2(.04f,.25f),new Vector2(.245f,.40f)).color=Muted;
            string difficultyLabel=new[]{"CADET","VETERAN","ACE"}[difficulty];
            var match=ActionCard(bots==0?"BOT MATCH // SELECT 1+ BOT":"OFFLINE BOT MATCH",
                bots==0?"Add an opponent in Session Setup to start a timed match.":$"{bots} opponents  /  {difficultyLabel}  /  Timed free-for-all","score",.525f,bots>0);
            match.onClick.AddListener(()=>Session.StartSession(false,bots,difficulty,TargetScenario.Combat));
            var practice=ActionCard(bots==0?"SOLO PRACTICE // UNTIMED":"PRACTICE // UNTIMED",
                bots==0?"Explore the arena. Refine your movement. No opponents.":"Target drills or combat practice. Train at your own pace.","target",.355f);
            practice.onClick.AddListener(()=>Session.StartSession(true,bots,difficulty,scenario));
            ActionCard("ONLINE DUEL // DIRECT IP","Host or join a two-player duel. Same build required.","duel",.185f).onClick.AddListener(OnlineMenu);
            var setup=Graphic(menu.transform,"Session setup panel",new Vector2(.69f,.14f),new Vector2(.96f,.67f),Surface);
            Text(setup.transform,"Setup heading","SESSION SETUP",22,new Vector2(.06f,.90f),new Vector2(.94f,.99f)).color=cyan;
            Raw(setup.transform,"Selected contender portrait",Resources.Load<Texture2D>("UI/ContenderPortraits/Character-"+Session.SelectedOfflineCharacter),
                new Vector2(.07f,.59f),new Vector2(.43f,.88f),.8f);
            Text(setup.transform,"Selected contender",OfflineRoster.Label(Session.SelectedOfflineCharacter).Replace(" / ","\n"),27,
                new Vector2(.48f,.70f),new Vector2(.95f,.86f)).font=displayFont;
            Text(setup.transform,"Appearance note","COSMETIC APPEARANCE\nEqual gameplay dimensions",16,new Vector2(.48f,.59f),new Vector2(.95f,.68f)).color=Muted;
            float y=.398f;
            Button("CHARACTER // "+OfflineRoster.Label(Session.SelectedOfflineCharacter),CharacterPicker,ref y,minX:.705f,maxX:.945f);
            Button(bots==0?"BOTS  0 // SOLO   +":$"BOTS  {bots}   +",()=>{bots=(bots+1)%(Simulation.CombatRules.MaximumBots+1);RefreshMenu();},ref y,minX:.705f,maxX:.945f);
            Button("DIFFICULTY  "+difficultyLabel,()=>{difficulty=(difficulty+1)%3;RefreshMenu();},ref y,minX:.705f,maxX:.945f);
            Button("PRACTICE TARGETS // "+scenario.ToString().ToUpperInvariant(),()=>{scenario=(TargetScenario)(((int)scenario+1)%3);RefreshMenu();},ref y,minX:.705f,maxX:.945f);
            Text(menu.transform,"Controls","SHIFT  Sprint     ALT  Walk     CTRL  Crouch     HOLD RIGHT MOUSE  ADS",17,
                new Vector2(.29f,.09f),new Vector2(.96f,.132f)).color=Muted;
        }
    }
}
