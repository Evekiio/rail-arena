# Rail Arena / v1 alpha art bible

**Gate 1 direction approved by Sanders on 2026-10-30. Production asset likeness and Gates 2/3 remain subject to review.**

## Intended result

A low-poly competitive arena with crisp anime cel shading, expressive and distinct contenders, precise first-person weapon handling, suspended ivory-stone gardens and clear traversal. The improvement must be obvious at ordinary gameplay distance and in motion, not only in a studio render. Sky Gardens, the summit, four staging/basement routes and their returns receive the complete pass. Foundry and Twin Platforms are inventoried and preserved.

The target workload is one human plus three bots at 1920×1080 on the RTX 2080, targeting sustained 60 FPS and a 16.67 ms frame budget. This package does not establish target performance. Measure representative combat, summit, basement, waterfall/foliage and portal-heavy views after the visual slice; do not substitute GPU particle counts or unavailable timer samples for performance evidence.

## Visual rules

- **Shape first:** clean large forms, deliberate bevels and sculpted planes. Spend geometry on face, hair, shoulders, gloves, optic and nearby cover silhouettes. Paint small construction accents instead of covering every surface in modeled bolts.
- **Cel response:** begin with three readable diffuse bands and restrained transition softness. Use a single controlled specular accent on metal and enamel. Maintain soft contact grounding without allowing broad noisy gradients to erase the graphic planes.
- **Outlines:** selective silhouette outlines, approximately 1–2 pixels at 1080p, fading with distance. Avoid internal wireframe-like edges, halos around foliage, thick black faces and outlines that hide targets or the sight opening.
- **Palette:** ivory stone, graphite cloth, teal enamel, bronze supports and cyan interaction accents. Preserve terracotta Bot 01, slate/teal and pale hair Bot 02, and plum/copper with locs Bot 03. Skin tones keep their existing identities and remain readable in shade.
- **Paint:** broad intentional color and roughness regions, fine restrained wear at construction edges, sparse hand-painted accents and clean UV joins. No photographic noise, repeated leaf-vein stamps, indiscriminate scratches or amplified normal-map grain.
- **Light:** warm sunlight, cool restrained ambient shadow and clear contact shadows. Keep character faces readable in the garden, bridge shade and basement. Atmospheric depth must separate levels without washing out opponents.
- **Effects:** brief directional shapes and event-based accents. Muzzle/impact feedback must identify a shot without concealing the target. Keep the removed cyan GPU cloud disabled. Preserve reduced-flash and visual-comfort settings.

The illustrative `concepts/Cel-Direction-Proposal.png` communicates mood and material grouping. It is **not gameplay, a measured blueprint, an approved replacement rifle, an approved HUD layout or map topology**. Its invented details cannot override the canonical measured sources or existing gameplay. The approved character identities remain the source of truth for likeness; the final common cel response is proven in Gate 2.

## Geometry, scale and authority

Use metres and one explicit transform convention per master. Character and Sky Gardens sources use Blender `(x,y,z)` to Unity `(x,z,-y)`. The production precision-carbine master is authored along Blender +X and uses `(x,y,z)` to Unity `(-y,z,x)`; do not apply the character convention to that weapon. The source-specific grip/aim/muzzle authoring records govern its attachments.

Seven representative families have new geometry-derived six-view sheets and editable reference copies. All six views on a sheet use the same orthographic span; rulers are drawn from the recorded pixels-per-metre scale. Source hashes identify the exact geometry used. The 1.80 m ruler is a gameplay-height comparison, not a command to resize every character. Character source heights differ and must be reconciled with shared rig, contacts and visual approval before any geometry replacement.

Keep separate evidence for: source geometry; imported local mesh bounds; scene/prefab renderer AABBs; controller/hurtbox dimensions; and installed first-person/third-person presentation. These are different measurements. A world AABB can include rotation, pose or an attached rifle, and does not certify a mesh's authored dimensions.

Every production family must eventually have front/rear/left/right/top/bottom sheets, construction sections, pivots, sockets, clearance envelopes, material slots and LOD specifications. Add exploded assemblies for devices, rifle and armor/clothing construction. Materials, textures, UI graphics and code use their appropriate state/pixel/interface specifications rather than meaningless physical-size claims. Reference coverage is incomplete until every applicable entry has evidence or a specific Sanders-approved exception.

## Category production standards

**Characters and first-person hands:** refine face planes, eyelids, mouth, jaw, hair flow and shoulder silhouette. Give cloth, armor, glove and boot construction intentional seams and folds. Keep a compatible shared skeleton and stable gameplay dimensions. Build arm/hand action masters with enough finger structure for trigger and magazine contact; inspect palm surfaces, not just wrist-marker error.

