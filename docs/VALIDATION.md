# Validation record - October 22, 2026

Status: **offline milestone and initial online integration, incomplete game**. The full objective has not been achieved.

## Bot reliability and hosting milestone

All 40 EditMode tests passed, including seven isolated reaction/aim/occlusion/health-seeking cases (`Validation/bot-brain-editmode.json`). Three concurrent real-time ten-minute seven-bot practice runs passed at easy/medium/hard (`Validation/Bot-Soak-600s/difficulty-*.json`, fingerprint `c2070a17...`): 2559/2674/2576 shots, 141/305/516 deaths, zero application exceptions and zero void deaths. Every actor accumulated at least 244 sampled route nodes. Longest sampled stationary time was under 1.1 seconds and time without route progress about 3.01 seconds. These are ten-minute sampled reliability observations, not infinite/all-seed coverage or a rendering benchmark. Definitions, thresholds and raw report scope are in `BOT-VALIDATION.md`.

An additional one-bot smoke exposed the runner's population-independent death floor. The failed sample is retained. Population-aware activity gates preserve the original seven-bot thresholds; a separately built candidate (`e0968521...`, `Validation/bot-population-build.json`) passed fresh one-bot and seven-bot smoke runs. Production bot/gameplay behavior was not changed.

`HOSTING.md` now supplies implementation-checked manual LAN, firewall and router-forwarding steps with primary documentation sources. Command availability and PowerShell syntax were checked, but no host firewall/router settings were changed and no remote-network acceptance is claimed. The user currently wants automated duel validation and development focused on mechanics, physics, graphics, models and bot/solo testing, without requiring them to test multiplayer now.

## Metre-sized hurtboxes and standalone mechanics

The new standalone runner uncovered an inherited 100x FBX bone-scale error in the original hurtbox builder. Earlier combat activity did not prove correct region geometry. The generator now compensates collider scale and uses world-space limb lengths; only hurtboxes were regenerated. All 33 EditMode tests, including real prefab geometry bounds and hit/miss checks, pass. All 28 accelerated standalone mechanics scenarios pass on corrected build `a6326328...`, including exact damage/cooldown/pickup/respawn boundaries and complete integer match clocks. See `MECHANICS-VALIDATION.md` for scope, original failure and reproduction.

The corrected build's normal 45-second seven-bot standalone soak passed with 192 shots, 22 deaths, two pickups, 55 pad launches, zero application exceptions and zero void deaths (`Validation/mechanics-offline.json`). This is a short-run regression, not long-duration bot or difficulty acceptance.

The same corrected build passed a 60-second two-process impaired UDP smoke test (`Validation/Duel-MetreHurtboxes/`): host recorded 56 shots, nine deaths and 28 validated rewinds, and both peers recorded zero errors. The relay recorded 308 drops and 193 reordered sends under configured 25 ms one-way delay, +/-10 ms jitter and 5% loss. Maximum raw client correction was 0.7855 m. This does not repeat full-match/rematch or actual internet acceptance.

## Original railgun milestone

Build `15f0d82be90b69f3f2d6ec23c8a8ffc4bdbd3362a19d12b7891c022c527af8f4` adds the editable original RA-01 weapon in first and third person (`WEAPONS.md`). All 32 EditMode regression cases passed (`Validation/weapon-editmode.json`), and the Windows build succeeded (`weapon-build.json`). The 45-second standalone offline run passed with 147 shots, 61 deaths, four pickups, 41 pad launches, zero exceptions and zero void deaths (`weapon-offline.json`). Hidden-window loop timing is not a visible rendering benchmark.

The updated controlled character-motion video decodes completely. The first-person image at `RailArena/Assets/Validation/railgun-first-person.png` is an Editor camera capture. Wrist target distances were approximately 0.000002 m and 0 m in the live Editor; this does not establish finger placement or creative approval. First-person arms remain unfinished.

An online pause presentation bug was corrected: actor animation and cosmetic settling now continue with online authority, while offline pause freezes them. Editor checks recorded animator speed 1 for a paused listening host and 0 for paused offline practice (`weapon-pause-online.json`, `weapon-pause-offline.json`). No peer was connected during these branch checks.

