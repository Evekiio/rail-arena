# Offline setup and four-contender roster

The current source supports one human with **0–3 bots**, with no more than four contenders in a session. This implements the approved population limit; the earlier public pre-alpha download remains unchanged.

The main menu separates launch actions from offline setup. Choose **Character** to open the [visual picker](CHARACTER-PICKER.md) and select among the four existing appearances: player/ivory, bot 01/ember, bot 02/teal and bot 03/plum. The choice is saved between runs. Bots take the remaining appearances in a stable cyclic order, without duplicates. All four choices retain the same movement controller and damage-collider dimensions.

Character choice changes the third-person body and the palette of the existing first-person hand/forearm rig. It does not add new arm geometry, damage bonuses or movement properties. During a paused session, the setup and picker describe the **Next session** choice: changes apply on restart/rematch, never as a live body swap.

Set bots to zero for solo practice. At zero, the timed bot-match action is disabled; practice remains available. Untimed practice supports combat, stationary or moving targets. Timed bot matches always use combat AI, so selecting a practice target drill cannot silently create a stationary-opponent competitive match. Direct calls also clamp populations to valid mode bounds.

Direct-IP duel continues to use exactly two humans and its existing fixed host/challenger appearances. Offline character selection does not alter online identities or the wire format. Ready-room character selection remains future work. Damage, shot timing, match duration and traversal are unchanged by this update.

## Review and validation

[Menu and character review gallery](../Validation/OfflineRoster/index.html) | [Render manifest](../Validation/OfflineRoster/manifest.json)

The original pinned Unity 6000.6.7f1 roster checkpoint passed **106/106 tests**. It covers all four selections, nonrepeating rosters, controller and damage-collider equivalence, first-person material matching, menu cycles, zero-bot action availability, upper/lower population bounds, combat/drill separation and fixed duel identities. [Test report](../Validation/OfflineRoster/tests.xml).

The review package contains controlled 720p/1080p menu geometry renders and four live Unity first-person camera views. Menu renders use the actual uGUI meshes/materials against a neutral background; first-person camera views omit the screen-space HUD. They do not establish human input acceptance, perfect skin/hand intersections or measured rendering performance.

The Windows x64 development build succeeds with a verified source identity. Its [mechanics](../Validation/OfflineRoster/Standalone/mechanics.json), [45-second three-bot combat](../Validation/OfflineRoster/Standalone/offline.json) and [host](../Validation/OfflineRoster/Standalone/host.json)/[client](../Validation/OfflineRoster/Standalone/client.json) loopback checks pass. [Build receipt](../Validation/OfflineRoster/build.json). Loopback and hidden-window checks do not establish external-peer or physical-input acceptance.

The complete game remains unfinished: the Foundry v2 map and environment models, other gameplay-rule migrations, remaining settings, final art and human/external-peer acceptance still require work.
