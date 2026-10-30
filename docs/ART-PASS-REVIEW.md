# Rail Arena v1 alpha: integrated art build 08

Review-08 includes the integrated graphical upgrades and original retreat animations for all four contenders. It is a local playtest milestone, not completion of the entire art-production plan. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-08/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-08.json).
- [Archive checksum and file verification](../Validation/art-package-verification-08.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical Review-07 is preserved.

## Installed presentation

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

This batch replaces reversed backward walk/jog/run with dedicated Blender-authored clips through the optional Sky Gardens controller override. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories, collision and network authority remain unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 158 shots, 9 deaths, 2 pickups and 20 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback duel peers passed statistics and movement checks. These automated tests do not establish real-internet or native-input acceptance.

The original retreat library has valid humanoid imports, installed direction/root checks and a 540-frame armed playback covering 2,160 character samples. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `33a4480980c0a252abb4c15282469bee5f6419d6`. Its source fingerprint independently reproduces as `84d08adda4d5b5d6351e1e2d7a6ff7aa462ecb3d50acf17f79505568a03ded0a`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| Combat | 9.76 | 11.10 |
| Summit | 9.29 | 11.76 |
| Basement | 9.43 | 11.30 |
| Foliage / waterfall | 10.45 | **17.93** |
| Portal | 10.41 | 13.66 |

The foliage/waterfall P95 exceeds the 16.67 ms target. Performance acceptance remains open; the report records this exception. Earlier black-swapchain timings are excluded.

## Continuing production and QA

Authored jump/landing and remaining FPS/body actions, moving-world stance transitions, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
