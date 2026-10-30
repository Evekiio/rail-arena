# Rail Arena design lock v1

Approved design: 2026-10-23. Creator: Sanders Riddle. This document supersedes conflicting gameplay requirements in earlier milestone documents. It specifies the target, not the current build's capabilities. Unity 6000.6.7f1, Windows keyboard/mouse, original assets, Git/LFS and headless automation remain required.

## Identity and scope

An accessible momentum-driven arena FPS: one precision rifle, readable opponents and quick matches. One bright floating Foundry arena, four cosmetic contenders, offline solo/practice/bot match and two-human direct-IP duel. No accounts, cloud service, spectators, monetization, progression, additional weapons, online bots or gamepad support in v1. Agent-assisted engineering and reproducible evidence support the portfolio; in-game bots are not LLM agents.

## Rules

| System | Approved target |
| --- | --- |
| Offline roster | One human, 0Ã¢â‚¬â€œ3 unique bots; use characters not selected by the human |
| Online roster | Two humans, distinct character selection negotiated in ready room |
| Practice | Untimed solo/stationary/moving/combat scenarios; unlimited respawns |
| Timed match | 300 seconds or leading score 20; tied leaders enter next-kill sudden death |
| Countdown / respawn | 3 seconds / 2 seconds |
| Rifle | Instant hit, 36-tick cycle at 60 Hz, one shot per click; unlimited ammo, no reload or firing charge-up |
| Damage | Head 75, body 40, limb 30; maximum health 100 |
| Regeneration | After 480 ticks without damage, restore 1 health per 30 ticks until 50; never lower health above 50 |
| Health pickups | Three sites; +25 up to 100; 1200-tick return |
| Zoom | Hold right mouse for 1.5Ãƒâ€” magnification; separate sensitivity; movement and accuracy unchanged |
| Movement | Accessible acceleration/air control, momentum preservation, hold-to-hop; sprint, walk and crouch added by the October 29 amendment; no stamina |
| Void | Death and minus one score |
| Spawn selection | Prefer occluded candidates, then larger nearest-opponent distance; no invulnerability |
| Statistics | Per-player kills, deaths, hits, shots and headshots; accuracy = hits/shots, zero when no shots |

Damage resets regeneration delay and its fractional accumulator. Dead actors do not regenerate; respawn resets the timer. Only accepted shots increment shots, only a damage-confirmed hit increments hits/headshots. Practice has statistics but no time/score ending. Cooldown clicks are discarded, never queued. Sample clicks into one consumable simulation command; redundant network commands must not repeat a consumed click. Bot shots are deliberate discrete decisions, not an always-held trigger.

Default WASD, Space, left-fire, hold-right-zoom, Tab scoreboard, Escape pause. Base FOV default retains the current 100 vertical degrees until human comfort review; adjustable 60Ã¢â‚¬â€œ110 vertical degrees. Magnification uses perspective projection: zoom FOV = 2 atan(tan(base FOV/2)/1.5). Zoom and rebindings are settings, not movement tuning changes.

## Map contract

The [Foundry arena design v2](ARENA-DESIGN.md) is the current detailed map proposal. Creator priorities are duel-first, flowing routes with deliberate exposure, and opaque shot-blocking portals. Its Map.json supersedes v1 placement proposals; exact geometry and traversal values still require layout/controller approval.

Keep 56m east-west Ãƒâ€” 40m north-south. Lower circulation 0m, opposing wing landings +2m and central bridge +4m. Four continuous ramps, four pads, three health sites and eight spawn candidates. Primary routes and resources are 180-degree rotationally paired. Use deliberate open gaps, clear edge markings, sparse cover and no all-map dominant perch. Art detail does not add collision. Actual-controller testing is required; visual symmetry alone does not prove balance.

One fixed bidirectional portal pair connects mirrored lower-route pockets. Preserve speed and rotate velocity/view into the destination frame. Spawn and pickup positions must not occupy the exit lane. Suppress traversal until the actor leaves its arrival trigger; blocked clearance cancels traversal without changing pose. Validate clearance before committing the position change. Portals must have an audible arrival cue and two escape directions. Historical hit tracing must treat portal traversal as a discontinuity, never interpolate a target through intervening space. Replicate traversal identity for prediction/view reset.

