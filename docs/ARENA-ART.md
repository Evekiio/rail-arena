# Foundry surface-art pass

The first surface-art pass adds original machined deck panels, restrained normal-map tread/grain, inset fasteners, beveled cover and pylon silhouettes, four-sided vent slats and warning markings, pylon bands, and segmented blue launch fields with directional chevrons. It follows the supplied references' industrial material layering without reproducing their textures, maps or branding. All new meshes, textures and generator code are original MIT work; no downloaded artwork is used in this pass.

## Editable sources

`RailArena/Assets/RailArena/Editor/FoundryArtBuilder.cs` generates the geometry and the three 512 x 512 panel textures. Native Unity materials, meshes, texture import settings and metadata are stored under `Assets/RailArena/Art/Foundry`. Floor UVs use a two-metre repeat independently of deck/ramp dimensions. Color, tangent-space normals and metallic/smoothness maps are separate, with linear import for normal and mask data and anisotropic filtering for grazing floor views.

Use **Rail Arena > Apply Foundry surface art** in the pinned Editor, or:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.FoundryArtBuilder.Apply();'
```

This updates the saved Foundry scene without regenerating movement tuning, spawn locations, navigation, pickup positions or pad destinations. It replaces its own named dressing root on repeat application and preserves mesh/material asset identities. The complete Foundry scene builder now calls this pass after saving the base geometry. The existing full-scene regeneration warning still applies to manual edits of generated assets.

Six precombined material groups render the added floor/pad/vent/marking/pylon details. Cover/pylon base MeshFilters use the shared beveled module while their original BoxColliders remain intact. Dressing has zero colliders; floor overlays sit four millimetres above existing top faces, and ramps follow their actual rotated top planes. This adds rendering detail rather than changing the tested movement surfaces or routes. Thin ornamental details do not provide additional gameplay cover.

## Verification and iteration

Before and after application, the generator hashes collider transforms, layers, enabled/trigger state, box dimensions, navigation nodes/links, spawns, pickups and pad positions/destinations. It refuses to save if that signature changes. `Validation/foundry-art-geometry.json` records matching signatures and zero cosmetic colliders. This signature covers the listed geometry; it is not a general proof of every gameplay setting.

Visual iteration found that `EditorUtility.CopySerialized` refreshed generated mesh CPU data without refreshing the displayed geometry consistently. A temporary unbatched cloned mesh exposed the complete vent faces, but removing static batching alone did not fix the saved asset. Mesh updates now use native vertex/normal/tangent/UV/index setters and upload the buffers while preserving GUIDs. The final fresh live capture shows the saved four-sided detailing correctly. The meshes are already combined by material, so redundant static batching is disabled.

`RailArena/Assets/Validation/foundry-surface-first-person.png` is an actual player-height Editor camera capture after live frame/tick progression, not a mockup, standalone screenshot, native-input acceptance or final creative approval. This is an initial art improvement. Backdrop, lighting depth, larger architectural forms, first-person arms, character polish, effects and sound still need work.
