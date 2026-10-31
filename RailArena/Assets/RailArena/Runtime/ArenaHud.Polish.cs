using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private UnityEngine.UI.Image healthTrail, healthAccent, weaponAccent;
        private UnityEngine.UI.Text healthState, weaponPrompt, hitCaption;
        private readonly UnityEngine.UI.Image[] damageBands = new UnityEngine.UI.Image[24];
        private readonly UnityEngine.UI.Image[] magazineSegments = new UnityEngine.UI.Image[30];
        private float trailingHealth = 1, damageHold, stanceBlend;
        private string lastStance, lastNotice;
        private float noticeAge;
        private readonly string[] lastFeedText = new string[CombatFeed.Capacity];
        private readonly float[] feedAge = new float[CombatFeed.Capacity];
        private int previousHealth = -1;

        private void BuildCombatPolish()
        {
            var health = combatRoot.transform.Find("HealthPanel");
            healthAccent = health.Find("Accent").GetComponent<UnityEngine.UI.Image>();
            weaponAccent = combatRoot.transform.Find("RailPanel/Accent").GetComponent<UnityEngine.UI.Image>();
            var track = healthFill.transform.parent;
            healthTrail = Graphic(track, "RecentDamage", Vector2.zero, Vector2.one, amber);
            healthTrail.transform.SetAsFirstSibling();
            healthState = Text(health, "HealthState", "VITALS STABLE", 15, new Vector2(.27f,.01f), new Vector2(.94f,.22f));
            // Lift the bar to leave a dedicated baseline for the state label.
            var bar = (RectTransform)track; bar.anchorMin = new Vector2(.06f,.24f); bar.anchorMax = new Vector2(.94f,.29f);
            weaponPrompt = Text(combatRoot.transform, "WeaponPrompt", "", 19, new Vector2(.39f,.34f), new Vector2(.61f,.38f));
            weaponPrompt.alignment = TextAnchor.MiddleCenter;
            hitCaption = Text(combatRoot.transform, "HitCaption", "", 15, new Vector2(.43f,.445f), new Vector2(.57f,.47f));
            hitCaption.alignment = TextAnchor.MiddleCenter;
            var weapon=combatRoot.transform.Find("RailPanel");
            for(int i=0;i<magazineSegments.Length;i++)
                magazineSegments[i]=Graphic(weapon,"MagazineSegment"+i,new Vector2(.205f+i*.025f,.23f),new Vector2(.223f+i*.025f,.26f),cyan);
            // Nested edge strips form a soft falloff without a fullscreen flash or an aim obstruction.
            for (int edge=0; edge<4; edge++)
            for (int band=0; band<6; band++)
            {
                float a=band*.006f, b=(band+1)*.006f;
                Vector2 min, max;
                if(edge==0) { min=new Vector2(0,a); max=new Vector2(1,b); }
                else if(edge==1) { min=new Vector2(0,1-b); max=new Vector2(1,1-a); }
                else if(edge==2) { min=new Vector2(a,.036f); max=new Vector2(b,.964f); }
                else { min=new Vector2(1-b,.036f); max=new Vector2(1-a,.964f); }
                var image=Graphic(combatRoot.transform,"DamageFalloff"+edge+"_"+band,min,max,Color.clear);
                image.transform.SetSiblingIndex(0);
                damageBands[edge*6+band]=image;
            }
        }

        private void ResetCombatPolish()
        {
            previousHealth=-1; lastStance=null; lastNotice=null; noticeAge=0;
            damageHold=0;
        }

        private void UpdateCombatPolish(int health, bool alive, bool reloading, bool locked, int rounds)
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f), now=Time.unscaledTime;
            float fraction=Mathf.Clamp01(health/100f);
            if(previousHealth<0 || !alive || health>previousHealth) trailingHealth=fraction;
            else if(health<previousHealth) damageHold=.35f;
            previousHealth=health;
            if(damageHold>0) damageHold=Mathf.Max(0,damageHold-dt);
            else trailingHealth=Mathf.MoveTowards(trailingHealth,fraction,dt*.65f);
            healthTrail.rectTransform.anchorMax=new Vector2(Mathf.Max(fraction,trailingHealth),1);
            healthTrail.color=new Color(1,.55f,.22f,.9f);
            bool danger=alive && health<=34;
            float pulse=Session.Comfort.ReducedFlash?0:(.5f+.5f*Mathf.Sin(now*4));
            Color vital=!alive?Muted:danger?warning:cyan;
            healthState.text=!alive?"SIGNAL LOST":danger?"CRITICAL // FIND HEALTH":health<70?"VITALS COMPROMISED":"VITALS STABLE";
            healthState.color=vital;
            healthAccent.color=new Color(vital.r,vital.g,vital.b,danger?.65f+.25f*pulse:.8f);
            float damage=alive?Session.Feedback.DamageStrength:0;
            for(int i=0;i<damageBands.Length;i++)
            {
                float falloff=1-i%6/6f;
                damageBands[i].color=new Color(1,.14f,.08f,damage*Session.Comfort.DamageEdgeOpacity*.32f*falloff*falloff);
            }
            float pickup=Session.Feedback.PickupStrength;
            healthGain.rectTransform.anchoredPosition=new Vector2(0,(1-pickup)*10);
            weaponAccent.color=locked?Muted:reloading?amber:rounds<=5?warning:cyan;
            weaponPrompt.text=locked?"":reloading?"RELOADING":rounds==0?"EMPTY   [ R ] RELOAD":rounds<=5?"LOW AMMO   [ R ] RELOAD":"";
            weaponPrompt.color=reloading?amber:warning;
            for(int i=0;i<magazineSegments.Length;i++)
                magazineSegments[i].color=i<rounds?(rounds<=5?warning:cyan):new Color(.16f,.23f,.27f,.65f);
            if(lastStance!=displayedMovement) {lastStance=displayedMovement;stanceBlend=1;}
            stanceBlend=Mathf.MoveTowards(stanceBlend,0,dt*5);
            movementIcon.color=Color.Lerp(cyan,Color.white,Session.Comfort.ReducedFlash?0:stanceBlend);
            movementLabel.color=(displayedMovement=="SPRINT" || displayedMovement=="SLIDE" ? amber : Ivory);
            if(lastNotice!=notice.text) {lastNotice=notice.text;noticeAge=0;}
            noticeAge+=dt;
            notice.rectTransform.anchoredPosition=new Vector2(0,Session.Comfort.ReducedFlash?0:Mathf.Max(0,1-noticeAge*6)*8);
            notice.color=!alive?warning:Ivory;
            for(int i=0;i<feedRows.Length;i++)
            {
                if(lastFeedText[i]!=feedRows[i].text) {lastFeedText[i]=feedRows[i].text;feedAge[i]=0;}
                feedAge[i]+=dt;
                feedRows[i].rectTransform.anchoredPosition=new Vector2(Session.Comfort.ReducedFlash?0:Mathf.Max(0,1-feedAge[i]*6)*12,0);
            }
            float hit=Session.Feedback.HitStrength;
            hitCaption.text=hit>0 && alive?(Session.Feedback.LastHitWasKill?"ELIMINATED":"HIT CONFIRMED"):"";
            hitCaption.color=new Color(1,Session.Feedback.LastHitWasKill?.45f:1,.75f,hit);
            float scale=Session.Comfort.ReducedFlash?1:1+(1-hit)*.16f;
            hitMarker.transform.localScale=Vector3.one*scale;
        }
    }
}
