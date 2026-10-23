# Rail Arena / dimensional reference approval

**Proposed construction references, awaiting creator approval.** These images describe the current rifle and low-cover benchmark geometry consistently. They do not turn that benchmark into an approved design or recover unknown measurements from an illustration.

**Stock supersession notice:** the user rejected the stock shown here. Use the [complete rifle modeling-reference package v2](../../RailRifle-Modeling-References-v2/README.md) for the revised coordinated construction proposal. These old sheets retain rejected stock geometry for history. The new package still requires creator approval.

## Approval sheets

- [Review both assets](index.html).
- Rifle: [six-view PNG](RailRifle-Six-View.png), [editable engineering SVG](RailRifle-Six-View.svg), [part bounds and dimensions](RailRifle-Parts.csv).
- Low wing cover: [six-view PNG](LowWingCover-Six-View.png), [editable engineering SVG](LowWingCover-Six-View.svg), [part bounds and dimensions](LowWingCover-Parts.csv).
- [Calibrated Blender image rig](Blender-Reference-Rig.blend), containing image references only and no production mesh.
- [Camera and scale calibration](Calibration.json), [image integrity manifest](manifest.json).

Each sheet shows left, right, top, underside, front and back, with the same scale across all six panels for that asset. The clean PNG files in `Clean/` contain transparent backgrounds and no dimensions or grids. Those are the files for Blender tracing. The dimensioned sheets are for approval and measurement review, not for image-plane calibration.

| Asset | Proposed/fixed overall dimensions | Full clean image frame | Pixel pitch |
| --- | --- | --- | --- |
| Rail rifle | X 1172.0 × Y 108.0 × Z 455.2mm, proposed from benchmark | 1.450 × 1.450m, 2048 × 2048px | 0.708mm/px |
| Low wing cover | X 2000 × Y 1000 × Z 1200mm, fixed design envelope | 2.500 × 2.500m, 2048 × 2048px | 1.221mm/px |

X is length/muzzle-forward, Y is width and Z is up in the Blender source. Left looks from -Y, right from +Y, top from +Z, underside from -Z, front from +X and back from -X. This explicit convention takes precedence over the name of a Blender keypad view. Do not mirror a view to make the weapon point in a preferred direction.

The rifle's supplied image has no measurable scale and does not establish hidden surfaces. Its overall size, underside, opposite face, bore and contact geometry remain proposals for approval. The cover's dimensions are prescribed by the environment design; its visual details still need approval. Source bounds include bevels and are rounded to 0.1mm on sheets. The full-precision values and per-part bounding envelopes are in the JSON/CSV files.

## Use in Blender without guessing the scale

The provided image rig uses metre units, common image-centre positions and exact camera-plane orientations from Calibration.json. Rifle references are visible by default; enable the cover collection and disable the rifle collection when working on the cover. Images display only in aligned orthographic views, with front-side visibility and 65% opacity behind geometry. Selection is locked to prevent accidental movement; unlock through the Outliner when necessary. All source images are packed into the `.blend`.

For manual setup, use an Image Reference empty with the clean square image. Set its full image size to **1.45m for the rifle** or **2.5m for the cover**. Use the asset `center` and each view's `right`, `up` and `normal` axes in Calibration.json. All six images belong at the same asset centre. Do not independently crop, stretch, auto-fit or rescale individual views. Centre crosshairs on the approval sheets mark source coordinates zero, which can differ from the image's bounding-box centre.

Raster tracing has finite pixel precision; it cannot guarantee mathematically perfect geometry. Use the written dimensions, source part bounds and editable master when exact existing surfaces matter. Part bounds alone do not describe every contour, wall thickness or mating clearance. Their authoritative existing surface geometry is in the [benchmark master](../Production-Benchmarks.blend). These drawings are a general arrangement for approval, not a complete mechanical manufacturing specification.

## Review before production changes

Approve or request changes to overall proportions, stock openings, grip and trigger clearance, optic/barrel alignment, enclosure vents, hidden surfaces and cover armor/end construction. Approving a view does not approve a different or contradictory view. After requested revisions, regenerate all views from the same master and repeat the calibration checks before locking construction.

No gameplay geometry, live prefab or production mesh was changed while creating this package. The reference rig is deliberately a blank modeling setup. Blender rendering remained headless.

## Reproduction

1. Run `.\tools\Build-DimensionalReferences.ps1` from the repository root. It uses background Blender 5.2, four threads, a ten-minute bound and cleanup of its owned process. After an interrupted job, use `-Resume` only when retaining the same master and view setup; it reuses complete PNGs newer than the master. Run without `-Resume` after design/camera changes.
2. Run `python tools/package_dimensional_references.py` from the repository root.
3. Run `node tools/rasterize_dimensional_sheets.cjs` with a Sharp module available, or pass its absolute module directory as the first argument. This converts the native SVG diagrams to PNG without changing their geometry or source images.
4. Run `tools/build_dimensional_reference_rig.py` in background Blender with the same repository argument; it packs the clean references into the blank image rig.
5. Run `tools/validate_dimensional_references.py` with Python and Pillow (available in the bundled workspace runtime). It compares actual alpha silhouettes against projected source bounds within three raster pixels, verifies the camera bases, validates PNG integrity and records hashes for the approval sheets and reference rig.

No image generation service, perspective reconstruction or AI-created alternate view was used for these dimensional drawings.
