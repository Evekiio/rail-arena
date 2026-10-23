# Rail Arena environment reference pack v1

Generated 2026-10-23 with the built-in image_gen tool. Six concept sheets for creator review and subsequent Blender work. No runtime assets, scene geometry or gameplay rules were changed. No visible Blender or Unity windows were opened.

## Gallery

| Sheet | Purpose |
| --- | --- |
| [Arena hero](01-Arena-Hero.png) | Floating Foundry architecture and player-height atmosphere |
| [Arena layout](02-Arena-Layout.png) | Proposed circulation, deck levels, cover and section studies |
| [Modular architecture](03-Modular-Architecture.png) | Eight structural module families and view studies |
| [Environment props](04-Environment-Props.png) | Cover, cabinets, vents, conduits, lights and health pedestal |
| [Traversal devices](07-Traversal-Devices.png) | Jump pad views, parts, states and optional portal concept |
| [World and sky](08-World-Sky.png) | Floating industrial islands, atmosphere and directional studies |

![Arena concept](01-Arena-Hero.png)

![Layout proposal](02-Arena-Layout.png)

![Architecture kit](03-Modular-Architecture.png)

![Environment props](04-Environment-Props.png)

![Traversal devices](07-Traversal-Devices.png)

![World and sky](08-World-Sky.png)

## Modeling authority and known inconsistencies

These generated images are not calibrated projections of one shared mesh. The written contract below takes precedence over image annotations and apparent dimensions. The design is a new proposal, not a measured export of the current Foundry scene. It is not a proven competitive map.

- The arena hero depicts more cyan circular features than the four gameplay pads requested. Only four should become launch triggers. Other circles should be removed or visually differentiated as cosmetic fixtures.
- Hero and plan differ in their bridges, openings, ramps and covered lower routes. Do not trace both into one mesh. Resolve the circulation in a single blockout before producing final orthographic references from that master.
- Layout health icons are green, while prop concepts use amber. Amber is the proposed art direction for health; choose one consistent runtime cue during review.
- Some plan labels are duplicated or misplaced. Written spawn/pickup counts govern. Spawn positions and fairness are not verified by the image.
- Plan upper/lower deck separation and section do not establish continuous navigable geometry. Reconcile all transitions in the blockout.
- The architecture kit's 4m ramp rises 1.6m, which does not divide the proposed 2m deck steps. Adjust the ramp or use longer runs after movement tests. Printed dimensions are proposals, not audited measurements.
- Jump pad concept is 0.35m tall; recess its body so the walkable surface is flush. Preserve launch volumes and destinations separately from decorative geometry.
- Health pedestal is grouped beneath dressing in the sheet, but its pickup marker is a gameplay feature. Separate pedestal art, pickup trigger and active/inactive visual state.
- Portal is an optional visual proposal. Teleportation is not implemented or added to the gameplay scope by this pack. Its base and aperture need player-clearance testing if adopted.
- Sky directions share a mood but are not seam-matched. This is not a usable cubemap, HDRI or seamless sky texture. Author one shared world/lighting scene and render actual cubemap faces later.
- Multi-view props may contain part/count discrepancies. Resolve them in one editable mesh and render true orthographic views before detailed production.

## Proposed blockout contract

This is a candidate for creator review, not a change to the existing game specification.

| Property | Target |
| --- | --- |
| Envelope | 56m east-west by 40m north-south, adjustable after movement tests |
| Structure | Lower circulation at 0m, opposing wings at +2m, central bridge at +4m |
| Balance approach | 180-degree rotationally paired primary routes and resources |
| Routes | Two alternatives between major combat spaces; no forced single choke or isolated dead end |
| Ramps | Four broad connections; slope, width and landing clearance tested with actual controller |
| Jump pads | Exactly four, paired destinations with broad landings and predictable arcs |
| Health | Three positions: paired wing pickups and one contested central pickup |
| Spawns | Eight candidates behind offset sight blockers; evaluate every spawn against all others |
| Cover | Two central sight blockers plus sparse deliberate cover, avoiding all-map dominant perches |
| Portal | Excluded from baseline blockout until gameplay scope is explicitly changed |

The low cover and cabinet are candidate gameplay cover. Small vents, conduits and lights are cosmetic. Keep snagging detail outside collision surfaces. Preserve existing gameplay values: rail-only combat, 1.5-second cycle, unlimited ammunition, 100 health, 25-health pickups and existing movement rules. Do not infer balance changes from art.

## Blender handoff

1. Review the visual direction and resolve the layout contradictions above.
2. Build one metre-scale greybox master with named floors, ramps, hard cover, spawns, pickups, pad triggers and destinations. Keep art collections separate from gameplay geometry.
3. Export measured top, bottom, front, back, side and section renders from that shared master headlessly. Use these to replace speculative plan geometry.
4. Playtest routes, spawn exposure, pad landings, void deaths, bot traversal and height advantage using the actual game controller. Measure travel times and repeated-match outcomes; adjust before art lock.
5. Model the modular kit against the accepted blockout. Preserve collision and navigation when applying cosmetic dressing.
6. Check target silhouettes at player height across all character palettes; reduce emissive bloom and background contrast where they hide opponents.

Suggested construction: 4m structural bays with 2m/1m subdivisions, restrained bevels, ivory shell over graphite frames, teal recesses, copper fasteners and cyan traversal energy. These are starting dimensions, not validated budgets or final collision settings.

## Weapon and effects coverage

The new rifle reference request was rejected by the image service's violence filter (input stage). The combined rail/impact/non-graphic blood effects sheet was rejected by the same filter (output stage). Neither sheet was delivered; the missing numbers 05 and 06 are intentional. Full intended prompts are retained in [PROMPTS.json](PROMPTS.json). No alternate generation service was used.

The previously published [rifle studio render](../Railgun/Railgun-Studio.png) remains available. Additional local side/rear refinement renders are outside this publication. Existing weapon renders do not represent a newly approved rifle redesign. See [weapon source notes](../../../docs/WEAPONS.md) and [existing effects implementation](../../../docs/COMBAT-FEEDBACK.md).

Unresolved VFX art brief: short narrow white/cyan rail core and translucent halo, compact muzzle pulse, surface sparks and small fading impact marks; restrained non-graphic crimson hit droplets without wounds, dismemberment or persistent obscuring clouds. Effects remain cosmetic and must not obscure aim or imply different firing mechanics. No effect textures, particle systems, sprite atlases or blood assets were produced here.

## Review status

All six images are review candidates. Technical file integrity is recorded in manifest.json; technical validation does not establish visual approval, geometric accuracy, competitive balance or completed modeling. This pack is published as concept documentation and linked from the project README. Publication does not imply creator approval or runtime integration.
