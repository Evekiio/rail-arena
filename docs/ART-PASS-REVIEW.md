# Rail Arena v1 alpha: integrated art build 19

Review-19 adds original third-person slide entry, feet-first gliding hold and compact recovery to all four contenders, retaining the five grounded crouch motions and all earlier graphical upgrades. The art controller now includes twenty-five original body clips. Full-body crouch enter/exit remain references. This is a verified local playtest milestone; the full art-production plan remains incomplete. Sanders retains final native input, sound and artistic acceptance. Public prerelease publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-19/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-19.zip`. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: 164,235,744 bytes; all 425 shipped file hashes verified after extraction. SHA-256: `fc89a140a2b166c5fe8ea12b6a63b3e8b2e69234dd09feb2d11df2d42ffd88be`. The package includes measured slide sheets, native motion/comparison reels, scoped test evidence, six earlier reference families, licenses and `Run-Checks.ps1`. Its Duel mode uses the same 90-second workload as this review. Historical packages through Review-17 remain preserved; Review-18 was rejected and was not promoted to a verified portable package.

- [Slide master, measured sheets and moving comparison](../ArtSource/Original/ArtPass-v1/Animation/Slide/index.html).
- [Integrated gameplay, menu and reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Current inventory](../ArtSource/Original/ArtPass-v1/current-asset-register.csv), [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md) and [complete action matrix](../ArtSource/Original/ArtPass-v1/animation-production-matrix.csv).
- [Shipping validation](../Validation/art-integrated-final-19.json) and [archive verification](../Validation/art-package-verification-19.json).

## Installed presentation and preservation

The slide uses original Blender ankle paths and baked bone actions on the unchanged shared rig. Entry and recovery connect to compact crouch; source frames follow remaining simulation ticks without moving the controller or stretching clip speed. Short configured slides use the hold directly. Same-tick stance updates prevent stale head/weapon poses, double lowering and delayed stand-up. Legacy maps keep their procedural treatment. Hitbox definitions/local attachments remain unchanged; world positions follow the new pose. [Slide contract and limits](AUTHORED-SLIDE.md).

The build retains four refined painted characters, calibrated first-person arms, character contours/portraits, the authored two-second reload, rifle materials and physical optic, stone paint/normal detail, summit and bridge construction, eight staging/basement decks, cover shells, canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and cel lighting/atmosphere. Sky Gardens, its summit, all four basement routes and returns remain intact. The distracting cyan particle cloud remains absent. Movement, damage, spread, weapon timing, trajectories and multiplayer protocol are unchanged. Unrelated dirty diagnostics are preserved.

Canonical source and prior evidence remain in [grounded crouch](AUTHORED-CROUCH.md), [locomotion](AUTHORED-LOCOMOTION.md), [combat reactions](AUTHORED-COMBAT-REACTIONS.md), [summit stone](SUMMIT-STONE-KIT.md), [bridge stone](BRIDGE-STONE-KIT.md), [basement stone](BASEMENT-STONE-KIT.md) and [under-bridge portal mounting](UNDERBRIDGE-PORTAL-VISIBILITY.md). The earlier controlled [portal render comparison](PORTAL-RENDER-PERFORMANCE.md) concerns submitted rendering work; it does not prove a universal frame-rate gain.

## Validation

All 39 standalone mechanics checks passed. The 45-second three-bot match recorded 155 shots, 8 deaths, 2 pickups and 22 pad launches, with zero exceptions and zero void deaths. Both 90-second loopback peers passed movement and statistics checks. Host slide observations were 1,508/1,826; client observations were 1,525/1,566. These are render-frame observations, not slide counts. Scripted duel void deaths are distinct from the zero-void offline match result. Real-internet and native-input acceptance remain open.

The slide family adds 108 six-view source images and three measured sheets. Complete source checks cover all 216 bound mesh parts. Native candidate review covers 120 character samples; installed review covers 1,176 duration/yaw/pitch poses and twelve lifecycle scenarios. A six-second matching motion comparison adds 1,440 paired actual-controller trajectories with 0.1 mm tolerance, including 840 active-slide samples. Minimum active-slide skin clearance is 7.779 mm; maximum reviewed wrist error is 0.036 mm. Graph preparation is byte-stable, and the affected crouch regression passes. These controlled runway results do not establish all-terrain contacts or every palm/finger surface.

Review-18's failed host slide coverage remains [recorded](../Validation/art-review-18-exception.json). Its opt-in driver forced Sprint while retaining backward/strafe bot input relative to aim. The corrected development-only driver faces the same world route and retries slide requests during the sprint phase, preserving actual forward-speed/grounding/cooldown requirements. Two focused driver regressions pass. No gameplay eligibility rule or pass criterion was weakened.

Build source commit: `3d9ced300e5f0e8f013e494ef785a6aded5c19bf`. Independently reproduced source fingerprint: `5e1fab11050f82367ff493f6524ccc4c131e508d62254c5a16d490e831b64c71`. Quality settings and the source diagnostic identity are restored after building. Shipping-player evidence was executed on this artifact directly. Source-stage reference documents retain their original wording; `ValidationEvidence/REFERENCE-CONTEXT.txt` and `integrated-final.json` identify the actual shipping status.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms | 16.67 ms budget |
|---|---:|---:|---|
| combat | 15.51 | 19.50 | over budget |
| summit | 13.92 | 16.78 | over budget |
| basement | 13.86 | 16.59 | pass |
| foliage-waterfall | 13.45 | 15.67 | pass |
| portal | 11.87 | 13.72 | pass |

Combat and summit P95 exceed the target. Performance acceptance remains open. Measurements are retained without repeats to select passing timings; differences across milestones are not a controlled A/B comparison and do not isolate slide or asset cost. Measurement version 2 stages the observer before portal rendering; older reports used different portal poses and are not equivalent workloads.

## Continuing production and QA

The joined inventory contains 3,343 unique file paths, including 1,120 production-evidence paths and zero missing paths. These are not counts of unique 3D models or complete reference certification. The original 34-action matrix remains intact; sixteen rows have integrated-review evidence, with final acceptance pending.

Remaining work includes standing/crouch cosmetic transitions, broader FPS/body actions and combat interruptions, complete terrain/traversal/remote-motion coverage, lower arena/ramp/structural production, device assembly and state refinement, nature variants, complete per-asset reference evidence, UI/audio acceptance and measured performance. The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md).

Playtest ADS, reloads, movement/slide readability, reticle bounds, basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
