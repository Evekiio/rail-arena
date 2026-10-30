# Approved low-poly contender direction

The separate [player character pilot](PLAYER-PILOT.md) develops the selected player references into an isolated editable model and carbine-fit review. It does not replace the installed meshes described below.

The creator approved the sheet preserved at `ArtSource/Original/StylizedContenders/Approved-Concept.png` as the design target. The player uses ivory armor, a dark suit, a teal collar and cyan insets. Bot 01 uses terracotta armor and tied-back hair; Bot 02 uses slate/teal armor, pale swept hair and reinforced collar plates; Bot 03 uses plum armor, copper details, sculpted locs and asymmetric armor tabs. These are appearance variants, not gameplay classes.

## Sources

The expanded [character reference package](../ArtSource/Original/StylizedContenders/References-v1/README.md) supplies four sheets per character: eight full-body directions, larger head/crown/under-jaw inspections, armor/clothing/glove/boot construction details, and guided full-body top/bottom concepts. Use its `Final` folder and modeling guide. These expand the approved concept for review; they do not certify exact shared-mesh projection or change the existing game assets.

- `ArtSource/Original/StylizedContenders/Player.blend`, `Bot01.blend`, `Bot02.blend` and `Bot03.blend`: individually named original mesh parts, modeled facial features, hair, clothing, armor and articulated gloves on a shared skeleton.
- The corresponding `*-Preparation.json` files record actual dimensions and export counts.
- `tools/build_stylized_contenders.py`: deterministic modeling, palette generation, three-LOD export and optional Blender review rendering. No source soldier mesh is reused.
- `tools/validate_stylized_contenders.py`: editable-source, geometry, scale, skin-weight and finger-joint audit.
- `RailArena/Assets/RailArena/Art/StylizedContenders`: twelve FBX files and four palette textures/materials. Each LOD has one skinned mesh and one palette material.
- `StylizedContenderBuilder.cs`: retains the calibrated runtime skeleton, humanoid controller, eleven hurtboxes and weapon/grip references while replacing presentation meshes. It saves calibrated skin assets with bind poses rebuilt for the retained rig's FBX bone scale.
- `Resources/Characters/Contender.prefab`: player. `Resources/Characters/Stylized/Bot01.prefab` through `Bot03.prefab`: bots. Actor slot 0 uses the player; other slots cycle through the three bot designs, including the challenger slot in an online duel.

The skeleton is retained from the existing CC0 Grab3D source, and locomotion/death animations remain the CC0 Quaternius clips. These dependencies and their provenance still apply. The new visible geometry is original procedural modeling; it is not a newly authored animation library or a new facial-animation rig. Eyes, eyebrows and lips are static modeled features. The approval sheet is AI-generated concept art, distinct from the modeled result.

## Regeneration

Run from the repository root, with the pinned Blender 5.2 and Unity 6000.6.7f1:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --threads 4 --python-exit-code 1 --python tools/build_stylized_contenders.py -- (Get-Location).Path --render
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --threads 4 --python-exit-code 1 --python tools/validate_stylized_contenders.py -- (Get-Location).Path
unity run .\RailArena --editor-version 6000.6.7f1 --timeout 300 -- -nographics -executeMethod RailArena.Editor.StylizedContenderBuilder.Build
unity test .\RailArena --editor-version 6000.6.7f1 --mode EditMode --filter RailArena.Tests --output .\Validation\StylizedContenders\EditMode.xml --timeout 600
unity run .\RailArena --editor-version 6000.6.7f1 --timeout 180 -- -executeMethod RailArena.Editor.StylizedContenderReview.Capture
```

Blender uses four CPU threads for the studio renders. The last Unity command needs a graphics device for offscreen rendering but remains in batch mode and opens no visible Editor or player window. Do not pass `-nographics` to that capture command. The new character build also regenerates arm-only first-person geometry with matching palette UVs.

## Review boundaries

### Reference matching revision 2

The second modeling pass reshapes the jaw and cheek planes, narrows eye spacing, adds modeled eyelids, lengthens and layers swept hair locks, fits the temple/nape hairline, and distinguishes the female jaw and torso silhouettes. Armor now has shallow crowned panels with inset perimeter faces, shaped shoulder fronts and recessed fasteners. The waist sits lower, cloth hip panels extend farther down the thighs, and boots use fitted instep plates, straps and clasps. The shin silhouette uses a dark base with smaller lateral guards. Bot 02 uses pale shoulder, forearm and knee plates over teal armor.

Palette PNGs now explicitly encode authored linear colors to sRGB. Previously the linear values were written directly into PNG bytes and then decoded again by Unity's sRGB importer, darkening the skin and cloth. An independent PNG-decode regression covers all four skin tones. The palette was also retuned against the approved concept, independently of studio lighting.

The supplied earlier Unity lineup and generator are preserved under `Validation/StylizedContenders/ReferenceMatch-v2/Before-*`. Updated studio images are rendered from the actual Blender geometry; `Unity-Lineup.png` and the front/side/back/face captures show the imported assets. The concept has more individually sculpted facial anatomy, hair flow, garment folds and surface detail than this procedural mesh revision. These remain visible differences, so this pass must not be described as a perfect reproduction or as final artistic approval.

The concept approval authorizes this modeling work; it does not establish that the resulting meshes reproduce every feature of the sheet perfectly. Blender studio renders show the actual geometry. Unity captures show the imported LOD0 surfaces. Geometry and animation checks are technical evidence, separate from final creative approval, native-input playtesting, movement feel and release readiness. Old evaluation ZIP archives remain historical and do not receive updated assets automatically.

The design uses segmented rigid armor over a skinned suit, without cloth simulation. Shared rig and hit regions preserve the existing game rules. Fine facial expression, bespoke hair deformation, cloth secondary motion and individually authored action clips are outside this modeling pass.

## Technical results

The Blender audit passes for all twelve LODs, including primary-channel palette UVs, normalized weights, finite vertices, no degenerate triangles and all thirty finger joints. Delivered LOD0 triangle counts are 10,311 (player), 9,573 (Bot 01), 10,487 (Bot 02) and 9,359 (Bot 03); LOD2 is approximately 2,500–2,850 triangles per character. Source and delivered tessellation counts are both recorded because FBX normalizes tiny slivers in reduced LODs.

`Validation/StylizedContenders/EditMode.xml` records all 87 Editor-suite tests passing. The new cases check the four distinct meshes and palettes, primary palette UVs, normalized skinning, retained humanoid/hurtbox/grip structure and animated metre scale. Existing finger deformation and live hand-IK tests also pass. Captures under `Validation/StylizedContenders/Unity-*` use actual imported meshes and explicit per-renderer palette bindings; they are offscreen studio views, not gameplay screenshots.

The suite also exposed a shutdown timing edge: a released session could receive another simulation tick while the Editor ignored `Application.Quit`. Resource release now disables the session before disposing its network and rewind resources; the shutdown regression verifies that state.

The rebuilt development player passed all 37 standalone mechanics checks and a 45-second automated offline session using all three bot designs, with zero reported application errors. Reports are `Validation/StylizedContenders/Mechanics.json` and `Offline.json`. The offline driver checks combat, movement and lifecycle activity; it does not establish native controls, sound quality or final artistic finish.

Both local Windows development and Evaluation players were rebuilt successfully from fingerprint `969b594c8ac3550a18bbd98310b026c99f9f21c08389faa923b266658b2db809`. The extracted historical evaluation archive remains unchanged. All Blender and Unity Editor work ran in background/batch mode; standalone validation players used hidden windows and exited after their checks.
