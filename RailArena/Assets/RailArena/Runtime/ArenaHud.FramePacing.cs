using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private void FramePacingSettings()
        {
            foreach (Transform child in menu.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
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
            Text(menu.transform, "Pacing scope", "Gameplay simulation stays at 60 ticks per second. Default: VSync off, 144 FPS limit.", 20,
                new Vector2(.1f,.18f), new Vector2(.94f,.24f));
            y = .1f; Button("BACK TO SETTINGS", Settings, ref y);
        }
    }
}
