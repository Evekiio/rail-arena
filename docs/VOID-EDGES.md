# Foundry void-edge warnings

The current arena adds muted ochre hatch groups just inside exposed flat-deck edges. They distinguish the open outer rim and central gap from ordinary deck seams. Existing cyan ramp guidance stays unchanged; this pass does not add ramp-edge geometry, barriers or collision.

The authoring builder checks three points beyond each candidate deck-edge group. A downward query looks for a load-bearing deck or access ramp within 32 metres. Groups with a landing surface are skipped, including connected deck seams and upper-wing edges above lower floors/ramps. Markings are a visual warning, not a guarantee that every unmarked movement trajectory is safe.

## Construction

`VoidEdgeArtBuilder.Apply` stamps three small diagonal hatches per accepted group, 18mm above the deck face to clear the existing surface/seam layers. The current saved arena produces 240 groups / 720 hatches / 1,440 triangles, combined into one mesh and one unlit matte-color material. No emission, textures, animation or extra runtime scripts are used.

The root uses Ignore Raycast, has no collider or lights and casts/receives no shadows. Its repeated generation replaces inactive roots and updates the saved mesh in place. The scene builder reapplies the paint after existing surface/lighting/backdrop work. Gameplay signature checks preserve collision, navigation, spawn positions, pickups and pad destinations.

These cues apply to the current arena, not the proposed Foundry v2 map. The original void-death threshold, score penalties, movement and respawn behavior are unchanged.

## Review and validation

[Matching-camera review](../Validation/VoidEdges/index.html) includes the whole arena, outer corner and central opening before/after. These are actual headless Unity scene renders at 1920 x 1080, not concept images or human playtest captures.

The scoped VoidEdgeTests fixture checks deterministic inactive-root replacement, unchanged gameplay signature, floor attachment, finite/upward-facing geometry, collision/light/script absence and known open-gap versus connected/supported-edge cases. A pinned Unity 6000.6.7f1 Windows development build verifies integration.

This is a readability aid, not an invisible wall or guaranteed fall prevention. Human movement/readability review and four-contender performance qualification remain open. No unrelated full suite or broad combat soak was run. The public pre-alpha ZIP is unchanged.
