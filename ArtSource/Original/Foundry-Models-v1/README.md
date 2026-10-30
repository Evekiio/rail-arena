# Foundry environment / modeled review candidates

[Side-by-side review gallery](index.html) · [Editable master](Foundry-Environment.blend) · [Geometry report](Model-Validation.json)

Created at the creator's request to begin modeling. This authorizes model review work; it does not record approval of the exact layout, completed gameplay integration or competitive qualification.

## Delivered

- 22 editable asset assemblies, individual Y-up metre-scale GLB exports, 65 actual Blender renders.
- Exact v2 deck placements, thirty cover envelopes, four ramps, four pads, two linked gate housings and three health mounts in a shared art assembly.
- Six additional orthographic construction views for floor, low cover, ramp, pad, portal and health.
- Closed hidden surfaces, single-facet bevels, material separation and generated UV islands. No photographic textures.
- Independently selectable original collision boxes. Art does not silently become collision.
- Shared ivory ceramic, graphite, teal, copper, restrained cyan and amber materials.

## Construction decisions and remaining work

Map.json is the dimensional authority. Floor bays use linked reusable meshes, with scaled closure bays where needed. Cover remains opaque within its source gameplay volume; the current elbow variants use that envelope rather than introducing a new see-through corner. Small applied face plates extend beyond collision by up to approximately 4cm and need intersection review before integration. This is a modeled first pass, not a claim of perfect concept reproduction.

Portal artwork retains a floor-level 1.4 × 2.2m opaque field and solid rear. Gate state VFX, A/B labels, portal traversal and jump-pad impulses are not implemented by these static meshes. The 2.4 × .8 × 2.8m portal source envelope is offset vertically in Map.json; its bottom is -.1m. The art retains that placement. Health pedestal is .8 × .8 × .25m; its separate hovering marker is .35 × .5m. Amber identifies health consistently.

The arena assembly uses source geometry and source cameras. No additional wall bays, railings or supports are placed where they could change routes or sightlines. Beam and edge assets are available in the library but await explicit placement review. Dressing objects need final inset mounting checks. Crate and cabinet are unplaced proposals.

Cloud sky artwork, distant islands/citadels, directional landmarks/signs, state animation, final texture atlases, LODs, collision/occlusion qualification and actual Unity import/playtesting remain outstanding. The renders use bright neutral world lighting, not a final skybox. UVs are authoring islands, not a final baked atlas. No performance or release readiness claim is made.

## Review

Compare the whole arena against matching measured cameras, then review each modeled asset beside its concept sheet. Approve or revise silhouettes, panel construction, material proportions and device clarity before integration. The six construction views and GLBs are generated from the same mesh collections as the editable master.

## Reproduce headlessly

Run `tools/build_foundry_models.py` through background Blender 5.2.2 with four threads and a bounded owned-process wrapper, then `python tools/package_foundry_models.py`. The script writes only this new package; it does not modify Unity runtime assets or earlier source references.
