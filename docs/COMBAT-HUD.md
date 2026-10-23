# Combat display

The combat overlay separates health, score/speed, rail recovery and match state into anchored cards. Health turns red at 34 or below. The rail meter fills during the 90-tick recovery and shows READY only when the local firing clock reaches the next allowed tick. The fixed center reticle changes from gray during countdown to amber during recovery and cyan when ready; it never changes aim or hit detection.

Offline and host presentation use the authority tick. A joining player's HUD and local weapon charge use the prediction tick, matching the clock used to predict that player's firing. Remote contender charge continues to use snapshot authority time. Damage, cadence and host validation are unchanged.

Tab holds the scoreboard. Death hides the reticle and displays the respawn countdown. Pause, connection waiting, menus and results hide the combat overlay. Every combat graphic ignores UI raycasts, preserving mouse menu controls. Practice shows UNTIMED and distinguishes solo from bot sessions.

The overlay follows the existing runtime uGUI Canvas, 1920 x 1080 reference scaler and bundled Unity font. It adds no packages, downloaded fonts, textures or paid assets. Cached component references avoid hierarchy searches during play.

`CombatHudTests.LiveHudTracksCombatAndProductionKeyboardMapping` loads Foundry in live Play mode, validates health/rail values and meter bounds, checks canvas-centered reticle and on-screen cards, then drives an Input System virtual keyboard through the production mapping. It observes simulation ticks for W/Space displacement and jumping, holds/releases Tab, pauses/resumes with Escape, checks frozen offline ticks while paused, and verifies death presentation. Editor focus routing is overridden only within the test and restored in `finally`. This is software-input evidence, separate from physical keyboard or native automation acceptance.

Initial test failures are retained in `Validation/combat-hud-initial-tests.json` and `combat-hud-live-tests.json`. The fixture needed to load the gameplay scene, compare layout against the actual Canvas viewport and wait for observed player frames/ticks rather than assuming Editor coroutine yields represented player frames. Full passing suite: `Validation/combat-hud-tests.json`.
