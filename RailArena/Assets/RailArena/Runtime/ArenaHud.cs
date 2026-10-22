using System.Text;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RailArena
{
    public sealed partial class ArenaHud : MonoBehaviour
    {
        public ArenaSession Session;
        private Transform canvas;
        private GameObject menu;
        private UnityEngine.UI.Text status;
        private UnityEngine.UI.Text notice;
        private UnityEngine.UI.Text score;
        private UnityEngine.UI.Text crosshair;
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
            status = Text(canvas, "Status", "", 30, new Vector2(.04f, .03f), new Vector2(.8f, .12f));
            notice = Text(canvas, "Notice", "", 24, new Vector2(.2f, .78f), new Vector2(.8f, .94f));
            notice.alignment = TextAnchor.MiddleCenter;
            score = Text(canvas, "Scoreboard", "", 25, new Vector2(.32f, .25f), new Vector2(.68f, .74f));
            crosshair = Text(canvas, "Crosshair", "+", 32, new Vector2(.48f, .48f), new Vector2(.52f, .52f));
            crosshair.alignment = TextAnchor.MiddleCenter;
            crosshair.color = cyan;
            RefreshMenu();
        }

        public void RefreshMenu()
        {
            if (canvas == null) return;
            if (menu != null) Destroy(menu);
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
                Button("OFFLINE BOT MATCH", () => Session.StartSession(false, bots, difficulty, scenario), ref y);
                Button("PRACTICE // UNTIMED", () => Session.StartSession(true, bots, difficulty, scenario), ref y);
                Button("ONLINE DUEL // DIRECT IP", OnlineMenu, ref y);
            }
            if (!Session.IsOnline)
            {
            Button($"BOTS  {bots}   +", () => { bots = bots % 7 + 1; RefreshMenu(); }, ref y);
            Button($"DIFFICULTY  {new[] { "CADET", "VETERAN", "ACE" }[difficulty]}", () => { difficulty = (difficulty + 1) % 3; RefreshMenu(); }, ref y);
            Button($"TARGETS  {scenario.ToString().ToUpperInvariant()}", () => { scenario = (TargetScenario)(((int)scenario + 1) % 3); RefreshMenu(); }, ref y);
            }
            Button("SETTINGS", Settings, ref y);
            if (Session.Running) Button("MAIN MENU", Session.MainMenu, ref y);
            else Button("QUIT", Application.Quit, ref y);
            Text(menu.transform, "Controls", "W A S D  MOVE     SPACE  JUMP     MOUSE  AIM / FIRE\nESC  PAUSE     TAB  SCOREBOARD\n\nUnlimited rail charges. 1.5 second cycle.\nHEAD 100   /   BODY 50   /   LIMB 34", 22,
                new Vector2(.53f, .24f), new Vector2(.94f, .64f));
            Text(menu.transform, "Version", "DEVELOPMENT BUILD  //  ONLINE INTEGRATION IN TEST", 18,
                new Vector2(.095f, .035f), new Vector2(.9f, .085f)).color = new Color(.55f, .63f, .7f);
            if (!string.IsNullOrEmpty(Session.ConnectionError)) Text(menu.transform, "Connection error", Session.ConnectionError, 22,
                new Vector2(.53f, .12f), new Vector2(.94f, .22f)).color = new Color(1, .65f, .3f);
        }

        private void Settings()
        {
            foreach (Transform child in menu.transform) Destroy(child.gameObject);
            Text(menu.transform, "Title", "SETTINGS", 64, new Vector2(.1f, .78f), new Vector2(.8f, .9f));
            Slider("Mouse sensitivity", Session.Sensitivity, .02f, .5f, .62f, value =>
            { Session.Sensitivity = value; PlayerPrefs.SetFloat("sensitivity", value); });
            Slider("Master volume", AudioListener.volume, 0, 1, .49f, value =>
            { AudioListener.volume = value; PlayerPrefs.SetFloat("volume", value); });
            float y = .35f;
            Button("1920 x 1080", () => Screen.SetResolution(1920, 1080, Screen.fullScreenMode), ref y);
            Button("1280 x 720", () => Screen.SetResolution(1280, 720, Screen.fullScreenMode), ref y);
            Button("TOGGLE FULLSCREEN", () => Screen.fullScreen = !Screen.fullScreen, ref y);
            Button("BACK", () => { PlayerPrefs.Save(); RefreshMenu(); }, ref y);
        }

        private void Slider(string title, float value, float min, float max, float y, UnityEngine.Events.UnityAction<float> changed)
        {
            Text(menu.transform, title, title, 28, new Vector2(.1f, y + .04f), new Vector2(.6f, y + .1f));
            var root = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Slider));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(.1f, y), new Vector2(.6f, y + .025f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.14f, .2f, .24f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            handle.transform.SetParent(root.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(22, 35);
            handle.GetComponent<UnityEngine.UI.Image>().color = cyan;
            var slider = root.GetComponent<UnityEngine.UI.Slider>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }

        private void Update()
        {
            if (status == null) return;
            if (connectionStatus != null) connectionStatus.text = Session.ConnectionStatus;
            bool running = Session.Running;
            crosshair.enabled = running && !Session.Paused && Session.Human.Alive;
            status.enabled = notice.enabled = running;
            score.enabled = running && (Keyboard.current?.tabKey.isPressed == true);
            if (!running) return;
            var match = Session.Match;
            float cycle = Mathf.Clamp01((Session.Human.NextShotTick - match.Tick) / 90f);
            int seconds = match.RemainingTicks / 60;
            string clock = Session.Practice ? "PRACTICE" : $"{seconds / 60:00}:{seconds % 60:00}";
            status.text = $"HEALTH  {Session.Human.Health:000}    |    RAIL  {(cycle > 0 ? $"{(1 - cycle) * 100:0}%" : "READY")}\n{clock}    |    SCORE  {match.Scores[Session.Human.Id]}    |    SPEED  {new Vector2(Session.Human.Velocity.x, Session.Human.Velocity.z).magnitude:0.0}";
            notice.text = match.Phase == MatchPhase.Countdown ? $"STAND BY\n{Mathf.CeilToInt((180 - match.Tick) / 60f)}" :
                match.Phase == MatchPhase.SuddenDeath ? "SUDDEN DEATH // NEXT KILL WINS\n" + Session.Feed :
                !Session.Human.Alive ? "RESPAWNING\n" + Session.Feed : Session.Feed;
            if (score.enabled)
            {
                var builder = new StringBuilder("FOUNDRY // SCOREBOARD\n\n");
                foreach (var actor in Session.Actors) builder.AppendLine($"{actor.DisplayName,-14} {match.Scores[actor.Id],4}");
                score.text = builder.ToString();
            }
            if (match.Phase == MatchPhase.Results && menu == null) RefreshMenu();
        }

        private void Button(string title, UnityEngine.Events.UnityAction action, ref float y)
        {
            var root = new GameObject(title, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(.095f, y), new Vector2(.46f, y + .055f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.055f, .1f, .15f);
            var button = root.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(action);
            Text(root.transform, "Label", title, 24, new Vector2(.035f, 0), Vector2.one).alignment = TextAnchor.MiddleLeft;
            y -= .073f;
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
