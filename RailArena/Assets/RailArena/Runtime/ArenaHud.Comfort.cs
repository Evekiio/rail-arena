using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private void ComfortSettings()
        {
            foreach(Transform child in menu.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Text(menu.transform,"Title","VISUAL COMFORT",64,new Vector2(.1f,.78f),new Vector2(.94f,.9f));
            Text(menu.transform,"Comfort help","Tune shot brightness and view-weapon movement. Changes save immediately.",22,
                new Vector2(.1f,.69f),new Vector2(.94f,.76f));
            float y=.55f;
            UnityEngine.UI.Button flash=null, motion=null;
            flash=Button("REDUCED FLASH // "+(Session.Comfort.ReducedFlash?"ON":"OFF"),() =>
            {
                Session.Comfort.ReducedFlash=!Session.Comfort.ReducedFlash; Session.Comfort.Save();
                flash.GetComponentInChildren<UnityEngine.UI.Text>().text="REDUCED FLASH // "+(Session.Comfort.ReducedFlash?"ON":"OFF");
            },ref y);
            Text(menu.transform,"Flash description","Removes muzzle and halo flashes.\nUses a shorter, dimmer beam and impact.\nReduces the damage-edge tint.",23,
                new Vector2(.53f,.48f),new Vector2(.94f,.63f));
            y=.34f;
            motion=Button("WEAPON MOTION // "+(Session.Comfort.WeaponMotion?"ON":"OFF"),() =>
            {
                Session.Comfort.WeaponMotion=!Session.Comfort.WeaponMotion; Session.Comfort.Save();
                motion.GetComponentInChildren<UnityEngine.UI.Text>().text="WEAPON MOTION // "+(Session.Comfort.WeaponMotion?"ON":"OFF");
            },ref y);
            Text(menu.transform,"Motion description","Off holds your hands and rifle steady.\nDisables view bob, sway, landing and recoil motion.\nMovement, aim and weapon timing stay the same.",23,
                new Vector2(.53f,.27f),new Vector2(.94f,.42f));
            Text(menu.transform,"Comfort scope","Shot sounds, hit markers and the recovery meter stay available.",20,
                new Vector2(.1f,.19f),new Vector2(.94f,.25f));
            y=.1f; Button("BACK TO SETTINGS",Settings,ref y);
        }
    }
}
