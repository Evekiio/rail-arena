using RailArena.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private GameObject combatRoot, crosshairRoot, scoreboardPanel;
        private UnityEngine.UI.Text healthValue, healthGain, railValue, modeValue, clockValue, statsValue, notice;
        private UnityEngine.UI.Image healthFill, railFill;
        private readonly UnityEngine.UI.Image[] hitMarks = new UnityEngine.UI.Image[4];
        private readonly UnityEngine.UI.Image[] damageEdges = new UnityEngine.UI.Image[4];
        private GameObject hitMarker;
        private GameObject feedPanel;
        private readonly UnityEngine.UI.Text[] feedRows = new UnityEngine.UI.Text[CombatFeed.Capacity];
        private readonly Color amber = new(1, .7f, .3f);
        private readonly Color warning = new(1, .35f, .3f);

        private UnityEngine.UI.RawImage movementIcon;
        private UnityEngine.UI.Text movementLabel;
        private string displayedMovement;

        private void BuildCombatHud()
        {
            combatRoot = new GameObject("CombatHud", typeof(RectTransform));
            combatRoot.transform.SetParent(canvas, false);
            Rect(combatRoot, Vector2.zero, Vector2.one);
            BuildFeedbackHud();
            var health = Card("HealthPanel", new Vector2(.035f,.045f), new Vector2(.215f,.14f));
            Icon(health,"Health icon","health",new Vector2(.055f,.33f),new Vector2(.205f,.83f),cyan);
            Text(health,"HealthLabel","HEALTH",15,new Vector2(.27f,.73f),new Vector2(.90f,.98f)).color=Muted;
            healthValue=Text(health,"HealthValue","100",48,new Vector2(.27f,.16f),new Vector2(.69f,.90f));
            healthGain=Text(health,"HealthGain","",26,new Vector2(.68f,.30f),new Vector2(.94f,.80f));
            healthGain.alignment=TextAnchor.MiddleRight;
            healthFill=Meter(health,"HealthFill",new Vector2(.06f,.09f),new Vector2(.94f,.15f));

            var stats=Card("StatsPanel",new Vector2(.035f,.89f),new Vector2(.19f,.97f));
            Icon(stats,"Score icon","score",new Vector2(.055f,.54f),new Vector2(.19f,.90f),cyan);
            Icon(stats,"Speed icon","sprint",new Vector2(.055f,.13f),new Vector2(.19f,.49f),Muted);
            statsValue=Text(stats,"StatsValue","",20,new Vector2(.26f,.10f),new Vector2(.96f,.94f));
            statsValue.alignment=TextAnchor.MiddleLeft;

            var movement=Card("MovementPanel",new Vector2(.225f,.045f),new Vector2(.335f,.13f));
            movementIcon=Icon(movement,"Movement icon","walk",new Vector2(.07f,.20f),new Vector2(.29f,.80f));
            movementLabel=Text(movement,"MovementValue","MOVE",22,new Vector2(.36f,.30f),new Vector2(.97f,.82f));
            Text(movement,"Movement heading","STANCE",12,new Vector2(.36f,.09f),new Vector2(.97f,.34f)).color=Muted;

            var rail=Card("RailPanel",new Vector2(.735f,.045f),new Vector2(.965f,.15f));
            Icon(rail,"Rail icon","rail",new Vector2(.045f,.33f),new Vector2(.15f,.80f),cyan);
            Text(rail,"RailLabel","AUTO CARBINE / 300 RPM",15,new Vector2(.205f,.72f),new Vector2(.88f,.97f)).color=Muted;
            Icon(rail,"Unlimited energy icon","unlimited",new Vector2(.90f,.72f),new Vector2(.97f,.96f),Muted);
            railValue=Text(rail,"RailValue","READY",30,new Vector2(.205f,.27f),new Vector2(.96f,.78f));
            railFill=Meter(rail,"RailFill",new Vector2(.045f,.10f),new Vector2(.96f,.17f));

            var match=Card("MatchPanel",new Vector2(.39f,.90f),new Vector2(.61f,.98f));
            modeValue=Text(match,"ModeValue","",15,new Vector2(.02f,.65f),new Vector2(.98f,.98f));
            modeValue.alignment=TextAnchor.MiddleCenter;
            Icon(match,"Time icon","clock",new Vector2(.15f,.15f),new Vector2(.28f,.60f),cyan);
            clockValue=Text(match,"ClockValue","",33,new Vector2(.32f,.05f),new Vector2(.95f,.72f));
            clockValue.alignment=TextAnchor.MiddleLeft;
            notice=Text(combatRoot.transform,"Notice","",30,new Vector2(.22f,.75f),new Vector2(.78f,.87f));
            notice.alignment=TextAnchor.MiddleCenter;
            feedPanel=Graphic(combatRoot.transform,"CombatFeedPanel",new Vector2(.715f,.69f),new Vector2(.965f,.88f),new Color(.02f,.035f,.045f,.60f)).gameObject;
            Icon(feedPanel.transform,"Feed icon","aim",new Vector2(.025f,.83f),new Vector2(.075f,.98f),cyan);
            Text(feedPanel.transform,"Feed heading","ELIMINATIONS",13,new Vector2(.10f,.85f),new Vector2(.96f,.99f)).color=Muted;
            for(int i=0;i<feedRows.Length;i++)
                feedRows[i]=Text(feedPanel.transform,"Feed row "+i,"",18,new Vector2(.04f,.57f-i*.27f),new Vector2(.96f,.83f-i*.27f));
            combatReticle=CreateReticle(combatRoot.transform,"Crosshair");
            crosshairRoot=combatReticle.Root.gameObject;
            CreateIlluminatedSight(combatRoot.transform);
            liveStandings=CreateStandings(combatRoot.transform,"ScoreboardPanel","FOUNDRY / LIVE STANDINGS",new Vector2(.12f,.23f),new Vector2(.88f,.77f));
            scoreboardPanel=liveStandings.Root;
            combatRoot.SetActive(false);
        }

        private Transform Card(string name, Vector2 min, Vector2 max)
        {
            var card = Graphic(combatRoot.transform, name, min, max, new Color(.025f, .040f, .052f, .82f)).transform;
            Graphic(card, "Accent", new Vector2(0, .987f), Vector2.one, new Color(cyan.r,cyan.g,cyan.b,.65f));
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
            string movement=human.IsSliding?"SLIDE":human.IsCrouched?"CROUCH":human.Command.Walk?"WALK":human.Command.Sprint&&human.Command.Move.y>0?"SPRINT":"MOVE";
            if(displayedMovement!=movement)
            {
                displayedMovement=movement;movementLabel.text=movement;
                movementIcon.texture=Resources.Load<Texture2D>("UI/Brand/Icons/"+((movement=="CROUCH"||movement=="SLIDE")?"crouch":movement=="SPRINT"?"sprint":"walk"));
            }
            int remaining = Mathf.Max(0, human.NextShotTick - Session.LocalSimulationTick);
            float ready = 1 - Mathf.Clamp01(remaining / (float)CombatRules.ShotIntervalTicks);
            bool locked = !match.IsActive || !human.Alive;
            var railColor = locked ? new Color(.55f, .63f, .7f) : remaining > 0 ? amber : cyan;
            healthValue.text = human.Health.ToString("000");
            healthValue.color = human.Health <= 34 ? warning : Color.white;
            healthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(human.Health / 100f), 1);
            healthFill.color = healthValue.color;
            float pickup=Session.Feedback.PickupStrength;
            healthGain.gameObject.SetActive(pickup>0 && human.Alive);
            if(healthGain.gameObject.activeSelf)
            {
                healthGain.text="+"+Session.Feedback.LastPickupAmount;
                healthGain.color=new Color(amber.r,amber.g,amber.b,pickup);
            }
            railValue.text = locked ? (human.Alive ? "STAND BY" : "OFFLINE") : remaining == 0 ? "READY" :
                $"CYCLE // {Mathf.Ceil(remaining / 6f) / 10f:0.0}s";
            railValue.color = railColor;
            railFill.rectTransform.anchorMax = new Vector2(locked ? 0 : ready, 1);
            railFill.color = railColor;
            combatReticle.Apply(Session.Reticle, locked);
            crosshairRoot.SetActive(human.Alive && Session.AimBlend < .5f);
            illuminatedSight.SetActive(human.Alive && !locked && Session.AimBlend >= .5f);
            float hit = Session.Feedback.HitStrength;
            hitMarker.SetActive(hit > 0 && human.Alive);
            var hitColor = Session.Feedback.LastHitWasKill ? warning : Color.white;
            hitColor.a = hit;
            foreach (var mark in hitMarks) mark.color = hitColor;
            var damageColor = new Color(1, .12f, .06f, Session.Feedback.DamageStrength * Session.Comfort.DamageEdgeOpacity);
            foreach (var edge in damageEdges) edge.color = damageColor;
            statsValue.text = $"SCORE  {match.Scores[human.Id]}\nSPEED  {new Vector2(human.Velocity.x, human.Velocity.z).magnitude:0.0}";
            modeValue.text = Session.IsOnline ? "DIRECT-IP DUEL" : Session.Practice ?
                (Session.BotCount == 0 ? "SOLO PRACTICE" : $"PRACTICE // {Session.BotCount} BOTS") : $"BOT MATCH // {Session.BotCount} BOTS";
            int seconds = match.RemainingTicks / CombatRules.TickRate;
            clockValue.text = Session.Practice ? "UNTIMED" : $"{seconds / 60:00}:{seconds % 60:00}";
            notice.text = match.Phase == MatchPhase.Countdown ? $"STAND BY // {Mathf.CeilToInt((CombatRules.CountdownTicks - match.Tick) / (float)CombatRules.TickRate)}" :
                !human.Alive ? $"RESPAWNING // {Mathf.Max(0, human.RespawnTick - match.Tick) / (float)CombatRules.TickRate:0.0}s" :
                match.Phase == MatchPhase.SuddenDeath ? "SUDDEN DEATH // NEXT KILL WINS" : "";
            double now=Time.realtimeSinceStartupAsDouble;
            Session.RecentCombat.Prune(now);
            var entries=Session.RecentCombat.Entries;
            bool showStandings=Keyboard.current?.tabKey.isPressed==true;
            feedPanel.SetActive(entries.Count>0 && !showStandings);
            for(int i=0;i<feedRows.Length;i++)
            {
                var row=feedRows[i];row.gameObject.SetActive(i<entries.Count);
                if(i>=entries.Count)continue;
                var entry=entries[entries.Count-1-i];
                row.text=entry.Text;
                row.color=new Color(.91f,.94f,.96f,Mathf.Clamp01((float)((entry.ExpiresAt-now)/.5)));
            }
            scoreboardPanel.SetActive(showStandings);
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
