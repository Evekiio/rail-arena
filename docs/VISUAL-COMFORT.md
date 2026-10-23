# Visual comfort controls

Open **Settings → Visual Comfort** from the main menu or pause menu. The two independent settings save immediately. Existing installations retain normal flashes and weapon motion until changed.

| Control | Behavior |
| --- | --- |
| Reduced Flash on | Removes muzzle and halo flashes; draws a dimmer, thinner beam for 0.14 seconds instead of 0.24; uses a subdued impact for up to 0.12 seconds; caps the damage-edge alpha at 0.175 instead of 0.7 |
| Weapon Motion off | Holds the first-person rifle at its fitted rest position; removes bob, look sway, landing dip, raise animation and recoil movement from the rifle and finger pose |

Reduced Flash applies to local and remote shot effects on this client, including already-active pulses on their next update. The fixed pool still holds twelve effects and adds no collision. Sounds, hit/elimination markers and the rail recovery meter remain available.

Weapon Motion affects cosmetic first-person pose only. The camera still follows the player and responds to mouse input. Character movement, hitboxes, aim rays, firing cadence, damage, zoom, third-person poses and simulation recoil state are unchanged. The rifle's internal recovery mechanism and energy indicator continue to show its cycle. Turning motion back on resumes normal presentation.

Both settings are local and require no new network fields. The source compatibility identity still changes when runtime code changes; use matching current builds for duels. The public pre-alpha ZIP remains the earlier checkpoint.

## Evidence and remaining review

[Review gallery](../Validation/VisualComfort/index.html) pairs normal/reduced shot renders from the same isolated camera, and includes settings/live-HUD layouts at 720p and 1080p. The shot captures use actual pooled LineRenderers at initial emission in a controlled neutral scene without bloom. HUD captures draw the actual uGUI meshes/materials against a neutral background, with injected feedback events. They are not human match records or standalone gameplay screenshots.

Tests cover preference defaults/persistence, isolated effect visibility and reduced image intensity, fixed pool/collision invariants, settings navigation, readable text, live damage-edge limits, hand/rifle rest fitting with motion disabled, restoration of motion, and unchanged camera orientation. Built-player mechanics, four-contender combat and loopback-duel checks cover ordinary play after the change. Human comfort, physical-input feel, external-peer reliability and measured 1080p/144fps qualification remain open.
