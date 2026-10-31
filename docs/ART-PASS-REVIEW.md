# Rail Arena v1 alpha: integrated art build 25

Review 25 retains all Review 23 art upgrades and corrects overlapping deck backing/coping faces in the canonical Blender model. Original collision hulls, deck top planes, route transforms, pads and portals remain authoritative. This is a verified local playtest milestone; the complete art-production plan remains unfinished. Production continues without intermediate approval holds. Sanders retains final native input, sound and artistic acceptance; public prerelease publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-25/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-25.zip`. Press **G** for Physics Lab. Movement and graphics controls remain in Settings.

Portable ZIP: **253,804,801 bytes**, **645 shipped file hashes verified in the archive and again after fresh extraction**. SHA-256: `6dfa6e8545bf65fed199854aaaa241c7819cab47562c89904604ec0302687af3`. The package contains ten verified prior reference families plus 47 current basement-deck references, including the corrected Blender master, measured construction views, six Unity-ready FBXs, native comparisons and focused test evidence. Licenses and `Run-Checks.ps1` are included. Historical packages remain preserved; Review 24 is an unpromoted rendering-research artifact.

- [Corrected deck master, measured views and comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Standing-ready source and native playback](../ArtSource/Original/ArtPass-v1/Animation/Standing/index.html).
- [First-person ready and ADS comparison](../ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady/index.html).
- [Painted nature, measured modules and wind](../ArtSource/Original/ArtPass-v1/Nature/GardenNature/index.html).
- [Full gameplay, menu and reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).

The build retains four painted characters and portraits, calibrated first-person arms, standing/locomotion/airborne/crouch/slide/reaction clips, the authored rifle raise and two-second reload, revised rifle materials and physical optic, stone paving/normal detail, arena/bridge/summit construction, eight staging/basement decks, cover shells, painted trees/shrubs/grass/ivy, floating medical cores, route labels, cloud banks, waterfalls, cel lighting, menus and HUD. All four basement routes and returns remain intact. The distracting cyan particle cloud remains absent. Gameplay movement, accuracy, damage, weapon cadence, reload timing, trajectories and multiplayer protocol are preserved. Unrelated dirty diagnostic work is preserved outside the clean shipping source.

## Validation on the shipped executable

Source commit: `76976046a4b0c78ad564a04048e7bfcc3e1c6892`. Verified build fingerprint: `0ea9acc9df8cc0d489140a9e8a8f3a5e941e6f0799bad3daf265315eb92a5ee7`. [Integrated shipping evidence](../Validation/art-integrated-final-25.json), [portable verification](../Validation/art-package-verification-25.json).

- All 39 mechanics checks passed.
- A 45-second match with one automated player and three bots passed: 179 shots, ten deaths, four pickups, twenty pad events, zero exceptions and zero actor voids.
- Both local multiplayer peers passed 90 seconds with zero errors. Slide observations: host 1477/2191, client 1541/2339. This is loopback evidence, not real-internet acceptance.
- Five actual 1920×1080 High-tier rendered views passed non-black-content checks and were visually inspected. All checks executed directly on this shipping artifact.
- Two focused native checks tied the deck images to the current master and preserved eight decks/24 LOD instances, collider identities and return-device transforms.

RTX 2080 / Direct3D12 forced offscreen probe, including synchronous readback overhead:

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 15.568 | 17.335 |
| summit | 13.893 | 16.009 |
| basement | 13.776 | 16.494 |
| foliage-waterfall | 14.206 | 16.444 |
| portal | 12.201 | 14.038 |

Combat P95 exceeds the 16.67 ms probe budget; the other four views meet it in this run. No valid GPU timing samples were returned. These timings include capture overhead and do not establish native frame pacing or sustained 60 FPS. Differences from earlier builds are not controlled A/B evidence and are not attributed to the deck correction.

The stone material experiment reduced draws 34.6–44.1% but produced mixed timings across two reverse-ordered same-executable pairs. Its 109 source files and derived exports are archived outside Unity compilation/resources; it is not installed. The independent deck geometry fix remains. [Research and measured decision](STONE-SUBMISSION-RESEARCH.md).

## Remaining production and QA

The current source inventory contains 3,909 paths: 1,673 production-evidence paths, thirteen archived-research paths and 2,223 baseline-only paths. These counts include the held assembly reference batch authored after Review 25; those source-only references are not added retroactively to its immutable ZIP. The structural audit found zero missing linked files. These are file/link counts, not unique model counts or complete reference certification. [Reference-link audit](../Validation/ArtReferenceLinks-07/audit.md), [current gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).

The original 34-action matrix remains intact; eighteen rows have source integration evidence. Full first-person action and assembled rig references, character combat/reload/respawn and temporal transitions, complete per-asset construction/variant evidence, remaining structural framing/cover and device assemblies, waterfall lips/foam/cloudscape, broader nature coverage, UI/audio acceptance and measured native performance remain unfinished. Distant paving joints and thin trim still need aliasing/readability review. The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance.

Playtest ADS, reload contacts, movement/slide readability, reticle bounds, basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. This build is ready for Sanders' feedback while production continues; it is not a claim that the full art pass or final artistic acceptance is complete.

## Source references after Review 25

The [calibrated first-person assembly](FIRST-PERSON-ASSEMBLY.md) adds an editable held-reference Blender master, four appearances, eight hip/ADS poses, 48 equal-scale orthographics and matched native/Blender eye views. Saved-master verification covers 31,012 posed arm vertices, 78,528 rigid part vertices, 432 bone heads, original weights/hierarchy, rifle topology/UVs and exact socket/camera frames. No runtime replacement or new motion clip is installed. The orthographics expose finger/grip defects for the next hand-art batch. This does not complete the full first-person action/import/reference requirements or change the verified Review 25 executable.
