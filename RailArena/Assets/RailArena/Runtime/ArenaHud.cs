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
        private readonly Color cyan = new(.25f, .95f, 1);

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            if (menu != null)
            {
                menu.SetActive(false);
                Destroy(menu);
            }
            connectionStatus = null;
            if (Session.IsOnline && (Session.OnlineWaiting || Session.Match.Phase == MatchPhase.Results))
            { ConnectionLobby(); return; }
            bool results = Session.Running && Session.Match.Phase == MatchPhase.Results;
            if (Session.Running && !Session.Paused && !results) return;
            menu = new GameObject("Menu", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            menu.transform.SetParent(canvas, false);
            Rect(menu, Vector2.zero, Vector2.one);
            menu.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .025f, .045f, .94f);
            string title = results ? "MATCH COMPLETE" : Session.Running ? "PAUSED" : "RAIL ARENA";
            Text(menu.transform, "Title", title, 72, new Vector2(.09f, .8f), new Vector2(.8f, .92f)).color = cyan;
            Text(menu.transform, "Subtitle", results ? $"WINNER: {Session.Actors[Session.Match.Winner].DisplayName}" :
                "FOUNDRY // ORBITAL COMBAT FACILITY", 24, new Vector2(.095f, .735f), new Vector2(.8f, .8f));
            float y = .66f;
            if (Session.Running)
            {
                if (!results) Button("RESUME", () => Session.SetPaused(false), ref y);
                if (!Session.IsOnline) Button(results ? "REMATCH" : "RESTART SESSION", () => Session.StartSession(Session.Practice, bots, difficulty, scenario), ref y);
            }
            else
            {
                Button(bots == 0 ? "BOT MATCH // SELECT 1+ BOT" : "OFFLINE BOT MATCH",
                    () => Session.StartSession(false, bots, difficulty, scenario), ref y, bots > 0);
                Button(bots == 0 ? "SOLO PRACTICE // UNTIMED" : "PRACTICE // UNTIMED",
                    () => Session.StartSession(true, bots, difficulty, scenario), ref y);
                Button("ONLINE DUEL // DIRECT IP", OnlineMenu, ref y);
            }
            if (!Session.IsOnline)
            {
            Button(bots == 0 ? "BOTS  0 // SOLO   +" : $"BOTS  {bots}   +", () =>
            { bots = !Session.Running || Session.Practice ? (bots + 1) % 8 : bots % 7 + 1; RefreshMenu(); }, ref y);
            Button($"DIFFICULTY  {new[] { "CADET", "VETERAN", "ACE" }[difficulty]}", () => { difficulty = (difficulty + 1) % 3; RefreshMenu(); }, ref y);
            Button($"TARGETS  {scenario.ToString().ToUpperInvariant()}", () => { scenario = (TargetScenario)(((int)scenario + 1) % 3); RefreshMenu(); }, ref y);
            }
            Button("SETTINGS", Settings, ref y);
            if (Session.Running) Button("MAIN MENU", Session.MainMenu, ref y);
            Button("QUIT", Session.QuitGame, ref y);
            Text(menu.transform, "Controls", "W A S D  MOVE     SPACE  JUMP     MOUSE  AIM / FIRE\nESC  PAUSE     TAB  SCOREBOARD\n\nUnlimited energy. Click to fire. 0.6 second cycle.\nHOLD RIGHT MOUSE  1.5x ZOOM\nHEAD 100   /   BODY 50   /   LIMB 34", 22,
                new Vector2(.53f, .24f), new Vector2(.94f, .64f));
            Text(menu.transform, "Version", "IN DEVELOPMENT  //  OFFLINE / DIRECT-IP PLAYTEST", 18,
                new Vector2(.095f, .035f), new Vector2(.9f, .085f)).color = new Color(.55f, .63f, .7f);
            if (!string.IsNullOrEmpty(Session.ConnectionError)) Text(menu.transform, "Connection error", Session.ConnectionError, 22,
                new Vector2(.53f, .12f), new Vector2(.94f, .22f)).color = new Color(1, .65f, .3f);
        }

        private void Settings()
        {
            foreach (Transform child in menu.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Text(menu.transform, "Title", "SETTINGS", 64, new Vector2(.1f, .78f), new Vector2(.8f, .9f));
            Text(menu.transform, "Aim heading", "AIM / CAMERA", 22, new Vector2(.1f,.71f), new Vector2(.48f,.76f)).color = cyan;
            Text(menu.transform, "Display heading", "AUDIO / DISPLAY", 22, new Vector2(.55f,.71f), new Vector2(.94f,.76f)).color = cyan;
            string SensitivityValue(float value) => value.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            Slider("Mouse sensitivity", Session.Sensitivity, AimPreferences.MinimumSensitivity, AimPreferences.MaximumSensitivity, .61f, SensitivityValue, value =>
            { Session.Sensitivity = value; Session.Aim.Save(); });
            Slider("Zoom sensitivity", Session.Aim.ZoomSensitivity, AimPreferences.MinimumSensitivity, AimPreferences.MaximumSensitivity, .48f, SensitivityValue, value =>
            { Session.Aim.ZoomSensitivity = value; Session.Aim.Save(); });
            Slider("Vertical FOV", Session.Aim.FieldOfView, AimPreferences.MinimumFov, AimPreferences.MaximumFov, .35f, value => Mathf.RoundToInt(value) + " degrees", value =>
            { Session.Aim.FieldOfView = value; Session.Aim.Save(); }, wholeNumbers: true);
            Slider("Master volume", AudioListener.volume, 0, 1, .61f, value => Mathf.RoundToInt(value * 100) + "%", value =>
            { AudioListener.volume = value; PlayerPrefs.SetFloat("volume", value); }, .55f, .91f);
            float y = .48f;
            Button("1920 x 1080", () => Screen.SetResolution(1920, 1080, Screen.fullScreenMode), ref y, minX: .55f, maxX: .91f);
            Button("1280 x 720", () => Screen.SetResolution(1280, 720, Screen.fullScreenMode), ref y, minX: .55f, maxX: .91f);
            Button("TOGGLE FULLSCREEN", () => Screen.fullScreen = !Screen.fullScreen, ref y, minX: .55f, maxX: .91f);
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
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.14f, .2f, .24f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(root.transform, false);
            Rect(fill, Vector2.zero, Vector2.one);
            fill.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f, .5f, .57f);
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
            handle.GetComponent<UnityEngine.UI.Image>().color = cyan;
            var slider = root.GetComponent<UnityEngine.UI.Slider>();
            slider.handleRect = handleRect;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
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
            UpdateCombatHud();
            if (Session.Running && Session.Match.Phase == MatchPhase.Results && menu == null) RefreshMenu();
        }

        private UnityEngine.UI.Button Button(string title, UnityEngine.Events.UnityAction action, ref float y, bool interactable = true, float minX = .095f, float maxX = .46f)
        {
            var root = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(minX, y), new Vector2(maxX, y + .055f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.055f, .1f, .15f);
            var button = root.GetComponent<UnityEngine.UI.Button>();
            button.interactable = interactable;
            button.onClick.AddListener(action);
            Text(root.transform, "Label", title, 24, new Vector2(.035f, 0), Vector2.one).alignment = TextAnchor.MiddleLeft;
            y -= .073f;
            return button;
        }

        private UnityEngine.UI.Text Text(Transform parent, string name, string value, int size, Vector2 min, Vector2 max)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            root.transform.SetParent(parent, false);
            Rect(root, min, max);
            var text = root.GetComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = size;
            text.color = new Color(.91f, .94f, .96f);
            text.text = value;
            text.raycastTarget = false;
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
