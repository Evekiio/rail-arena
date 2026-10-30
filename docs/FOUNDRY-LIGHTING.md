# Foundry daylight and cloud world

The current source replaces the original space-backdrop presentation with a bright, static cloud-world treatment on the existing playable arena. The public `v0.1.0-pre-alpha.1` download remains unchanged.

[Side-by-side visual review](../Validation/FoundryLighting/index.html) | [World direction reference](../ArtSource/Original/Foundry-Arena-v2/References-v1/Concepts/07-World-Lighting.png)

An original procedural sky shader supplies a blue-to-bright horizon, layered faceted cloud banks and a restrained sun aligned with the directional light. It contains no scrolling texture or animated weather. Warm neutral sunlight, cool sky fill and darker ground fill replace the old blue flat ambient treatment. Reflection strength is reduced to limit distracting glare. Existing tone mapping, bloom and vignette are retained.

This is a presentation update to the older playable Foundry. It does not install the 56 x 40m v2 map, portals or new production-review environment meshes. Distant islands, industrial landmarks, final sky art, material refinement and model acceptance remain separate work.

## Reproduce

Run pinned Unity 6000.6.7f1 headlessly through `unity run RailArena --editor-path <pinned-editor> --timeout 240 -- -job-worker-count 4 -executeMethod RailArena.Editor.FoundryLightingBuilder.ApplyAndReview`. Run `python tools/package_foundry_lighting.py` afterward. The editor command saves the lighting in the existing scene and produces matching-camera renders. Use `FoundryLightingBuilder.Apply` to reapply without overwriting the before images. The full arena builder also reapplies this lighting, so regenerating the saved scene does not restore the old backdrop.

The old star-field mesh remains recoverable as an inactive, collider-free scene object. No new gameplay geometry or fog is introduced. The authoring operation compares the existing collider/route/spawn/pad/pickup signature before and after and fails on a mismatch. [Geometry receipt](../Validation/FoundryLighting/geometry.json).

## Qualification

The pinned Unity suite passes **99/99 tests**, including daylight persistence through practice, pause and menu, shader support, sky/sun alignment and a non-black live camera render. [Test report](../Validation/FoundryLighting/tests.xml). The Windows x64 development build succeeds and its source compatibility identity is verified. [Build receipt](../Validation/FoundryLighting/build.json).

The built player passes [mechanics validation](../Validation/FoundryLighting/Standalone/mechanics.json) and [45 seconds of three-bot offline combat](../Validation/FoundryLighting/Standalone/offline.json). Those hidden, no-graphics checks verify operation of the built client, not its rendering performance.

The review gallery contains controlled Unity renders; screen-space HUD is not part of those captures. These checks do not certify physical-input feel, visual acceptance across displays or the 1080p/144fps performance target.

## Volume persistence repair

The shared Foundry grading profile now persists its ACES, bloom and vignette components as Unity sub-assets. Previously the serialized component references were null, so these effects could disappear after reopening the project. The scene builder now persists components when recreating the profile. A headless reopen and serialization check passed; visual appearance and performance still need a player review.

The headless PC rendering configuration check selects the PC quality level and verifies the URP asset, Linear color space, HDR, depth/opaque textures, 2048 main-light shadows, four cascades and soft-shadow support. This is a configuration check, not a rendered shadow-quality or performance measurement.
