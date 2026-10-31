# Measured garden nature production

The source package adds eleven original Blender modules for the four existing Sky Gardens planting sites: a trunk, three crown variants, two shrub variants, two grass variants, a vine-stem assembly and two vine-leaf variants. Thirty-three reducing LOD exports use one original opaque painted palette material per mesh. No external or paid dependency is introduced.

## Authority and geometry

`GardenNatureArtBuilder.Audit` reads the installed Sky Gardens Basement scene without saving it. `nature-source.json` records twenty-four renderings, mesh GUIDs, vertex/index hashes, world-space metre bounds and source wind/collider roles. All twenty-four are noncolliding. The four pairs of ivy stems/leaves share their original union frame so independently authored leaves do not drift from their stems. Original physics, navigation and device placement remain authoritative.

`ArtSource/Original/ArtPass-v1/Nature/GardenNature/Garden-Nature-v2.blend` is the canonical metre-scaled unit-frame master. Editable components remain in named family/LOD collections. CrownA LOD0 is initially visible; other collections are available separately. Blender coordinates convert to Unity as `(x,z,-y)`; native axis conversion and every imported vertex remain to be checked before installation.

Each module has a one-metre reference frame. Its actual geometry dimensions are recorded separately, and original installation frames give the precise world-space scale. All three LODs use a common frame rather than stretching each simplified mesh to refill its bounds. Ivy's shared stem/leaf frame stays fixed across both families. There are no rig, clip, grip or root-motion dependencies. Existing decorative wind will be retained by role during integration.

The master contains original tapered tube branches, faceted crowns, folded opaque foliage and a packed painted palette. The 384x384 atlas has nine inset swatches, original gradients, leaf veins and bark strokes. It is not an alpha-card foliage system. Source models do not establish runtime draw-call savings or frame-time acceptance.

## Reference package and reproduction

The package contains sixty-six equal-scale geometry-derived orthographic images, eleven measured sheets, perspective/detail views and four audited installation-scale diagrams. Sheets include metre rulers, actual geometry dimensions, LOD budgets, installation-frame dimensions, source identity and a 1.8 m / 0.72 m controller comparator at the same pixels/metre. Detail crops use their separately labelled 0.6 m span. Installation diagrams use source frame transforms plus temporary planter/controller context; they are not Unity screenshots or new production meshes.

Run hidden Unity `GardenNatureArtBuilder.Audit`, hidden Blender `tools/build_garden_nature_art.py`, then `tools/render_garden_nature_refs.py`. Bundled Pillow Python `tools/package_garden_nature_refs.py` composes sheets and resolves the audited per-site module bindings. Hidden Blender `tools/render_garden_nature_sites.py` produces actual-scale diagrams, then rerun the package composer to link them. Reference scripts check the master hash and never save it. Native preparation, integration and a Windows milestone follow after visual/source refinement.

## Current scope and gaps

This is a source/reference milestone, not installed artwork in the verified Review 20 build. Its production record, model hashes and measured views identify the exact candidate. The first installed-scale preview reveals flat shrub masses and several branch/crown mismatches. Preserve that candidate in Git, then refine cupped leaf rosettes and branch contacts before native review. The original canopies, bark, grass, shrubs and vines remain installed until the new family passes import, clearances, visual comparisons and cleanup checks.

The full nature category still includes waterfall lips/foam, cloudscape and distant ruins. The broader art pass, complete reference coverage, measured performance and Sanders' final native/artistic acceptance remain open. No new public release is implied.
