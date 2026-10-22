# Validation record - October 22, 2026

Status: **offline milestone and initial online integration, incomplete game**. The full objective has not been achieved.

The initial direct-IP session integration passed all 22 EditMode cases and a rebuilt Windows player test using two separate processes over loopback UDP. Evidence: `Validation/online-session-editmode.json`, `Validation/online-session-build.json` and `Validation/Duel-Corrected/`. In the 45-second smoke test, the host recorded 30 shots, 13 deaths and 10 pad launches; the client received 898 snapshots, acknowledged 2,690 inputs and measured maximum continuous-life correction of 0.0449 m. Both logged zero errors. This establishes initial session activity, not complete match/rematch or internet acceptance. See `docs/NETWORKING.md` for scope and the initial serialization failure and fix.

The shared-loop changes also passed a fresh 45.02-second offline regression: 143 shots, 62 deaths, four pickups, 45 pad launches, zero errors and zero void deaths (`Validation/online-change-offline-regression.json`). Hidden-window timing was mean 6.96 ms and p95 6.97 ms; the runs were concurrent and these values are not a visible rendering benchmark.

## Armored character integration

The current Windows build includes the textured humanoid contender, four palette variants, three LODs, skeleton-following hurtboxes, aiming IK and locomotion/jump/hit/death animation. Eleven EditMode cases passed, including the original seven simulation cases and character rig, hurtbox, forward/backward motion and animation-loop checks. Evidence: `Validation/character-editmode.json`.

The rebuilt Windows player passed a fresh 45.02-second seven-bot plus automated-human soak: 2,703 ticks, 145 shots, 56 deaths, four pickups, 41 pad launches, zero void deaths and zero errors/exceptions. Evidence: `Validation/character-standalone.json`; successful build: `Validation/character-windows-build.json`. Hidden-window loop timing was mean 6.95 ms, p95 6.96 ms. This is not a visible rendering benchmark or native-input playtest.

The 180-frame, 24 fps controlled motion preview was inspected for idle, running, backward movement, staged jumping, hand aiming and grounded death motion; its MP4 decoded completely without errors. It uses Unity's normal animation/IK loop. See `Validation/CharacterMotion/Contender-Motion-Review.mp4` and `docs/CHARACTERS.md` for asset provenance and remaining art limitations. Human approval of creative quality is still outstanding.

## Verified

- Exact Editor: Unity 6000.6.7f1 (b22767f04f04); URP 17.6.0.
- Seven EditMode tests passed: combined location damage, capped healing, firing boundary, jump momentum versus ground friction, directional air acceleration, countdown/scoring/sudden death, and untimed practice with void penalties. Evidence: `Validation/editmode.xml`.
- Windows development build succeeded. The current executable is `Builds/Windows/RailArena.exe`; the build folder is intentionally excluded from Git.
- Editor Play mode frames advanced, and live menu and first-person captures were inspected. Oversized starfield halos were removed by replacing the double-sided mesh's lit star material with an unlit material and bounding bloom.
- Corrected standalone smoke/soak passed with seven bots plus an automated human input driver, using the actual local authority and movement. Evidence: `Validation/standalone-candidate.json`.

Final standalone sample: 45.01 seconds, 2,701 simulation ticks, 179 shots, 31 deaths, two health pickups, 53 pad launches, zero void deaths, and zero logged errors/exceptions. This demonstrates activity in the integrated offline loop; it does not exhaustively validate every hit region, cooldown, respawn or pickup boundary in a standalone game.

## Failures found and corrected

Initial bots incorrectly entered the navigation graph at node zero. They now select their nearest entry node and reset routing on respawn. Launch velocity initially risked cancellation by the controller's previous grounded contact; launch integration now preserves an airborne state for two ticks.

The first standalone test failed with four void deaths. Landing navigation was changed to wait for ground contact and brake with normal friction before turning. A second test still failed with three void deaths; traces identified all three as the automated human, whose input driver was missing the launch-state notification used by bots. The notification now applies to either driver. The third run passed without loosening the void-death threshold. Failed reports are retained in `Validation/`.

## Performance scope

Machine: Intel Core i7-8700K, NVIDIA GeForce RTX 2080, approximately 64 GB system RAM. At 1280 x 720, the final hidden-window standalone run recorded mean frame time 6.95 ms and 95th percentile 6.97 ms after excluding five seconds of startup. This is loop timing under hidden-window automation, **not a visible-window rendering benchmark**. No GPU timing, application memory profile, input latency or internet latency measurements are claimed.

## Unperformed or incomplete

- Native standalone UI interaction: Computer Use launch approval timed out. Menu clicks, mouse feel and sound-on quality remain unverified by native input. The automated runner starts sessions through code.
- The requested standalone screenshot did not materialize during hidden-window testing; Editor captures are not presented as standalone screenshots.
- Long-duration bot reliability and separate difficulty acceptance tests.
- Complete standalone mechanical boundary tests, timed results/rematch interaction and settings interaction.
- Validated 200 ms hit rewind; prediction/interpolation and combat presentation acceptance under impaired networking.
- Full two-client timed match, results, rematch, compatibility and disconnect scenarios; simulated packet loss/latency and actual remote-network testing.
- Further character polish and creative approval, dedicated first-person arms, weapon, effects, audio and arena art.
- Public repository publication and portfolio-ready README/screenshots.

Repeat the current smoke/soak with `tools/Test-Offline.ps1`. Stationary and moving target modes can also be selected from the main or pause menu and applied by starting/restarting a session. No completion claim should be based on this milestone.