The greybox master defines one geometric proposal. Its exact coordinates are stored in Layout.json, with Blender Z-up and explicit Unity conversion (x,z,-y). It is not a replacement runtime scene until movement, spawn exposure and bot traversal tests pass.

## Art, animation and audio

Polished production low-poly: broad faceted silhouettes and selective bevels, ivory shells over graphite frames, teal recesses, cyan energy and copper hardware. Amber health markers use plus symbols as well as color. Approved character identities and anatomical left/right features remain as recorded in the character modeling guide. All four have equal gameplay dimensions. Offline has no duplicates; a duel rejects a conflicting ready selection.

First-person hands/forearms match the selected character. The creator-approved weapon is a 1.0m enclosed precision carbine: reinforced open stock, compact optic, long vented fore-end, fixed curved energy cassette and angled hand stop, in ivory/graphite with restrained cyan. See [v3 references](../ArtSource/Original/PrecisionCarbine-References-v3/README.md). Design approval was granted October 23, 2026. The [production package](../ArtSource/Original/PrecisionCarbine-Production-v1/README.md) is integrated with fitted hands and shoulder contact; final human visual acceptance and performance qualification remain open. Earlier rifle packages are historical. This art pass also aligns the older firing input, cooldown and zoom with the weapon contract; remaining gameplay-rule migration is separate.

Locomotion/jump/landing, two-hand rifle hold/fire/recovery, non-graphic hit/death and respawn states need pose references. First-person idle, locomotion, fire and zoom require contact diagrams. Use authored death fall and body fade within two seconds, no corpse collision. Small unobstructed opponent name/marker only; no wall outlines or health bars. Restrained blood and sparks; blood-off retains informative non-blood feedback. Effects may not obscure aiming.

Sharp futuristic rifle transient, short electromagnetic crack, restrained mechanical recovery; no shot delay. One menu music loop; quiet match ambience. Footsteps, landings, pads, portal arrival, pickup and cooldown readiness must remain distinguishable. Separate master/music/effects controls.

## UI and settings

Clean competitive sci-fi interface: health, recovery, score/time, reticle, kill feed and match phase. Include main menu, cosmetic choice, practice configuration, duel host/join/ready, HUD, pause, settings, scoreboard, results, errors and quick-start panel. No tutorial level. Include empty, disabled, disconnected, invalid-address, incompatible-build, full-room, duplicate-character and rematch states.

Rebindable keyboard/mouse; base/zoom sensitivity, invert Y, FOV; master/music/effects volume; resolution/window/fullscreen, VSync, FPS cap; reticle size/color, HUD scale, blood, reduced flash and weapon bob/shake. No motion blur by default. Settings must persist, reject invalid values and provide restore defaults. Keyboard navigation, focus states and text labels supplement color. Binding conflicts require explicit swap or cancel; never silently lose a mandatory action.

## Acceptance and production gates

1. Design lock: this approved target plus asset register and engineering migration checklist.
2. Greybox resolution: one metre-scale editable master; matching plans, sections and camera views; measure routes with actual controller before declaring balance.
3. Concept completion: all listed assets have consistent required views, materials, states and scale.
4. Creator approval: explicit per-family approval. Proposed hidden surfaces, generated art and a published image are not automatic approval. Missing or filtered artwork stays blocked.
5. Detailed modeling: only approved families, editable Blender sources, separate art/collision collections.
6. Integration: shared offline/online rules, versioned commands/snapshots/build identity, compatibility rejection and regression validation.

1080p/144 fps is a target, not a benchmark result. Record CPU/GPU/RAM/build/settings and frame-time distributions during real four-contender rendering. Hidden checks cannot establish visual quality, physical-input feel, audible quality, remote-internet reliability or final acceptance. Visible windows require permission; jobs need bounded timeouts and owned-process cleanup.

## October 29 movement and aiming amendment

The creator authorized physical ADS with a simple illuminated reticle, retained 1.5x magnification and existing accuracy/base movement, plus sprint, slow walking and crouch. This supersedes the earlier no-sprint restriction. See [implementation and local review](ADS-AND-MOVEMENT.md) for bindings, tuning, network compatibility and validation limits. These additions do not authorize unrelated damage, regeneration or match-rule migration.
