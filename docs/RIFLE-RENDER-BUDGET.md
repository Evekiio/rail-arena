# Rigid rifle rendering budget

## Measured reason for the change

The verified Review 20 player was profiled at 1920x1080 High on the RTX 2080 with one automated human and three bots, seed 2147, fixed 60 Hz simulation and uncapped render throughput. Each of five views warmed for 120 frames and sampled 360. The combat view recorded 1,966.25 mean submitted draw calls and 820,093 mean triangles across the main and portal views; four portal views rendered each frame. Portal CPU markers averaged 5.15 ms, physics 0.32 ms and synchronized readback 6.42 ms. Counters are inclusive and not additive. The combat sample had no managed GC collections. GPU samples remain unavailable. This identifies rendering work worth reducing; it does not isolate GPU cost or certify native frame pacing.

The installed inventory includes every LOD candidate, including inactive and hidden renderers. It is not a draw-call count. It records 286 enabled/active rifle-related renderers, including view arms, and 429 environment/effect renderers with 906 material slots. Characters already use combined painted skins. Original measurements and renderer inventory are retained under `Validation/ArtFrame-Review-20/`.

## Visual-only production contract

The archived `RifleRenderBatch` prototype combines rigid shell surfaces within each existing LOD. Material, shadow mode, receive-shadow state, probe settings/anchors, rendering layer, motion-vector mode, lightmap state and object layer define compatible groups. Each batch stays under the original LOD parent. World positions, normals, tangents, UVs, material identity, triangle order/winding and LOD thresholds remain verifiable against the original renderers.

The physical optic, magazine, trigger, energy surfaces, actuators, sockets and grip transforms remain separate. Existing charge property blocks and authored reload contacts remain authoritative. No simulation, weapon cadence, spread, hitboxes or network protocol is changed. Older maps do not install this art-profile component. Canonical Blender masters and exports remain editable and unchanged; combined meshes are instance-owned Unity presentation buffers.

All original renderers and LOD lists are retained for synchronous restoration. Owned combined meshes are released on removal, including when the weapon is destroyed first. A whole weapon may start hidden and appear later; eligibility checks each part's own active state rather than its ancestor's current visibility. This prevents first-person startup visibility from skipping the optimization.

The native geometry/restore check currently verifies five rifles, 150 rigid source renderers reduced to 18 combined renderers across all LOD candidates and 111,115 vertex records. These are candidate renderer counts, not submitted draw-call measurements. Optic/magazine/trigger/energy meshes and transforms are checked through restoration. Same-frame first-person and all three third-person LOD comparisons use recorded matching camera poses.

## Validation and remaining work

The first string-quantized normal comparison failed at rounding-bin boundaries. It was replaced with direct 20-micrometre position and 0.00002 normal/tangent tolerance checks, plus 0.000001 UV tolerance and exact tangent handedness/material identity. An additional startup check caught ancestor visibility incorrectly skipping the first-person weapon; the own-part-state correction fixes that behavior. The first visual capture's first-person field of view was not reset after its third-person view; those images remain unaccepted diagnostics. The corrected capture records actual pose and projection for every pair. Initial failures remain in `Validation/rifle-batch-native-*.xml` and raw logs.

The matched native geometry and visual test passes. Each before/after pair has identical recorded position, rotation, field of view and projection. Mean absolute RGB differences are below 0.018 on the 0–255 scale; only two first-person pixels and no third-person pixels exceed ten luma units. These controlled captures show equivalent presentation, not a newly refined silhouette. The lowest rifle LOD retains its pre-existing triangular barrel-vent simplification, which remains an art-production gap.

The reload contact/timing/interruption/reset regression passes. Review 21 was built as a research candidate from commit `ece7ae6989376cdac8e80408618d4ef4f0630af5`; its independently verified source fingerprint is `87566282bcb61c5a3010bce2e465b51f9542a56ebdad5d397f475a7cfcf8697a`. The controlled on/off profiles use this same executable, recorded launch arguments and executable hash, with unchanged aperture cropping, view order, portal render counts, warmup/sample counts, fixed-step workload and random seed. Off ran first and on second, once each; no repeated timings were selected.

| View | Draw calls off → on | Reduction | Wall mean off → on, ms |
|---|---:|---:|---:|
| combat | 1966.92 → 1896.25 | 3.6% | 15.77 → 16.24 |
| summit | 1770.58 → 1703.43 | 3.8% | 15.73 → 16.09 |
| basement | 1690.67 → 1657.68 | 2.0% | 14.25 → 17.30 |
| foliage-waterfall | 1858.73 → 1826.79 | 1.7% | 14.69 → 16.15 |
| portal | 526.92 → 521.21 | 1.1% | 12.54 → 13.82 |

Submitted draw calls decrease only 1.1–3.8% in the full workload. Wall time is worse in all five views in this run pair; readback and scheduling remain included, and unavailable GPU samples prevent isolating GPU cost. The evidence does not establish a frame-time improvement or attribute every timing difference to batching. Fewer renderers alone is insufficient to approve this as a performance fix.

**Decision:** archive this experiment as reference material in PR #46, restoring production runtime code and retaining the candidate in noncompiled source files under `ArtSource/Original/ArtPass-v1/Experiments/RifleBatch`. Do not install its runtime integration or promote Review 21 as the current build. Review 20 remains the verified playtest package. The candidate passes all 39 mechanics checks, its 45-second three-bot match and both 90-second loopback peers, with zero reported errors. That correctness evidence does not establish a performance benefit; see `Validation/rifle-batch-candidate-21.json`. No verified portable Review 21 package is produced from this experiment. Future work should investigate the larger environment/material submission budget and native GPU/pacing evidence while continuing the remaining art-production categories.
