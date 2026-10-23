using RailArena.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private GameObject combatRoot, crosshairRoot, scoreboardPanel;
        private UnityEngine.UI.Text healthValue, railValue, modeValue, clockValue, statsValue, notice;
        private UnityEngine.UI.Image healthFill, railFill;
        private readonly UnityEngine.UI.Image[] hitMarks = new UnityEngine.UI.Image[4];
        private readonly UnityEngine.UI.Image[] damageEdges = new UnityEngine.UI.Image[4];
        private GameObject hitMarker;
        private readonly Color amber = new(1, .7f, .3f);
        private readonly Color warning = new(1, .35f, .3f);

        private void BuildCombatHud()
        {
            combatRoot = new GameObject("CombatHud", typeof(RectTransform));
            combatRoot.transform.SetParent(canvas, false);
            Rect(combatRoot, Vector2.zero, Vector2.one);
            BuildFeedbackHud();
            var health = Card("HealthPanel", new Vector2(.035f, .04f), new Vector2(.22f, .18f));
            Text(health, "HealthLabel", "HEALTH", 19, new Vector2(.08f, .71f), new Vector2(.93f, .96f));
            healthValue = Text(health, "HealthValue", "100", 60, new Vector2(.08f, .2f), new Vector2(.92f, .79f));
            healthFill = Meter(health, "HealthFill", new Vector2(.08f, .1f), new Vector2(.92f, .17f));

            var stats = Card("StatsPanel", new Vector2(.235f, .04f), new Vector2(.365f, .18f));
            statsValue = Text(stats, "StatsValue", "", 25, new Vector2(.1f, .1f), new Vector2(.95f, .93f));
            statsValue.alignment = TextAnchor.MiddleLeft;

            var rail = Card("RailPanel", new Vector2(.385f, .04f), new Vector2(.615f, .18f));
            Text(rail, "RailLabel", "RAIL // UNLIMITED", 19, new Vector2(.07f, .71f), new Vector2(.95f, .96f));
            railValue = Text(rail, "RailValue", "READY", 36, new Vector2(.07f, .3f), new Vector2(.95f, .78f));
            railFill = Meter(rail, "RailFill", new Vector2(.07f, .12f), new Vector2(.93f, .22f));

            var match = Card("MatchPanel", new Vector2(.38f, .89f), new Vector2(.62f, .98f));
            modeValue = Text(match, "ModeValue", "", 19, new Vector2(.02f, .64f), new Vector2(.98f, .99f));
            modeValue.alignment = TextAnchor.MiddleCenter;
            clockValue = Text(match, "ClockValue", "", 42, new Vector2(.02f, .03f), new Vector2(.98f, .7f));
            clockValue.alignment = TextAnchor.MiddleCenter;
            notice = Text(combatRoot.transform, "Notice", "", 28, new Vector2(.22f, .75f), new Vector2(.78f, .87f));
            notice.alignment = TextAnchor.MiddleCenter;

            combatReticle = CreateReticle(combatRoot.transform, "Crosshair");
            crosshairRoot = combatReticle.Root.gameObject;
            liveStandings=CreateStandings(combatRoot.transform,"ScoreboardPanel","FOUNDRY / LIVE STANDINGS",new Vector2(.12f,.23f),new Vector2(.88f,.77f));
            scoreboardPanel=liveStandings.Root;
            combatRoot.SetActive(false);
        }

        private Transform Card(string name, Vector2 min, Vector2 max)
        {
            var card = Graphic(combatRoot.transform, name, min, max, new Color(.012f, .024f, .04f, .87f)).transform;
            Graphic(card, "Accent", new Vector2(0, .97f), Vector2.one, cyan);
            return card;
        }

        private UnityEngine.UI.Image Meter(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var track = Graphic(parent, name + "Track", min, max, new Color(.12f, .19f, .23f));
            return Graphic(track.transform, name, Vector2.zero, Vector2.one, cyan);
        }

        private UnityEngine.UI.Image Graphic(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            root.transform.SetParent(parent, false); Rect(root, min, max);
            var image = root.GetComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private void UpdateCombatHud()
        {
            bool visible = Session.Running && !Session.Paused && !Session.OnlineWaiting && Session.Match.Phase != MatchPhase.Results;
            combatRoot.SetActive(visible);
            if (!visible) return;
            var human = Session.Human; var match = Session.Match;
            int remaining = Mathf.Max(0, human.NextShotTick - Session.LocalSimulationTick);
            float ready = 1 - Mathf.Clamp01(remaining / (float)CombatRules.ShotIntervalTicks);
            bool locked = !match.IsActive || !human.Alive;
            var railColor = locked ? new Color(.55f, .63f, .7f) : remaining > 0 ? amber : cyan;
            healthValue.text = human.Health.ToString("000");
            healthValue.color = human.Health <= 34 ? warning : Color.white;
            healthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(human.Health / 100f), 1);
            healthFill.color = healthValue.color;
            railValue.text = locked ? (human.Alive ? "STAND BY" : "OFFLINE") : remaining == 0 ? "READY" :
                $"CYCLE // {Mathf.Ceil(remaining / 6f) / 10f:0.0}s";
            railValue.color = railColor;
            railFill.rectTransform.anchorMax = new Vector2(locked ? 0 : ready, 1);
            railFill.color = railColor;
            combatReticle.Apply(Session.Reticle, locked);
            crosshairRoot.SetActive(human.Alive);
            float hit = Session.Feedback.HitStrength;
            hitMarker.SetActive(hit > 0 && human.Alive);
            var hitColor = Session.Feedback.LastHitWasKill ? warning : Color.white;
            hitColor.a = hit;
            foreach (var mark in hitMarks) mark.color = hitColor;
            var damageColor = new Color(1, .12f, .06f, Session.Feedback.DamageStrength * .7f);
            foreach (var edge in damageEdges) edge.color = damageColor;
            statsValue.text = $"SCORE  {match.Scores[human.Id]}\nSPEED  {new Vector2(human.Velocity.x, human.Velocity.z).magnitude:0.0}";
            modeValue.text = Session.IsOnline ? "DIRECT-IP DUEL" : Session.Practice ?
                (Session.BotCount == 0 ? "SOLO PRACTICE" : $"PRACTICE // {Session.BotCount} BOTS") : $"BOT MATCH // {Session.BotCount} BOTS";
            int seconds = match.RemainingTicks / CombatRules.TickRate;
            clockValue.text = Session.Practice ? "UNTIMED" : $"{seconds / 60:00}:{seconds % 60:00}";
            notice.text = match.Phase == MatchPhase.Countdown ? $"STAND BY // {Mathf.CeilToInt((CombatRules.CountdownTicks - match.Tick) / (float)CombatRules.TickRate)}" :
                match.Phase == MatchPhase.SuddenDeath ? "SUDDEN DEATH // NEXT KILL WINS\n" + Session.Feed :
                !human.Alive ? $"RESPAWNING // {Mathf.Max(0, human.RespawnTick - match.Tick) / (float)CombatRules.TickRate:0.0}s\n" + Session.Feed : Session.Feed;
            scoreboardPanel.SetActive(Keyboard.current?.tabKey.isPressed == true);
            if (scoreboardPanel.activeSelf)
            {
                RefreshStandings(liveStandings);
            }
        }

        private void BuildFeedbackHud()
        {
            var bounds = new[] { new Vector4(0, 0, 1, .008f), new Vector4(0, .992f, 1, 1),
                new Vector4(0, 0, .0045f, 1), new Vector4(.9955f, 0, 1, 1) };
            for (int i = 0; i < bounds.Length; i++)
            {
                var b = bounds[i];
                damageEdges[i] = Graphic(combatRoot.transform, "DamageEdge" + i,
                    new Vector2(b.x, b.y), new Vector2(b.z, b.w), Color.clear);
            }
            hitMarker = new GameObject("HitConfirmation", typeof(RectTransform));
            hitMarker.transform.SetParent(combatRoot.transform, false);
            Rect(hitMarker, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            for (int i = 0; i < hitMarks.Length; i++)
            {
                float x = i % 2 == 0 ? -1 : 1, y = i < 2 ? -1 : 1;
                var mark = Graphic(hitMarker.transform, "HitMark" + i, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Color.white);
                mark.rectTransform.sizeDelta = new Vector2(13, 2.5f);
                mark.rectTransform.anchoredPosition = new Vector2(x * 12, y * 12);
                mark.rectTransform.localRotation = Quaternion.Euler(0, 0, x * y > 0 ? 45 : -45);
                hitMarks[i] = mark;
            }
            hitMarker.SetActive(false);
        }
    }
}
