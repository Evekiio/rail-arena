using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private void AudioSettings()
        {
            BeginPage(Settings);
            Text(menu.transform, "Title", "AUDIO MIX", 64, new Vector2(.1f,.78f), new Vector2(.94f,.9f));
            Text(menu.transform, "Audio help", "Changes apply and save immediately. Test the rifle without starting a match.", 22,
                new Vector2(.1f,.69f), new Vector2(.94f,.76f));
            var sound = Session.Sound;
            var status = Text(menu.transform, "Audio status", "", 24, new Vector2(.53f,.29f), new Vector2(.94f,.37f));
            float y = .44f;
            var preview = Button("TEST RIFLE SOUND", () => Session.Feedback.PreviewShot(), ref y, minX:.53f, maxX:.94f);
            void Refresh()
            {
                bool muted = sound.Master <= 0 || sound.Effects <= 0;
                preview.interactable = !muted;
                status.text = muted ? "MUTED // RAISE BOTH LEVELS TO TEST" : "PREVIEW USES YOUR CURRENT LEVELS";
                status.color = cyan;
            }
            void Apply()
            {
                sound.Save(); sound.ApplyMaster(); Session.Feedback.ApplySound(); ApplyInterfaceSound(); Refresh();
            }
            string Percent(float value) => Mathf.RoundToInt(value * 100) + "%";
            Slider("Master volume", sound.Master, 0, 1, .55f, Percent, value => { sound.Master=value; Apply(); });
            Slider("Effects volume", sound.Effects, 0, 1, .36f, Percent, value => { sound.Effects=value; Apply(); });
            Text(menu.transform, "Audio scope", "Master controls all game sound.\nEffects controls shots, hits, eliminations,\ndamage, local health pickups and pad launches.", 23,
                new Vector2(.53f,.49f), new Vector2(.94f,.65f));
            Text(menu.transform, "Audio note", "The preview plays sound only: no shot, damage, score or visual effects.", 20,
                new Vector2(.1f,.19f), new Vector2(.94f,.25f));
            Refresh(); y=.1f; Button("BACK TO SETTINGS", Settings, ref y);
        }
    }
}
