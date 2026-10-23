# Rail Arena / precision carbine v3

**Design approved October 23, 2026.** See the [integrated production package](../PrecisionCarbine-Production-v1/README.md). The original reference-stage validation manifest is historical. This is the new 1.0m precision-carbine proposal selected by the creator. The creator subsequently authorized production and replacement.

## Review

Open the [complete review gallery](index.html). It contains the five new source images, the revised study, actual Unity first-/third-person captures and the six construction boards:

1. [Six calibrated views](RailRifle-Six-View.png): left, right, top, underside, front and rear.
2. [Stock construction](02-Stock-Construction.png): reinforced stock, cheek rest and angled pad.
3. [Axial and contact details](03-Axial-Contacts.png): enlarged ends, grip/trigger area and muzzle.
4. [Exploded assembly](04-Exploded-Assembly.png): twelve named art assemblies and measured envelopes.
5. [Section and anchors](05-Section-Anchors.png): cosmetic centre cutaway and contact/aim/muzzle targets.
6. [Material and motion guide](06-Materials-Motion.png): art surface zones and unchanged weapon behavior.

All six boards also have editable SVG siblings. Clean construction images are in `Clean/`; enlarged/form views are in `Details/`; Unity captures are in `Unity/`.

## Locked design direction

| Decision | Selected target |
| --- | --- |
| Type | Original precision carbine, replacing the older rifle after approval |
| Overall envelope | 1,000mm long, at most 100mm wide, approximately 330mm high |
| Length allocation | Rear/stock 300mm, receiver region 300mm, forward assembly including muzzle 400mm; small mating overlaps remain inside the overall envelope |
| Stock | Broad graphite cheek rest, angled shoulder pad, reinforced single opening and small cosmetic sling recess |
| Forward assembly | Long faceted enclosed handguard, real vents and recessed cyan rail elements |
| Optic | Compact tube optic; no large scope or bipod |
| Support | Integrated angled hand stop; no vertical foregrip |
| Cassette | Short curved fixed energy module; no cartridge details or reload function |
| Palette | Ivory ceramic, graphite and restrained cyan, with deep teal recesses |
| Gameplay | Instant-hit click-only semi-auto; 0.6s cooldown, unlimited ammunition, no reload or charge-up; existing damage and 1.5Ã— zoom preserved |

The source photographs establish shape vocabulary, not measured geometry, a license to use their branding or a claim of real weapon construction. [Provenance.json](Provenance.json) records their roles and unchanged file hashes. All five originals retain any visible watermarks. No source photograph becomes a model texture. The new study meshes are authored independently; only the original Rail Arena study palette and review lighting are reused.

## Modeling handoff

- [Editable carbine study](Carbine-Reference-Study.blend): new original reference geometry, separate parts and RF-01 through RF-12 assembly IDs. This is a construction study, not the final topology/LOD deliverable.
- [Calibrated image rig](Blender-Reference-Rig.blend): blank modeling setup with six packed image planes, metre units and locked selection.
- [Calibration.json](Calibration.json): exact bounds, per-view camera axes/frame sizes, source part bounds and anchors.
- [Part dimensions](RailRifle-Parts.csv), [assembly dimensions](Assembly-Dimensions.csv) and [assembly definitions](Assembly.json).
- [ReviewPose.json](ReviewPose.json): candidate-only Unity wrist/mount targets and orientations. This does not replace the live hand profile.
- [Unity review report](Unity-Review.json): camera FOVs, actual capture count, wrist reach errors and any pose warnings.
- [Validation manifest](manifest.json): hashes, projection checks, coverage and explicit remaining gates.

Source axes are **X muzzle-forward, Y width, Z up**. The six clean square PNGs are 2048px and represent a **1.25m square** at the common bounding-box centre. Do not crop or stretch individual images. Front/back look from +X/-X; left/right from -Y/+Y; top/underside from +Z/-Z. Use the rig rather than aligning images by eye. Each enlarged detail records its own frame size and centre; those images are not interchangeable with the common-scale set.

