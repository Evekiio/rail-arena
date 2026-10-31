# Rail Arena v1 alpha: integrated art build 23

Review-23 adds an original four-second standing-ready loop and a Blender-authored first-person rifle raise. It retains the eleven painted nature modules across 24 placements, stone/ramp kits, characters, rifle/reload, authored movement, waterfalls, lighting, UI and existing gameplay. This is a verified local playtest milestone; the full art-production plan remains incomplete. Sanders retains final native input, sound and artistic acceptance. Public prerelease publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-23/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-23.zip`. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: 229,004,042 bytes; all 598 shipped file hashes verified in the archive and again after fresh extraction. SHA-256: `89fa3bef752f185617c89f16558a3e5add7a90b5b4e8794458da22b86ab7d43a`. The package includes nine verified earlier reference families and 106 ready-motion references, including both editable Blender masters, measured sheets, native images and normal-speed reels, scoped shipping-player test evidence, licenses and `Run-Checks.ps1`. Its Duel mode uses the same 90-second workload as this review. Historical packages remain preserved; Review-18 was rejected and was not promoted to a verified portable package.

- [Standing-ready source and native playback](../ArtSource/Original/ArtPass-v1/Animation/Standing/index.html).
- [First-person ready master and player-eye comparison](../ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady/index.html).
- [Garden master, measured sheets, native comparisons and wind reel](../ArtSource/Original/ArtPass-v1/Nature/GardenNature/index.html).
- [Arena stone master, measured sheets and installed comparisons](../ArtSource/Original/ArtPass-v1/Environment/ArenaStone/index.html).
- [Slide master, measured sheets and moving comparison](../ArtSource/Original/ArtPass-v1/Animation/Slide/index.html).
- [Integrated gameplay, menu and reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Current inventory](../ArtSource/Original/ArtPass-v1/current-asset-register.csv), [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md) and [complete action matrix](../ArtSource/Original/ArtPass-v1/animation-production-matrix.csv).
- [Shipping validation](../Validation/art-integrated-final-23.json) and [archive verification](../Validation/art-package-verification-23.json).

## Installed presentation and preservation

Nineteen lower-deck/ramp renderings now use original jointed paving, weathered stone regions, teal/bronze edge bands, underside coffers and directional arrows. Seven metre-scaled Blender families provide twenty-one reducing LOD assets. The revised levels retain the same material regions and arrow wells; simplified levels remove hidden depth and subpixel joints. Native checks preserve all original colliders, navigation, return devices and renderer cleanup. [Canonical geometry, reproduction and evidence limits](ARENA-STONE-KIT.md).

The slide uses original Blender ankle paths and baked bone actions on the unchanged shared rig. Entry and recovery connect to compact crouch; source frames follow remaining simulation ticks without moving the controller or stretching clip speed. Short configured slides use the hold directly. Same-tick stance updates prevent stale head/weapon poses, double lowering and delayed stand-up. Legacy maps keep their procedural treatment. Hitbox definitions/local attachments remain unchanged; world positions follow the new pose. [Slide contract and limits](AUTHORED-SLIDE.md).

The new nature family adds 66 equal-scale orthographic views, eleven measured sheets, eight native camera pairs and an eight-second silent wind reel. Native checks preserve 117 collider definitions, navigation, devices and removal behavior; all 33 canonical mesh buffers match the Blender source exactly. Paired ivy stem/leaf motion stays synchronized without root-position or scale drift. [Nature contract](GARDEN-NATURE.md).

The build retains four refined painted characters, calibrated first-person arms, character contours/portraits, the authored two-second reload, rifle materials and physical optic, stone paint/normal detail, summit and bridge construction, eight staging/basement decks, cover shells, painted trees, shrubs, grass and ivy, floating medical cores, route labels, cloud banks, responsive waterfalls and cel lighting/atmosphere. Sky Gardens, its summit, all four basement routes and returns remain intact. The distracting cyan particle cloud remains absent. Movement, damage, spread, weapon timing, trajectories and multiplayer protocol are unchanged. Unrelated dirty diagnostics are preserved.

Canonical source and prior evidence remain in [grounded crouch](AUTHORED-CROUCH.md), [locomotion](AUTHORED-LOCOMOTION.md), [combat reactions](AUTHORED-COMBAT-REACTIONS.md), [summit stone](SUMMIT-STONE-KIT.md), [bridge stone](BRIDGE-STONE-KIT.md), [basement stone](BASEMENT-STONE-KIT.md) and [under-bridge portal mounting](UNDERBRIDGE-PORTAL-VISIBILITY.md). The earlier controlled [portal render comparison](PORTAL-RENDER-PERFORMANCE.md) concerns submitted rendering work; it does not prove a universal frame-rate gain.

## Validation

All 39 standalone mechanics checks passed on this shipping build. The 45-second three-bot match recorded 154 shots, 8 deaths, 2 pickups and 19 pad launches, with zero exceptions and zero void deaths. Both 90-second loopback peers passed movement and statistics checks with zero errors. Host slide observations were 1,659/1,758; client observations were 1,985/1,302. These are render-frame observations, not slide counts. Scripted duel void deaths are distinct from the zero-void offline match result. Real-internet and native-input acceptance remain open.

The arena family adds forty-two equal-scale orthographic views, seven dimensioned sheets and seven matching in-engine camera pairs. Focused native checks cover nineteen placements, fifty-seven LOD mesh instances, twenty-seven original-floor rays and 704 cross-LOD material-region comparisons. Near geometry retains identical triangle counts and per-material vertex positions across all seven families. Controlled views do not establish every possible LOD transition. Clean-checkout verification caught a source-JSON line-ending mismatch; the scoped LF checkout rule resolves it while keeping exact hash validation.

The slide family adds 108 six-view source images and three measured sheets. Complete source checks cover all 216 bound mesh parts. Native candidate review covers 120 character samples; installed review covers 1,176 duration/yaw/pitch poses and twelve lifecycle scenarios. A six-second matching motion comparison adds 1,440 paired actual-controller trajectories with 0.1 mm tolerance, including 840 active-slide samples. Minimum active-slide skin clearance is 7.779 mm; maximum reviewed wrist error is 0.036 mm. Graph preparation is byte-stable, and the affected crouch regression passes. These controlled runway results do not establish all-terrain contacts or every palm/finger surface.

Review-18's failed host slide coverage remains [recorded](../Validation/art-review-18-exception.json). Its opt-in driver forced Sprint while retaining backward/strafe bot input relative to aim. The corrected development-only driver faces the same world route and retries slide requests during the sprint phase, preserving actual forward-speed/grounding/cooldown requirements. Two focused driver regressions pass. No gameplay eligibility rule or pass criterion was weakened.

Build source commit: `e11805d55c1cf2d235d665b584fe9fdf4e609df9`. Independently reproduced source fingerprint: `d941a433bbc25bf5366c01c30b6258d9dbf4a2a0139479cc1ccfd7ecb5426458`. Quality settings and the source diagnostic identity are restored after building. Shipping-player evidence was executed on this artifact directly. Source-stage reference documents retain their original wording; `ValidationEvidence/REFERENCE-CONTEXT.txt` and `integrated-final.json` identify the actual shipping status.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms | 16.67 ms budget |
|---|---:|---:|---|
| combat | 17.83 | 36.12 | over budget |
| summit | 14.66 | 20.09 | over budget |
| basement | 15.46 | 24.96 | over budget |
| foliage-waterfall | 15.71 | 25.48 | over budget |
| portal | 13.52 | 20.05 | over budget |

All five P95 measurements exceed the target in this run. Performance acceptance remains open. Measurements are retained without repeats to select passing timings; differences across milestones are not a controlled A/B comparison and do not isolate slide or asset cost. Measurement version 2 stages the observer before portal rendering; older reports used different portal poses and are not equivalent workloads.

## Continuing production and QA

The joined inventory contains 3,803 unique file paths, including 1,570 production-evidence paths, 10 explicitly archived research paths and zero missing paths. These are not counts of unique 3D models or complete reference certification. The original 34-action matrix remains intact; eighteen rows have source integration evidence, with final acceptance pending. Review23 contains the standing-ready loop and first-person raise; source-stage documents packaged alongside them retain their original pending wording, with actual shipping status in integrated-final.json.

Remaining work includes temporal standing/movement and crouch cosmetic transitions, broader FPS/body actions and combat interruptions, complete terrain/traversal/remote-motion coverage, additional structural framing and cover production, device assembly and state refinement, nature variants, complete per-asset reference evidence, UI/audio acceptance and measured performance. The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md).

Playtest ADS, reloads, movement/slide readability, reticle bounds, basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.

Source-only rendering research after Review 20 is recorded in [the rifle budget findings](RIFLE-RENDER-BUDGET.md). The candidate passes correctness checks but does not establish a frame-time improvement; its runtime integration is not installed and Review 21 is not promoted. The archived prototype remains absent from Review 23; historical Review 20 and its ZIP/checksum are preserved.
