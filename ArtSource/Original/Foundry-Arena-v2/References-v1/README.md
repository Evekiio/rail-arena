# Rail Arena / Foundry full environment references

Review package for the duel-first 56 × 40m arena. **Eight selected art sheets, twenty-two matching Blender renders, exact cover-envelope studies, editable master, measured placement data and reproducible tools.** This is a review handoff, not approved final art, detailed production models or a playtested competitive map.

[Open the offline visual gallery](index.html) · [Arena specification](../../../../docs/ARENA-DESIGN.md) · [Placement authority](../Map.json) · [Environment inventory](../Environment-Kit.json) · [Validation manifest](manifest.json)

## Which source governs what?

1. Creator decisions and the arena specification govern gameplay intent.
2. Map.json governs dimensions, routes, cover, spawns, resources, pads, portals and drops.
3. The measured Blender views show that same data from one master. Their neutral colors are review semantics, not final materials.
4. Concept sheets propose surface construction, silhouette refinements, material treatment and effects. They do not authorize geometry changes or additional playable structures.

If artwork and the measured model disagree, preserve the measured gameplay volume and record the required art revision. Do not average the drawings into another layout.

## Selected visual sheets

| Sheet | Modeling / visual coverage |
| --- | --- |
| [Structural kit](Concepts/01-Structural-Kit.png) | Floor bays, closure, wall skin, edge cap, support beam, landing, balcony and 6.5/3/6.5m ramp profile |
| [Cover kit](Concepts/02-Cover-Kit-v2.png) | Nine functional cover cards with corrected opaque Underpass pier; eight reusable size variants |
| [Jump pad](Concepts/03-Jump-Pad.png) | Six directions, parts, recessed mounting, trigger/landing diagrams and idle/contact/launch/fade |
| [Portal](Concepts/04-Portal.png) | Six directions, solid back, opaque field, front entry, free exit, parts and blocked/arrival/departure states |
| [Health pickup](Concepts/05-Health-Pickup.png) | Six directions, marker/mount separation, materials, available/collected/returning states |
| [Dressing and landmarks](Concepts/06-Dressing-Landmarks.png) | Recessed vent, conduit and light; West/East distant silhouettes; North/South identifying headers |
| [World and lighting](Concepts/07-World-Lighting.png) | Cloud-world mood, directional studies, southwest sun and material spheres |
| [Arena art overview](Concepts/08-Arena-Art-Overview.png) | Art-over using the measured overview as its geometry/camera reference |

All eight are creator-review candidates. The original open-pier cover sheet is retained as superseded provenance; use v2. Full prompts, reference roles, edit prompt and generation provenance are in PROMPTS.json. Raster sheets use built-in image_gen; measured renders use headless Blender 5.2.2.

## Same-master measured references

[Editable Blender master](Foundry-v2-Reference-Master.blend) · [Camera/source bounds](Measured-Views.json) · [Six-view cover envelopes](Measured/Cover-Envelopes.svg)

| Reference | Files |
| --- | --- |
| Opposing overview | [Southwest](Measured/Overview-SW.png), [Northeast](Measured/Overview-NE.png) |
| Whole-map poles | [Top](Measured/Top.png), [Bottom](Measured/Bottom.png) |
| Cardinal elevations | [North](Measured/North.png), [South](Measured/South.png), [East](Measured/East.png), [West](Measured/West.png) |
| Isolated floor decks | [Lower](Measured/Lower-Plan.png), [Wings](Measured/Wing-Plan.png), [Upper](Measured/Upper-Plan.png) |
| Cutaway sections | [North/south at x=0](Measured/Section-NS-X0.png), [East/west at n=8](Measured/Section-EW-N8.png) |
| Crown / Underpass | [Crown player view](Measured/Player-Crown.png), [Underpass player view](Measured/Player-Underpass.png) |
| Both wings | [West](Measured/Player-West-Wing.png), [East](Measured/Player-East-Wing.png) |
| Loop and gates | [Outer Loop](Measured/Player-Outer-Loop.png), [North Gate](Measured/Player-North-Gate.png), [South Gate](Measured/Player-South-Gate.png) |
| Spawn-pocket inspection | [Corner S1](Measured/Player-Spawn-S1.png), [Inner S5](Measured/Player-Spawn-S5.png) |