The palette study uses overlapping swatch UVs and two materials for reference appearance. A 2K production surface atlas, refined material response, production topology, three LODs and final animation remain later work. The section exposes modeled cosmetic surfaces; the exploded offsets are visual separation, not working mechanisms or assembly clearances.

## First-person and character previews

The Unity review uses the exact pinned Editor, **6000.6.7f1**, with the candidate FBX imported only under `Assets/RailArena/Art/CarbineReferenceReview`. It instantiates current character and arm rigs for static captures, uses new candidate wrist targets and a new muscle-based finger pose, and leaves their prefab assets untouched.

Each of the four appearances has normal first-person, zoom first-person, third-person three-quarter and third-person side captures. The normal camera uses the game's current **100Â° vertical FOV**; zoom uses `2 atan(tan(100Â°/2)/1.5)`. Lighting is a controlled review setup. These are actual Unity renders with character geometry, not composited hand illustrations or gameplay screenshots.

Wrist positions are fitted using a two-bone review solver and reported numerically. Fingers, shoulder contact, clipping during motion, screen-space occlusion and subjective comfort still require review. The static pose is not an accepted live calibration or a proof of runtime IK behavior. Pose warnings, if any, remain visible in the report; they must be resolved during the fitting stage rather than hidden.

## Approval gate and later production

Approve or request changes to the complete silhouette, the six construction views, proposed dimensions, materials and first-person framing. The prior rifle packages are history. Approval of one view does not approve conflicting hidden surfaces.

Only after approval:

1. Finish intentional low-poly topology, complete hidden surfaces, a 2K surface atlas and restrained emission, with at most three material slots.
2. Produce LOD targets of 12k / 6k / 3k triangles and review transitions; these are budgets, not performance evidence.
3. Fit the actual hands/fingers/shoulder, author trigger and recoil/recovery presentation, and verify muzzle/camera axes.
4. Replace the shared `Resources/Weapons/Railgun` prefab through its builder, retaining its presentation contract and recoverable old sources.
5. Run relevant hand/motion/feedback/simulation tests, offline and duel presentation checks, and later measured combat performance qualification.

**This historical reference stage changed no live weapon, character prefab or hand-calibration file. The linked production stage now replaces them.** No gameplay/network rule change or GitHub publication is included. Performance at 1080p/144fps is not certified by reference renders.

## Reproduction

The source builder is `tools/build_carbine_reference_v3.py`, executed in background Blender 5.2 with four threads and a twelve-minute process bound. It writes only this reference folder and a review-only FBX/palette import folder. `tools/prepare_carbine_reference_v3.py` writes candidate pose targets and source provenance.

The existing dimensional and rifle packagers accept `--folder ArtSource/Original/PrecisionCarbine-References-v3`; pass `--master ArtSource/Original/PrecisionCarbine-References-v3/Carbine-Reference-Study.blend` to dimensional packaging/validation. The SVG rasterizer accepts the folder after its Sharp-module argument. The image-rig builder accepts the same folder argument after the repository root.

Capture with `RailArena.Editor.CarbineReferenceReview.Capture` through the Unity CLI in batch mode with a graphics device, four workers and a bounded timeout. Import/render jobs remain headless with no visible windows. Reference-generation geometry is never exported into the live weapon resource path.

Reproduce the full sequential package with `tools/Build-CarbineReferenceV3.ps1` (or `-SkipRendering` to retain the existing master and rendered views). The grouped review export is made by `tools/export_carbine_review_v3.py`; it permits review-only rear-assembly hiding in first person. Final coverage checks run through `tools/finalize_carbine_reference_v3.py`.

Validation: six silhouette bounds agree with the master dimensions within one raster pixel; finite, manifold, nondegenerate reference geometry and UV/material assignments pass. Unity 6000.6.7f1 generated 16 static previews with maximum wrist-target error below 5mm. Finger intersections, shoulder contact, optic readability and first-person composition still require creator review and final fitting. These images do not establish animation, gameplay, LOD or performance acceptance.
