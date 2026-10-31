# Rail Arena v1 alpha: integrated art build 17

Review-17 adds five original grounded crouch motions to all four contenders, retaining the summit/bridge stone kits and all earlier graphical upgrades. The art controller now includes twenty-two original body clips. Full-body crouch enter/exit clips remain references. This is a local playtest milestone; the full art-production plan remains incomplete. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-17/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: `Builds/RailArena-v1Alpha-ArtPass-Review-17.zip` (150,614,335 bytes). SHA-256: `a433ee16c05f1fc759071e5179d53ae5d3a8e6a5ae8bf1e19e4fbc6b3ec88c21`. All 405 shipped file hashes verified after extraction. The package includes the grounded crouch reel, contacts/lifecycle evidence, summit and bridge sheets/captures, combat reaction reel and earlier references. Reference production documents retain source-stage wording; REFERENCE-CONTEXT.txt and integrated-final validation distinguish it from the shipping result.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Grounded crouch master, measured poses and native controller reel](../ArtSource/Original/ArtPass-v1/Animation/Crouch/index.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Current complete inventory register](../ArtSource/Original/ArtPass-v1/current-asset-register.csv) and [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).
- [Airborne and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward gait playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [Lateral master, equal-scale pose sheets and native-rate reels](../ArtSource/Original/ArtPass-v1/Animation/Strafe/index.html).
- [New hit/death master, six-view sheets, controlled comparison and lifecycle reel](../ArtSource/Original/ArtPass-v1/Animation/Reactions/index.html).
- [New measured deck kit and matched comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Bridge stone master, dimensioned modules and matching in-game cameras](../ArtSource/Original/ArtPass-v1/Environment/BridgeStone/index.html).
- [Summit stone master, controller-scale sheets and matching in-game cameras](../ArtSource/Original/ArtPass-v1/Environment/SummitStone/index.html).
- [Corrected under-bridge destinations and measured mounting reference](../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-17.json).
- [Archive checksum and file verification](../Validation/art-package-verification-17.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical packages through Review-16 are preserved.

## Installed presentation

The summit deck, central cover and four columns now use three original metre-scaled modules with nine reducing LODs. Radial paving, a compass seal, coping/frieze, underside rays and banded columns share the established painted stone materials. Eighteen orthographics, three dimensioned sheets with a controller-scale comparator and eight matching in-game cameras accompany the source. The installed review preserves original collider vertices/triangles, transforms, all return-device transforms and pad destinations, and cleanup. Captures exposed a column core overlap, corrected before the final review. [Summit contract](SUMMIT-STONE-KIT.md).

Five bridge-family placements use three original metre-scaled modules with nine LOD exports. Jointed paving, coping, teal/bronze trim, inset wall panels and underside ribs replace the original rendering. Eighteen geometry-derived orthographics, three dimensioned sheets and eight matching 1080p world-camera captures accompany the canonical Blender master. The focused installed review checks all fifteen LOD meshes, source envelopes, unchanged colliders/source mesh identities, unchanged return devices and cleanup. The captured review exposed a backing/panel overlap, corrected before packaging. [Bridge stone contract](BRIDGE-STONE-KIT.md).

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

Eight staging/basement visuals now use original stone construction: radial paving, coping, friezes, bronze inlay, inlaid landing crests and ribbed undersides. Two metre-scaled families retain the original 20-sided collision meshes and 0.6 m envelope. Three LODs contain 6,876 / 3,556 / 748 triangles. The focused installed test checks all 24 meshes and restores original visuals on removal.

The Sky Gardens art controller now installs original forward/backward and left/right walk, jog and run, takeoff, airborne hold and landing recovery. A 13-child blend follows actual velocity in the aim frame, keeps the body facing aim and plays the art locomotion state/children at native speed 1. The legacy controller retains its Gait multiplier. Five grounded crouch loops now use native foot IK and immediate stance synchronization, preventing double lowering. Airborne, sliding and legacy maps retain their procedural fallback. Crouch enter/exit body clips remain reference-only to preserve immediate controller response. Hitbox definitions and local attachments stay unchanged; their world positions follow the new pose. New native-speed HitResponse and DeathFall states suppress reaction/grip interference on death and restore the normal lifecycle on respawn. Cached skin-contact points replace coarse corpse support for refined characters; only the cosmetic model moves, with no added collider or ragdoll. The forward gaits correct measured legacy sole penetration and landing blends. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories and network authority remain unchanged. The two under-bridge portal assemblies move 17 cm toward the corridor to expose their fields and restore reverse controller clearance; deck and pier geometry stay unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 144 shots, 8 deaths, 1 pickup and 20 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback peers passed movement and statistics checks over 90 seconds. Scripted duel void deaths are distinct from the zero-void offline match result. These smoke checks do not establish deterministic stance coverage, real-internet or native-input acceptance.

The retained lateral review covers 360 captured frames / 1,440 character samples, 256 sampled diagonal/crouched/yaw poses, handedness, grip reach, authority preservation and legacy restoration checks. Three actual-controller jump scenarios also pass against that graph. The current inventory joins 3,214 unique file paths, with 991 production-evidence paths and zero missing; these are not counts of unique 3D models or complete reference certification.

The crouch review covers 676 candidate samples, 2,400 installed samples and 544 directional/yaw/phase poses across four characters. Immediate crouch/stand response, jump/slide fallback, death/respawn, legacy restoration and stable actor roots pass. Native whole-skin floor clearance is at least 8.4 mm in the staged review. The ten-second controller reel does not certify moving-world foot lock or native input/audio. [Crouch contract](AUTHORED-CROUCH.md).

The combat-reaction review adds 1,200 character samples with independent whole-skin checks, a hit active before death, repeated death, facing/root preservation, hand/weapon/hitbox lifecycle and the existing 120-tick respawn. Minimum measured skin height is 6.822 mm; settled corpse contact is about 10 mm above the reference floor. Contact means of 0.103-0.117 ms per character come from a warm Editor probe, not player frame timing. [Contract, failure evidence and limits](AUTHORED-COMBAT-REACTIONS.md).

The retreat and forward libraries each have valid humanoid imports and a 540-frame armed playback covering 2,160 character samples. The airborne pose review adds 540 frames / 2,160 character samples. Three actual-controller jump scenarios add 360 frames and verify state transitions, discrete ballistic height and more than 5 mm sole clearance. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `09662f15f2da7e407baf3051019ac74eff9853e3`. Its source fingerprint independently reproduces as `6ecf431e602d942be73456b42c7d05fa992daac02848325d1735cd164a3448c4`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 15.55 | 18.15 |
| summit | 13.60 | 16.58 |
| basement | 14.04 | 16.26 |
| foliage-waterfall | 14.56 | 16.78 |
| portal | 12.16 | 14.18 |

Combat and foliage/waterfall P95 exceed the 16.67 ms target. These measurements are retained without a repeat to select passing timings. The changing view timings across milestones are not a controlled A/B comparison; no isolated cause or performance gain is attributed to the new assets. Performance acceptance remains open. Measurement version 2 stages the observer before portals; older reports used different portal poses and are not equivalent workloads. The separate fixed-step comparison measures rendering work, not native pacing. GPU samples remain unavailable.

## Continuing production and QA

Remaining full directional transitions, cosmetic crouch transitions, authored slide/traversal, full interrupted hit/death stance combinations and FPS/body actions, full stance combinations, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The [under-bridge obstruction and reverse traversal failure](UNDERBRIDGE-PORTAL-VISIBILITY.md) are corrected with measured wall mounting, 24 standing/crouched crossings, 36 aperture rays, all four route checks and normal/ADS destination captures.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
