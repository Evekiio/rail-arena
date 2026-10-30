# Expanded Twin Platforms

Each deck is now 44 by 56 metres (2,464 square metres), previously 32 by 36 (1,152). Both expand outwards and along the long axis, preserving the 36-metre gap and all portal, launch-pad, landing and health-pickup positions. Existing movement, weapon tuning and traversal physics are unchanged.

Fourteen new cover pieces supplement the eight existing ones: eight full-height armor blocks and six low barriers. New outer flanking lanes connect across both ends of each platform and back to the original paths. Eighteen new route nodes extend the connected bot graph to 42. Portal approaches and pad landing areas remain open.

Original deterministic panel albedo and normal textures add metal joints, fasteners and restrained wear. Ceramic caps, recessed team-colored strips, perimeter guidance and dark lane surfaces improve orientation. Warmer directional sunlight, balanced ambient fill and four unshadowed deck luminaires improve depth and outer-route visibility. No new external texture dependencies were introduced.

The editable upgraded scene remains `RailArena/Assets/RailArena/Generated/TwinPlatforms.unity`; the original Foundry scene and previous builds are preserved. `ExpandedPlatformsBuilder.Apply` upgrades the prior Twin Platforms scene once and refuses to duplicate an existing expansion. Run it on an unexpanded copy if regenerating; edit the saved upgraded scene for subsequent adjustments.

Build: `Builds/ExpandedPlatforms-2026-10-29/RailArena.exe`. Keep the executable beside its runtime/data files. Test both outer flanks, cover heights while crouched/standing, both portals, both pads and health respawn. Automated navigation/collision checks and captures do not establish human combat balance, movement comfort or performance on other PCs.
