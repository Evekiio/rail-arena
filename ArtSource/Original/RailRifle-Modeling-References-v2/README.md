> Historical package. Current proposal: [precision carbine v3](../PrecisionCarbine-References-v3/README.md). No production approval is implied.

# Rail Arena / complete rifle modeling references v2

**Current reference proposal, ready for creator review.** This package consolidates the enclosed ivory rifle and the newly requested reinforced stock. All construction views come from one isolated editable reference study. It does not mark the stock, dimensions, hidden surfaces or final model approved.

## Start here

- [Visual review gallery](index.html), including the stock input beside the revised study.
- [Six coordinated views](RailRifle-Six-View.png): left, right, top, underside, front and back, dimensioned in mm.
- [Stock construction](02-Stock-Construction.png): side, top, rear, material boundaries and shoulder/receiver orientation.
- [Axial and contact details](03-Axial-Contacts.png): enlarged front/back, grip/trigger region and muzzle assembly.
- [Exploded art assembly](04-Exploded-Assembly.png): twelve named groups and their measured envelopes.
- [Section and anchors](05-Section-Anchors.png): centre cutaway and source-coordinate contact/aim/muzzle targets.
- [Materials and motion guide](06-Materials-Motion.png): material zones, proposed surface responses and modeling states.

Each board also has an editable SVG sibling. Individual clean images are in `Clean/`; enlarged/cropped details are in `Details/`. The gallery links every image at full resolution.

## What changed

The old skeletal stock and cyan stock disc are replaced in this **reference study** by a broad graphite cheek rest, thick angled shoulder pad, single compact reinforced opening, solid attachment neck and small recessed sling socket. The reference photo's attachment direction is reversed to suit the right-facing rifle. Both stock sides share the same construction and restrained cyan seam.

The receiver, optic, grip, enclosed barrel, muzzle and lower accessories retain the benchmark geometry. The study removes the environment module entirely. Underfill lighting makes bottom and recessed surfaces easier to read. Enlarged end views address the small front/back silhouettes on the common-scale sheet.

## Modeling files and authority

| File | Purpose |
| --- | --- |
| [Rifle-Reference-Study.blend](Rifle-Reference-Study.blend) | Assembled reference geometry with separately editable parts, packed palette and assembly IDs |
| [Blender-Reference-Rig.blend](Blender-Reference-Rig.blend) | Blank modeling setup: six locked, correctly oriented image references, no production mesh |
| [Calibration.json](Calibration.json) | Exact bounds, camera axes/frames, detail scales, part bounds, anchors and source hashes |
| [RailRifle-Parts.csv](RailRifle-Parts.csv) | Every part's source min/max and envelope, in mm |
| [Assembly-Dimensions.csv](Assembly-Dimensions.csv) | Twelve assembly envelopes and part counts |
| [Assembly.json](Assembly.json) | Machine-readable assembly groups and bounds |
| [manifest.json](manifest.json) | Reference status, image hashes and validation evidence |
| [Sources](Sources) | Supplied rifle and stock images and the native stock profile proposal |

Authority order: creator-approved revisions, the written locked game design, accepted construction dimensions, the shared study geometry, then rendered illustration. Approval of an overall look does not approve contradictory hidden surfaces. The original rifle image has no scale; physical rifle dimensions remain proposed. Part/assembly dimensions here describe the study after beveling and are not real-world manufacturing measurements.

**Geometry is ready as a coordinated modeling reference, not certified production topology.** The source study reuses triangulated benchmark pieces so the views agree. It is not a claim of completed retopology, UV painting/bakes, LOD transitions, final hand fitting or measured runtime cost. The section shows only modeled cosmetic surfaces and does not specify a working firing mechanism. Exploded offsets identify visual parts and must not become assembled positions or mating gaps.

## Exact Blender setup

Use metres with source axes **X muzzle-forward, Y width, Z up**. The clean six-view images are 2048 Ã— 2048px and each represents a **1.45 Ã— 1.45m square**. The provided image rig sets their size, common centre and orientation automatically. Selection is locked; visibility is limited to aligned orthographic front-facing views. Images are packed for portability.

Do not crop, stretch or resize those six images independently. Front and back are viewed from +X and -X; left and right from -Y and +Y; top and underside from +Z and -Z. These names are explicitly defined, regardless of Blender keypad naming. The common centre is the source bounding-box centre, which differs from object coordinate zero; use `Calibration.json` rather than placing every image at the world origin.

