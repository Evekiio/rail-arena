# Acceptance evidence and remaining work

This is an interim audit against `OBJECTIVE.md`, not a completion claim. A passing smoke test proves only its recorded conditions. Historical reports identify their build fingerprint; they do not automatically certify a newer build.

| Requirement | Current evidence | Remaining acceptance |
| --- | --- | --- |
| Unity 6000.6.7f1, URP, pinned dependencies | ProjectVersion, package manifest/lockfile, successful Windows build records | Final clean-clone build check |
| Local Git/LFS and public MIT source | Public Evekiio/rail-arena; binary source assets in LFS; licenses/notices | Final source and artifact audit |
| Offline bot match, 1-7 bots, three difficulties | Implemented menu/configuration; repeated seven-bot standalone combat soaks | Separate difficulty and long-duration reliability checks |
| Untimed practice, unlimited respawns, restart/configuration | Rule tests, native practice launch, live deaths/respawns | Native restart and configuration acceptance |
| Direct-IP host/join, port, ready-up | Real two-process UDP tests; native port entry and unused-host timeout | Native two-human connection path and standalone bind/full/incompatible errors |
| Ten-minute countdown, results, rematch | Normal and impaired runs completed tick 36180, matching results and round two; impaired scores 102-66 | Final-build regression after presentation changes and native acceptance |
| Quake-style acceleration, air control, hopping, momentum | Shared tuning derived from reference units; simulation tests; bot traversal | Sustained keyboard/mouse movement feel and fidelity review |
| Rail-only, unlimited ammo, 90-tick cadence | Shared rules and cooldown-boundary tests; sustained combat | Standalone boundary scenarios and weapon feedback review |
| 100 health, head/body/limb damage, mixed hits | Shared damage tests, bone hurtbox tests, integrated combat | Explicit standalone tests for every hit region and mixed sequence |
| Health +25, cap 100, return after 1200 ticks, no regeneration/armor | Shared rule tests and pickup activity counters | Standalone pickup timing/contention boundaries |
| Two-second respawns, void penalty, sudden death | Shared rule tests; integrated deaths/respawns | Standalone exact boundaries and tied-leader sudden-death scenario |
| Original floating arena, ramps/gaps/four pads, reference direction | Foundry scene, reference review, repeated traversal without void deaths in short soaks | Final arena art, flow/readability and visible play review |
| Bots navigate, see targets, move/fire, seek health, respawn | Explicit node graph and LOS code; standalone shots/deaths/pickups/pads | Long-run stuck-rate and isolated difficulty/LOS acceptance |
| Same rules for bots/humans and offline/online | Shared 60 Hz authority and movement; no online bot participants | Final regression with complete mechanics scenarios |
| Stationary, moving and combat repeatable scenarios | Implemented main/pause configuration | Native scenario selection and damage acceptance |
| Host-authoritative movement/combat/pickups/scoring at 60 Hz | Command-only client packets and host snapshots; full-match clock evidence | Final-build network regression |
| Client prediction, reconciliation and interpolation | Real UDP, impairment smoke tests, correction diagnostics | Visual motion acceptance; camera candidate has tests but human feel is unapproved |
| Validated hit rewind <=200 ms | Exact twelve-tick, world-wall, historical box/capsule and life-isolation tests; integrated rewound shots | Final impaired-network and visual acceptance |
| Compatibility and graceful disconnect | Protocol mismatch/full-host/timeout/leave tests; native timeout | Complete standalone error/disconnect scenarios |
| Character readability and high-quality animation | Textured armored base, three LODs, four skins, editable packed Blender source, motion preview/tests | Creative approval, first-person arms, weapon and directional-animation polish |
| Rail effects/audio and full HUD/match presentation | Original shot sound/effect, implemented HUD/scoreboard/feed/results | Visible and sound-on review; art/feedback polish |
| Sensitivity, volume, resolution/fullscreen | Implemented settings and persistence | Native save/reload and display-mode acceptance |
| Simulated latency/loss | Full network-candidate match/rematch passed configured delay/jitter/5% loss, with 2823 recorded drops and 2020 reordered sends | Final-build regression, additional conditions and visual acceptance |
| Actual remote-network multiplayer | No evidence yet | A real peer on another network and verified routing/firewall setup |
| Performance on available machine | Hardware and qualified hidden-window frame times recorded | Visible rendering, memory and input-latency measurements |
| Editable project and runnable Windows build | Local source/Blender masters and successive Windows builds | Final packaged Windows artifact and clean-clone instructions |
| Portfolio README, screenshots, hosting guide | Current development README, real Editor captures, notices and validation reports | Finished presentation, downloadable build and verified operating guide |

Current evidence is indexed in `VALIDATION.md`, networking details in `NETWORKING.md`, and model provenance/limitations in `CHARACTERS.md`. Source milestones must continue to be pushed under the policy in `DEVELOPMENT.md`.
