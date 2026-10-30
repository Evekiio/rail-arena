# Gate 2 / playable visual slice

Sanders approved Gate 1 with **“The art is approved”** on 2026-10-30. This approves the art direction and measured-reference standard. It does not approve the finished slice or final asset likeness.

## Current batch

- Shared `CelArtProfile` production interface and URP cel-surface shader: three diffuse bands, explicit sRGB-to-linear palette conversion, texture/normal/cavity/emission support, main-light shadows, Forward+ additional lights and screen-space occlusion.
- Slice-scoped material instances: shared sources, collision, character rigs, movement and network state remain intact. Transparent devices, portal views, water and specialist effects retain their shaders.
- Original Blender cover family `SG-COVER-2M-v1`: 19 editable construction parts; exported as one visual mesh with four material slots. Exact 2 x 1.2 x 1 m Unity envelope and centered pivot verified during import. Four existing wing covers receive these visual shells in the slice; original colliders remain authoritative.
- Six equal-scale geometry-derived cover views and current in-engine comparison captures. Motion blur is disabled only in the temporary capture session for static detail inspection; no saved graphics preferences change.
- The slice build entry point is scaffolded for integration. It has not produced a review build yet.

## Gate 2 work still required

1. Refine the player and first-person arms/hands, face and silhouette, with compatible rigs and measured export evidence.
2. Finish rifle surface/detail treatment and authored reload contacts, magazine handling and FPS action presentation at the existing cadence/timing.
3. Complete a coherent garden-to-bridge area with foliage, water and supporting stone construction.
4. Integrate the health pickup, portal and jump-pad appearance, preserving clear device states and destination views.
5. Finish the representative menu/HUD treatment and capture all action/interaction states.
6. Integrate the existing experimental graphics where they improve the approved look. Do not restore the distracting cyan particle cloud.
7. Produce matching gameplay comparisons, ADS views and a normal-speed motion reel, then measure the defined 1080p workload and run focused integration checks.
8. Deliver the local slice build with identity and checksum for Sanders' Gate 2 approval. Full production and public release remain gated.

## Reproduce this batch

Run Blender 5.2 in background mode with `--threads 4 --python tools/build_art_slice_stone.py -- <repository-root>`. Run Unity 6000.6.7f1 headlessly with `-executeMethod RailArena.Editor.ArtSliceBuilder.Prepare`. Run the single graphics-enabled hidden EditMode test `RailArena.Tests.ArtSliceLookCaptures` to capture the prototype and matching cover comparison. Run `tools/package_art_slice_review.py` with the bundled Python/Pillow runtime to compose the review sheet and verify its local links.

This batch is a material/construction prototype, not a completed art overhaul or performance acceptance. Editor captures do not establish native input, sound, motion quality or packaged-build parity.
