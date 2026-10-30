# Physical ADS, sprint, walk and crouch

October 29, 2026. This creator-requested local playtest adds physical aiming and movement options to the existing baseline. It supersedes the earlier no-sprint restriction without migrating damage, regeneration or match rules.

## Controls and behavior

| Hold | Action |
| --- | --- |
| Right mouse | Bring the optic onto the camera axis over 0.18 seconds; 1.5x magnification and an illuminated mint aim point |
| Left Shift + forward movement | Sprint at 1.35 times base wish speed |
| Left Alt | Walk at 0.5 times base wish speed |
| Left Ctrl | Crouch at 0.45 times base wish speed; release to stand when clearance permits |

Base wish speed remains 10 m/s; the initial sprint/walk/crouch targets are 13.5/5/4.5 m/s. Multipliers live in the existing MovementTuning asset. Crouch takes precedence over walk, which takes precedence over sprint. Sprint requires forward input. Diagonal input is normalized. Grounded walk/crouch cap inherited horizontal speed; airborne momentum remains intact. Existing acceleration, friction, jump, pads, damage and shot cadence are unchanged.

ADS adds no movement-speed penalty, accuracy/spread change, fire delay or sprint lockout. Independent zoom sensitivity remains available. At full aim the optic is centered, sway/bob rotation is removed and recoil travels along the sight axis. The normal crosshair yields to the illuminated aim point. Pause, menu, death and life/round changes clear aiming state. This is a single camera view, with no scope render texture or second camera. The original exterior optic was opaque: first-person instances now hide its glass plates and remove the tube end caps on owned mesh copies, retaining the housing and rims. Source meshes and third-person weapons remain unchanged; instance meshes are released with the weapon.

Crouching reduces controller height from 1.8 m to 1.2 m and eye height from 1.58 m to 0.98 m, preserving the feet/base position. The model lowers its hips and solves bent legs onto the animated feet, retaining body scale and bone-bound hurtboxes. Standing checks the full upright capsule against world and other enabled actor controllers. A blocked release stays crouched, including its speed and eye height. Respawn/placement resets stance. This procedural pose still needs human motion review.

## Networking

Protocol version 5 carries sprint/walk/crouch input bits and authoritative crouch state. Authority, local prediction and reconciliation use the same stance/movement path. Opponent interpolation applies discrete crouch state, and historical hurtboxes retain the captured bone poses. Earlier protocol/build identities are rejected; both peers need this build. Movement modifiers remain client intent, with speed and clearance resolved by the shared simulation.

## Local build and validation

Validation uses `Builds/ADS-2026-10-29/Project`, separate from the supplied baseline. A fresh non-development player is built to `Builds/Competitive-ADS-2026-10-29/RailArena.exe`. Keep its data folder and DLLs beside it. Build/test results and controlled captures are recorded under `Validation/ADS-2026-10-29/` after qualification.

Automated checks cover speed precedence, diagonal input, backward sprint rejection, airborne momentum, blocked standing, stance reset, protocol flags/old-version rejection, optic alignment, 1.5x projection, crouch foot placement/body scale and pause/death behavior. Relevant existing movement/network/weapon/HUD tests are run alongside them. Runtime checks use a separate Development player so opt-in validation runners are available.

Qualification results:

- 59 distinct affected Editor cases have passing latest results across the recorded runs. The first capture failure was a test frame-scheduling issue and was corrected; the opaque sight discovered in the render was corrected and recaptured. Failed/diagnostic reports are retained rather than overwritten.
- All 37 standalone mechanics checks passed with zero recorded errors.
- A 45-second three-bot offline run passed: 200 shots, 15 deaths, two pickups, 22 pads, no application exceptions or void deaths, and reconciled statistics.
- Clean 45-second and impaired 60-second two-process duels passed with movement-mode cycling enabled. The host observed sprint/walk intent and live crouch for both actors; the client observed both actors' live crouch and its own sprint/walk intent. Both peers reported zero application errors and matching identities. Remote sprint/walk intent is not included in snapshots, so the client does not claim to observe those opponent flags.
- Impairment: 25 ms one-way delay, +/-10 ms jitter, 5% loss, seed 7191; 304 drops, 202 reorders and zero queue overflow. Maximum active client corrections were 0.612 m clean and 0.786 m impaired. These nondeterministic short runs do not prove a reliable improvement over the baseline or smooth competitive feel.
- Generated identity `b3bea61fe442532f41735217d7e99a0bb85c0d611d61863d0b6d847f683cb3db` verifies against the actual project source. The previous baseline executable remains unchanged. Test/build imports and captures ran in the isolated copy; the refreshed generated identity was synchronized to the main project.
- The final non-development player built successfully at `Builds/Competitive-ADS-2026-10-29/RailArena.exe` and passed the hidden menu-start/close check with exit code 0. Its runtime assembly excludes Development validation drivers. `PLAYTEST.txt` records controls and scope; `SHA256SUMS.txt` covers 191 shipped files. Detailed provenance is in `Validation/ADS-2026-10-29/playtest-provenance.json`.
- Preservation audit found no missing files among the 1,819 baseline inputs. Changes to existing inventoried files are limited to the intended gameplay, UI, protocol, validation, generated identity and documentation files; original scene/model/art inputs remain unchanged. Final process inspection found no owned ADS Editor/player/relay/copy job left running. Prior builds are retained.

The opt-in `-MovementModes` switch on the existing duel scripts requires observed movement activity in addition to the existing combat checks. Default validator behavior is retained, and non-development players contain no movement-mode automation. Full-match/rematch, long soaks and real-internet duels were not repeated for this playtest.

This is a local playtest, not a competitive balance or final-animation acceptance claim. Human keyboard/mouse feel, normal-speed visual/audio review, real-internet play and rendering-performance certification remain open. No visible game or Editor window is opened by the automated workflow.

## Controlled captures

[Hip view](../Validation/ADS-2026-10-29/Captures/Hip.png), [physical ADS](../Validation/ADS-2026-10-29/Captures/ADS.png), [crouched ADS](../Validation/ADS-2026-10-29/Captures/Crouched-ADS.png) and [procedural crouch body](../Validation/ADS-2026-10-29/Captures/Crouch-body.png) are batch-mode review captures, not native gameplay or motion approval.

## Sprint slide extension

The current source adds a momentum slide on sprint-to-crouch input, with protocol 6 state for prediction. See [SLIDE.md](SLIDE.md). Earlier build evidence above remains specific to its original build.
