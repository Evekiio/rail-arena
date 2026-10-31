# Review 29 HUD presentation

This scoped milestone polishes the existing uGUI combat HUD. The broader art-production goal remains paused; final artistic, sound and native-input acceptance belongs to Sanders.

The exact health number and primary health meter remain authoritative and immediate. A separate amber trail holds recent damage for 350 ms, then catches up at 65 health per second. Healing and respawn reset the trail. The state label distinguishes stable, compromised, critical and lost signals without relying only on colour.

Damage uses a restrained six-step edge falloff, leaving the aiming area clear. It follows the existing damage cue and reduced-flash opacity. Critical-health accent breathing and hit-marker scaling stop with Reduced Flash. Positive pickup feedback rises gently; existing gain amounts and damage cancellation are retained. Low-ammo, empty and reload prompts sit below the aiming area. Existing reload progress, movement icons, match clock, score feed and scoreboard remain available.

No damage, movement, spread, reload cadence, weapon timing, multiplayer messages, portal logic or reticle bounds change. All added graphics ignore raycasts. Hidden-menu transitions reset the damage trail so stale combat feedback does not carry into another session.

Validation uses offscreen captures of healthy, damaged, critical, healing, low-ammo, empty, reload, elimination, reduced-flash and death states at 1080p and 720p, plus the existing relevant HUD checks. These renders establish layout and state behaviour, not physical input or sound acceptance.

The magazine now has thirty readable segments. Stance changes briefly brighten the icon, while notices and elimination-feed entries settle over roughly 170–200 ms without moving the reticle. Repeated numeric countdown updates do not restart the entrance motion. All three final focused HUD tests passed; the magazine test also covers 2560×1080. [State gallery](../ArtSource/Original/ArtPass-v1/UI/HudPolish-29/index.html).
