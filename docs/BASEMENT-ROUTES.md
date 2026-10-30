# Sky Gardens basement route candidate

Four identical diagonal routes descend from the +22 m summit to +14 m staging decks, then to basement decks at -10 m, the midpoint of the waterfalls (0 to -20 m). Existing scenery and the original scene remain preserved. The new scene is SkyGardensBasement.unity.

Staging centers are 21 m from the summit center with a 3 m radius; basement centers are 45 m out with a 6 m radius. Use a forward sprint jump from the marked summit sector and another from the outside edge of staging. With standard 8 m/s movement, 1.35 sprint multiplier, 8.4375 m/s jump and 25 m/s² gravity, actual controller samples land after 72 and 106 ticks. Landing clearance beyond the capsule is approximately 1 m and 3 m. Normal 8 m/s attempts miss both gaps; momentum boosting and custom Physics Lab settings can alter that distinction.

Two opposite basement platforms bounce to the middle bridge. The other two have two-way portals to opposite walls beneath the center bridge. Portal clearance, opposing orientation and safe support are checked by the existing traversal code. The void limit remains -20 m; no fall damage was added. The new decks have no health or spawn advantage.

This is a competitive layout candidate requiring Sanders' approval, human multiplayer timing/readability review and performance validation. Existing bots retain their original navigation graph; the new descent routes are currently for human traversal. Added decks use realtime/ambient lighting and are not included in the earlier light bake.

Validation: basement-route-offset-tests.xml passed all four routes with -0.5/0/+0.5 m lateral takeoffs, normal-speed miss controls, two return bounces and two portal exits. The local player passed all 39 standalone mechanics checks with zero logged errors (basement-route-player-mechanics.json). Local build: Builds/SkyGardens-BasementRoutes-2026-10-30/RailArena.exe. Native input, sound and visual acceptance remain open.
