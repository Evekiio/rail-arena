# Sky Gardens summit stone kit

This original family extends the measured ivory, teal and bronze stone language onto the highest deck, central cover and four columns. It adds radial limestone paving, an eight-point compass seal, segmented coping, a frieze, underside coffer rays, framed cover and banded column construction. The existing summit height, jump launches, destinations, route edges and source collision remain authoritative.

## Source contract

`SummitStoneArtBuilder.Audit` reads the installed basement scene without saving it. Its collision report retains three canonical module profiles, all six installed transforms and the exact geometry records for all four column instances. Existing painted render meshes differ from the source collider meshes; the collision geometry defines the authoring envelope. The four column point sets differ by at most 9.22e-16 metres after local top-centre conversion, despite their byte-level hashes differing. The audit rejects deviations above twenty micrometres; it does not claim identical source files.

Blender uses metres with `(x,y,z)` mapped to Unity `(x,z,-y)`. Each module has the original top-centre pivot. All vertices are checked against the full original convex hull and vertical envelope. A four-millimetre recess separates the centre stone bed from its compass inlay. Reduced cylindrical LODs retain the exact cardinal extrema and stay inside the original hull. They intentionally simplify the intervening silhouette. No collision mesh or root motion is added.

| Module | Dimensions | LOD0 / LOD1 / LOD2 triangles |
|---|---|---|
| Summit deck | 14 m diameter × 0.6 m depth | 7,088 / 3,944 / 1,584 |
| Central cover | 2 m × 2 m × 1.1 m high | 192 / 144 / 96 |
| Column | 0.56 m diameter × 5 m high | 880 / 388 / 224 |

The editable master retains per-part construction layers; `production.json` records their bounds, materials and exact source hashes. Eighteen geometry-derived orthographic views and three perspective views accompany three dimensioned sheets. Every module sheet uses identical scale across its six views and includes a 1.8 m × 0.72 m controller comparator. This is a scale diagram, not a new character model. Shared painted stone and cel materials reuse the existing original basement family.

## Reproduction

1. Hidden Unity batch: `RailArena.Editor.SummitStoneArtBuilder.Audit`.
2. Blender background: `tools/build_summit_stone_art.py -- "F:/Rail Arena"`.
3. Blender background: `tools/render_bridge_stone_refs.py -- "F:/Rail Arena" --summit`.
4. Bundled Pillow Python: `tools/package_bridge_stone_refs.py --summit`.
5. Hidden Unity batch: `RailArena.Editor.SummitStoneArtBuilder.Prepare`, after shared basement stone materials exist.
6. Hidden graphics-enabled Unity test: `-testPlatform EditMode -testFilter RailArena.Tests.SummitStoneArtReview`. The Editor-only test enters play mode internally.

## Acceptance limits

The final focused installed test passed in `Validation/summit-stone-final.xml` after the column correction. Final import passed in `summit-stone-final-import.log`. Eight matching 1920×1080 captures are retained in the source family's `UnityReview` directory. Earlier audit/import failures are preserved as diagnostic evidence, including the byte-hash column comparison and the reduced-LOD cardinal-extremum mismatch; they are not final passing results.

Import checks require metre-scale bounds, original top pivots, valid shared material mapping, three strictly reducing LODs and no added collider. The installed review checks six placements, eighteen LOD meshes against actual source hulls, source collider identities/vertices/triangles/transforms, all portal/pad transforms, pad destinations and removal/restoration. Matching world cameras isolate original versus authored summit geometry under the same cel pipeline at 1920×1080. Captures exposed the column core covering its narrower shaft; the core was inset before the final review. Hidden review does not establish native input, sound, sustained frame pacing or Sanders' final artistic acceptance. Review-16 now includes the Windows integration milestone; source reference copies retain their earlier capture-stage wording. See ART-PASS-REVIEW.md for the current identity, checksum, direct-player checks and performance exceptions.
