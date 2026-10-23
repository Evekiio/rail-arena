# Benchmark validation

## Completed

| Check | Rifle | Low wing cover |
| --- | --- | --- |
| Editable source parts | 96 | 31 |
| LOD0 triangles | 8,384 | 1,652 |
| LOD1 triangles | 4,566 | 884 |
| Renderers per active LOD | 1 | 1 |
| Shared material slots per renderer | 2 | 2 |
| Degenerate triangles / non-manifold edges | 0 / 0 | 0 / 0 |
| Bounds and axes | 1.172m long; Unity +Z muzzle-forward | 2 × 1.2 × 1m in Unity X/Y/Z |
| Headless Unity 6000.6.7f1 import | Passed | Passed |
| Actual Unity graphics capture | Saved and visually inspected | Saved and visually inspected |

The authoritative counts are in [geometry.json](geometry.json); Unity independently counted imported triangles in [the import report](../../../Validation/production-benchmarks-unity.txt). The build samples rifle recoil at 0.045 seconds and the reset at 0.6 seconds. This verifies the authored clip's transforms, not gameplay firing behavior or subjective feel.

Visual iteration corrected polygon winding on boolean cutters, inherited primitive UVs selecting the wrong palette colors, coplanar cover surfaces and FBX socket-name suffixes. Final images show the imported materials and corrected end panel. Lighting and antialiasing differ between the neutral Blender studio and the Unity review scene; they are not pixel-identical reference comparisons.

Blender ran in background mode with four threads and a bounded process wrapper. Unity ran in batch mode with four workers, without graphics for import checks and with a graphics device for image capture. An initial sandbox attempt could not initialize licensing. The successful import/capture jobs used the same pinned local Editor outside that sandbox; no visible Editor/game window was opened. Neither model replaces live gameplay assets.

## Still required

- Creator approval of silhouette, hidden surfaces, detail density and reference fidelity.
- Character-specific hand/finger fitting, optic alignment and first-person camera review.
- LOD transition and silhouette review in motion at representative distances.
- Controller collision review and actual Foundry placement.
- Surface-response refinement if the shared palette material does not meet the approved look.
- Four-contender combat measurement at 1080p on a documented reference PC.

No automated image or topology test establishes perfect likeness, competitive balance, completed production art or release readiness. These are concrete benchmark candidates ready for that next review.
