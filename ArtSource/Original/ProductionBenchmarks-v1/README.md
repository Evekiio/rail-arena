# Rail Arena / production benchmarks v1

**Editable model candidates for creator review.** These establish the rifle and environment quality target; they are not a claim of final approval, calibrated hand fit, competitive validation or measured combat performance.

**Stock revision pending:** the user rejected the existing skeletal stock. The [complete rifle modeling-reference package v2](../RailRifle-Modeling-References-v2/README.md) is now the current review proposal, including the revised stock, six-view drawings, details and assembly guide. These benchmark renders retain the old stock and are historical for stock construction.

## Review the models

**[Open the side-by-side comparison page](index.html).** Each reference stays on the left while you select Unity, Blender studio or an orthographic render on the right. The cover comparison isolates card A; the full original sheet remains linked. Perspective, lighting and scale differ, so these are visual comparisons rather than calibrated overlays.

For geometry approval and Blender setup, use the separate **[dimensioned six-view reference package](Dimensional-References-v1/README.md)**. It supplies consistent left/right/top/underside/front/back views, clean calibrated PNGs, scale metadata, part dimensions and a reference-only Blender rig. Those drawings describe the benchmark proposal and require approval before becoming a construction lock.

| Model | Source of visual direction | Review |
| --- | --- | --- |
| Ivory rail rifle | [Supplied rifle image](../Design-v1/Sources/User-Rifle-Reference.png); enclosed ivory shell, open stock, optic and cyan inlays | [Blender studio](RailRifle-Studio.png), [left](RailRifle-Left.png), [right](RailRifle-Right.png), [top](RailRifle-Top.png), [underside](RailRifle-Bottom.png), [muzzle](RailRifle-Front.png), [rear](RailRifle-Rear.png) |
| Low wing cover | Card A in the [Foundry cover reference](../Foundry-Arena-v2/References-v1/Concepts/02-Cover-Kit-v2.png); exact 2 × 1 × 1.2m envelope | [Blender studio](LowWingCover-Studio.png), [front](LowWingCover-Left.png), [back](LowWingCover-Right.png), [top](LowWingCover-Top.png), [underside](LowWingCover-Bottom.png), [end A](LowWingCover-Front.png), [end B](LowWingCover-Rear.png) |

![Ivory rail rifle model candidate](RailRifle-Studio.png)

![Low wing cover model candidate](LowWingCover-Studio.png)

The small supplied rifle illustration is the available weapon authority. No completed, approved multi-view rifle construction sheet exists in this repository. The unseen side, underside, internal bore and grip contact locations are authored interpretations requiring review. The cover's illustrated perspective is also not calibrated CAD; the measured collision envelope takes precedence.

## Editable delivery

- [Blender master](Production-Benchmarks.blend): named Rifle and Low wing cover collections, separately editable mesh parts, packed palette texture and review lighting. Both collections use a local origin; isolate a collection while editing.
- [Geometry evidence](geometry.json): source and exported LOD counts, finite coordinates, manifold/degenerate checks, bounds, anchors and source-reference hash.
- [Unity assets](../../../RailArena/Assets/RailArena/Art/ProductionBenchmarks): two FBX LODs per model, shared palette, generated materials, separate prefabs, authored 0.6s rifle recoil/recovery clip and an isolated review scene after the headless importer succeeds.
- [Unity validation report](../../../Validation/production-benchmarks-unity.txt) records actual import results. Unity images, if present, are [rifle](RailRifle-Unity.png) and [cover](LowWingCover-Unity.png); Blender views above are offline renders.

Both models use one joined render mesh per LOD and two shared material slots: palette surface and restrained cyan emission. The 128 × 16 palette uses intentional overlapping swatch UVs. These are not baked-lightmap unwraps or unique paintable texture islands. The materials use one common surface response, so distinct ceramic/metal micro-roughness is not yet authored. This is an intentional small baseline for review, not a claim of a completed PBR bake.

All surfaces are closed solids, with real through-openings in the stock, trigger guard and enclosure. Hidden and underside surfaces are present. Source geometry is triangulated for repeatable export; the source parts remain individually editable. LOD1 is a decimated candidate that still requires a visual transition review at gameplay distance.

## Coordinates and integration

Blender source: X forward, Y width, Z up; metre scale. Rifle export: Unity +Z muzzle-forward and +Y up. Cover export: long axis +X, height +Y, depth Z. Cover pivot is at the ground centre. Its deliberately simple box collision remains 2 × 1 × 1.2m and introduces no extra snags.

Rifle anchors: Muzzle, RightGrip, LeftSupport, AimLine and StockContact. Contact markers are authoring targets, not an approved finger pose. The old weapon's hand calibration must not be reused without fitting it to this rifle. No reload or firing charge-up is introduced. The recoil/recovery clip is an isolated presentation asset; it does not change authoritative firing rules or hook into current gameplay.

The importer creates assets only under `Assets/RailArena/Art/ProductionBenchmarks`. Existing live railgun prefabs, character grips and arena geometry are not replaced. No non-rendering weapon collider is added to the rifle.

## Reproduce headlessly

From the repository root:

```powershell
.\tools\Build-ProductionBenchmarks.ps1
unity run "F:/Rail Arena/RailArena" --editor-path "D:/Unity Editor/6000.6.7f1/Editor/Unity.exe" --timeout 300 --log-file "F:/Rail Arena/Validation/production-benchmarks-import.log" --no-tail -- -nographics -job-worker-count 4 -executeMethod RailArena.Editor.ProductionBenchmarkBuilder.Build
unity run "F:/Rail Arena/RailArena" --editor-path "D:/Unity Editor/6000.6.7f1/Editor/Unity.exe" --timeout 180 --log-file "F:/Rail Arena/Validation/production-benchmarks-render.log" --no-tail -- -job-worker-count 4 -executeMethod RailArena.Editor.ProductionBenchmarkBuilder.RenderReview
```

The render command needs a graphics device but remains batch/headless with no visible Editor or game window. Run jobs sequentially. The Blender wrapper limits work to four threads and eight minutes and cleans up its owned process. The CLI bounds Unity process lifetime. No install or external service is required when the pinned tools are already present.

## Acceptance still open

1. Creator comparison of silhouette, construction, materials and hidden surfaces.
2. Actual first-person/third-person grip fitting, optic alignment, trigger clearance and motion review with the character rig.
3. Camera-distance and LOD transition review, palette readability under arena lighting, reduced-flash presentation and fire/recovery feel.
4. Controller collision checks around the cover in the actual Foundry geometry.
5. Measured four-contender performance on documented reference hardware.

The benchmark authoring and exports are local work. Nothing in this package establishes approval, gameplay replacement or GitHub publication.