The first impaired smoke attempt under restricted execution delivered zero UDP packets and timed out without gameplay (`Validation/Duel-Weapon/`). That report is retained as a failed environment-limited attempt, not passing network evidence. The separate localhost-network-enabled 60-second run passed on both peers with 41 observed shots, 20 validated rewinds and zero errors (`Validation/Duel-Weapon-LocalNetwork/`). The relay recorded 302 drops and 206 reordered sends with configured 25 ms one-way delay, +/-10 ms jitter and 5% loss. Maximum raw client correction was 0.9485 m; feel remains unapproved. This is a smoke regression, not a repeated full ten-minute match or internet test.

The network candidate completed its full real-time ten-minute match/rematch under seeded delay, jitter and 5% loss (`Validation/Duel-FullMatch-Impaired/`). Both peers agreed on tick 36,180 and scores 102-66, then entered round two for at least 900 ticks with zero errors. The relay recorded 2,823 drops and 2,020 reordered sends over approximately 629 seconds. Raw maximum client correction was 1.2060 m. This is simulated impairment around loopback, not internet multiplayer.

Subsequent correction diagnostics and client-only camera continuity passed 32 tests plus a separate impaired smoke test (`Validation/view-reconciliation-editmode.json`, `Validation/Duel-View-Reconciliation/`). Camera smoothing does not alter authority, collision state or raw correction reporting. Visual feel remains unapproved; the camera candidate is distinct from the build used for the full impaired match.

Extended candidate: all 28 cases passed after correcting permanent command backlog under jitter (`Validation/jitter-editmode.json`). A separate candidate Windows build passed the 60-second impaired smoke test with configured 25 ms each-way delay, jitter and 5% loss. Actual relay records show 309 drops and 211 reordered sends; both players logged zero errors. The host validated 17 rewound shots and rejected one expired timestamp. Maximum client correction was 0.9911 m, which still requires visual acceptance. See `docs/NETWORKING.md` for failed runs, root causes, candidate/build distinction and native UI coverage.

The earlier full-match build (`9c50d65e...`) passed a real-time ten-minute duel, matching results and rematch (`Validation/Duel-FullMatch/`). Both peers agreed on tick 36,180, winner 0 and scores 96-69, then entered round two for at least 900 ticks. Elapsed time was approximately 619 seconds, with zero errors. Maximum client correction was 1.4100 m. This result predates the jitter fix and does not establish full-match acceptance for the latest candidate under impaired conditions; that separate run is in progress.

Latest rewind milestone: all 27 tests passed (`Validation/rewind-editmode.json`) and the Windows build succeeded (`Validation/rewind-build.json`). A fresh two-process standalone run passed with both slots firing, 12 host-validated remote rewinds, zero rejected timestamps and zero errors (`Validation/Duel-Rewind/`). The shared offline path passed with 138 shots, 62 deaths, four pickups, 46 pads, zero errors and zero void deaths (`Validation/rewind-offline-regression.json`). Full timed duel/rematch and impaired-network acceptance remain outstanding.

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

- Native keyboard movement/jump/pause, mouse feel, settings and sound-on acceptance remain unverified. Native mouse menu navigation, UDP port text entry, join timeout, practice launch and firing were subsequently observed (`Validation/native-ui-checks.json`). Automated duel runners still start sessions through code.
- The requested standalone screenshot did not materialize during hidden-window testing; Editor captures are not presented as standalone screenshots.
- Long-duration bot reliability and separate difficulty acceptance tests.
- Complete standalone mechanical boundary tests, timed results/rematch interaction and settings interaction.
- Rewind, prediction/interpolation and combat presentation acceptance under impaired networking.
- Final-build full-match/rematch regression after presentation changes, complete standalone compatibility/disconnect scenarios and actual remote-network testing. The network candidate's full impaired-match evidence is recorded above.
- Further character polish and creative approval, dedicated first-person arms, weapon, effects, audio and arena art.
- Portfolio-ready README/screenshots and a downloadable Windows release. Public source publication is verified at `https://github.com/Evekiio/rail-arena`; the initial upload included all commits through `914fcd2` and 22 LFS objects (105 MB). See `Validation/github-publication.json` and the development delivery policy.

Repeat the current smoke/soak with `tools/Test-Offline.ps1`. Stationary and moving target modes can also be selected from the main or pause menu and applied by starting/restarting a session. No completion claim should be based on this milestone.
