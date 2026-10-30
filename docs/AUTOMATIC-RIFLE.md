# Automatic rifle and recoil

Hold left mouse for automatic fire at 300 RPM: one shot every twelve 60 Hz simulation ticks (0.2 seconds). Release stops the trigger. A quick tap between simulation ticks is retained for one sample; it cannot queue a burst. Countdown, death, pause and results still prevent firing. The trigger is reset across menu and session transitions.

Damage remains 100/50/34 for head/body/limbs, with unlimited energy and no reload. Body-shot time to kill is 0.2 seconds when both shots hit. All contenders, including bots, use the same cadence. This is intentionally a much more lethal balance candidate, not competitive balance approval. ADS remains 1.5x; accuracy, hit rays, movement and slide tuning are unchanged.

## Recoil presentation

The local view rifle now uses an analytic damped spring, with fast backward travel, upward pitch, slight alternating roll and smooth recovery after release. Repeated shots add bounded impulses. Grip articulation follows the weapon, and the third-person rifle receives a smaller shoulder kick. The camera and authoritative aim stay stable: this pass adds visual recoil, not random spread or a recoil-control aim pattern.

ADS retains the physical sight alignment and reduces angular/lateral kick to 20 percent of the hip-fire effect. The existing backward recoil travel remains. WEAPON MOTION OFF suppresses the new local view recoil and clears the spring so it does not catch up when re-enabled. Death, respawn and session/life changes reset view recoil.

## Network behavior

Protocol 7 identifies the automatic-fire behavior. Both duel peers must use this build. The host still limits cadence and resolves damage. Held fire survives a maximum three-tick packet gap (50 ms), using the original received aim and viewed-tick evidence. A fresh release immediately stops held fire; older redundant commands cannot re-enable it. Longer gaps stop firing, and received bursts never turn into extra simulation steps or catch-up volleys.

## Review

Validation runs headlessly from `Builds/Automatic-Rifle-2026-10-29/Project`; reports and controlled captures are in `Validation/Automatic-Rifle-2026-10-29/`. A separate development build contains opt-in validation drivers. The requested local player will be a separate non-development build.

Human recoil feel, normal-speed visual/sound approval, competitive balance and real-internet multiplayer acceptance remain open. Hidden tests do not establish those qualities.

The first combined presentation runs passed recoil/ADS/layout checks but exposed an isolated import-cache failure: Unity could resolve the compiled MovementTuning type while MonoScript.GetClass returned null, causing the serialized movement asset to load as null. Failed reports and direct asset diagnostics are retained. The copied Library was archived, and validation was restarted from a fresh import cache with only ShaderCache retained; no source movement/scene fallback was introduced.

Fresh import resolved MonoScript.GetClass and the scene tuning reference before any forced asset refresh. The next combined run passed 72/73 cases; its remaining HUD assertion still staged an 18-tick midpoint from the old 36-tick weapon. The corrected HUD check stages half the current six-tick cooldown with its simulation clock held, and the branded review uses that same current midpoint. Initial failures and rechecks remain in the evidence folder.

The standalone driver verified ten shots across sixty ticks and no continued shots after release. Its first run then reached a legacy solo-practice assertion expecting one shot over thirty ticks; automatic fire correctly produced five. That expectation was updated to five, and a new validation build/recheck was made. The initial report is retained.

## Qualification record (600 RPM build, 2026-10-29)

- 74 distinct targeted Editor cases have passing latest results, covering automatic input/cadence, bounded/recovering springs, packet-gap/release behavior, existing slide/movement and prediction, installed recoil/ADS, hand contacts, comfort controls and menu/HUD/help layout. A virtual-mouse test exercised the production Update/FixedUpdate path: held input repeated and release stopped firing. This does not replace physical-input acceptance.
- Six controlled world-only recoil keyframes and 45 UI review captures are retained in the evidence folder. The spring moves the installed weapon while camera aim remains unchanged; ADS kick is reduced and steady-weapon mode clears it.
- All 39 standalone mechanics checks passed after updating the two legacy cadence expectations; the initial failed report is retained. Exact six-tick cadence, ten shots over sixty ticks, release stop, countdown/death/respawn and match rules were exercised.

The 45-second four-contender offline run passed with 577 shots, 28 deaths, two pickups, 25 pad launches, reconciled statistics and no exceptions or void deaths. Hidden timing data is not a rendering-performance certification.

Both peers passed the 45-second clean duel and 60-second impaired duel, with zero application errors and individually reconciled combat statistics. Client maximum active correction was 0.000025 m clean and 1.211 m impaired. The impaired run forwarded 5,215 packets, dropped 304, had no queue overflow and stopped cleanly. Movement-mode cycling was not enabled for these rifle runs; existing slide/movement and snapshot tests passed separately. These short runs do not establish competitive smoothness, a full-match result or internet acceptance.

Final source identity: `30feda7f963b1864aea35aca1eccf1cceea7c352e20aa57d36ceba020409105d`. The isolated input difference was the generated identity file, copied back and independently verified; diagnostic files under Assets/Validation are excluded from the production input hash. All 196 files in the previous sprint-slide build verified unchanged.

Local non-development player: `F:\Rail Arena\Builds\Assault-Rifle-2026-10-29\RailArena.exe`. Keep every adjacent data folder and DLL with it. PLAYTEST.txt contains the controls; package provenance and full file hashes are retained in the evidence folder.

The final non-development build completed and passed hidden menu startup/close with exit code 0. All 196 package-file hashes verify, full font/icon notices are included, and all 147 managed assemblies were scanned to confirm the opt-in duel/mechanics drivers are absent. The preceding sprint-slide player's 196-file manifest was reverified unchanged. No task-owned build, Editor, player or relay processes remained after qualification; no visible Editor or player window was opened.

## 300 RPM adjustment

The current source slows the shared cadence to twelve ticks (300 RPM, 0.2 seconds between shots), with matching HUD/help labels. Recoil, damage, ADS and movement are unchanged. A separate local player is `Builds/Assault-Rifle-300RPM-2026-10-29/RailArena.exe`. No tests or player validation were run for this adjustment at the user's request; the earlier qualification above applies to the 600 RPM build.

Current magazine build: 250 RPM, 30 rounds, R reload (2 seconds), unlimited reserve, protocol 8. See [magazine controls and state](MAGAZINES.md). Local player: `F:\Rail Arena\Builds\Magazines-250RPM-2026-10-29\RailArena.exe`. Earlier fire-rate/build qualification remains specific to those preceding players.
