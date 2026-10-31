# Rail Arena v1 alpha: integrated art build 26

Review 26 fixes detached glove segments by retaining authored finger bind frames instead of replacing them with legacy rest frames. Twelve character LOD skins and four first-person arm skins are regenerated. All previous graphical work, four basement routes and returns, gameplay, movement, accuracy, damage, weapon cadence, reload timing, trajectories and multiplayer protocol are preserved. This is a verified local playtest milestone; the complete art-production plan remains unfinished. Continue production without intermediate approval holds. Sanders retains final native input, sound and artistic acceptance; public prerelease publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-26/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-26.zip`. Press **G** for Physics Lab; movement and graphics controls remain in Settings.

Portable ZIP: **365,611,248 bytes**, **762 file hashes verified in the archive and again after fresh extraction**. SHA-256: `960e8f0c1fe0a8dc08ccccca4a0aab1e236c984881ae25ebddfdddbcade77142`. The package contains eleven verified prior reference families plus 117 current hand-binding references, including the editable Assembly-v2 Blender master, all measured views, native captures and before/after motion. Licenses and `Run-Checks.ps1` are included. Historical packages and diagnostic work remain preserved; Review 24 is an unpromoted rendering experiment.

- [Corrected hands, measured assembly and motion comparison](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2/index.html).
- [Binding root cause, implementation and reproduction](HAND-BIND-CALIBRATION.md).
- [Full gameplay, menu and reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Current inventory and production gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).

The build retains painted characters/portraits, calibrated arms, standing/locomotion/airborne/crouch/slide/reaction clips, authored rifle raise and two-second reload, rifle materials and physical optic, stone paving/normal detail, arena/bridge/summit construction, staging/basement decks, cover shells, painted trees/shrubs/grass/ivy, floating medical cores, route labels, clouds, waterfalls, cel lighting, menus and HUD. The distracting cyan particle cloud remains absent.

## Focused hand evidence

All twelve LODs preserve 36 byte-identical vertex streams and topology, 360 authored finger bind matrices and 288 established body matrices. Original Blender character masters, FBXs, weights, skeleton and Avatar are unchanged. Eight native hip/ADS views cover four appearances. Reopening the measured Assembly-v2 master verifies all 31,012 posed arm vertices, 432 bone heads, rifle parts, weights/hierarchy and camera/socket frames.

The native same-state motion comparison covers equip, ADS/recoil articulation, authored reload and settling. Across 3,600 corrected sleeve-joint centre samples, maximum separation falls from 50.4365 mm to 0.00274 mm. Actor position, velocity, capsule, simulation tick, ammo and next shot time remain unchanged. This is narrow deformation evidence, not complete surface/contact fit or a newly authored action library. Clips are silent reference, not native input/audio acceptance. Assembly-v1 remains historical.

## Validation on the shipped executable

Source commit: `d5d13a3b352000dcc9d7499fda041a50cd334f50`. Build fingerprint: `52b097a64a3d30c085a971e9c8ee53b7a0089f78c2d2f8c10f89879ddd7deac6`. [Integrated shipping evidence](../Validation/art-integrated-final-26.json), [portable verification](../Validation/art-package-verification-26.json).

- All 39 mechanics checks passed.
- A 45-second automated player/three-bot match passed: 157 shots, ten deaths, nineteen pad events, zero exceptions and zero actor voids. No pickup occurred in this particular smoke run.
- Both local multiplayer peers passed 90 seconds with zero errors. Slide observations: host 1200/2541, client 1836/1954. This is loopback evidence, not real-internet acceptance; the movement driver intentionally exercises falls.
- Five actual 1920×1080 High-tier rendered views passed non-black checks and were visually inspected. Checks ran directly on the shipping artifact.

RTX 2080 / Direct3D12 forced offscreen probe, including synchronous readback overhead:

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 14.259 | 16.627 |
| summit | 14.238 | 18.605 |
| basement | 15.162 | 20.338 |
| foliage-waterfall | 14.835 | 18.499 |
| portal | 12.141 | 14.289 |

Summit, basement and foliage/waterfall P95 exceed the 16.67 ms probe budget. Zero valid GPU timing samples were returned. These readback-inclusive timings do not establish native frame pacing or sustained 60 FPS; differences from prior builds are not controlled A/B evidence and are not attributed to the hand correction.

## Remaining production and QA

The inventory contains 4,017 paths: 1,678 production-evidence paths, 103 historical references, thirteen archived-research paths and 2,223 baseline-only paths. [Structural audit](../Validation/ArtReferenceLinks-08/audit.md) found zero missing linked files. These counts describe files and links, not unique models or complete reference certification. The original 34-action matrix remains intact; eighteen rows have source integration evidence.

Next: fine rifle/magazine finger contacts and the complete first-person action/transition library; remaining character combat/reload/respawn and stance transitions; structural framing/cover and measured device assemblies; waterfall lips/foam, cloudscape and distant ruins; broader nature/variant evidence; UI states/audio review; native GPU/pacing measurement and optimization of demonstrated bottlenecks. Distant paving joints and thin trim still need readability review. The ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The archived rendering experiments remain outside the installed runtime.

Playtest ADS, reload contacts, movement/slide readability, reticle bounds, basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. This build is ready for feedback while production continues; the full art pass and final artistic acceptance remain open.

## Source art after Review 26

The [authored reload hand action](AUTHORED-RELOAD-HANDS.md) adds release, magazine grasp and return while retaining the original wrist/magazine curves and two-second timing. Four appearance FPS sweeps, 28 third-person contact poses, interruption/reset checks and reopened Blender-master verification pass. Source integration is pending the next Windows package. Inventory now has 4,126 paths, with 1,787 production-evidence paths, 103 historical references, thirteen archived research paths and 2,223 baseline-only paths. [Updated structural audit](../Validation/ArtReferenceLinks-09/audit.md) reports zero missing linked files. The existing Review 26 ZIP is immutable.