![Measured overview](Measured/Overview-SW.png)

Meters, Blender Z-up; Unity conversion (x,h,-n). Lower floor 0m, wings +2m, upper bridge +4m. Camera eyes are 1.58m above the relevant deck. Orthographic views preserve parallel projection; player views are perspective. Sections are clipped half-model cutaways, not thin-slice drawings. Use the numeric vector sections in the parent folder for exact section-plane profiles. Isolated deck views hide cover, ramps and markers to make the floor footprint readable.

Review colors: graphite cover, dark teal lower structure, pale teal wings, ivory upper structure; cyan pad starts AND landing squares, amber health, purple portal housing, pink spawns and green safe-drop lips. There are four pads and four landing squares, not eight launch devices. Device markers are envelopes, not finished meshes.

The master retains all thirty original cover source boxes in hidden CoverSourceBoxes. A separate exact Boolean union renders their combined volume without coplanar seams. The source boxes remain independently editable. Do not delete a screen because it visually merges into another: each source ID traces to the map.

## Modeling constraints and review notes

- Cover stays opaque at eye height. The cover art's corrected pier replaces an open I-beam concept. Filled six-view envelopes, not illustrated panel proportions, define the tactical mass and front/side axes.
- Ramp = 16m horizontal run, 4m width, 0→2m over 6.5m, 3m level turnout, 2→4m over 6.5m. Wing joins and drop edges remain clear. Do not add side rails across them.
- Pad body is recessed below a flush top. The pad sheet's trigger/landing inset illustrates sizes only: the actual landing is seven horizontal metres away on a +2m wing, not concentric with the pad. Read the trajectory and landing coordinates in Map.json.
- Portal field is opaque and shot-blocking. Back housing is solid; front-entry only at either gate. Recess any illustrated base plate so the clear aperture starts at h=0 without a raised sill. A/B and North/South headers are identifiers, not extra gates.
- Health is nonblocking. Any .25m illustrated mount must be recessed/kept out of collision; do not introduce a step or obstruction on the required route.
- Wall-bay art describes a reusable surface treatment; do not place extra walls that close planned sightlines or routes. Bevels/detail remain inside the prescribed volume or in nonblocking cosmetic collections.
- Concept-sheet backgrounds, example floors, towers and connecting scenery are context illustrations, not additional playable architecture. The overview art-over also remains qualitative: use measured views to resolve any shifted details or marker counts.
- Sky thumbnails are not seam-matched cubemap faces. Distant landmarks are outside the playable footprint and must not become traversable routes or aim-obscuring silhouettes. Soft lighting must preserve all four character palettes.
- Illustrated glow intensity is a look proposal. Keep the field readable without flash, overexposure or particle clouds hiding an opponent. Review reduced-flash presentation separately.
- Source dimensions and construction views establish a modeling target. Exact final part agreement still comes from one editable asset master; generated view drawings are not calibrated CAD.

## Coverage and next gate

This pack covers the arena and environment families in Environment-Kit.json: structure, cover, traversal, health, fixtures, navigation landmarks and world/lighting. It excludes weapons, character modeling, HUD and combat/blood artwork.

Before detailed production, review the measured layout and each visual family. Live controller clearance, spawn silhouette/visibility, pad trajectories, portal behavior, bot routes, human duel balance and performance remain unverified. Static checks in the parent folder are not replaced by these images. Original character/art approvals do not automatically approve this new map or hidden construction.

## Reproduce behind the scenes

From the repository root:

```powershell
python tools/design_foundry_arena.py
.\tools\Build-FoundryReferences.ps1
python tools/package_foundry_references.py
```

The Blender job uses four CPU threads, bounded eight-minute execution and cleanup of its owned process. No Unity/game player or visible windows are opened. This package remains local until publication is requested.
