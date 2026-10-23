# Amber health beacons

The current arena's three health sites now use a shared low-poly beacon: four outward-facing amber plus panels above an ivory/graphite pedestal. The collectible symbol and amber pedestal segments disappear/dim when collected, leaving the mount visible so the site remains recognizable. They return together at the existing availability tick.

The plus shape identifies health without relying on color alone. It stays static, with restrained emission and no bob, strobe, timer overlay or through-wall marker. The pedestal and symbol have no colliders and cast no gameplay-obscuring shadows.

## Construction

The editable source is `HealthPickupArtBuilder.cs`, with shared mesh/material assets under `Assets/RailArena/Art/HealthBeacon` and one reusable `Resources/Environment/HealthBeacon.prefab`. It reuses the existing faceted-box authoring function. Rebuilds update mesh buffers in place to retain asset identities, and full arena regeneration reapplies the beacon.

The pedestal footprint is 0.8 × 0.8m with a 0.25m structural stack. The four-face marker occupies a 0.68m cube; its broad amber panels prioritize readability around the current arena. The assembly has three renderers, three shared materials and 912 triangles. The surfaces use flat material separation and generated UVs, with no photographic textures or runtime material cloning. These counts are authoring evidence, not a frame-rate qualification.

The [health concept sheet](../ArtSource/Original/Foundry-Arena-v2/References-v1/Concepts/05-Health-Pickup.png) establishes amber identification, broad faceted forms and visible collected/available states. The [side-by-side gallery](../Validation/HealthBeacon/index.html) shows the current retrofit beside that direction. This version uses four readable panels and a larger marker rather than reproducing the sheet's single 0.35 × 0.5m plus exactly. The full Foundry v2 model package remains under review.

## Gameplay and validation

All three pickup root positions, collection range, +25 capped restoration, 1200-tick return, navigation, spawn points and map collision remain unchanged. `HealthPickup.Visuals` retains its collectible-renderer binding for existing validation code. The pedestal state follows the same `SetAvailable` calls used by offline authority and received duel snapshots; no network fields or gameplay timing are added.

The geometry receipt records matching before/after gameplay signatures. Tests check finite mesh data, nonzero triangles, normals/UVs, bounded triangle count, reusable materials, collision absence, normal collection/return timing and authoritative/stale snapshot behavior. Gallery images are actual headless Unity camera renders of the authored asset and one current arena site. Human readability across viewing distances and the 1080p/144fps target still require playtesting. The public pre-alpha ZIP is unchanged by this source update.
