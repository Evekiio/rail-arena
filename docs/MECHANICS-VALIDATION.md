# Standalone mechanics scenarios

Run the development Windows player with:

```powershell
.\tools\Test-Mechanics.ps1
```

The script launches a hidden standalone process, requires a fresh JSON report, checks its exit code and enforces a 120-second deadline. It uses `--validate-mechanics`; the harness is excluded from non-development players. It does not run during ordinary play.

The harness arranges two stationary actors in the saved arena. Native input and bot decisions are disabled for these actors. The offline session is paused between controlled ticks, and a development-only entry point invokes the same `AdvanceAuthority()` method used by offline and online host play. Movement still uses real CharacterControllers. Damage still passes through captured animated hurtbox geometry, the normal shot trace, health application, scoring and death handling. No test-only damage function or shortened match duration is substituted.

Aim selection first confirms an unobstructed ray to the requested live hit region. The normal authority then independently traces the fired shot. Twenty-eight checks cover:

- Countdown through tick 179 and active combat at tick 180.
- Lethal headshot, two body hits, three limb hits and mixed body/limb damage.
- Held fire blocked for 89 ticks, then accepted at the 90-tick boundary.
- No firing while dead, exact 120-tick respawn, restored health and changed life identity.
- Full-health pickup rejection, single-winner contention, +25 healing, 100 cap, exact 1200-tick return and absent passive regeneration.
- Void death penalty, disabled actor and exact respawn boundary.
- Complete integer match clock through tick 36180, tied-leader sudden death, next-kill resolution, unique-leader results and frozen results state.
- Practice active beyond the timed-match limit and continuing to respawn.

This is accelerated integer-clock integration evidence, not native keyboard/mouse interaction, visual feel, audio acceptance or a real-time ten-minute session. The existing separate two-process full-match records establish their own real-time scope. Actual public-network multiplayer remains unverified.

## Scale regression discovered by this runner

The initial attempt (`Validation/mechanics-first.json`, build `f694df59...`) could not find an unobstructed head ray. Live inspection identified the imported FBX's 100x bone scale being inherited by metre-sized colliders. Head/body centers and dimensions were displaced or enlarged, and limb radii were enlarged. Earlier combat activity counts did not prove correct hit-region geometry.

The builder now compensates each hurtbox's inherited scale and measures limb height from world-space bone distances. `UpdateHurtboxes()` updates only this part of the existing prefab. The new geometric EditMode regression checks unit world scale, bounded dimensions and bone proximity, a direct head hit and a two-metre-offset miss.

The corrected standalone report is `Validation/mechanics-corrected.json`, fingerprint `a6326328c71a681aa0fc8a4624b703dbd58c35a62a0d4b965e0bfdbeaa2779f6`. All 28 checks passed without application errors. All 33 EditMode tests passed in `Validation/mechanics-editmode.json`; build evidence is in `Validation/mechanics-build.json`. Earlier reports remain historical and do not certify the corrected build.