**Rifle:** retain the approved measured precision-carbine silhouette, actual sight opening and calibrated grip/muzzle/aim anchors. Separate ceramic, enamel, dark metal, grip and energy regions with painted values and controlled cel specular. Verify candidate UV/AO compatibility before replacing runtime meshes. The original induction railgun remains a legacy reference.

**Environment:** author a reusable stone/trim/cover/support kit and assemble it on the measured playable layout. Preserve the tested collision envelopes, summit jump sectors, four descents and return-device clearances. Make the summit, main arena, staging decks and basement identifiable through structure, value and signage. Do not trade a safe playable edge for a purely decorative silhouette.

**Nature:** use shaped leaf clusters, varied tree/shrub silhouettes, calm wind and graphic water/foam layers. Keep the cloud sea and distant ruins compositionally quiet. Waterfall depth and basement visibility must coexist; avoid excessive transparent overdraw and effects across aiming space.

**Devices:** health availability, portal entry/exit and pad launch/landing destinations must be recognizable without relying only on color. Create explicit active, idle, depleted/cooldown, reduced-flash and paused visual states. Maintain live destination views and the actual portal opening.

**UI/UX:** retain Rail Arena's name, original wordmark, Barlow typography and established icon foundations where compatible. Improve hierarchy, spacing, portraits, action states and presentation across main menu, setup, character selection, duel room, pause/results, scoreboard, settings and Physics Lab. Keep health, ammo and reticle readable in bright gardens and dark basement views. The full accuracy boundary must never animate inside the possible shot cone; only the separate accent animates.

## Animation specification

`animation-matrix.csv` lists every required first-person and body action; `imported-clips.csv` records actual imported clip durations, frame rates and loops. A clip-name match is only a candidate. Reversed run clips, accelerated stand-ins and procedural stance offsets do not count as final authored directional, crouch or slide coverage.

Produce in-place authored action and transition references on the compatible skeleton. For each action: key poses, contact/timing sheet, 24 FPS review strip, normal-speed playback and actual gameplay capture. Inspect first-person hip and ADS views, side hand contact, third-person front/side/rear, feet, weapon attachment and interrupted transitions. Record exact source/clip versions.

Preserve 250 RPM firing and the current authoritative reload, respawn, movement, spread and jump rules. Animation follows simulation; root motion cannot displace controllers or change hitboxes. Reloads must detach/reseat the magazine and move the fingers plausibly without changing ammo timing. Preserve physical ADS sight alignment and camera clearance. Include idle/equip, recoil/recovery, ADS, reload/empty, walk/sprint/crouch/slide/jump/land/traversal for first person; all directional locomotion, stance transitions, weapon actions, reactions, authored death and respawn for bodies. Runtime ragdolls are outside scope.

## Workflow, provenance and integration

Author original work in Blender and use existing licensed rigs/clips only where their results meet the approved direction. Generated images may support concepts and paint sources; they cannot establish technical scale. No new paid or external asset dependencies are authorized. Preserve upstream attribution and consult `THIRD-PARTY-NOTICES.md` before redistribution; do not automatically publish externally supplied reference images.

Headless Blender and Unity jobs are the default, with four-thread bounded Blender jobs and sequential heavy Unity work. Use authorized visible applications only when needed for judgment that headless tools cannot supply. Keep masters editable, export versions reproducible, texture colorspace explicit, Unity identities stable, and candidate folders distinct from installed production assets.

The current audit is read-only: it does not apply new materials, rebuild gameplay scenes or alter the runtime. Local diagnostic files are preserved. The deferred ADS/portal projection issue remains recorded and is only reopened if it blocks the presentation or final acceptance.

## Gates and acceptance

1. **Gate 1 — this package:** inventory/dispositions, gap report, measured samples, art bible, complete action matrix and dependency-based production schedule. Sanders approves the direction and reference standard before the slice.
2. **Gate 2 — complete playable slice:** refined player, rifle/hands with functional actions, garden-to-bridge kit, health/portal/pad, water/foliage/lighting/VFX and HUD/menu together. Supply matching before/after gameplay cameras, closeups, ADS and motion clips. Obtain Sanders' approval before bulk production.
3. **Gate 3 — integrated completion:** finish every production family and action; measure the target workload, perform focused final mechanics/traversal/bot/multiplayer checks, and provide the verified Windows build, checksums, complete sources, reference gallery, reels and exceptions. Sanders approves the final integrated art before publication.

Do focused scale/import/rig/contact checks per changed batch and visual review per milestone. Do not rerun broad mechanics suites for every texture or bevel. Final validation does not replace human input, sound, normal-speed motion, competitive balance or artistic acceptance. No new public alpha package is published before final approval. Continue v1 alpha polish until Sanders explicitly approves progression.
