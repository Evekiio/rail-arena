# Measured basement stone kit

The optional Sky Gardens art profile replaces four staging and four basement deck visuals with a Blender-authored limestone kit. Review-10 is the verified portable package containing this batch; earlier packages remain preserved. Other maps and original deck objects remain preserved.

## Geometry and construction

| Profile | Diameter | Thickness | Top plane | Instances |
|---|---:|---:|---:|---:|
| Staging | 6 m | 0.6 m | Existing Y=14 m | 4 |
| Basement | 12 m | 0.6 m | Existing Y=-10 m | 4 |

`BasementDeckArtBuilder.Audit` measures the actual 20-sided source collision meshes, including vertices and triangles, without saving the scene. The canonical Blender master uses metres and a top-center pivot. Axis conversion is Blender `(x,y,z)` to Unity `(x,z,-y)`. Each profile has independent construction collections and three runtime LODs. The hidden `CollisionReference` collection retains the two exact source meshes for editing and comparison.

The kit adds radial paving, coping, recessed teal friezes, bronze inlay, an inlaid crest, cornices and a ribbed underside. Original periodic limestone paint uses a four-metre UV repeat, with packed colour, normal and cavity maps. The Unity cel profile remains the runtime lighting authority; technical Workbench sheets describe geometry and palette, not identical cross-renderer shading.

Every visual vertex is checked against the original convex footprint and vertical envelope. The visuals contain no colliders. Source meshes, vertices, triangles, route transforms, device transforms and the scene collider set remain unchanged. The old solid teal landing marker is hidden while the art profile is installed; its object and transform remain, and the authored ring keeps the two-metre landing cue. Removing the profile restores original renderers/materials and removes owned visuals.

## Source and evidence

- [Reference and comparison gallery](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/index.html).
- [Canonical Blender master](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/Basement-Stone-Decks-v1.blend).
- [Exact collision source audit](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/deck-source.json).
- [Exports, dimensions, hashes and LOD counts](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/production.json).
- [Per-part construction bounds, layers and measured views](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/measured-construction.json).
- [Installed 24-mesh review and comparison poses](../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/review-evidence.json).
- [Executed focused test](../Validation/art-basement-installed-production.xml).

The three LODs contain **6,876 / 3,556 / 748 triangles** per deck and use five material slots. Their measured bounds retain the original source envelope. The distant LOD keeps a simplified landing ring; it is explicitly shown in the comparison gallery.

Six identical-scale orthographic views cover each profile, with a one-metre ruler and a 1.8 m controller-envelope comparison. Two exploded views show temporary construction-layer offsets; those offsets are not installed dimensions. Matched in-game top, standing-eye and underside views isolate deck/marker visibility while keeping production lighting and other art installed.

## Reproduction

1. Pinned Unity 6000.6.7f1, batchmode/nographics: execute `RailArena.Editor.BasementDeckArtBuilder.Audit`.
2. Run `tools/paint_basement_paving.py` with the Pillow/NumPy-enabled Python runtime.
3. Blender 5.2 background mode, four threads and `--python-exit-code 1`: run `tools/build_basement_deck_art.py -- <repository-root>`, then `tools/render_basement_deck_refs.py -- <repository-root>`.
4. Unity batchmode/nographics: execute `RailArena.Editor.ArtSliceBuilder.Prepare`. It imports the six LOD exports, validates scale/top pivots, creates shared cel materials and saves two visual-only prefabs.
5. Run `RailArena.Tests.BasementStoneArtReview` in a hidden graphics-enabled EditMode session. Check that one test executed and passed. It restores the prior pipeline and quality-source bytes after review.
6. Run `tools/package_basement_deck_review.py`, then `tools/package_integrated_art_review.py` and `tools/join_current_art_inventory.py`.

Final native traversal, sustained frame pacing and artistic acceptance remain open. This is one completed environment production batch, not completion of the entire map kit or the full art pass. The broad map structure, remaining devices/nature families, full action library and complete reference coverage continue in production.
