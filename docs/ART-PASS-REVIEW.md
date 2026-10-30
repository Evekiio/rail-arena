# Rail Arena v1 alpha: integrated art build 12

Review-12 includes the integrated graphical upgrades and nine original forward/backward and airborne animations for all four contenders, plus the measured staging/basement stone kit, aperture-cropped portal rendering and corrected under-bridge wall mounts. The controlled profile reduces submitted draw calls by 61-71%; total frame timings are mixed. See [the measured comparison](PORTAL-RENDER-PERFORMANCE.md). It is a local playtest milestone, not completion of the entire art-production plan. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-12/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: `Builds/RailArena-v1Alpha-ArtPass-Review-12.zip` (112,275,985 bytes). SHA-256: `f40b532f063179bd34292379eca48bab6243dec38cd788daa502fb2c92e8ab01`. All 342 shipped file hashes verified after extraction. The package includes the under-bridge focused reports, mounting reference and four normal/ADS destination captures.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Current complete inventory register](../ArtSource/Original/ArtPass-v1/current-asset-register.csv) and [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).
- [Airborne and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward gait playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [New measured deck kit and matched comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Corrected under-bridge destinations and measured mounting reference](../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-12.json).
- [Archive checksum and file verification](../Validation/art-package-verification-12.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical Review-07 through Review-11 are preserved.

## Installed presentation

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

Eight staging/basement visuals now use original stone construction: radial paving, coping, friezes, bronze inlay, inlaid landing crests and ribbed undersides. Two metre-scaled families retain the original 20-sided collision meshes and 0.6 m envelope. Three LODs contain 6,876 / 3,556 / 748 triangles. The focused installed test checks all 24 meshes and restores original visuals on removal.

The Sky Gardens art controller now installs original forward/backward walk, jog and run, takeoff, airborne hold and landing recovery at native state speeds. The forward gaits correct measured legacy sole penetration and landing blends. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories and network authority remain unchanged. The two under-bridge portal assemblies move 17 cm toward the corridor to expose their fields and restore reverse controller clearance; deck and pier geometry stay unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 141 shots, 8 deaths, 1 pickup and 20 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback duel peers passed statistics and movement checks in a 90-second run. Historical Review-11 failed attempts remain preserved separately in its evidence; these smoke checks do not establish deterministic stance coverage. These automated tests do not establish real-internet or native-input acceptance.

The retreat and forward libraries each have valid humanoid imports and a 540-frame armed playback covering 2,160 character samples. The airborne pose review adds 540 frames / 2,160 character samples. Three actual-controller jump scenarios add 360 frames and verify state transitions, discrete ballistic height and more than 5 mm sole clearance. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `f8a3cd66c04f7492f5e8d81a06c988988b0622fc`. Its source fingerprint independently reproduces as `98421b356db342b69bb5b5ba6645e5df44e4b7c3034911b9f1d810358678e6a5`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 18.08 | 35.94 |
| summit | 13.65 | 16.05 |
| basement | 14.08 | 17.09 |
| foliage-waterfall | 13.81 | 15.79 |
| portal | 12.67 | 14.82 |

Combat and basement P95 exceed the 16.67 ms target. Performance acceptance remains open. Measurement version 2 stages the observer before portals; older reports used different portal poses and are not equivalent workloads. The separate fixed-step comparison measures rendering work, not native pacing. GPU samples remain unavailable.

## Continuing production and QA

Remaining strafe, crouch/slide/traversal, death/reaction and FPS/body actions, full stance combinations, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The [under-bridge obstruction and reverse traversal failure](UNDERBRIDGE-PORTAL-VISIBILITY.md) are corrected with measured wall mounting, 24 standing/crouched crossings, 36 aperture rays, all four route checks and normal/ADS destination captures.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
