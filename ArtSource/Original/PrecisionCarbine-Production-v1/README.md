# Precision carbine / production v1

The creator approved the v3 design on October 23, 2026 and authorized final modeling, game replacement and hand/shoulder fitting. [Open the production review gallery](index.html) for approved references beside final renders and installed Unity captures.

The shared `Assets/Resources/Weapons/Railgun.prefab` entry point now uses this carbine. All four contender prefabs and first-person forearms share the replacement. Earlier RA-01 meshes, materials, sources and reference packages remain recoverable; `Previous/` contains the pre-replacement prefab, hand profile and builder snapshot.

## Editable handoff

- `Precision-Carbine.blend`: 79 individually editable parts, metre scale, separated stock, shoulder pad, optic, grip, cassette and forward assemblies; named source-space contact anchors. `Final-Anchors.json` records the final fitted wrist positions/rotations and their coordinate conversion into the master.
- Three runtime LODs: **6,174 / 5,666 / 2,682 triangles**, within the 12k / 6k / 3k budgets. Unity exports use +Z muzzle-forward and +Y up.
- Four 2048px maps in `RailArena/Assets/RailArena/Art/PrecisionCarbine`: base color, tangent normal, metallic/smoothness and emission. Two runtime materials; ceramic, metal and contact-surface roughness is encoded in the atlas. The flat tangent-normal map is deliberate: the faceted surfaces and bevel normals are authored in geometry.
- `SurfaceAtlas.json` records part tiles, palette values, metallic/smoothness values and 8px gutters. UV islands are unwrapped into individual part tiles.
- Named muzzle, aim, shoulder and two wrist anchors. The trigger has an independent pivot, recovery presentation and exact reset. No charge-up, reload, case ejection or new gameplay weapon type is introduced.

## Installed fitting

The new wrist profile is derived from shared bind-pose hand geometry, rather than copying RA-01 offsets. Fingers have an authored carbine curl. A shared two-bone solver applies bone-space wrist contacts after humanoid animation, before finger articulation. The torso takes a rifle stance; the shoulder anchor follows the animated right shoulder. Retargeted facing is normalized from the hips/leg basis before solving the arms.

First person uses the same asset with rear stock surfaces hidden from the view and character-matched forearms/gloves. The rig retains its original bones but renders forearms and hands, avoiding shoulder caps in the camera. Controlled captures include normal framing, 1.5x zoom, recoil/reset and three LODs for every appearance. Human review of fine finger contact and animation remains a separate acceptance step.

The replacement exposed older runtime behavior. This change aligns firing to **36 ticks / 0.6 seconds**, consumes each valid click once, discards cooldown clicks and adds tangent-correct **1.5x hold zoom**. Zoom uses the saved `zoomSensitivity` value independently of normal sensitivity. Existing 100/50/34 head/body/limb damage is preserved; the broader 75/40/30 design migration is still pending. No wire schema changes; build compatibility identity is regenerated with the build.

## Validation and limits

`Geometry-Validation.json`, `Export-Validation.json`, `Unity-Validation.json` and `manifest.json` contain the current evidence. Reference geometry matches the approved master; orthographic raster bounds are checked within one pixel. Runtime LOD geometry, normals, UVs, bounds, material slots and anchors are checked separately.

This is an integrated production asset, not release certification. Four-contender 1080p/144fps measurement, sound-on acceptance, external-peer duel and human visual/input review remain open. Hidden standalone checks do not establish those results.

## Reproduce

Run `tools/Build-CarbineProduction.ps1` from PowerShell. Blender and Unity jobs are sequential, headless, bounded and owned-process cleanup is applied. `-SkipModeling` reuses the existing source/exports. `-SkipWindowsBuild` omits the standalone build. The builder at `RailArena.Editor.RailgunBuilder.Build` regenerates the carbine, never the old RA-01. No GitHub publication is performed by this pipeline.

The ordinary Windows playtest is `Builds/Carbine-v1/RailArena.exe`. Standalone qualification uses the separate `Builds/Carbine-v1-Validation/RailArena.exe` development build because validation hooks are excluded from ordinary builds. Run `tools/Test-CarbineProduction.ps1` for bounded CPU/headless mechanics, three-bot offline combat and a loopback duel. These checks use batch mode with graphics disabled; they are not rendering or frame-rate benchmarks.

Final automated evidence: 90 Unity tests, 37 standalone mechanics checks, a 45-second four-contender offline run and a 45-second local duel all passed. The offline run recorded 208 shots and 11 deaths without exceptions; both duel peers reported zero errors and matching compatibility identities. See [the validation record](../../../docs/VALIDATION.md) for scope and limits.

The current public pre-alpha has its own [release qualification record](../../../Validation/PreAlpha/release-validation.json). Its identity supersedes earlier integration-checkpoint identities.
