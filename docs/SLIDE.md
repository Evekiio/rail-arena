# Sprint-to-crouch slide

Hold Shift and move forward to build sprint speed, then press Ctrl while grounded. Entry needs at least 11 m/s and a preceding sprint tick. Starting crouched, walking, moving backward, airborne or jumping cannot trigger it.

The slide lasts 36 simulation ticks (0.6 seconds at 60 Hz). It carries current horizontal momentum without an entry boost, loses 15 m/s each second down to crouch speed, and permits at most 45 degrees of steering per second. Releasing movement keys coasts along the existing direction. It uses the existing 1.2 m crouch controller and 0.98 m eye height. Hold Ctrl to remain crouched afterward; released Ctrl allows standing only when overhead space is clear. No extra camera roll or forced aim movement is added.

Jumping, leaving the ground or launching from a pad ends the slide. A new crouch press and grounded sprint are required for another slide, with 48 ticks (0.8 seconds) of cooldown after a complete slide. Early cancellation retains the original cooldown deadline. Death and placement/respawn clear slide state and input history. Walls still use CharacterController collision resolution.

ADS stays at 1.5x; shot accuracy, damage, normal movement, sprint/walk/crouch speeds and match rules are unchanged. The HUD shows SLIDE using the existing crouch icon. How to Play includes the input.

## Networking

Protocol 6 snapshots carry remaining slide ticks, cooldown ticks, prior crouch input and prior sprint eligibility. The host resolves entry from ordinary movement intent. Client reconciliation restores this state before replaying pending controls; opponent interpolation uses the corresponding snapshot stance/state. Prior protocols are rejected. Use this same build on both peers.

## Validation

The isolated project and evidence are under `Builds/Slide-2026-10-29/Project` and `Validation/Slide-2026-10-29/`. Focused tests cover trigger gates, exact duration, momentum loss, steering/coasting, cooldown, cancellation, overhead clearance, placement reset, snapshot bounds and client replay. Existing movement, simulation, networking and snapshot lifecycle checks run alongside them. Standalone checks use a separate development player; the supplied local test build is non-development.

Human physical-input feel, motion/audio approval and real-internet multiplayer remain open. Hidden runtime checks cannot establish those qualities.

## Qualification record (2026-10-29)

- 59 distinct targeted Editor cases passed: slide simulation/actor collision, existing movement/simulation/networking/lifecycle, physical ADS presentation and the branded UI flow. The UI flow checks 15 states at 1280x720, 1920x1080 and 2560x1080; 45 controlled captures are retained. These are headless Editor renders, not native-input acceptance.
- All 37 standalone mechanics checks passed, with zero recorded errors.
- A 45-second clean two-process duel and 60-second impaired duel passed with sprint/walk/crouch/slide cycling. Both peers observed live slides for both actors, with zero application errors and reconciled combat statistics. Clean client maximum active correction: 0.348 m; impaired: 0.828 m. These values do not certify competitive smoothness.
- The impaired relay used 25 ms one-way delay, �10 ms jitter and 5% configured packet loss. It forwarded 5,219 packets, dropped 304, had no queue overflow and stopped cleanly. This is simulated loopback impairment, not an internet or full-match qualification.
- Source identity: `8791d91424078a916659455de5a2608a37520a77ff30609065cee7fe33d6feb7`. The isolated project's only generated input difference was BuildIdentity.txt, copied back and independently verified. All 196 files in the preceding branded UI build verify unchanged.

Local player: `F:\Rail Arena\Builds\Sprint-Slide-2026-10-29\RailArena.exe`. Keep all adjacent files together. Complete test reports, the feature-only diff and package provenance are in `Validation/Slide-2026-10-29/`.

The 45-second four-contender offline regression passed with 200 shots, 15 deaths, two pickups, 22 pad launches, reconciled statistics and no exceptions or void deaths. Hidden timing data is not a rendering-performance certification.

The final non-development build completed successfully and passed hidden menu startup/close with exit code 0. The 196-file package manifest verifies; font/icon licenses and PLAYTEST.txt are included, and development validation drivers are absent. Task-owned build, player and relay processes were checked after completion. No visible Editor or player window was opened.

Sprint fire uses a 2.843 degree half-angle around the aimed direction, sampled uniformly over a disc by the host. This is based on a 25% larger radius than the default reticle at 1080p and 100-degree vertical FOV; reticle cosmetics do not change accuracy. Non-sprinting shots retain their existing accuracy. Double-tap Shift within 0.35 seconds while moving forward at sprint speed to slide; hold Ctrl to crouch. Duel protocol version 10 is required on both players.

While sprinting, a circle around the reticle shows the bounds of the sprint-fire cone. It is projected using the current camera FOV and HUD scale, including while aiming down sights, and disappears when sprint spread is inactive.
The sprint spread circle expands and contracts over 0.16 seconds. A separate full-size boundary appears immediately whenever sprint spread is active, while the inner accent animates. Firing accuracy does not animate.


Grounded movement now adds a 1-degree shot-cone half-angle; grounded crouch movement uses 0.5 degrees. Stationary shots remain exact, sprint retains 2.843 degrees, and airborne accuracy is unchanged. The HUD uses the same cone calculation and shows its full boundary immediately for every active movement spread.
