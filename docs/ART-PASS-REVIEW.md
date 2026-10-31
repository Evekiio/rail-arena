# Rail Arena v1 alpha: integrated art build 14

Review-14 includes the integrated graphical upgrades and seventeen original locomotion, airborne and combat-reaction clips for all four contenders, plus the measured staging/basement stone kit, aperture-cropped portal rendering and corrected under-bridge wall mounts. The controlled profile reduces submitted draw calls by 61-71%; total frame timings are mixed. See [the measured comparison](PORTAL-RENDER-PERFORMANCE.md). It is a local playtest milestone, not completion of the entire art-production plan. Sanders retains final native input, sound and artistic acceptance. Public publication remains held.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-14/RailArena.exe` with its folder intact, or extract the corresponding ZIP first. `G` opens Physics Lab. Existing controls and graphics presets remain in Settings.

Portable ZIP: `Builds/RailArena-v1Alpha-ArtPass-Review-14.zip` (121,848,619 bytes). SHA-256: `63dac3f79a814854f7d2cc8e77ec9675f23d15472e30a431b30de3c2256dc4fd`. All 360 shipped file hashes verified after extraction. The package includes the combat reaction reel/comparison, lifecycle/contact records and retained earlier visual references.

- [Integrated gameplay, menu and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Armed retreat playback and measured poses](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Reload animation and contacts](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html).
- [Current complete inventory register](../ArtSource/Original/ArtPass-v1/current-asset-register.csv) and [coverage gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).
- [Airborne and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward gait playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [Lateral master, equal-scale pose sheets and native-rate reels](../ArtSource/Original/ArtPass-v1/Animation/Strafe/index.html).
- [New hit/death master, six-view sheets, controlled comparison and lifecycle reel](../ArtSource/Original/ArtPass-v1/Animation/Reactions/index.html).
- [New measured deck kit and matched comparisons](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Corrected under-bridge destinations and measured mounting reference](../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted/index.html).
- [Inventory delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json).
- [Build validation](../Validation/art-integrated-final-14.json).
- [Archive checksum and file verification](../Validation/art-package-verification-14.json).

The portable package contains Run-Checks.ps1 (Mechanics, Bots, Duel, Performance), shipping licenses, source/build identities, render configuration, validation records and a SHA-256 manifest. Unity backup/debug source folders are excluded. Historical Review-07 through Review-13 are preserved.

## Installed presentation

Four refined painted characters and calibrated arms, selective contours, studio-lit menu portraits, authored two-second magazine/wrist reload, revised rifle surfaces and physical optic, ivory stone paint and normal detail, modular cover shells, faceted canopies, floating medical cores, route labels, cloud banks, responsive waterfalls and the cel-lighting/atmosphere pass remain integrated together.

Eight staging/basement visuals now use original stone construction: radial paving, coping, friezes, bronze inlay, inlaid landing crests and ribbed undersides. Two metre-scaled families retain the original 20-sided collision meshes and 0.6 m envelope. Three LODs contain 6,876 / 3,556 / 748 triangles. The focused installed test checks all 24 meshes and restores original visuals on removal.

The Sky Gardens art controller now installs original forward/backward and left/right walk, jog and run, takeoff, airborne hold and landing recovery. A 13-child blend follows actual velocity in the aim frame, keeps the body facing aim and plays the art locomotion state/children at native speed 1. The legacy controller retains its Gait multiplier. The current procedural crouch solve stays installed. New native-speed HitResponse and DeathFall states suppress reaction/grip interference on death and restore the normal lifecycle on respawn. Cached skin-contact points replace coarse corpse support for refined characters; only the cosmetic model moves, with no added collider or ragdoll. The forward gaits correct measured legacy sole penetration and landing blends. Legacy maps retain their controller. Movement, damage, weapon spread/cadence, reload timing, trajectories and network authority remain unchanged. The two under-bridge portal assemblies move 17 cm toward the corridor to expose their fields and restore reverse controller clearance; deck and pier geometry stay unchanged. The distracting cyan cloud remains absent. Unrelated diagnostic work is preserved.

## Validation

All 39 standalone mechanics checks passed. A 45-second match with three bots recorded 153 shots, 9 deaths, 2 pickups and 21 pad launches, with zero exceptions or void deaths. Both slide-enabled loopback duel peers passed statistics and movement checks in a 90-second run. Historical Review-11 failed attempts remain preserved separately in its evidence; these smoke checks do not establish deterministic stance coverage. These automated tests do not establish real-internet or native-input acceptance.

The lateral review adds 360 captured frames / 1,440 character samples, 256 sampled diagonal/crouched/yaw poses, handedness, grip reach, authority preservation and legacy restoration checks. Three actual-controller jump scenarios also pass against the changed graph. The current inventory joins 2,839 unique file paths, with 616 production-evidence paths and zero missing; these are not counts of unique 3D models or complete reference certification.

The combat-reaction review adds 1,200 character samples with independent whole-skin checks, a hit active before death, repeated death, facing/root preservation, hand/weapon/hitbox lifecycle and the existing 120-tick respawn. Minimum measured skin height is 6.822 mm; settled corpse contact is about 10 mm above the reference floor. Contact means of 0.103-0.117 ms per character come from a warm Editor probe, not player frame timing. [Contract, failure evidence and limits](AUTHORED-COMBAT-REACTIONS.md).

The retreat and forward libraries each have valid humanoid imports and a 540-frame armed playback covering 2,160 character samples. The airborne pose review adds 540 frames / 2,160 character samples. Three actual-controller jump scenarios add 360 frames and verify state transitions, discrete ballistic height and more than 5 mm sole clearance. Staged sole clearance is approximately 8.4–8.5 mm. Numerical wrist reach checks do not certify every palm/finger surface or moving-world foot slip.

The player built successfully from source commit `2e6aca2e476894bf1997c0f36b2b025bdf250422`. Its source fingerprint independently reproduces as `725b9b4435ac801d4fe8faa00a6f14058d41aee1853ff63d12c6ac387c97bdd2`. The build restores the source quality settings and diagnostic identity.

### Render workload

RTX 2080 / Direct3D12 / High / 1920x1080 / one automated human and three bots. Each view forces offscreen rendering with synchronized one-pixel GPU readback, 60 warmup frames and 180 samples. Readback overhead is included; overlay HUD and native swapchain are excluded. GPU timers returned zero valid samples.

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 18.19 | 40.76 |
| summit | 13.59 | 15.87 |
| basement | 14.02 | 16.45 |
| foliage-waterfall | 14.29 | 16.17 |
| portal | 12.29 | 13.92 |

Combat P95 exceeds the 16.67 ms target. This result is preserved; no repeat was run to select a passing timing. Performance acceptance remains open. Measurement version 2 stages the observer before portals; older reports used different portal poses and are not equivalent workloads. The separate fixed-step comparison measures rendering work, not native pacing. GPU samples remain unavailable.

## Continuing production and QA

Remaining full directional transitions, authored crouch/slide/traversal, full interrupted hit/death stance combinations and FPS/body actions, full stance combinations, complete reference/variant coverage and additional environment/device construction detail remain unfinished. See [production status](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md). The deferred ADS/portal corner case remains recorded for reproduction if it blocks acceptance. The [under-bridge obstruction and reverse traversal failure](UNDERBRIDGE-PORTAL-VISIBILITY.md) are corrected with measured wall mounting, 24 standing/crouched crossings, 36 aperture rays, all four route checks and normal/ADS destination captures.

Playtest ADS, reloads, retreat and slide readability, reticle bounds, all basement routes/returns, menu/character states, Physics Lab, deaths, sound and sustained frame pacing. Continue production without intermediate approval holds; final artistic approval remains Sanders' decision.
