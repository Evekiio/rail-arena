using RailArena.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RailArena
{
    public sealed partial class ArenaHud : MonoBehaviour
    {
        public ArenaSession Session;
        private Transform canvas;
        private GameObject menu;
        private int bots = 3;
        private int difficulty = 1;
        private TargetScenario scenario;
        private Font font;
        private readonly Color cyan = new Color(.282f, .855f, .910f).linear;

        private void Start()
        {
            font = Resources.Load<Font>("UI/Brand/Fonts/Barlow-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            displayFont = Resources.Load<Font>("UI/Brand/Fonts/BarlowCondensed-SemiBold") ?? font;
            mediumFont = Resources.Load<Font>("UI/Brand/Fonts/Barlow-Medium") ?? font;
            canvas = new GameObject("Arena interface", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).transform;
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            new GameObject("Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            BuildCombatHud();
            RefreshMenu();
        }

        public void RefreshMenu()
        {
            if (canvas == null) return;
            var selected = EventSystem.current?.currentSelectedGameObject;
            retainedFocus = selected == null ? null : FocusKey(selected.name);
            backAction = null;
            readyButton = null;
            if (menu != null)
            {
                menu.SetActive(false);
                Destroy(menu);
            }
            connectionStatus = null;
            if(!Session.Running && !string.IsNullOrEmpty(Session.ConnectionError))
            { OnlineMenu(); return; }
            if (Session.IsOnline && (Session.OnlineWaiting || Session.Match.Phase == MatchPhase.Results))
            { ConnectionLobby(); return; }
            bool results = Session.Running && Session.Match.Phase == MatchPhase.Results;
            if (Session.Running && !Session.Paused && !results) return;
            menu = new GameObject("Menu", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            menu.transform.SetParent(canvas, false);
            Rect(menu, Vector2.zero, Vector2.one);
            menu.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .025f, .045f, .94f);
            if (!Session.Running) { MainMenuLayout(); return; }
            BrandChrome();
            string title = results ? "MATCH COMPLETE" : Session.Running ? "PAUSED" : "RAIL ARENA";
            Text(menu.transform, "Title", title, 72, new Vector2(.09f, .8f), new Vector2(.8f, .92f)).color = cyan;
            Text(menu.transform, "Subtitle", results ? $"WINNER: {Session.Actors[Session.Match.Winner].DisplayName}" :
                "FOUNDRY // PRECISION COMBAT ARENA", 24, new Vector2(.095f, .735f), new Vector2(.8f, .8f));
            float y = .66f;
            if (Session.Running)
            {
                if (!results) Button("RESUME", () => Session.SetPaused(false), ref y);
                if (!Session.IsOnline) Button(results ? "REMATCH" : "RESTART SESSION", () => Session.StartSession(Session.Practice, bots, difficulty, scenario), ref y);
            }
            else
            {
                Button(bots == 0 ? "BOT MATCH // SELECT 1+ BOT" : "OFFLINE BOT MATCH",
                    () => Session.StartSession(false, bots, difficulty, TargetScenario.Combat), ref y, bots > 0);
                Button(bots == 0 ? "SOLO PRACTICE // UNTIMED" : "PRACTICE // UNTIMED",
                    () => Session.StartSession(true, bots, difficulty, scenario), ref y);
                Button("ONLINE DUEL // DIRECT IP", OnlineMenu, ref y);
            }
            if (!Session.IsOnline && !results)
            {
                Text(menu.transform,"Setup heading",Session.Running ? "NEXT SESSION / OFFLINE SETUP" : "OFFLINE SETUP",22,
                    new Vector2(.53f,.70f),new Vector2(.94f,.74f)).color = cyan;
                float setupY = .61f;
                Button("CHARACTER // " + OfflineRoster.Label(Session.SelectedOfflineCharacter), CharacterPicker,ref setupY,minX:.53f,maxX:.94f);
                Button(bots == 0 ? "BOTS  0 // SOLO   +" : $"BOTS  {bots}   +", () =>
                { bots = !Session.Running || Session.Practice ? (bots+1)%(CombatRules.MaximumBots+1) : bots%CombatRules.MaximumBots+1; RefreshMenu(); },ref setupY,minX:.53f,maxX:.94f);
                Button($"DIFFICULTY  {new[] { "CADET", "VETERAN", "ACE" }[difficulty]}", () =>
                { difficulty=(difficulty+1)%3; RefreshMenu(); },ref setupY,minX:.53f,maxX:.94f);
                Button($"PRACTICE TARGETS // {scenario.ToString().ToUpperInvariant()}", () =>
                { scenario=(TargetScenario)(((int)scenario+1)%3); RefreshMenu(); },ref setupY,minX:.53f,maxX:.94f);
            }
            Button("SETTINGS", Settings, ref y);
            if(!results)Button("HOW TO PLAY", QuickStart, ref y);
            if (Session.Running) Button("MAIN MENU", Session.MainMenu, ref y);
            Button("QUIT", Session.QuitGame, ref y);
            if(results)
            {
                var final=CreateStandings(menu.transform,"FinalStandings","FINAL STANDINGS",new Vector2(.50f,.21f),new Vector2(.97f,.70f));
                RefreshStandings(final);
            }
            else
            Text(menu.transform,"Controls",Session.IsOnline ? "W A S D  MOVE     SPACE  JUMP\nLEFT CLICK  FIRE     HOLD RIGHT  1.5x ZOOM\nESC  PAUSE     TAB  SCOREBOARD" :
                "0 BOTS = SOLO / ADD BOTS FOR TARGET DRILLS\nBOT MATCHES ALWAYS USE COMBAT AI\n\nW A S D  MOVE     SPACE  JUMP\nLEFT CLICK  FIRE     HOLD RIGHT  1.5x ZOOM\nESC  PAUSE     TAB  SCOREBOARD",21,
                new Vector2(.53f,.13f),new Vector2(.94f,Session.IsOnline ? .64f : .34f));
            if (!Session.IsOnline) Text(menu.transform,"Weapon help",WeaponSummary,22,
                new Vector2(.095f,.16f),new Vector2(.46f,.29f));
        }

        private void Settings()
        {
            BeginPage(RefreshMenu);
            Text(menu.transform, "Title", "SETTINGS", 64, new Vector2(.1f, .78f), new Vector2(.8f, .9f));
            float reticleY = .815f;
            Button("RETICLE SETTINGS", ReticleSettings, ref reticleY, minX: .45f, maxX: .67f);
            float comfortY = .815f;
            Button("VISUAL COMFORT", ComfortSettings, ref comfortY, minX: .69f, maxX: .94f);
            Text(menu.transform, "Aim heading", "AIM / CAMERA", 22, new Vector2(.1f,.71f), new Vector2(.48f,.76f)).color = cyan;
            Text(menu.transform, "Display heading", "AUDIO / DISPLAY", 22, new Vector2(.55f,.71f), new Vector2(.94f,.76f)).color = cyan;
            string SensitivityValue(float value) => value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            Slider("Mouse sensitivity", Session.Sensitivity, AimPreferences.MinimumSensitivity, AimPreferences.MaximumSensitivity, .61f, SensitivityValue, value =>
            { Session.Sensitivity = value; Session.Aim.Save(); });
            Slider("Zoom sensitivity", Session.Aim.ZoomSensitivity, AimPreferences.MinimumSensitivity, AimPreferences.MaximumSensitivity, .48f, SensitivityValue, value =>
            { Session.Aim.ZoomSensitivity = value; Session.Aim.Save(); });
            Slider("Vertical FOV", Session.Aim.FieldOfView, AimPreferences.MinimumFov, AimPreferences.MaximumFov, .35f, value => Mathf.RoundToInt(value) + " degrees", value =>
            { Session.Aim.FieldOfView = value; Session.Aim.Save(); }, wholeNumbers: true);
            float audioY = .61f;
            Button("AUDIO MIX / SOUND TEST", AudioSettings, ref audioY, minX: .55f, maxX: .91f);
            float y = .48f;
            Button("1920 x 1080", () => Screen.SetResolution(1920, 1080, Screen.fullScreenMode), ref y, minX: .55f, maxX: .91f);
            Button("1280 x 720", () => Screen.SetResolution(1280, 720, Screen.fullScreenMode), ref y, minX: .55f, maxX: .91f);
            Button("TOGGLE FULLSCREEN", () => Screen.fullScreen = !Screen.fullScreen, ref y, minX: .55f, maxX: .91f);
            float frameY = .26f;
            Button("FRAME PACING", FramePacingSettings, ref frameY, minX: .55f, maxX: .91f);
            y = .22f;
            UnityEngine.UI.Button invertButton = null;
            invertButton = Button("INVERT Y // " + (Session.Aim.InvertY ? "ON" : "OFF"), () =>
            {
                Session.Aim.InvertY = !Session.Aim.InvertY; Session.Aim.Save();
                invertButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "INVERT Y // " + (Session.Aim.InvertY ? "ON" : "OFF");
            }, ref y);
            y = .1f;
            Button("BACK", () => { PlayerPrefs.Save(); RefreshMenu(); }, ref y);
            Text(menu.transform, "Aim help", "Hold right mouse for 1.5x zoom.\nZoom sensitivity is independent.\nChanges apply immediately.", 22,
                new Vector2(.55f,.13f), new Vector2(.94f,.25f));
        }

        private void Slider(string title, float value, float min, float max, float y, System.Func<float, string> format,
            UnityEngine.Events.UnityAction<float> changed, float minX = .1f, float maxX = .46f, bool wholeNumbers = false)
        {
            var label = Text(menu.transform, title + " value", title + " // " + format(value), 24, new Vector2(minX, y + .04f), new Vector2(maxX, y + .1f));
            var root = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Slider));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(minX, y), new Vector2(maxX, y + .025f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.11f,.17f,.21f).linear;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(root.transform, false);
            Rect(fill, Vector2.zero, Vector2.one);
            fill.GetComponent<UnityEngine.UI.Image>().color = cyan;
            fill.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var handleArea = new GameObject("Handle area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Rect(handleArea, Vector2.zero, Vector2.one);
            var area = handleArea.GetComponent<RectTransform>();
            area.offsetMin = new Vector2(11, 0); area.offsetMax = new Vector2(-11, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22, 35);
            handle.GetComponent<UnityEngine.UI.Image>().color = Color.white;
            var slider = root.GetComponent<UnityEngine.UI.Slider>();
            slider.handleRect = handleRect;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            var sliderColors=slider.colors;
            sliderColors.normalColor=Ivory;sliderColors.highlightedColor=cyan;sliderColors.selectedColor=cyan;
            sliderColors.pressedColor=new Color(.16f,.50f,.57f);slider.colors=sliderColors;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = value;
            slider.onValueChanged.AddListener(current => { label.text = title + " // " + format(current); changed(current); });
        }

        private void Update()
        {
            if (combatRoot == null) return;
            if (connectionStatus != null) connectionStatus.text = Session.ConnectionStatus;
            RefreshReadyAction();
            UpdateCombatHud();
            if (menu != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                UnityEngine.UI.Selectable first = null, preferred = null;
                foreach (var selectable in menu.GetComponentsInChildren<UnityEngine.UI.Selectable>())
                {
                    if (!selectable.IsInteractable()) continue;
                    if (first == null) first = selectable;
                    if (retainedFocus != null && FocusKey(selectable.name) == retainedFocus) preferred = selectable;
                }
                if (preferred != null || first != null) EventSystem.current.SetSelectedGameObject((preferred ?? first).gameObject);
                retainedFocus = null;
            }
            if (Session.Running && Session.Match.Phase == MatchPhase.Results && menu == null) RefreshMenu();
        }

        private UnityEngine.UI.Button Button(string title, UnityEngine.Events.UnityAction action, ref float y, bool interactable = true, float minX = .095f, float maxX = .46f)
        {
            var root = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(MenuButton));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(minX, y), new Vector2(maxX, y + .055f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.055f, .1f, .15f);
            Graphic(root.transform,"Focus Rail",Vector2.zero,new Vector2(.008f,1),cyan);
            var button = root.GetComponent<MenuButton>();
            button.interactable = interactable;
            button.onClick.AddListener(action);
            button.onClick.AddListener(InterfaceClick);
            var caption=Text(root.transform, "Label", title, 23, new Vector2(.13f, 0), new Vector2(.96f,1));
            caption.alignment = TextAnchor.MiddleLeft;
            caption.font = mediumFont;
            Icon(root.transform,"Action icon",ActionIcon(title),new Vector2(.035f,.25f),new Vector2(.095f,.75f),Muted);
            button.ConfigureCaption(caption);
            y -= .073f;
            return button;
        }

        private UnityEngine.UI.Text Text(Transform parent, string name, string value, int size, Vector2 min, Vector2 max)
        {
            if (name == "Title" && parent == menu?.transform && max.y>.87f) { min.y=.765f;max.y=.873f;size=Mathf.Min(size,60); }
            var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            root.transform.SetParent(parent, false);
            Rect(root, min, max);
            var text = root.GetComponent<UnityEngine.UI.Text>();
            text.font = size >= 32 || name == "Title" ? displayFont : font;
            text.fontSize = size;
            text.color = Ivory;
            text.text = value;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static void Rect(GameObject root, Vector2 min, Vector2 max)
        {
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
