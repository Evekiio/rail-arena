# Sky Gardens bridge stone kit

The bridge family extends the original staging and basement stone language into the central bridge, two cover screens and two portal-adjacent piers. Original Blender construction adds jointed limestone paving, coping, teal friezes, bronze strings, inset wall panels and underside coffer ribs.

## Authoritative scale

`BridgeStoneArtBuilder.Audit` reads the installed Sky Gardens basement scene without saving it. The source report includes 17 original render parts, their world-space vertices and triangles, bounds, materials, collider types and hierarchy. Three representative collision-bearing modules define the repeated family. Blender uses metres with `(x,y,z)` mapped to Unity `(x,z,-y)`. Module pivots sit at the source bounds centre.

Every authored vertex is checked inside the measured source envelope. Decoration is inset; the bridge top, underpass headroom, cover silhouette, pier faces and portal mounting remain authoritative. Runtime installation replaces rendering only, keeps source colliders and return devices, and restores the original visuals when removed. Older maps do not install this presentation.

## Reproduction

1. Run hidden Unity batch mode with `RailArena.Editor.BridgeStoneArtBuilder.Audit` on the RailArena project.
2. Run Blender background with `tools/build_bridge_stone_art.py -- "F:/Rail Arena"`.
3. Run Blender background with `tools/render_bridge_stone_refs.py -- "F:/Rail Arena"`.
4. Run `tools/package_bridge_stone_refs.py` with the bundled Pillow Python.
5. Run hidden Unity batch mode with `RailArena.Editor.BridgeStoneArtBuilder.Prepare`. Shared basement stone materials must already exist.
6. Run the Editor-only `RailArena.Tests.BridgeStoneArtReview` using `-testPlatform EditMode`; it enters play mode internally. Graphics must be enabled for the render captures.

The master, nine FBX exports, construction manifest, 18 equal-scale orthographic views, three perspective views and three dimensioned reference sheets are retained under the BridgeStone source family. Paving uses the existing original basement stone image, packed into the master; Unity shares the existing cel materials.

## Evidence limits

The refined installed review passed one focused test in `Validation/bridge-stone-refined.xml`: five placements, fifteen LOD meshes, all authored vertices inside source envelopes, strictly reducing triangle counts, unchanged collider set and source mesh identities, unchanged return-device transforms, and visual cleanup/restoration. Eight matching 1920×1080 captures are retained in `BridgeStone/UnityReview`. Import validation passed in `bridge-stone-import-refined.log`. An initial empty wrong-platform test run is retained separately and is not passing validation evidence.

Metre-scale import, LOD reduction and vertex-envelope checks are separate from artistic acceptance. Matching hidden in-game cameras cover the bridge, gameplay deck, underpass and underside. They do not establish native input, sound, sustained performance or Sanders' final visual acceptance. This batch is not yet a newly packaged Windows review build.
