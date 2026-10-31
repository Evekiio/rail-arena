# Stone submission research

The current shipping review remains Review 23. This candidate is disabled by default and enabled only in an Editor or development player with `--profile-stone-palette`. It is not yet a performance improvement claim.

The derived export combines each measured stone mesh's existing material sections into one draw section. Vertex colours preserve linear material tint; a separate vertex channel selects the original paving textures and highlight strength. Source meshes, UVs, normals, tangent frames, topology, transforms, colliders and LOD membership remain authoritative. No source import is made readable. Read-only Editor snapshots produce the derived exports.

## Focused native evidence

`Validation/stone-deck-inset-native.xml`: two tests passed. `Validation/StoneSubmissionNative-03/review.json`: 45 unique source meshes, 281,217 triangle corners, 186 preserved collider definitions/poses, 114 renderer/LOD instances, 555 original material slots versus 114 derived slots. These are installed candidates across all LODs, not measured draws per frame. Restore checks passed. Five frozen 1920 x 1080 camera pairs retain the strict image thresholds. Combat mean absolute RGB difference is 0.00003585 on a 0–255 scale, with one pixel exceeding a ten-level channel difference; the other views have none.

The first colour comparison exposed the need to bake the active linear colour-space tint. A subsequent comparison exposed pre-existing coplanar dark backing and ivory coping faces on the deck wall. The canonical Blender JointBed is now inset to 0.997 of the source radius (9 mm staging, 15 mm basement). Coping bevels and joints leave about 2.5 mm clearance at the staging axis extrema. Import validation checks the centred pivot, exact vertical envelope, containment and at most 10 mm silhouette recession per horizontal edge. Native checks still verify every installed vertex stays inside the unchanged original 20-sided collision footprint. Gameplay dimensions and return devices do not change.

The deck reference package now requires the current Blender master's SHA-256 in both the native review index and the passed test's output. A historical passed XML cannot certify new source geometry.

## Controlled measurement

Run `tools/Profile-StoneSubmission.ps1` against a single development executable. It profiles off then on once, using identical seeded simulation, camera paths, resolution, quality, cropped portals and executable hash. Inspect Render-category draw counters, SetPass and timing together. UI Toolkit has a separate zero-valued draw counter. Report GPU timing unavailable when no valid samples exist.

Forced offscreen submission and readback add overhead and serialize work; these probes do not establish native input acceptance, live frame pacing or 60 FPS. Promote only after a useful measured benefit and visual preservation. Public release remains held for Sanders' final acceptance.
