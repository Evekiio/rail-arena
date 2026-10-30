# Rail Arena v1 alpha: integrated art build 07

The Windows review package is working and includes the current graphical upgrades and runnable self-checks. Sanders' native playtest remains the final acceptance gate. The broader art-production goal remains active; this milestone does not claim complete animation replacement or 100% reference coverage.

## Play and inspect

Use `Builds/RailArena-v1Alpha-ArtPass-Review-07/RailArena.exe`, keeping its complete folder together, or extract `RailArena-v1Alpha-ArtPass-Review-07.zip` first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html)
- [Inventory production delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json) and [asset register](../ArtSource/Original/ArtPass-v1/integrated-asset-register.csv)
- [Normal-speed rifle reload and contact evidence](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html)
- [Validation and reproducibility record](../Validation/art-integrated-final.json)
- [Portable archive checksum and verification](../Validation/art-package-verification.json)

The package contains `Run-Checks.ps1`. Modes are `Mechanics`, `Bots`, `Duel` and `Performance`; results go to `TestResults`. It also contains shipping licenses, the exact build-input manifest, build identity, actual render configuration, validation evidence and a SHA-256 file manifest. Unity's backup/debug source folders are excluded from the archive.

## Installed presentation

Four refined painted characters and calibrated arms; selective contours; closer studio-lit menu portraits; authored two-second magazine/wrist reload; revised rifle surfaces with the physical open optic preserved; ivory stone paint with normal/cavity detail; modular cover shells; faceted canopies; floating medical cores; depth-tested route labels; shaped cloud banks; responsive waterfalls; baked-GI cel lighting and the production atmosphere pass are integrated together.

Movement, damage, spread, firing cadence, reload timing, trajectories, collision and network authority are preserved. The distracting cyan particle cloud is absent. Older sources and unrelated diagnostic work remain preserved.

## Validation

- Five focused visual/reload/portrait tests passed, followed by the refreshed three-resolution UI capture and the source-restoration check.
- All **39 mechanics checks** passed in the standalone player.
- A 45-second match with one automated human and three bots passed: **170 shots, 7 deaths, 2 pickups, 19 pad launches, zero void deaths and zero exceptions**.
- Both loopback duel peers passed statistics, shots, snapshots and movement coverage. Slide activity was confirmed for both actors. The test generator now requests the current slide command rather than the obsolete crouch-to-slide input.
- The final build's source fingerprint was independently reproduced. Its executable, Unity player, gameplay assemblies, resource assets and level data are byte-identical to the validated build. The last build-recipe correction restores an unloaded source pipeline by its saved asset identity; it changes no runtime content. The real build leaves the source quality settings and identity unchanged.

### Render workload

RTX 2080, Direct3D12, 1920x1080, High preset, one automated human and three bots. Each view has 60 warmup frames and 180 samples. Rendering is forced into an offscreen target, followed by a synchronized one-pixel GPU readback; the times include that overhead. Exported images contain actual non-black rendered scenes.

| View | Mean frame ms | 95th percentile ms |
|---|---:|---:|
| Combat | 9.53 | 11.55 |
| Summit | 9.12 | 10.64 |
| Basement | 9.85 | 11.87 |
| Foliage and waterfall | 10.34 | 12.96 |
| Portal | 10.37 | 12.62 |

These samples meet the 16.67 ms render budget. Isolated GPU timings were unavailable. The runs used `-noaudio`; overlay HUD, visible swapchain, native input, audible playback and sustained competitive play remain unverified. Earlier hidden-swapchain measurements with black screenshots were discarded.

## Remaining production and QA

The full authored FPS/body action library, replacement of reversed backward locomotion and accelerated transitions, complete per-family reference/variant evidence and remaining environment/device construction detail continue in production. Wrist reach checks do not certify finger/palm contact at every interpolated frame. The deferred ADS/portal projection corner case remains recorded and is addressed if it blocks presentation or acceptance.

For playtest, inspect ADS and reload contacts, movement and slide readability, reticle accuracy bounds, all four basement routes and returns, character/menu states, Physics Lab, deaths, audio and sustained frame pacing. Real-internet multiplayer acceptance remains separate from loopback checks. Public alpha publication remains held for Sanders' final acceptance.
