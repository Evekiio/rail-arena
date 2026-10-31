# Calibrated first-person assembly references

Current calibrated source: [Assembly-v2](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2/index.html), with authored finger bindings retained and matched native motion comparison. [Root cause, focused checks and remaining limits](HAND-BIND-CALIBRATION.md). Assembly-v1 below is preserved as the pre-correction historical reference.

Four appearance skins now have a combined rifle/forearm/hand Blender master, held hip and ADS poses, 54 editable bones per assembly, original four-weight skin groups, 14 visible first-person rifle parts, and exact grip, sight, muzzle, shoulder and native-eye frames. Original character and rifle masters remain preserved and hash-linked. This is source/reference production; no runtime rig or animation is replaced.

- [Master and gallery](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1/index.html).
- [Dimensions, source hashes and numerical pose proof](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1/production.json).
- [Construction layers, clearance bounds and attachment matrices](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1/construction.json).
- [Passed native capture with source hash](../Validation/fps-assembly-export-scale-correct.xml).
- [Saved-master verification after reopening Blender](../Validation/fps-assembly-saved-master-pivots.json).

The package has 48 equal-scale front/rear/left/right/top/bottom views, sixteen isolated arm/rifle construction-layer images, eight measured sheets with scale rulers and a schematic 1.8 m human comparison, eight native Unity images, eight matching Blender eye views and eight paired comparisons. All 31,012 posed arm vertices match the corrected native bake within 0.0023 mm; all 432 bone heads, original weights/hierarchy, rifle topology/material sections/UVs, original part frames/pivots and camera/socket frames are verified in the saved artifact. Blender Workbench shading is technical reference. The native Unity captures remain the final cel-shading authority.

The export initially applied the imported 100x renderer scale twice. It now explicitly uses `BakeMesh(mesh, true)` and asserts that the compensated bake agrees with independent four-weight skinning math before accepting the source capture. Unity's [BakeMesh documentation](https://docs.unity3d.com/ja/6000.0/ScriptReference/SkinnedMeshRenderer.BakeMesh.html) describes this scale compensation. The failed/provisional scale evidence remains local diagnostics and is not a certified reference. No gameplay fix was required.

## Reproduce

Use Unity 6000.6.7f1 and Blender 5.2.2. Keep heavy jobs sequential and hidden; do not use `-nographics` for native captures.

1. The focused `RailArena.Tests.FirstPersonAssemblyExport` Unity EditMode capture writes a new `RuntimeSource` folder and refuses to overwrite existing frozen evidence. Preserve/version that folder before intentionally recapturing a changed runtime assembly. The passing XML records the compressed source SHA-256.
2. Blender background, `--threads 4 --python-exit-code 1`: run `tools/build_first_person_assembly.py -- <root>`. The frozen capture is the deterministic geometry/pose input; regenerated master/reference hashes must stay together.
3. Blender background: run `tools/render_first_person_assembly_refs.py -- <root>`, then `tools/render_first_person_assembly_layers.py -- <root>`.
4. Blender background: run `tools/verify_first_person_assembly.py -- <root> --output Validation/<new-evidence-name>.json`. It reopens the actual master and verifies all eight assemblies without saving it.
5. With the Pillow-enabled Python runtime, run `tools/package_first_person_assembly.py`, then the integrated gallery/inventory tools and reference-link audit.

## Remaining quality and integration work

Arm-only construction-layer images confirm the detached-looking pieces belong to the arm mesh rather than rifle controls. Orthographics expose finger/grip intersections and detached-looking fingertip shapes in the historical Assembly-v1 held baseline. Those are defects for the hand-art pass, not accepted final hand placement. The full first-person action library, animated magazine/finger contacts, movement and traversal transitions, compatible runtime replacement-rig import, normal-speed/native input/audio review and Sanders' final artistic acceptance remain unfinished. This reference batch does not add a nineteenth action to the existing eighteen source-integrated rows of the 34-action matrix.

Imported 100x rifle frame bases are compared relatively, with strict physical pivot and vertex tolerances in metres; original native matrix values are retained exactly as object metadata. The trigger pivot is preserved in the editable object frame.

The reconstructed rest frames preserve deformation through an explicit calibrated mapping; they are not a claim that a replacement humanoid Avatar can be imported without validation. Existing compatible character masters and the installed skeleton remain authoritative. The canonical assembly is in Blender weapon-root metres: X right, Y forward, Z up, converted from Unity (x,y,z) as (x,z,y).

The latest verified playable package and execution state are recorded in [ART-PASS-REVIEW.md](ART-PASS-REVIEW.md), including the corrected authored finger calibration. Its gameplay, physical ADS, 1.5x zoom, cadence, accuracy, movement, collision and multiplayer are unchanged by the reference package and finger calibration correction. Continue production without intermediate approval holds; public release remains held for final acceptance.

The held assembly includes visible first-person geometry. Shadows-only rear surfaces and disabled optic glass are excluded from that visible reference; the original rifle master remains the complete weapon source for third-person geometry, mechanical hierarchy, LODs and exports.
