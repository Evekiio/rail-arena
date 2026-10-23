using System.Globalization;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private static string RecoverySeconds => ((float)CombatRules.ShotIntervalTicks/CombatRules.TickRate).ToString("0.0",CultureInfo.InvariantCulture);
        private static string WeaponSummary => $"ONE RIFLE. KEEP MOVING.\nUnlimited energy / {RecoverySeconds} second recovery\n"+
            $"HEAD {CombatRules.Damage(HitRegion.Head)}   /   BODY {CombatRules.Damage(HitRegion.Body)}   /   LIMB {CombatRules.Damage(HitRegion.Limb)}";

        private void QuickStart()
        {
            foreach(Transform child in menu.transform) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            Text(menu.transform,"Title","HOW TO PLAY",64,new Vector2(.1f,.80f),new Vector2(.94f,.92f)).color=cyan;
            var subtitle=Text(menu.transform,"Quick start subtitle",Session.IsOnline?
                "ONLINE MATCHES KEEP RUNNING WHILE MENUS ARE OPEN":"ONE RIFLE. FIND YOUR RHYTHM. KEEP MOVING.",22,
                new Vector2(.1f,.73f),new Vector2(.94f,.79f));
            subtitle.color=Session.IsOnline?amber:Color.white;
            QuickCard("Controls","MOVE / AIM", "W A S D  /  Move\nSPACE  /  Jump; hold to keep hopping\nLEFT MOUSE  /  One shot per click\nHOLD RIGHT MOUSE  /  1.5x zoom\nTAB  /  Hold for live standings\nESC  /  Pause or resume",.095f,.49f);
            int minutes=CombatRules.MatchTicks/(CombatRules.TickRate*60);
            QuickCard("Modes","CHOOSE YOUR SESSION", "SOLO PRACTICE  /  Explore with zero bots\nTARGET DRILLS  /  Stationary or moving bots\nCOMBAT PRACTICE  /  Untimed opponents\n"+$"BOT MATCH  /  1-{CombatRules.MaximumBots} bots; combat AI\n"+
                $"MATCHES  /  {minutes} minutes; most points wins\nTIED LEADERS  /  Next kill wins",.53f,.94f);
            Text(menu.transform,"Combat basics",$"Click when the rail meter is READY. Recovery takes {RecoverySeconds}s; holding fire will not repeat.\n"+
                $"Amber health restores +25, up to {CombatRules.MaximumHealth}. It returns after {CombatRules.PickupRespawnTicks/CombatRules.TickRate}s.\n"+
                $"Eliminate opponents for points. Void falls cost one point. Respawn takes {CombatRules.RespawnTicks/CombatRules.TickRate}s.",23,
                new Vector2(.1f,.29f),new Vector2(.94f,.42f));
            float y=.20f;
            if(Session.Running)Button("RESUME",()=>Session.SetPaused(false),ref y);
            else Button("START SOLO PRACTICE",()=>Session.StartSession(true,0,1,TargetScenario.Stationary),ref y);
            y=.1f;Button("BACK",RefreshMenu,ref y);
            Text(menu.transform,"Quick start footer","Aim, reticle and visual comfort controls are available in Settings.",20,
                new Vector2(.53f,.1f),new Vector2(.94f,.22f));
        }

        private void QuickCard(string name,string title,string body,float minX,float maxX)
        {
            var card=Graphic(menu.transform,name+" card",new Vector2(minX,.45f),new Vector2(maxX,.69f),new Color(.035f,.065f,.09f));
            Text(card.transform,name+" heading",title,22,new Vector2(.035f,.80f),new Vector2(.97f,.99f)).color=cyan;
            Text(card.transform,name+" body",body,24,new Vector2(.035f,.02f),new Vector2(.97f,.79f));
        }
    }
}
