# Acceptance evidence and remaining work

This is an interim audit against `OBJECTIVE.md`, not a completion claim. A passing smoke test proves only its recorded conditions. Historical reports identify their build fingerprint; they do not automatically certify a newer build.

Latest presentation milestone: build `ba7f4cc8` passes 76 Editor-suite tests, 37 standalone mechanics checks, a seven-bot smoke and an impaired UDP smoke. Added finger articulation and a fitted grip have skinned-deformation, live wrist/palm-alignment and controlled animation/closeup evidence. Glove/cuff surface defects remain visible. Native mouse review of preceding HUD build `94ab8a77` observed ready, recovery and respawn states; later native effect review stopped at a firewall prompt. Physical keyboard, sound-on, creative quality, final-build full-match and internet acceptance remain open. See `HAND-ARTICULATION.md` and `VALIDATION.md`.

| Requirement | Current evidence | Remaining acceptance |
| --- | --- | --- |
| Unity 6000.6.7f1, URP, pinned dependencies | ProjectVersion, package manifest/lockfile, successful Windows build records | Fresh GitHub clone/build and 37 mechanics checks passed on the licensed local machine; fresh-machine setup remains unverified |
| Editable gameplay and bot tuning | Movement asset and validated bot profile asset; session snapshots and behavioral edit/isolation tests | Native tuning workflow and broader balancing review |
| Local Git/LFS and public MIT source | Public Evekiio/rail-arena; binary source assets in LFS; licenses/notices | Final source and artifact audit |
| Offline bot match, 1-7 bots, three difficulties | Three real-time ten-minute seven-bot difficulty soaks; isolated reaction/aim/LOS tests; fresh one/seven-bot smoke | Native configuration acceptance, broader seed/population coverage and fairness review |
| Untimed practice, unlimited respawns, restart/configuration | Rule tests, native practice launch, live deaths/respawns | Native restart and configuration acceptance |
| Zero-bot solo practice for movement/arena testing | Single-participant rule regression; native zero-bot selector and solo launch; standalone movement/fire/untimed/void/respawn/mode-switch checks | Native keyboard movement/jump/pause and sustained mouse feel |
| Direct-IP host/join, port, ready-up | Real two-process UDP tests; native port entry and unused-host timeout | Native two-human connection path and standalone bind/full/incompatible errors |
| Ten-minute countdown, results, rematch | ba7f4cc8 clean/impaired full runs completed tick 36180 with matching scores/lives and round two >=900 ticks | Native results/rematch interaction, non-development full-duel regression and remote-network acceptance |
| Quake-style acceleration, air control, hopping, momentum | Shared tuning derived from reference units; simulation tests; bot traversal | Sustained keyboard/mouse movement feel and fidelity review |
| Rail-only, unlimited ammo, 90-tick cadence | Shared rules; standalone held-fire boundaries; sustained combat | Weapon feedback and native input review |
| 100 health, head/body/limb damage, mixed hits | Corrected bone-scale geometry regression and standalone head/body/limb/mixed sequences | Dynamic aiming and visual alignment review |
| Health +25, cap 100, return after 1200 ticks, no regeneration/armor | Standalone full-health rejection, contention, healing/cap and exact return boundaries | Visible pickup interaction review |
| Two-second respawns, void penalty, sudden death | Standalone exact respawn/void boundaries and full integer-clock tied-leader next-kill resolution | Native interaction and final real-time regression |
| Original floating arena, ramps/gaps/four pads, reference direction | Foundry scene, reference review, ten-minute bot traversal; first original surface-art pass with unchanged collider/navigation signature | Further architecture/backdrop/lighting art, flow/readability and visible play review |
| Bots navigate, see targets, move/fire, seek health, respawn | Ten-minute per-actor navigation/stall samples at all difficulties with zero voids/errors; isolated LOS/reaction/health-seeking checks | Broader seed coverage, visual/tactical quality and fairness review |
| Same rules for bots/humans and offline/online | Shared 60 Hz authority and movement; no online bot participants | Final regression with complete mechanics scenarios |
| Stationary, moving and combat repeatable scenarios | Implemented main/pause configuration | Native scenario selection and damage acceptance |
| Host-authoritative movement/combat/pickups/scoring at 60 Hz | Command-only client packets, life-aware snapshots and build-specific clean/impaired full-match evidence | Native human interaction, release-build and actual remote-network acceptance |
| Client prediction, reconciliation and interpolation | Real UDP, impairment smoke tests, correction diagnostics | Visual motion acceptance; camera candidate has tests but human feel is unapproved |
| Validated hit rewind <=200 ms | Exact twelve-tick, world-wall, historical box/capsule and life-isolation tests; integrated rewound shots | Final impaired-network and visual acceptance |
| Compatibility and graceful disconnect | Protocol mismatch/full-host/timeout/leave tests; native timeout | Complete standalone error/disconnect scenarios |
| Character readability and high-quality animation | Textured armored base, three LODs, four skins, packed Blender source; railgun; arm-only first-person rig with shared grips, procedural travel/landing/raise/recoil motion, live IK test and controlled previews | Creative approval, finger fit, authored first-person actions and directional-animation polish |
| Rail effects/audio and full HUD/match presentation | Original shot sound/effect, implemented HUD/scoreboard/feed/results | Visible and sound-on review; art/feedback polish |
| Sensitivity, volume, resolution/fullscreen | Native sensitivity/volume changes, save/relaunch and restored values; live numeric labels; resolution and fullscreen toggles | Display-mode persistence after relaunch, mouse feel and sound-on review |
| Simulated latency/loss | Full network-candidate match/rematch passed configured delay/jitter/5% loss, with 2823 recorded drops and 2020 reordered sends | Final-build regression, additional conditions and visual acceptance |
| Actual remote-network multiplayer | No evidence yet | A real peer on another network and verified routing/firewall setup |
| Performance on available machine | Hardware and qualified hidden-window frame times recorded | Visible rendering, memory and input-latency measurements |
| Editable project and runnable Windows build | Local source/Blender masters and successive Windows builds | Final packaged Windows artifact and clean-clone instructions |
| Portfolio README, screenshots, hosting guide | Development README with actual standalone capture and evidence table; notices; implementation-checked LAN/firewall/forwarding guide | Finished presentation, downloadable build and actual remote verification of operating guide |

Current evidence is indexed in `VALIDATION.md`, networking details in `NETWORKING.md`, and model provenance/limitations in `CHARACTERS.md`. Source milestones must continue to be pushed under the policy in `DEVELOPMENT.md`.

`MECHANICS-VALIDATION.md` explains the current 37 accelerated standalone checks, including the original 28 that discovered the inherited-bone-scale hitbox bug. Historical combat counts are not proof of corrected geometry; use build-specific evidence.
