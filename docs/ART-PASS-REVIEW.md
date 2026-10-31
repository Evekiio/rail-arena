# Rail Arena v1 alpha: integrated art build 15

Review-15 adds original measured stone construction to the bridge, its two cover screens and two portal-adjacent piers. It retains the integrated graphical upgrades and seventeen original locomotion, airborne and combat-reaction clips for all four contenders, the staging/basement stone kit, aperture-cropped portal rendering and corrected under-bridge wall mounts. The earlier controlled portal profile reduced submitted draw calls by 61-71%; total frame timings were mixed. See [the measured comparison](PORTAL-RENDER-PERFORMANCE.md). This is a local playtest milestone, not completion of the entire art-production plan. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-15/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: `Builds/RailArena-v1Alpha-ArtPass-Review-15.zip` (133,326,235 bytes). SHA-256: `7c8d015932ece6a32f56aa2ed063741d5a50d4548295e92cedb77d531a913cf4`. All 376 shipped file hashes verified after extraction. The package includes the new bridge sheets/captures, combat reaction reel/comparison, lifecycle/contact records and retained earlier visual references. Reference production documents retain their source-stage status; this handoff and integrated-final validation identify the shipped milestone.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Current complete inventory register](../ArtSource/Original/ArtPass-v1/current-asset-register.csv) and [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).
- [Airborne and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward gait playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [Lateral master, equal-scale pose sheets and native-rate reels](../ArtSource/Original/ArtPass-v1/Animation/Strafe/index.html).
- [New hit/death master, six-view sheets, controlled comparison and lifecycle reel](../ArtSource/Original/ArtPass-v1/Animation/Reactions/index.html).
- [New measured deck kit and matched comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Bridge stone master, dimensioned modules and matching in-game cameras](../ArtSource/Original/ArtPass-v1/Environment/BridgeStone/index.html).
- [Corrected under-bridge destinations and measured mounting reference](../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-15.json).
- [Archive checksum and file verification](../Validation/art-package-verification-15.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical packages through Review-14 are preserved.

## Installed presentation

Five bridge-family placements use three original metre-scaled modules with nine LOD exports. Jointed paving, coping, teal/bronze trim, inset wall panels and underside ribs replace the original rendering. Eighteen geometry-derived orthographics, three dimensioned sheets and eight matching 1080p world-camera captures accompany the canonical Blender master. The focused installed review checks all fifteen LOD meshes, source envelopes, unchanged colliders/source mesh identities, unchanged return devices and cleanup. The captured review exposed a backing/panel overlap, corrected before packaging. [Bridge stone contract](BRIDGE-STONE-KIT.md).

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

Eight staging/basement visuals now use original stone construction: radial paving, coping, friezes, bronze inlay, inlaid landing crests and ribbed undersides. Two metre-scaled families retain the original 20-sided collision meshes and 0.6 m envelope. Three LODs contain 6,876 / 3,556 / 748 triangles. The focused installed test checks all 24 meshes and restores original visuals on removal.

The Sky Gardens art controller now installs original forward/backward and left/right walk, jog and run, takeoff, airborne hold and landing recovery. A 13-child blend follows actual velocity in the aim frame, keeps the body facing aim and plays the art locomotion state/children at native speed 1. The legacy controller retains its Gait multiplier. The current procedural crouch solve stays installed. New native-speed HitResponse and DeathFall states suppress reaction/grip interference on death and restore the normal lifecycle on respawn. Cached skin-contact points replace coarse corpse support for refined characters; only the cosmetic model moves, with no added collider or ragdoll. The forward gaits correct measured legacy sole penetration and landing blends. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories and network authority remain unchanged. The two under-bridge portal assemblies move 17 cm toward the corridor to expose their fields and restore reverse controller clearance; deck and pier geometry stay unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 150 shots, 8 deaths, 1 pickup and 21 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback duel peers passed statistics and movement checks in a 90-second run. Their scripted duel includes void deaths; the zero-void result applies only to the offline bot match. These smoke checks do not establish deterministic stance coverage, real-internet or native-input acceptance.

The retained lateral review covers 360 captured frames / 1,440 character samples, 256 sampled diagonal/crouched/yaw poses, handedness, grip reach, authority preservation and legacy restoration checks. Three actual-controller jump scenarios also pass against that graph. The current inventory joins 2,888 unique file paths, with 665 production-evidence paths and zero missing; these are not counts of unique 3D models or complete reference certification.

The combat-reaction review adds 1,200 character samples with independent whole-skin checks, a hit active before death, repeated death, facing/root preservation, hand/weapon/hitbox lifecycle and the existing 120-tick respawn. Minimum measured skin height is 6.822 mm; settled corpse contact is about 10 mm above the reference floor. Contact means of 0.103-0.117 ms per character come from a warm Editor probe, not player frame timing. [Contract, failure evidence and limits](AUTHORED-COMBAT-REACTIONS.md).

The retreat and forward libraries each have valid humanoid imports and a 540-frame armed playback covering 2,160 character samples. The airborne pose review adds 540 frames / 2,160 character samples. Three actual-controller jump scenarios add 360 frames and verify state transitions, discrete ballistic height and more than 5 mm sole clearance. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `88a41dcb22574167389d425540eff8da9f2eab7a`. Its source fingerprint independently reproduces as `cc25f7e3139d5ab84fa7260e573490ceda7d43d7b8b7711d9646c158c00c3e68`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 20.24 | 40.98 |
| summit | 14.54 | 17.30 |
| basement | 14.59 | 16.91 |
| foliage-waterfall | 14.53 | 17.31 |
| portal | 12.54 | 14.23 |

Combat mean and P95 exceed the 16.67 ms target; summit, basement and foliage/waterfall also exceed it at P95. These measurements are retained without a repeat to select passing timings. No isolated cause is attributed to the new stone assets without profiling. Performance acceptance remains open. Measurement version 2 stages the observer before portals; older reports used different portal poses and are not equivalent workloads. The separate fixed-step comparison measures rendering work, not native pacing. GPU samples remain unavailable.

## Continuing production and QA

Remaining full directional transitions, authored crouch/slide/traversal, full interrupted hit/death stance combinations and FPS/body actions, full stance combinations, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The [under-bridge obstruction and reverse traversal failure](UNDERBRIDGE-PORTAL-VISIBILITY.md) are corrected with measured wall mounting, 24 standing/crouched crossings, 36 aperture rays, all four route checks and normal/ADS destination captures.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
