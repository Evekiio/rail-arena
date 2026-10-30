# Sky Gardens / approved Foundry v2 playtest

The separate `SkyGardens.unity` scene implements the measured Foundry v2 lower loop, underpass, four ramp/wing connections and Crown, with the approved high summit. Four pads launch from the Foundry positions to four marked +22 m summit landings; ordinary ramp access to the wings is preserved. Open summit edges allow controlled drops back to the Crown or underpass. There is no new fall damage.

The map uses the approved detailed blueprint meshes with world-scaled stone albedo/normal textures, ivory/bronze/teal materials, warm sunlight, ambient fill, a cloud sea and distant non-colliding ruins. Foliage has restrained cosmetic wind; waterfall rest meshes use a flowing two-sided water/foam shader. Offline pause freezes these animations and Reduced Flash slows the water. These are textured blockout materials, not a claim of final production art quality.

The existing rifle (250 RPM, 30-round magazine, reload), ADS, movement, crouch, slide, live portal views, traversal sounds and health animations remain. Three health pickups use Foundry v2 positions. The narrow concept portals use a capsule-front crossing offset so their solid backing cannot stop the controller before teleportation. Both fields still block bullets, verify full exits, preserve velocity and prevent immediate re-entry. Previous Twin Platforms and original Foundry scenes/builds are preserved.

Physics import retains the original collision volumes. Canonical-to-Unity axis mapping already preserves mesh orientation; extra winding reversal was removed after physics ray checks exposed downward floors. Standing-capsule support/clearance checks produce 444 navigation nodes. Sky Gardens bots use tighter arrival tolerance and reduce input near unsupported edges; marked summit returns use slow approach input. These AI changes are enabled only for the new map.

Press G for the offline Physics Lab. See [Physics Lab](PHYSICS-LAB.md). Default movement remains unchanged; enhanced physics here means correctly oriented collisions, climbable ramps, supported routes, clear landing areas and safe configuration bounds.

Local player: `Builds/SkyGardens-PhysicsLab-2026-10-29/RailArena.exe`. Keep the executable beside all data/runtime files. Test all four launch pads, summit return drops, both portals, health collection, crouch/slide around cover, and G / save configuration. Human balance, input comfort, sound-on review, native PNG output and real multiplayer acceptance remain playtest tasks.

## Material audit

The headless material audit inspects renderers in Sky Gardens and the instantiated weapon prefab. Its output is `Validation/SkyGardens-Playable-2026-10-29/pbr-audit.json`. The active Precision Carbine surface and energy materials use URP Lit with base-color, normal and metallic/smoothness maps; neither currently has assigned AO, height or detail-normal maps. Sky Gardens stone materials use base-color and normal maps but lack assigned metallic/smoothness, AO, height and detail-normal maps. Water/foam use a custom shader, so the standard Lit map names do not establish their capabilities. Presence of a texture is not proof of visual quality or correct UVs.

Sky Gardens stone now uses a seamless procedural micro-normal in the URP Lit detail input. Generated stone meshes have tangents; the base color and collision geometry are unchanged. `SkyStoneDetailBuilder.Apply` upgrades a freshly generated scene once. Headless shader/traversal checks passed; close-range appearance and GPU cost still need native-player evaluation.

## Lightmapping baseline

The headless scene audit reports 346 mesh filters, 101 with a complete UV2 array, zero marked Contribute GI, four realtime lights and no assigned Lighting Settings asset. A UV2 count alone does not certify non-overlapping lightmap unwraps. The local GPU is an RTX 2080 (8 GiB); actual Progressive GPU bake support and a successful bake remain unverified. Static geometry selection, unwrap validation, light/probe configuration and a measured bake are still required.
