# Pad directions and landing guides

The current arena's pad chevrons now point along each pad's configured destination vector. Previously, the left/right bar rotations made the V's tip face backward even though the launch itself went forward. Swapping those rotations corrects all four pads without changing traversal physics.

Two shared destinations on the existing elevated wings now have muted, non-emissive corner brackets. The brackets sit just above the actual load-bearing deck, excluding raised seam geometry from the placement ray. They identify the nominal landing centre; air control can still move a player away from it. They add no collision, trigger, light, automatic steering or new interaction.

The current map has four pads feeding two shared landing centres. Pad positions, destinations, 1.25-second flight parameter, launch velocities, cooldown/retrigger logic, map collision and navigation remain unchanged. The separate Foundry v2 map and device models remain under review.

## Evidence

[Review gallery](../Validation/TraversalCues/index.html) contains an overhead view of the corrected pad and matching before/after landing views. These are actual headless Unity camera renders. The archived before landing image was captured from the pre-change `8150907` source checkpoint; rerunning art authoring on an already-updated scene naturally produces the current guides in both states.

Tests inspect the generated launch-field mesh in each pad's destination coordinate frame: the forward ends must converge at the arrow spine. They also check all landing-guide vertices against the deck, non-emissive material, collision absence, four pads/two destinations and the unchanged ballistic endpoint. Before/after gameplay signatures match.

The full pinned Unity suite and Windows build are followed by hidden-client mechanics, four-contender combat and loopback-duel regressions. Human route/readability acceptance, external-network qualification and measured 1080p/144fps performance remain open. The public pre-alpha ZIP remains its earlier checkpoint.
