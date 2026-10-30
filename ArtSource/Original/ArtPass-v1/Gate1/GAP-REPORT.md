# Gate 1 gap report

This is a measured baseline inventory, not completed production art or 100% reference acceptance.

## Coverage evidence

- **importedAssets:** 1079
- **sourceFiles:** 1145
- **inventoryEntries:** 2224
- **rendererInstances:** 1384
- **inspectedContainers:** 11
- **importedClips:** 47
- **missingScripts:** 0
- **measuredSampleFamilies:** 7
- **measuredSampleViews:** 42
- **finalApprovedAssets:** 0
- **referenceCoverage:** not complete; representative measured samples only
- **blenderMasters:** 23
- **blenderObjects:** 3392
- **blenderActions:** 17
- **unresolvedImageBindings:** 2

## Priority gaps

- **Precision carbine, optic, magazine and actuators** — refine. Retain measured silhouette and open ADS optic; improve cel surface separation and magazine/action presentation. Repacked runtime candidate parity remains to be certified. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Original induction railgun** — archive-reference. Preserve editable source and earlier exports; not the current precision-carbine art target. Complete measured construction/reference package remains open.
- **Player character** — refine. Retain identity; refine anatomy, face, hair, armor, cloth and deformation against approved direction. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Bot 01 / terracotta and tied hair** — refine. Distinct head and armor silhouette, shared compatible skeleton and gameplay bounds. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Bot 02 / slate-teal and pale swept hair** — refine. Preserve identity and common gameplay dimensions; refine face, hair flow and readable cel planes. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Bot 03 / plum, copper and locs** — refine. Preserve identity and hair silhouette; refine facial anatomy, garment structure and silhouette. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Isolated player pilot** — archive-reference. A separate candidate, not the installed player. Evaluate useful construction details without silently replacing gameplay assets. Complete measured construction/reference package remains open.
- **First-person arms, hands and grip contacts** — rebuild. Derive an intentional arm/hand master from the approved player rig; create action and camera-clearance sheets. Complete measured construction/reference package remains open.
- **Lower arena, ramps, bridge and underpass kit** — refine. Keep collision footprint and gameplay route geometry. Add intentional trims, edge construction and structural supports. Complete measured construction/reference package remains open.
- **Summit deck, central cover and columns** — refine. Retain the four diagonal launch sectors, open exits and existing jump-pad landing clearances. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Four staging and basement deck assemblies** — rebuild. Current decks originate as Unity primitives. The measured sample is a geometry-derived reference copy; author a proper modular Blender master without changing tested jump gaps. A measured baseline sheet is supplied; detailed construction and production approval remain open.
- **Cover, parapets, piers and supports** — refine. Readable knee/chest/full cover proportions; preserve sightlines and avoid outline noise against opponents. Complete measured construction/reference package remains open.
- **Trees, bark, shrubs, grass and vines** — rebuild. Shaped leaf clusters, painted accents and controlled wind. Existing texture seams and tangent gaps require review. Complete measured construction/reference package remains open.
- **Waterfalls, water lips, foam and spray** — refine. A shared light-responsive cel water style with clear layered foam; preserve traversal visibility and restrained spray. Complete measured construction/reference package remains open.
- **Sky, cloud sea and distant ruins** — refine. Separate gameplay and noncolliding scenery. Use quiet distant values, distinct levels and restrained atmosphere. Complete measured construction/reference package remains open.
- **Health beacon and animated pickup** — refine. Create a canonical assembly and dimension/socket sheet; readable available/depleted states, float and spin. Complete measured construction/reference package remains open.
- **Portal frames, fields and live destination views** — refine. Preserve gate openings, backing, exit capsule clearance and existing teleport rules. Deferred ADS/projection issue is tracked separately. Complete measured construction/reference package remains open.
- **Summit launch pads and basement return bounces** — refine. Canonical Blender device, clear landing/return destination language and charge/burst/reduced-flash states. Complete measured construction/reference package remains open.
- **Route numbers, takeoff markers and destination signs** — rebuild. A consistent measured sign/decal kit, readable without enlarging or obstructing the gameplay surface. Complete measured construction/reference package remains open.
- **First-person weapon and arm action library** — rebuild. Authored action references and clips; match current fire, ADS, reload and interrupt timing. Complete measured construction/reference package remains open.
- **Shared character locomotion and action library** — refine. Retain compatible licensed foundation where suitable; replace reversed/accelerated substitutes with authored directional actions. Complete measured construction/reference package remains open.
- **Muzzle, shot, impact, hit and elimination feedback** — refine. Short readable cel effects, correct muzzle origin, reduced-flash handling and sound synchronization. Complete measured construction/reference package remains open.
- **Portal, pad and pickup effects** — refine. Keep the distracting cyan GPU cloud disabled; introduce only readable silhouettes and event-driven accents. Complete measured construction/reference package remains open.
- **Cel lighting, shadows, grading and atmospheric tiers** — replace. Shared URP cel-material response, clean contact shadows and controlled bloom; real performance evidence required. Complete measured construction/reference package remains open.
- **Main menu, setup, characters, pause, results and duel flows** — refine. Retain name, core branding and input conventions; polished hierarchy, portraits, spacing and responsive states. Complete measured construction/reference package remains open.
- **HUD, icons, reticle, scoreboard and combat feed** — refine. Keep spread boundary truthful during animation, readable health/ammo and uncluttered central aiming space. Complete measured construction/reference package remains open.
- **Settings, visual comfort, audio and Physics Lab** — refine. Consistent controls and screenshot/export clarity; preserve exact tuning behavior and saved preferences. Complete measured construction/reference package remains open.
- **Weapon, traversal, pickup and interface cues** — retain. Existing original/licensed cues remain; assess synchronization and polish with the visual actions, no new music project. Complete measured construction/reference package remains open.
- **Foundry and Twin Platforms** — retain. Inventory and preserve, without a complete new production art rebuild in this milestone. Complete measured construction/reference package remains open.
- **Licenses, fonts, icons, exports, LODs and build art** — refine. Keep source attribution, import identity, export recipes, manifests and release-facing images synchronized. Complete measured construction/reference package remains open.

## Source dependency findings

- Missing image binding `BossDiffuse.png` in `ArtSource/Candidates/Irondust/Extracted/Sci-fi Soldiers 2/Mesh.blend`. This is an inventoried source issue; candidate inputs are not approved for production or new redistribution.
- Missing image binding `BossDiffuse.png` in `ArtSource/Candidates/Irondust/Extracted/Sci-fi Soldiers 2/Mesh.blend`. This is an inventoried source issue; candidate inputs are not approved for production or new redistribution.

## Interpretation limits

- All entries have an explicit disposition; dispositions are proposed, not proof of final asset approval.
- Scene/prefab bounds are world-space AABBs and may include pose, attachments and rotation. Imported mesh bounds are in local units. Do not resize gameplay from these values without canonical-master and export verification.
- Existing concept approval does not approve generated geometry, new shaders or this proposal. The new illustrative board cannot define exact weapon geometry, dimensions, UI placement or map topology.
- The four character source heights differ; their measured source sheets do not change the common controller/hitbox rules.
- The basement deck sample is extracted from actual scene geometry into a reference master. The new modular production kit remains to be authored.
- Runtime-created objects and conditional review-build material/mesh overrides are flagged separately from serialized scene dependencies.
- No new artistic performance measurement, visual acceptance or new gameplay build is claimed at Gate 1.
