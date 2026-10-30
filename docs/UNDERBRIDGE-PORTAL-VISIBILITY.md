# Under-bridge portal visibility finding

This is a separately recorded map-placement issue discovered during portal performance work. It is not a repaired issue or evidence that cropping causes occlusion. The existing ADS/portal projection diagnostics remain preserved separately.

## Observed evidence

`PortalCropRenderingReview` captures the full-frame reference with the original map visible before isolating the colour-chart alignment check. At both under-bridge portals, the normal and wide ADS poses expose zero chart pixels in the original scene. A ray from the observer to the aperture hits `Underpass-Pier-1` or `Underpass-Pier1`. With the map layers excluded, the same portal/camera/exit/chart transforms expose more than 4,000 chart pixels, and the full-frame/cropped images agree within the recorded tolerance.

Evidence: [alignment and world-occlusion record](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/alignment-review.json), [portal 1 world view](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/Under-bridge-portal-1-world.png), [portal 3 world view](../ArtSource/Original/ArtPass-v1/Performance/PortalCrop/Under-bridge-portal-3-world.png).

The tested under-bridge observer stands two metres in front of the gate at 1.7 m height. This establishes obstruction for those poses. It does not alone prove the complete visible aperture or both traversal directions are blocked at every approach.

## Required follow-up

Measure the pier collision/render bounds, aperture plane, frame and standing-controller clearance in the actual scene. Verify travel in both directions before changing any placement. If adjustment is needed, calculate the smallest placement or assembly correction that exposes the aperture while retaining the intended under-bridge return route and safe exit support. Capture normal gameplay views and run the affected route checks once after the correction.

Do not weaken image checks or treat an opaque wall as a valid portal-alignment reference. The isolated chart validates projection/sampling only; native visibility and map acceptance remain separate requirements.
