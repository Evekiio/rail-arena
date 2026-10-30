# Under-bridge portal visibility and reverse traversal correction

This is a separately recorded map-placement issue discovered during portal performance work. The source correction is now verified by focused geometry, traversal and render checks. The original finding is not evidence that cropping caused occlusion. The existing ADS/portal projection diagnostics remain preserved separately.

## Observed evidence

`PortalCropRenderingReview` captures the full-frame reference with the original map visible before isolating the colour-chart alignment check. At both under-bridge portals, the normal and wide ADS poses expose zero chart pixels in the original scene. A ray from the observer to the aperture hits `Underpass-Pier-1` or `Underpass-Pier1`. With the map layers excluded, the same portal/camera/exit/chart transforms expose more than 4,000 chart pixels, and the full-frame/cropped images agree within the recorded tolerance.

Evidence: [alignment and world-occlusion record](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/alignment-review.json), [portal 1 world view](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/Under-bridge-portal-1-world.png), [portal 3 world view](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/Under-bridge-portal-3-world.png).

The tested under-bridge observer stands two metres in front of the gate at 1.7 m height. This establishes obstruction for those poses. It does not alone prove the complete visible aperture or both traversal directions are blocked at every approach.

## Root cause and correction

The audit records each pier's inner face at X = +/-3.0 m, and the portal root at +/-3.1 m. The 6 cm field is consequently embedded in stone. The entry plane was only 30 cm ahead of the wall, while the standing radius plus default skin is 44 cm. The baseline real-controller test reproduced reverse traversal failure: the standing actor stopped at X = -2.56 m without crossing the portal plane. Original basement-to-arena coverage did not exercise this approach.

`UnderbridgePortalArtBuilder.Mount` derives the outward offset from actual pier/field bounds, the shared 0.36 m standing radius, default 0.08 m skin, unchanged 0.4 m entry offset and a 3 cm minimum margin. The result is 17 cm per gate: roots at +/-2.93 m, entry-plane clearance 47 cm and field rear clearance 4 cm. The capsule has 3 cm beyond its default radius/skin envelope. Original back collision remains behind the entry plane. Only the two saved root transforms change in the scene; aperture dimensions, linked routes, movement, cooldown, trajectory and network protocol remain unchanged.

The route generator applies the same authoring function, and repeated application is verified idempotent. `ArenaActor.StandingRadius` replaces the identical actor-creation literal to prevent authoring/controller drift. Physics Lab can still alter skin width; extreme custom values need their own clearance review.

## Verification

- Original bounds: `Validation/underbridge-original-bounds.json`.
- Baseline failure: `Validation/underbridge-before-graphics.xml`.
- Corrected review: `Validation/underbridge-mounted.xml`, three tests passed. This covers 24 standing/crouched/lateral crossings across both pairs and both directions, floor support, exit capsule clearance and 36 unobstructed eye/strafe/aperture rays. Existing four-route sprint/drop and return-device coverage remains green.
- Twelve full/cropped projection cases remain within tolerance. All four under-bridge normal/wide-ADS full-world cases expose 4,465-4,928 chart pixels with no ray occluder. Earlier isolated-only results remain preserved.
- `Validation/underbridge-destination-final.xml`: two tests passed, checking idempotence and producing four normal/ADS destination views without charts.
- [Before/after gallery and placement reference](../ArtSource/Original/ArtPass-v1/Performance/UnderbridgeMounted/index.html).

The initial graphics-disabled play-mode attempt crashed Unity's null graphics backend in `Camera.SubmitRenderRequest` through the live portal renderer; it produced no valid test result. Graphics-enabled hidden batch mode ran the scene successfully. Live portal validation needs graphics; source import, scene authoring and builds remain headless.

These checks establish sampled scene visibility and real-controller paths, not every custom Physics Lab configuration or native HUD/input/audio/artistic acceptance. Older deferred ADS/projection diagnostics remain untouched.
