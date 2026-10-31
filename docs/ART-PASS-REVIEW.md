# Rail Arena v1 alpha: integrated art build 27

Review 27 adds Blender-authored finger release, magazine grasp and return during the existing two-second rifle reload. Original wrist/magazine tracks, right-hand grip, recoil, meshes and compatible skeleton remain unchanged. It retains all prior graphical upgrades and Review 26's corrected finger bindings. Gameplay, movement, accuracy, damage, cadence, reload timing, trajectories and network protocol are preserved. This verified local playtest milestone does not complete the full art-production plan. Production continues without intermediate approval holds; Sanders retains final native input, sound and artistic acceptance. Public prerelease publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-27/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-27.zip`. Press **G** for Physics Lab; movement and graphics controls remain in Settings.

Portable ZIP: **451,900,175 bytes**, **877 file hashes verified in the archive and again after fresh extraction**. SHA-256: `9b26e7f6258c27e26eabe2c14706870612929900dcf235968ac9428fec32ff8c`. The package contains twelve verified prior reference families plus 115 new reload-hand references, including the editable Blender master, measured sheets, native before/after motion, source calibration and focused evidence. Licenses and `Run-Checks.ps1` are included. Historical packages and diagnostic work remain preserved.

- [Authored reload hands, measured poses and native motion](../ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1/index.html).
- [Corrected hands, measured assembly and motion comparison](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2/index.html).
- [Binding root cause, implementation and reproduction](HAND-BIND-CALIBRATION.md).
- [Full gameplay, menu and reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Current inventory and production gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).

The build retains painted characters/portraits, calibrated arms, standing/locomotion/airborne/crouch/slide/reaction clips, authored rifle raise and two-second reload, rifle materials and physical optic, stone paving/normal detail, arena/bridge/summit construction, staging/basement decks, cover shells, painted trees/shrubs/grass/ivy, floating medical cores, route labels, clouds, waterfalls, cel lighting, menus and HUD. The distracting cyan particle cloud remains absent.

## Focused hand evidence

[Authored reload hand workflow and limits](AUTHORED-RELOAD-HANDS.md). The measured master contains the original visible rifle/arm geometry, 54-bone compatible rig, 121 action frames and six same-scale six-view pose sheets. Native images and silent before/after reels accompany the technical views. All four appearance skins have current normal-camera key views.

Native validation passes 484 corrected FPS poses plus the 121-pose original-grip reference, 12,100 total sleeve-joint centre samples, 28 third-person contact poses, interruption/reset checks and disabled/inactive corpse-hand ownership. Maximum FPS wrist error is 0.00295 mm, maximum sleeve-joint gap 0.00276 mm and authored/native fingertip-centre difference 0.00225 mm. Reopening the saved master verifies 468,512 skin samples within 0.001 mm. The original right-hand grip stays unchanged.

At grasp, five fingertip sleeve-ring centres sit 9 mm from the magazine surface versus roughly 32–65 mm with the original fixed support pose. This is fitting evidence rather than complete glove/palm collision certification. Fine fit and the broader action library remain unfinished. Clips include 121 inclusive 60 Hz boundary samples of the two-second action (2.0167 seconds encoded), with no audio/input acceptance claim.

The Review 26 binding correction remains installed across twelve character LOD skins and four arm skins. Its byte-identical geometry/weights/topology and original skeleton/Avatar contracts remain preserved. Early stale-pose and camera-origin reload diagnostics are retained locally outside production artwork; the runtime wrist path/IK required no correction.

## Validation on the shipped executable

Source commit: `8c4f44ac6745a9004dbebf8028cbec65480258ab`. Build fingerprint: `0554960c3e2ccce4b10dbf19782434b1c894c0197988abf38a114c9ca244d3e1`. [Integrated shipping evidence](../Validation/art-integrated-final-27.json), [portable verification](../Validation/art-package-verification-27.json).

- All 39 mechanics checks passed.
- A 45-second automated player/three-bot match passed: 155 shots, eight deaths, two pickups, twenty-two pad events, zero exceptions and zero actor voids.
- Both local multiplayer peers passed 90 seconds with zero errors. Slide observations: host 1769/2711, client 2078/2406. This is loopback evidence, not real-internet acceptance; the movement driver intentionally exercises falls.
- Five actual 1920×1080 High-tier rendered views passed non-black checks and were visually inspected in a labelled contact sheet. Checks ran directly on the shipping artifact.

RTX 2080 / Direct3D12 forced offscreen probe, including synchronous readback overhead:

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 20.722 | 44.061 |
| summit | 15.797 | 19.682 |
| basement | 14.516 | 16.770 |
| foliage-waterfall | 14.050 | 17.141 |
| portal | 12.289 | 14.667 |

Combat, summit, basement and foliage/waterfall P95 exceed the 16.67 ms probe budget. Combat has a large 44.061 ms P95 spike in this run. Its cause is not established; this run is not controlled A/B evidence of a hand-action cost. Zero valid GPU timing samples were returned. These readback-inclusive timings do not establish native frame pacing or sustained 60 FPS. Preserve this exception for focused attribution and native performance work.

## Remaining production and QA

The inventory contains 4,126 paths: 1,787 production-evidence paths, 103 historical references, thirteen archived-research paths and 2,223 baseline-only paths. [Structural audit](../Validation/ArtReferenceLinks-09/audit.md) found zero missing linked files. These counts describe files and links, not unique models or complete reference certification. The original 34-action matrix remains intact; eighteen rows have source integration evidence.

Next: attribute the combat timing spike and remaining frame-budget misses; continue fine palm/tap contact fit and the complete first-person action/transition library; remaining character combat/reload/respawn and stance transitions; structural framing/cover and measured device assemblies; waterfall lips/foam, cloudscape and distant ruins; broader nature/variant evidence; UI states/audio review; native GPU/pacing measurement and optimization of demonstrated bottlenecks. Distant paving joints and thin trim still need readability review. The ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The archived rendering experiments remain outside the installed runtime.

Playtest ADS, reload contacts, movement/slide readability, reticle bounds, basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. This build is ready for feedback while production continues; the full art pass and final artistic acceptance remain open.
