# Rail Arena / design and concept review v1

The creator-approved design is consolidated in [DESIGN.md](../../../docs/DESIGN.md). This package implements the design-lock and pre-modeling handoff stages. It does **not** establish that the runtime has been migrated or that all artwork is ready for final modeling.

## Review entry points

- [Offline visual gallery](index.html)
- [Per-asset review register](ASSET-REGISTER.md)
- [Machine-readable asset register](AssetRegister.json)
- [Runtime migration and test gates](../../../docs/DESIGN-IMPLEMENTATION.md)
- [Technical validation manifest](manifest.json)
- [Generation prompts](PROMPTS.json)

The register separates existing references, new candidates, missing views and approval. No entry becomes approved merely because a file exists. Approval requires a creator decision and resolved visual/geometry discrepancies.

## Selected concept gallery

| Sheet | Coverage |
| --- | --- |
| [First-person arms](Concepts/FirstPerson-Arms.png) | Four cosmetic glove/cuff variants, palms, backs and grip studies |
| [Traversal and health](Concepts/Traversal-Health-States.png) | Pad construction, portal states and pickup availability |
| [Animation poses](Concepts/Animation-Poses.png) | Locomotion, jumping, landing, fall/fade and respawn direction |
| [Navigation and roster](Concepts/UI-Navigation.png) | Main menu, offline setup, character selection and duel room |
| [HUD and results](Concepts/UI-HUD-Settings-v2.png) | Corrected click-to-fire instructions, results and error states |
| [Settings and scoreboard](Concepts/UI-Settings-Scoreboard.png) | Controls, rebinding conflict, video/audio and clarity settings |
| [Architecture](Concepts/Architecture-Resolved.png) | Simplified modular kit and level ramp turnout |
| [Props and materials](Concepts/Props-Materials.png) | Cover/dressing separation and surface direction |
| [Hidden construction](Concepts/Hidden-Construction-v2.png) | Corrected ramp, backs and undersides |
| [Support effects](Concepts/Support-Effects-v2.png) | Corrected regeneration cap and instantaneous portal presentation |

## Shared Foundry greybox

[Editable Blender master](Greybox/Foundry-Design-v1.blend) Â· [Measured layout](Greybox/Layout.json) Â· [Labelled vector plan](Greybox/Labelled-Plan.svg)

![Shared Foundry greybox overview](Greybox/Overview.png)

| View | Purpose |
| --- | --- |
| [Top](Greybox/Top.png) | Full projected geometry and semantic markers |
| [Lower plan](Greybox/LowerPlan.png) | Lower walkable surfaces, isolated |
| [Upper plan](Greybox/UpperPlan.png) | Wing/bridge walkable surfaces, isolated |
| [North](Greybox/North.png) / [South](Greybox/South.png) | Opposing architectural elevations |
| [East](Greybox/East.png) / [West](Greybox/West.png) | Opposing architectural elevations |
| [Section](Greybox/Section-Y0.png) | Actual mesh clipping at Blender Y=0, viewed from the south |
| [Lower player view](Greybox/Player-Lower.png) | Player-height lower-route visibility |
| [Bridge player view](Greybox/Player-Bridge.png) | Elevated bridge view |
| [Wing player view](Greybox/Player-Wing.png) | Wing-to-center view |

Units are metres; Blender coordinates are Z-up. Unity conversion is (x,z,-y). Lower floor is 0m, wings 2m and bridge 4m. The four 16m ramp assemblies use 7m rise / 2m level turnout / 7m rise sections, each sloped section rising 2m. Markers indicate four pad triggers plus four distinct landing areas, three pickups, eight candidate spawns and two portal apertures. Marker boxes are not finished models. Purple portals and pink spawn markers are review colors, not final materials.

The master is a new proposal rather than an export of the existing playable arena. Controller dimensions, spawn exposure, pad trajectories, portal clearance and bot routes still need actual-game tests. A passing structural audit proves counts, bounds and paired marker placement only. It does not prove walkability or competitive balance. The lower/upper plans isolate horizontal floor objects; use full plan and elevations for ramps.

## Concept coverage

New sheets cover first-person gloves/cuffs, traversal and health states, animation poses, navigation/roster/duel screens, HUD/results/error screens, settings/scoreboard, simplified architecture, environment props/materials and support effects. Selected existing character/world sheets remain linked in the register. Full source prompts and edits are retained.

The supplied [rifle appearance reference](Sources/User-Rifle-Reference.png) is retained as a user-provided visual source. It is not an original project model or approval of unseen views. The required new rifle multi-view sheet and combat/blood effect sheet remain blocked after prior image-service filtering. No alternate service or substitute asset is represented as completing them.

## Corrections and limits to inspect

- Original character sheets and fixed-feature modeling guide govern appearance. The animation sheet's amber chest inset and the UI mockups' lighter player hair/new logo treatments are illustrative errors; do not replace the approved player identity or existing brand logo with them.
- The props material swatch labelled SKIN is teal paint, not a valid skin-material target. Use skin references from the approved characters; preserve separate skin/cloth/paint/metal behavior.
- UI-Navigation is a layout study, not actual selected portraits. Timed matches require at least one bot; zero bots belongs to practice. Host/join/ready mockups must distinguish connection and ready controls in the actual interface.
- Superseded Support-Effects and UI-HUD-Settings sheets are retained for provenance. Prefer their v2 edits where present. The initial sheets incorrectly implied full regeneration and held firing. DESIGN.md governs exact rules.
- Support-effects timing is illustrative. Portal travel commits immediately; there is no tunnel delay. Regeneration starts only after eight damage-free seconds, tops out at 50/100 and must never expose opponent health. UI fill lengths in concept artwork are not numerical authority.
- Portal art needs a final side/back consistency pass: the gameplay contract is a bidirectional connection. Aperture blocking and retrigger behavior require controller validation.
- Animation overview is not a complete rig/clip package. Rifle-dependent hold/fire/recovery and first-person contact poses remain blocked until rifle geometry/reference approval. Falling/respawn art must preserve the original costume.
- Concept views are not perfectly calibrated projections of a shared final asset mesh. Only the greybox views share a measured master. Final sheets must resolve the register's required views before an asset can pass its gate.

## Reproduce headlessly

From the repository root:

```powershell
.\tools\Build-DesignGreybox.ps1
python tools/render_design_plan.py
python tools/build_design_register.py
python tools/validate_design_package.py
```

The Blender runner uses four CPU threads, a bounded timeout and owned-process cleanup, with logs under Validation. It writes only this review package. It never launches the game, edits Unity scenes or opens a visible window. Image generation uses the built-in image_gen tool; the Python scripts do not call an image API.

## Current gate

Design approved. Greybox and art are review candidates. Missing or contradictory references remain blocked/revise. Detailed modeling and runtime integration are subsequent stages of the approved sequence. This package is published for design review. Publication does not imply asset approval, runtime migration or release qualification.
