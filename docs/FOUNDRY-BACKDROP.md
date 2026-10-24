# Foundry distant-world backdrop

The current playable Foundry now has six original low-poly floating islands and industrial tower silhouettes. This develops the [world/sky reference](../ArtSource/Original/ArenaReferences-v1/08-World-Sky.png) into restrained distant scenery while retaining the current cloud sky and lighting.

The islands use faceted stone, ivory caps and muted teal structures. Their centers sit 180 or 225 metres from the arena origin, below the deck, with taller structures breaking the horizon. Every generated vertex stays more than 140 metres horizontally from the origin. This is background context, not a destination or additional playable level.

## Construction

`FoundryBackdropBuilder.Apply` creates three saved combined meshes and three shared materials: 2,544 triangles across the six island assemblies. The saved scene includes them, and the Foundry scene builder reapplies them after surface/lighting authoring. Repeated application replaces the old root, including an inactive one, rather than accumulating hidden scenery.

The backdrop contains no colliders, lights, gameplay scripts or animation. It uses the Ignore Raycast layer, disables cast/receive shadows and does not request reflection/light probes. Collision geometry, navigation nodes/links, spawns, pickups and jump-pad destinations retain the same gameplay signature before and after authoring. No fog, light, sky shader or combat rule changes are included.

The parameterized Unity authoring source and saved mesh/material assets remain editable. This pass uses flat material zones rather than new photographic textures. It does not install Foundry v2, its portal pair, or the full proposed modular environment kit.

## Review and validation

[Matching-camera gallery](../Validation/FoundryBackdrop/index.html) includes before/after overview, lower route, upper horizon and outward edge views. These are actual Unity scene renders captured headlessly at 1920 x 1080, not concept paintings or human gameplay recordings. The gallery also links the world concept for style comparison; the concept is not a dimensional authority for this scenery.

The scoped backdrop fixture checks deterministic regeneration, inactive-root replacement, finite/nondegenerate geometry, distance bounds, shader availability, collider/light/script absence and unchanged gameplay signature. A pinned Unity 6000.6.7f1 Windows development build validates player integration.

This inventory is not measured rendering performance. Human combat readability, final art approval, device coverage and documented four-contender 1080p/144 fps qualification remain open. No unrelated full test suite or broad combat soak was run. The public pre-alpha ZIP remains unchanged.
