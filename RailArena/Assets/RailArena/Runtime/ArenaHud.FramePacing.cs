using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private void FramePacingSettings()
        {
            BeginPage(Settings);
            Text(menu.transform, "Title", "FRAME PACING", 64, new Vector2(.1f,.78f), new Vector2(.94f,.9f));
            Text(menu.transform, "Frame help", "Choose a frame limit or synchronize with your display. Changes save immediately.", 22,
                new Vector2(.1f,.69f), new Vector2(.94f,.76f));
            var preferences = Session.FramePacing;
            float y = .55f;
            UnityEngine.UI.Button sync = null, limit = null;
            var status = Text(menu.transform, "Pacing status", "", 24, new Vector2(.1f,.25f), new Vector2(.94f,.32f));
            void Refresh()
            {
                sync.GetComponentInChildren<UnityEngine.UI.Text>().text = "VSYNC // " + (preferences.VSync ? "ON" : "OFF");
                limit.GetComponentInChildren<UnityEngine.UI.Text>().text = "FPS LIMIT // " + preferences.LimitLabel;
                limit.interactable = !preferences.VSync;
                status.text = preferences.VSync ? "ACTIVE // DISPLAY SYNC - FPS LIMIT INACTIVE" : "ACTIVE // " + preferences.LimitLabel;
                status.color = cyan;
            }
            sync = Button("VSYNC", () =>
            {
                preferences.VSync = !preferences.VSync; preferences.Save(); preferences.Apply(); Refresh();
            }, ref y);
            Text(menu.transform, "Sync description", "On synchronizes frames to display refresh.\nOverrides the FPS limit while enabled.\nMay add input latency.", 23, new Vector2(.53f,.48f), new Vector2(.94f,.63f));
            y = .36f;
            limit = Button("FPS LIMIT", () =>
            {
                preferences.CycleLimit(); preferences.Save(); preferences.Apply(); Refresh();
            }, ref y);
            Text(menu.transform, "Limit description", "60 / 120 / 144 / 240 FPS or unlimited.\nYour selected limit is retained with VSync on.\nA limit is a target, not a measured result.", 23, new Vector2(.53f,.29f), new Vector2(.94f,.44f));
            Refresh();
            float aaY = .18f;
            UnityEngine.UI.Button aa = null;
            void RefreshAntialiasing() => aa.GetComponentInChildren<UnityEngine.UI.Text>().text = "ANTIALIASING // " + (Session.Graphics.Smaa ? "SMAA HIGH" : "OFF");
            aa = Button("ANTIALIASING", () =>
            {
                Session.Graphics.Smaa = !Session.Graphics.Smaa; Session.Graphics.Save(); Session.Graphics.Apply(Session.View); RefreshAntialiasing();
            }, ref aaY, minX: .55f, maxX: .91f);
            RefreshAntialiasing();
            float tierY = .1f;
            UnityEngine.UI.Button tier = null;
            void RefreshTier() => tier.GetComponentInChildren<UnityEngine.UI.Text>().text = "GRAPHICS // " + Session.Graphics.Tier.ToString().ToUpperInvariant();
            tier = Button("GRAPHICS", () =>
            {
                Session.Graphics.Tier = GraphicsTierProfiles.Next(Session.Graphics.Tier);
                Session.Graphics.Save(); Session.Graphics.Apply(Session.View); RefreshTier();
            }, ref tierY, minX: .55f, maxX: .91f);
            RefreshTier();
            Text(menu.transform, "Pacing scope", "Simulation: 60 ticks/s. SMAA affects the main view.", 20,
                new Vector2(.1f,.18f), new Vector2(.49f,.24f));
            y = .1f; Button("BACK TO SETTINGS", Settings, ref y);
        }
    }
}
