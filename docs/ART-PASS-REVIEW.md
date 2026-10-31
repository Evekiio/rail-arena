# Rail Arena v1 alpha: integrated art build 28

Review 28 is the verified Windows handoff requested by Sanders. Production is paused after this milestone at his request; the full art-production goal remains incomplete. No public prerelease or final artistic acceptance is claimed.

## Play and inspect

Launch `Builds/RailArena-v1Alpha-ArtPass-Review-28/RailArena.exe` with its folder intact, or extract `Builds/RailArena-v1Alpha-ArtPass-Review-28.zip`. Press **G** for Physics Lab; movement and graphics controls remain in Settings.

Portable ZIP: **452,496,730 bytes**, **928 file hashes verified in both archive and fresh extraction**. SHA-256: `0f21173a05fb826a1ddb3625716b6e598a677a115b6e7d2d7adf20451d40304d`. The package includes thirteen hash-verified prior reference families and 51 current portal-performance evidence files, licenses and `Run-Checks.ps1`. Historical builds, release history and unrelated diagnostic work are preserved.

Source commit: `7cb3cb46b3f6e4c792de49a89a1500ad7cf898bb`. Embedded build identity: `a8f8432178d1dd5e88b49b6edc27d8c4cb92ae4d9741b56bc4711e7990b7b730`. [Shipping checks](../Validation/art-integrated-final-28.json), [portable verification](../Validation/art-package-verification-28.json), [GitHub PR #54](https://github.com/Evekiio/rail-arena/pull/54).

## Included result

All prior graphical upgrades remain: four painted characters and portraits, corrected calibrated fingers, authored reload hand release/grasp/return, standing/locomotion/airborne/crouch/slide/reaction clips, rifle raise and two-second reload, revised rifle materials and physical optic, stone paving/normal detail, arena/bridge/summit construction, staging and basement decks, cover shells, painted trees/shrubs/grass/ivy, floating medical cores, wayfinding, clouds, waterfalls, cel lighting, menus and HUD. The cyan particle cloud remains absent. Movement, accuracy, damage, 250 RPM cadence, reload timing, hitboxes, trajectories, four basement routes/returns and multiplayer protocol are preserved.

Sky Gardens now reuses very small distant portal destination images for two or four rendered frames. The age limit is checked on visible updates; nearby, large and scoped views stay full rate. Camera/projection/layer changes, re-entry, resize and invalid targets force a refresh, with the cached crop and shader coordinates kept together. Older-map defaults remain full rate, and removing the art profile restores previous settings. This changes distant visual refresh frequency; it does not change traversal or combat authority.

- [Portal measurements, source hashes and frozen destination comparisons](../ArtSource/Original/ArtPass-v1/Performance/PortalRefresh/index.html).
- [Performance implementation, controlled results and limits](PORTAL-REFRESH-PERFORMANCE.md).
- [Reload hands, measured Blender action and native before/after reel](../ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1/index.html).
- [Full art/reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html).
- [Inventory and remaining production gaps](../ArtSource/Original/ArtPass-v1/CURRENT-GAPS.md).

## Validation on the shipping artifact

- All 39 mechanics checks passed.
- A 45-second automated player/three-bot match passed: 171 shots, eleven deaths, one pickup, twenty-one pad events, zero exceptions and zero actor voids.
- Both local multiplayer peers passed 90 seconds with zero errors. Slide samples: host 2168/2687, client 1841/2301. The movement driver intentionally exercises falls. This is loopback evidence, not real-internet acceptance.
- Five actual 1920×1080 High-tier views passed content checks and were visually inspected in a labelled contact sheet. Checks ran directly on this executable.
- Four focused refresh-policy checks and two integrated native tests passed. All six non-black frozen destination pairs have zero changed pixels. Nearby/scoped full cadence, projection/layer/movement/re-entry gates, profile restoration and older-map defaults are covered.
- The original hand-art native and saved-Blender-master evidence remains included. No geometry or Blender master is changed by the portal optimization.

Two reverse-ordered comparisons in one hash-verified candidate reduced combat mean offscreen wall time by **18.6–30.1%**, distant portal renders by about **62%**, and the inclusive portal submission marker mean by about **54–64%**. The close portal retained all 360 refreshes in both pairs. This is controlled component evidence, not native frame-pacing acceptance or a demonstrated single cause of every earlier spike.

The shipping RTX 2080 / Direct3D12 probe includes forced rendering and synchronous readback:

| View | Mean ms | P95 ms |
|---|---:|---:|
| combat | 12.997 | 19.372 |
| summit | 11.245 | 13.415 |
| basement | 11.004 | 13.253 |
| foliage-waterfall | 12.111 | 15.154 |
| portal | 11.925 | 14.254 |

Combat P95 (19.372 ms) exceeds the 16.67 ms probe budget; the other four views meet it in this run. GPU timers returned zero valid samples. Readback/driver/scheduling overhead is included; sustained native 60 FPS remains unproven. Differences between shipping milestones are not controlled A/B measurements.

## State when resuming

The inventory remains at 4,126 paths: 1,787 production-evidence paths, 103 historical references, thirteen archived-research paths and 2,223 baseline-only paths. The [structural audit](../Validation/ArtReferenceLinks-09/audit.md) has zero missing linked files. These are file/link counts, not unique models or complete reference certification. The original 34-action matrix remains intact, with eighteen rows having source integration evidence.

Remaining work includes full first-person actions/transitions and fine palm/tap contacts; remaining character combat/reload/respawn and stance coverage; structural framing/cover and measured device assemblies; waterfall lips/foam, cloudscape and distant ruins; broader nature/variant references; UI states, audio/provenance review and complete legacy/archive coverage. Native GPU/pacing work, real-internet multiplayer, sound/input and Sanders' final artistic acceptance remain open. The deferred ADS/portal projection corner case is recorded separately; this refresh work does not claim to fix it.

Review 28 is ready for playtesting while the goal is paused. Resume production only when Sanders asks; do not publish the final public alpha without his final approval.