Enlarged detail frames differ: axial 0.55m, stock 0.52m, grip 0.40m and muzzle 0.14m. Use their recorded frames and centres if loading them as extra reference planes. The angled hero, muzzle and exploded views illustrate form; use the six axis-aligned views for orthographic construction. Raster tracing has finite precision. Written dimensions and the editable study govern sub-pixel details.

## Construction and finishing requirements

Preserve the source object origin and metre scale. The final game export must map the source forward axis +X to Unity +Z and source up +Z to Unity +Y, with the existing project export convention verified on the muzzle anchor. Do not fix an import by arbitrarily mirroring the mesh or moving the origin.

- Keep the stock opening, cheek-rest silhouette, angled pad, receiver interface and bore/optic axes consistent in every view.
- Preserve part IDs RF-01 through RF-12. Stock, pad and cheek rest remain separately editable. The trigger is a separate source object; the closed guard must retain its opening.
- Model both faces, underside, muzzle/back surfaces and hidden overlaps. Do not add unsupported vents, ejection hardware, rails or an extra stock aperture from imagination during final modeling.
- Use intentional low-poly facets and small silhouette bevels. Avoid random triangulation in visible planar regions during production retopology. Validate normals, degenerate geometry and silhouette again after export.
- The palette provides six art surface zones through two reference materials; the board's per-zone roughness/metallic values are proposed production targets. They are not already baked into the palette study. Cyan should remain restrained under normal and reduced-flash presentation.
- Use an asset-specific UV/export plan for final production. Current overlapping palette swatches are deliberate study UVs, not unique paintable or baked-lightmap islands. Texture density, LOD budgets and bake detail need measurement against the game's complete scene.

## Contacts, movement and gameplay

Muzzle, AimLine, RightGrip, LeftSupport and revised StockContact are authoring targets in source coordinates. Fit the actual character hands and shoulder before declaring ergonomic acceptance. The optical axis is above the cosmetic bore; that offset must remain explicit when aligning the first-person camera and reticle.

The locked weapon is instant-hit, click-only semi-auto with a 0.6s cooldown, unlimited ammunition and no reload or firing charge-up. The capacitor cassette is fixed gameplay decoration; the exploded assembly is not a reload sequence. Suggested presentation uses whole-weapon recoil, a brief muzzle effect, a separate trigger movement and a return to rest. This package specifies reference states; it does not implement or approve a new animation.

## Approval checklist

| Review | Current status |
| --- | --- |
| Revised stock profile, width, pad angle, cheek rest and hidden faces | Awaiting creator approval |
| Coordinated six-view construction and proposed overall dimensions | Awaiting creator approval |
| Receiver/shroud/optic/grip/muzzle detail interpretation | Awaiting creator review |
| Material zones and low-poly edge treatment | Awaiting creator review |
| Hand/finger/shoulder contacts and first-person framing | Not fitted to the revised stock |
| Production topology, final UV/material response, LOD and performance | Production qualification remains open |

Approve or request changes against this version before a final modeling lock. Any geometry revision must regenerate the shared views, dimensions, details and image rig together. The old benchmark still uses the rejected stock and is historical for stock construction.

## Reproduce and validate

The end-to-end command is `.\tools\Complete-RifleReferenceV2.ps1`. Use `-SkipRendering` only to repackage and validate an already completed current study. Tool paths are parameters; defaults use installed Blender and the bundled Python/Pillow/Sharp runtime.

Run `.\tools\Build-RifleReferenceV2.ps1` from the repository root. It uses background Blender 5.2, four threads, a twelve-minute bound and owned-process cleanup. Then run `tools/package_dimensional_references.py` with `--folder ArtSource/Original/RailRifle-Modeling-References-v2 --master ArtSource/Original/RailRifle-Modeling-References-v2/Rifle-Reference-Study.blend`, followed by `tools/package_rifle_reference_v2.py`.

Rasterize the six SVG boards using Sharp. Run `tools/build_dimensional_reference_rig.py` headlessly with the repository after `--` and `--folder ArtSource/Original/RailRifle-Modeling-References-v2`. Validate using `tools/validate_dimensional_references.py` with the same folder/master arguments and the bundled Python/Pillow runtime. Additional rifle validation checks all detail images, assembly coverage, unchanged non-stock geometry and the source hash.

No Unity process, player, visible Blender window or live asset replacement is required. This package remains local and awaiting approval; it is not published to GitHub.
