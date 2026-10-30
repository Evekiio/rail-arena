# Rail Arena v1 alpha art production

Current milestone: **Gate 1 approved by Sanders; Gate 2 playable visual slice in production.** The overall goal remains the complete approved art-production plan. This package is the evidence and production basis for the first approval gate, not a completed graphical overhaul or permission to skip Gates 2 and 3.

**Execution update, 2026-10-30:** Sanders directed: “Resume all work and don't get stuck or waiting my approval. I want the final working version and I'll play test as the beta tester / play tester / user QA gate.” Intermediate art gates now serve as production review milestones without approval stops. Continue through the full integrated local build; Sanders' final playtest remains acceptance. Public publication remains held until final approval. This direction supersedes the earlier stop-at-Gate-2 workflow in the original planning package.

- [Gate 1 review gallery](../ArtSource/Original/ArtPass-v1/Gate1/index.html)
- [Art bible](../ArtSource/Original/ArtPass-v1/Gate1/ART-BIBLE.md)
- [Asset register](../ArtSource/Original/ArtPass-v1/Gate1/asset-register.csv)
- [Machine-readable manifest](../ArtSource/Original/ArtPass-v1/Gate1/asset-manifest.json)
- [Gap report](../ArtSource/Original/ArtPass-v1/Gate1/GAP-REPORT.md)
- [Animation matrix](../ArtSource/Original/ArtPass-v1/Gate1/animation-matrix.csv)
- [Production schedule](../ArtSource/Original/ArtPass-v1/Gate1/production-schedule.csv)
- [Gate 2 current production and comparison gallery](../ArtSource/Original/ArtPass-v1/Gate2/index.html)
- [Gate 2 remaining work](../ArtSource/Original/ArtPass-v1/Gate2/PRODUCTION-STATUS.md)
- [Authored rifle action and normal-speed playback](../ArtSource/Original/ArtPass-v1/Gate2/RifleActions/index.html)
- [Integrated gameplay, UI and measured-reference gallery](../ArtSource/Original/ArtPass-v1/integrated-review.html)
- [Integrated production asset delta](../ArtSource/Original/ArtPass-v1/integrated-manifest.json)
- [Current working build and validation handoff](ART-PASS-REVIEW.md)

The target is crisp anime cel shading for Sky Gardens including the summit and basement routes, with a measured 1080p/60 FPS target on the RTX 2080 with one human and three bots. Older maps are inventoried and preserved. Source ownership and visual approval must remain distinct from runtime inclusion and technical import validity.

## Regenerating Gate 1

1. Run the pinned Unity Editor headlessly with `-executeMethod RailArena.Editor.ArtProductionInventory.Audit`. It reads imported assets and generated scenes/resource prefabs and writes `unity-audit.json` without saving those sources.
2. Run Blender 5.2 in background mode with four threads and `--python tools/audit_art_blender_sources.py -- <repository-root>` to inventory objects, rigs, actions and image dependencies in existing masters, without saving them. Run `--python tools/render_art_gate1_references.py -- <repository-root>` to render representative copies of the four current character masters, precision carbine, summit assembly and measured Unity basement deck. It does not edit original masters.
3. Run the focused Editor capture `RailArena.Tests.ArtGate1BaselineCaptures` for current scene, device, character and UI images. Captures use the serialized PC scene; conditional review-build graphics are explicitly excluded. The HUD uses offscreen geometry composition, not native-player screenshot acceptance.
4. Run `tools/build_art_gate1_package.py --root <repository-root>` using the bundled Python with Pillow. It composes the annotated sheets, joins source files with Unity evidence, creates JSON/CSV registers, gaps, animation matrix and the browsable gallery, then verifies IDs, dispositions, source hashes and measured-image hashes.
5. Run `tools/verify_art_gate1_review.py <node-executable>` for offline link, inventory-file, pagination, search and filter checks. Browser visual QA is not established when the local file URL is blocked by the browser tool; do not bypass that restriction.

The illustrative direction board is separately generated concept art, not gameplay or a scaled reference. Preserve its original source and the two existing project-reference inputs. Rerunning the package script does not regenerate that image or apply any art to the game.

Asset and instance IDs are separate: Unity files use their existing GUID; ArtSource files use a stable path-derived ID; renderer occurrences use container/hierarchy/occurrence IDs. Renderer bounds are pose/rotation-dependent AABBs in Unity metres, while imported mesh bounds are local units. Neither alone certifies authored scale or source/export parity.

The manifest explicitly flags conditional review-build inputs, Resources inclusion, candidates and legacy dependencies. Runtime-created content requires the owning code/state specification; an unreferenced serialized asset is not automatically safe to delete.

Gate 1 was approved by Sanders on 2026-10-30. Production is proceeding to the complete in-game visual slice, followed by full integration and final approval. The first cel/stone batch is a prototype, not the completed slice. GitHub source delivery follows the repository's normal PR process; public alpha packages change only after final visual approval.
