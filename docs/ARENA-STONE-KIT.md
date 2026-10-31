# Measured lower arena and ramp stone kit

This batch replaces nineteen existing deck/ramp renderings with seven original Blender families. It retains the measured collision envelopes, original three-part ramp profile, navigation and traversal devices. The kit uses jointed paving, inset coping, teal/bronze edge bands, underside coffers and inset arrows that point toward the upper arena. Wider environment, nature and device production remain separate unfinished work.

## Canonical geometry

`ArtSource/Original/ArtPass-v1/Environment/ArenaStone/Arena-Stone-v1.blend` is the metre-scaled master. `arena-source.json` is a read-only audit of the installed colliders, including mesh asset GUIDs, vertices, triangles, centres and mirror rotations. The audit checks family equivalence within 20 micrometres and confirms the saved scene bytes stay unchanged. Right-side ramp instances normalize to the same canonical +X ascent with a 180-degree Y rotation; their maximum source deviation is approximately 0.715 micrometres.

| Family | Unity W × H × D, metres | Placements | Triangles LOD0 / LOD1 / LOD2 |
|---|---|---:|---:|
| OuterX | 6 × 0.6 × 40 | 2 | 11772 / 924 / 644 |
| OuterN | 44 × 0.6 × 6 | 2 | 12916 / 996 / 692 |
| Spine | 4 × 0.6 × 12 | 2 | 2708 / 372 / 260 |
| Wing | 10 × 0.6 × 4 | 4 | 2312 / 344 / 244 |
| Landing | 4 × 0.6 × 4 | 4 | 1124 / 260 / 196 |
| Underpass | 8 × 0.6 × 28 | 1 | 10804 / 820 / 612 |
| Ramp | 16 × 4.4 × 4 | 4 | 1109 / 459 / 399 |

There are twenty-one unique LOD exports and fifty-seven installed LOD mesh instances. If all nineteen placements use the same level, their combined visual geometry is 83,776 / 9,656 / 7,160 triangles. This is a geometry budget, not a measured frame-time claim. Material slots share the existing original ivory, stone, teal, bronze and shade cel materials and painted paving texture; no new external dependency is introduced.

The ramp has two 6.5 m sloping spans separated by a 3 m level landing. Its travel rises 4 m in total, while its vertical slab thickness is 0.4 m. The complete 4.4 m bounds height includes rise and slab thickness. Source vertex levels define the actual piecewise upper/lower surfaces; visual vertices must remain inside that profile, not merely its enclosing box. Deck slab heights round to 0.6 m; source float precision is retained in the manifest.

## Editable references and imports

Each family uses the original collider bounds-centre pivot. Individual paving, core, coping, frieze, bronze, underside/coffer and wayfinding parts remain editable in named master collections. The hidden `SOURCE_COLLISION_REFERENCES_DO_NOT_EXPORT` collection preserves the canonical source hulls and asset GUIDs. Static stone modules have no rig, clips or weapon sockets; placement bindings retain the original route/device attachments in Unity.

The reference package has forty-two geometry-derived orthographic images, seven measured sheets with metre rulers and 1.8 m controller comparators, seven perspective views, seven cropped detail views and seven exploded detail views. All six orthographics share a scale within each family sheet. Cropped details use a separate, explicitly labelled 5.5 m span. Exploded views apply display offsets only: paving/arrow +0.9 m, coping +0.45 m, underside/coffers -0.65 m. They do not change canonical/exported geometry. Concept images do not override these measurements.

`ArenaStoneArtBuilder.Prepare` verifies canonical master/source and FBX hashes, imported triangle counts, reducing LODs, material assignments, scale and pivots. It creates seven prefabs and the nineteen-entry `LowerArenaStone` binding asset. Imported art never adds collision. Large-module LOD thresholds are 0.55/0.18/0.001 screen-relative size, medium modules 0.32/0.10/0.001, small modules 0.20/0.065/0.001; these require native visual/performance review.

The first asymmetric native hull check exposed the FBX conversion's half-turn: negative-X top was +2.2 m while positive-X top was -1.8 m, opposite the canonical ascent. Both imported hierarchy rotations were zero, so symmetric bounds alone hid the error. Prefab preparation now normalizes every imported model with a 180-degree Y rotation before applying each source instance rotation. The ramp end-height guard and full native hull review establish the resulting orientation. Original colliders and paths are never rotated. The initial failure and axis measurements remain under `Validation/arena-stone-installed-native.xml` and `arena-stone-import-axes.json`.

## Reproduction and acceptance

Run hidden Unity batch `RailArena.Editor.ArenaStoneArtBuilder.Audit`, then hidden Blender `tools/build_arena_stone_art.py -- "F:/Rail Arena"`. Render with `tools/render_bridge_stone_refs.py -- "F:/Rail Arena" --arena` and compose sheets with bundled Pillow Python `tools/package_bridge_stone_refs.py --arena`. Run `ArenaStoneArtBuilder.Prepare`, then the focused `ArenaStoneArtReview`. The complete art build also prepares this family through `ArtSliceBuilder.Prepare`.

`ArtSlicePresentation` installs these renderings only with the Sky Gardens art profile. Original collider meshes, scene transforms, navigation links/nodes, pad destinations and portal placements stay authoritative. The shared cleanup path restores original renderer flags and removes instances. Older maps retain their existing art.

Native integration evidence, matched gameplay captures, LOD readability, the next Windows build and representative frame timings must be read from current results before claiming acceptance. Native input/audio, all camera/terrain cases, complete inventory reference coverage and Sanders' final artistic approval remain open. Review 19 remains the current verified Windows package until this batch completes integration.

The final focused native review passes nineteen placements, fifty-seven LOD mesh instances, twenty-seven floor rays and 704 cross-LOD material-region comparisons, with original colliders/navigation/devices and cleanup preserved. Seven matching camera pairs and two forced simplified-level views are recorded in `UnityReview/installed-review.json`. Direct inspection confirms visible top surfaces at both simplified levels. All levels retain the same paving grid, weathered regions and arrow wells; intermediate geometry removes hidden paving depth and the far level closes subpixel joints. This preserves colour placement while reducing geometry, without claiming imperceptible transitions in every gameplay view.

The first candidate, committed as `791e57d`, retains the earlier changing-grid appearance for provenance. `Validation/arena-stone-near-preservation.json` compares the original and revised canonical LOD0 vertices by material across all seven families: identical triangle counts and zero bidirectional vertex deviation. This does not certify every UV or triangle index. Final sheets and installed evidence bind to master SHA-256 `5610080a36c54726d48e268c233692b352ae0108b811148bb09959fa3e6283b5`. The next shipping-player milestone remains pending.
