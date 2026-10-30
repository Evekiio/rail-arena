# Rail Arena v1 alpha: integrated art build 11

Review-11 includes the integrated graphical upgrades and nine original forward/backward and airborne animations for all four contenders, plus the measured staging/basement stone kit and aperture-cropped portal rendering. The controlled profile reduces submitted draw calls by 61�71%; total frame timings are mixed. See [the measured comparison](PORTAL-RENDER-PERFORMANCE.md). It is a local playtest milestone, not completion of the entire art-production plan. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-11/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: `Builds/RailArena-v1Alpha-ArtPass-Review-11.zip` (109,317,194 bytes). SHA-256: `a9f62a67d2aa70a21257517d765800295a8bb4f57d93ad90088c276f9d18ca53`. All 335 shipped file hashes verified after archive extraction.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Current complete inventory register](../ArtSource/Original/ArtPass-v1/current-asset-register.csv) and [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).
- [Airborne and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward gait playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [New measured deck kit and matched comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-11.json).
- [Archive checksum and file verification](../Validation/art-package-verification-11.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical Review-07 through Review-10 are preserved.

## Installed presentation

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

Eight staging/basement visuals now use original stone construction: radial paving, coping, friezes, bronze inlay, inlaid landing crests and ribbed undersides. Two metre-scaled families retain the original 20-sided collision meshes and 0.6 m envelope. Three LODs contain 6,876 / 3,556 / 748 triangles. The focused installed test checks all 24 meshes and restores original visuals on removal.

The Sky Gardens art controller now installs original forward/backward walk, jog and run, takeoff, airborne hold and landing recovery at native state speeds. The forward gaits correct measured legacy sole penetration and landing blends. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories, collision and network authority remain unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 143 shots, 6 deaths, 3 pickups and 17 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback duel peers passed statistics and movement checks in the extended 90-second run. The restricted-network failure and connected 45-second run missing host slide coverage are preserved; see the profiling report for limits. These automated tests do not establish real-internet or native-input acceptance.

The retreat and forward libraries each have valid humanoid imports and a 540-frame armed playback covering 2,160 character samples. The airborne pose review adds 540 frames / 2,160 character samples. Three actual-controller jump scenarios add 360 frames and verify state transitions, discrete ballistic height and more than 5 mm sole clearance. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `09e4e506907d492510ad79221a0cb6ad79489738`. Its source fingerprint independently reproduces as `f577ac97b6235a6b15e7b685c78c4e991e80dc6bd444328291263d13ac4a73ac`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 18.43 | 38.78 |
| summit | 13.98 | 16.16 |
| basement | 14.18 | 16.35 |
| foliage-waterfall | 14.06 | 16.23 |
| portal | 12.41 | 14.61 |

Combat P95 exceeds the 16.67 ms target. Performance acceptance remains open. This measurement version 2 stages the observer before portals; older reports used a different portal pose and are not directly equivalent workloads. The fixed-step controlled profile separately measures rendering work, not native pacing. GPU samples remain unavailable.

## Continuing production and QA

Remaining strafe, crouch/slide/traversal, death/reaction and FPS/body actions, full stance combinations, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The newly observed [under-bridge pier occlusion](UNDERBRIDGE-PORTAL-VISIBILITY.md) also requires a measured placement correction and bidirectional route validation.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
