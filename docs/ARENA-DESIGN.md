# Foundry / complete arena authoring design

Version 2, 2026-10-23. Rail Arena by Sanders Riddle.

**Locked by the creator:** duel-first, four-contender offline support, flowing routes with deliberate exposure, fixed 56 × 40m footprint, bright floating Foundry, and one opaque portal pair that blocks shots. The exact placements and dimensions below are the authored proposal implementing those decisions. They require layout review and actual-controller validation before detailed environment modeling. No balance or runtime-integration approval is implied.

This document supersedes the earlier environment-pack layout, including its exposed spawns, overlapping health/landing locations and optional portals. [Gameplay authority](DESIGN.md) remains the source for combat rules. [Measured plans and placement data](../ArtSource/Original/Foundry-Arena-v2/README.md) accompany this specification.

## 1. Arena identity and competitive purpose

Foundry is a suspended industrial precision-sport arena above a bright cloud sea. Readable ivory structures sit on graphite frames, with teal recesses, restrained cyan traversal energy and small copper fittings. Distant industrial islands establish scale. No moving machinery, destructible cover, doors, elevators, environmental damage or random hazards alter a fight. Intentional void gaps are the only environment hazard.

The match rhythm is **locate, approach, expose, fire, reposition**. The fastest upper crossing trades protection for information; the lower loop maintains momentum while breaking long sightlines; the underpass hides movement beneath the bridge. Health is a reason to contest space, not a reward placed directly on a launch landing. There are no bases or assigned territories.

Duel decisions take precedence: avoid a position that watches every approach, allows reliable spawn farming or controls all three pickups. Four-player free-for-all uses exactly the same geometry, pickups and traversal. Do not add mode-specific walls or resources. In four-player tests, prioritize escape choices and readable combat over hiding places.

## 2. Coordinates, dimensions and movement envelope

All placement data uses metres: **x east, n north, h up**. Origin is bridge centre projected to lower-floor level. Blender uses (x,n,h); Unity uses (x,h,-n). Footprint bounds are x ±28 and n ±20. Structural grid is 2m with 1m subdivisions; purposeful cover offsets can use 0.1m increments.

| Element | Authoring target |
| --- | --- |
| Lower floor / wings / bridge | h=0 / +2 / +4m |
| Outer loop | 6m nominal width; no low wing slab intrudes overhead |
| Underpass | 8m nominal width, 3.4m clear overhead below bridge |
| Ramp assemblies | Four; 4m wide, 16m total run: 6.5m rises 2m, 3m flat turnout, 6.5m rises 2m |
| Wing balcony | Four 10 × 4m lobes, composing west/east wings |
| Bridge centre | 12 × 6m, with 4m-wide north/south side-spine approaches |
| Full-height sight blockers | 2.6m high, opaque at eye height |
| Low tactical cover | 1.2m high; obscures lower body but does not promise head protection |
| Main-route clearance | Target at least 2.4m through deliberate cover breaks; never depend on crouching |
| Portal clear aperture | 1.4m wide × 2.2m high; final collider clearance verified separately |
| Launch landing clear area | 3 × 3m reserved, free of cover and pickup mounts |

Use the existing baseline movement asset initially: ground wish speed 10m/s, acceleration 10, air acceleration 1, friction 6, stop speed 3.125, jump speed 8.4375m/s, gravity 25m/s². Preserve these during layout comparisons so map changes are measurable. They remain subject to the separate movement-feel review.

Continuous ballistic approximation gives a 1.424m normal-jump apex and 0.675s same-height airtime. Actual integration/collision differs. Normal jumping therefore does not provide dependable access from lower floor directly onto a +2m wing. All required routes use ramps or pads. Bunny hopping may improve traversal; it is not required to reach any pickup or escape any spawn. No mandatory precision gap jumps.

## 3. Named combat spaces and route network

| Space / callout | Role | Connections and exposure |
| --- | --- | --- |
| **Crown** | Central +4m bridge and H2 contest | Four ramp approaches through side spines; paired offset screens break east/west dominance. No overlooking all spawns. |
| **Underpass** | Protected lower north/south route | Connects North Gate to South Gate beneath Crown. Two structural piers frame, rather than close, the corridor. Allows a concealed rotation, not a dead-end bunker. |
| **West Wing** | Northwest and southwest +2m balconies | Each joins its ramp's flat midpoint and receives one pad. Northwest holds H1; southwest offers an alternate approach without duplicating health. |
| **East Wing** | Rotational counterpart | Northeast is the alternate balcony; southeast holds H3. Geometry matches West Wing under 180° rotation. |
| **Outer Loop** | Continuous lower circulation | Four corners and both gate junctions connect without using pads or portals. Staggered full-height screens remove uninterrupted perimeter rails. |
| **North Gate / South Gate** | Portal and underpass junctions | Each connects clockwise loop, counterclockwise loop and underpass; front-facing portal offers a fast relocation at the cost of an audible arrival. |

Four ramp assemblies connect lower sides to Crown, with wing exits at their midpoint. Players can reverse, branch onto a wing or continue upward. Pad routes offer a faster wing entry but announce trajectory and arrival. Portals shorten north/south rotation; they do not provide elevation or bypass every exposure break.

Each wing also has a marked 3m-wide safe walk-off edge at (±19.5,±14,2), landing on its corresponding clear lower-loop area at (±19.5,±15.5,0). This ordinary two-metre drop has no launch impulse or fall damage; it provides an escape independent of the ramp doorway. Each landing reserves 3 × 3m and must stay clear.

**No single-route rule:** every named combat space needs two practical ordinary-movement escape choices once entered (walk/ramp or a clearly marked safe drop). Pads and portal travel count as optional accelerators, not the only escape. The balcony's pad landing must connect visibly to its ramp turnout. A player who ignores special traversal must still reach every zone and pickup.

All exact slab, ramp, screen, marker and destination placements are listed in Map.json. Individual named cover IDs are the building contract; do not replace them with visually similar props of different height or footprint without repeating sightline/clearance tests.

## 4. Cover, sightlines and spawn pockets

Six-metre lower corridors use alternating full-height screens rather than a line of waist-high crates. Side-lane elbows alternate near x=±24/±26 at n=±3; north/south elbows sit at x=±16, n=±16.2. Their offset leaves a broad bypass while preventing one straight shot down the perimeter. Corner and inner-spawn screens interrupt direct spawn lines. Underpass piers reach the bridge underside; upper screens sit on the bridge and rise 2.6m above it.

At Crown, the two upper screens occupy opposite sides of the bridge. Each leaves at least 3.6m nominal crossing space beside it. The screens must prevent one stationary player watching both ramp pairs, while allowing a moving player to clear angles deliberately. Decorative supports below Crown may not create unplanned floor-level hiding slits.

Sightline targets: 8–18m local fights, 18–30m deliberate bridge/wing shots, no uninterrupted spawn-to-spawn centre-eye ray. Longer diagonals may exist only if they do not expose a spawn and have visible escape cover. These are design targets, not measured combat distributions.

| Spawn group | x,n at h=0 | Intended use |
| --- | --- | --- |
| Corner pockets S1–S4 | (-25.5,-17.5), (25.5,17.5), (-25.5,17.5), (25.5,-17.5) | Two perpendicular lower-loop departures, protected by offset corner/lane screens |
| Inner pockets S5–S8 | (-9,-17), (9,17), (9,-17), (-9,17) | Depart along either direction of the north/south loop, with an offset route toward Underpass |

Initial facing points toward the arena centre. Reserve a 0.65m horizontal radius around the standing origin and verify full capsule/head clearance. Selection first prefers no living enemy line of sight, then maximizes nearest-enemy distance. If every candidate is exposed, select the least exposed, then farthest candidate deterministically; no invulnerability is added. Spawn fairness is evaluated from actual opponent eye and body bounds, not just these marker points.

The static authoring check tests all 28 pairs at eye height 1.58m and finds an intervening specified solid for each. It does not prove full-body concealment, safe initial turning, peeking fairness, occupancy handling or live physics clearance. Those remain required tests.

## 5. Health, jump pads and portals

Health: H1=(-14,12,2), H2=(0,0,4), H3=(14,-12,2). All use amber plus-symbol identification, +25 health, 20-second return and the global health cap. H1/H3 are rotational counterparts, at least 4m from their nearest pad landing centre. H2 is exposed between Crown's screens: either approach can challenge it. Pickup mounting is nonblocking. Do not add armor, ammunition or oversized central-health rewards.

Pad starts are (±25,±12,0); destinations are the corresponding (±18,±12,2). Each destination reserves 3 × 3m. A nominal vertical speed of 14m/s yields a 3.92m apex and approximately 0.952s descending arrival at +2m; horizontal launch speed is approximately 7.35m/s toward the wing. These are initial authoring values, not tested controller results. Preserve normal airborne steering, with no cinematic camera, control lock or invulnerability. Trigger on grounded/descending top contact, require leaving before retrigger, and use a 30-tick guard against duplicate contacts. Re-entry from above must not repeatedly trap the player in a launch loop.

Portal PA housing is centred at (0,-18,1.3), facing north; PB housing at (0,18,1.3), facing south. Exit foot positions are (0,-15.8,0) and (0,15.8,0). The clear field starts flush at h=0 and is centred at h=1.1; do not centre the clear aperture on the taller housing and accidentally introduce a sill. They form exactly one bidirectional connection: either gate can lead to the other, but each gate accepts entry from its front only. The back is solid housing. Entry plane crossing, rather than touching a side or back, activates travel.

Both energy fields are opaque and absorb shots; housing also blocks shots. No shot, hit rewind or live destination image passes through the connection. A blocked shot gets a small field ripple, never a remote hit. Teleport immediately, rotate view and velocity into the exit frame, retain speed and weapon cooldown, and preserve relative lateral entry offset only within safe aperture clearance. Validate full destination capsule before committing; if blocked, retain entry pose and show a blocked icon/audio cue. Process simultaneous requests by authority actor ID and include already-reserved exit volumes when checking the next request. No telefragging. Require leaving the arrival trigger before another traversal. Do not add automatic reversal, input lock or immunity.

The paired portal arrival sound is positional and distinct from pad launch. Initial authoring radius is 24m, subject to sound-on review; a nearby rival should recognize that the route was used. Gates are machinery and tactical cues, not hidden escape buttons. The facing direction and safe front exit must be obvious from rim chevrons and floor arrows.

## 6. Environmental construction and visual hierarchy

| Family | Required pieces and role |
| --- | --- |
| Walkable structure | 4m floor bays, 2m/1m closures, lower corner/straight sections, 4m ramp-width pieces, 3m turnout, wing/bridge landings, bridge side beam, perimeter edge cap |
| Tactical cover | 2.6m full screen in the specified footprints; 1.2m low cover; piers and offset gate baffles. Separate simple gameplay collision. |
| Traversal | One shared recessed pad mesh ×4; one shared portal housing ×2 with A/B identifiers; floor chevrons and reserved landing decals |
| Health | Shared low pedestal and amber collectible ×3; available, absent and returning states |
| Dressing | Recessed vents, flat conduit bays, flush access panels, inset lights, corner fasteners; no dangling cables or walkable machinery |
| Landmarks | West: graphite segmented turbine silhouette; East: ivory split fin silhouette. Cosmetic only, matching collision footprints. North: three-bar gate header; South: two-chevron header. |
| World | Distant faceted industrial islands, restrained citadel silhouettes, soft cloud layers, pale sky and dark arena undersides; no moving background focal object behind target lanes |

The floor tiling target is 77 full 4×4m bays plus 49 half 4×2m bays (1,624m² across all decks, excluding ramp surfaces). The kit specifies eight cover-size variants across nine functional roles for the thirty fixed placements, one shared ramp assembly ×4, pad ×4, portal housing ×2 and health assembly ×3. Initial dressing is fixed at ten recessed vents, eight conduit bays, twelve inset light fixtures, one west turbine landmark, one east split-fin landmark, two gate headers, eight distant islands, three citadels and one sky setup. Mounting coordinates are in Environment-Kit.json. Edge caps and understructure beams repeat along the approved mesh boundaries; derive their trim counts during the shared-master build rather than inventing new gameplay geometry.

West/east landmark differences help callouts without introducing asymmetric cover. North/south identifiers use shape and text, not color alone. Never recolor character palettes as world navigation signage. Cyan identifies traversal and limited service energy; amber plus identifies health. Avoid bright cyan floor borders resembling interactable pads.

Floor tops use broad, low-contrast panels with flush joints. Fasteners and vents remain inset; ordinary seams must not alter collision. Use a single consistent chamfer language, matte cloth/paint, satin ceramic and controlled metal highlights. No dense microgreebles, glossy mirror floors, opaque steam in routes, heavy bloom or motion blur. Large architectural forms do the visual work.

Use soft daylight with an initial sun direction from southwest at approximately 35° elevation, neutral exposure and gentle skylight fill. This is an art starting point: if it creates material visibility advantage between paired positions, soften/adjust lighting before approval. Target visibility takes precedence over dramatic shadows. The far backdrop has lower contrast and detail than contenders; check ivory, terracotta, slate and plum separately against every route background. Author sky faces from one scene; concept thumbnails are not a seamless skybox.

Open edges are intentional. Mark void boundaries with a contrasting continuous deck lip and sparse nonblocking warning decals. Keep cosmetic rails out of launch trajectories and mandatory movement lines. No invisible safety walls or deceptive solid-looking surfaces over void. Nothing outside the footprint is traversable.

## 7. Production and acceptance

The new measured plan supersedes v1 for layout authoring; v1 art remains style/reference history. The [full reference pack](../ArtSource/Original/Foundry-Arena-v2/References-v1/README.md) now includes a 3D greybox generated directly from Map.json, 22 measured renders and eight visual concept sheets. Review remains pending. Regenerate geometry from Map.json, not by tracing illustration perspective. Keep structure, cover, markers, cosmetics and world in separate named collections. Import into an isolated validation scene before replacing the playable Foundry. Use headless tools; visible play/inspection requires permission.

Required reference deliverables: combined plan, isolated three deck plans, four elevations, two perpendicular sections, overview, one player-height view per named space, module front/back/side/top/underside, traversal state sheets, pickup states, world/lighting views and a material board. Every view must reference the same geometry/version. The v2 ramp turnout is 3m, replacing the v1 2m turnout to satisfy the branch-clearance target. Artwork approval is separate from layout/controller acceptance.

Acceptance checks before geometry lock:

1. Exact footprint, four ramps/pads, two linked gates, three pickups, eight spawns; rotationally paired critical geometry and resource access.
2. Full controller can walk both directions around the lower loop, enter/leave every balcony, traverse all ramps/turnouts and reach each pickup without advanced hopping. Verify 2.4m critical-route clearances and 3.4m Underpass overhead.
3. Test all 28 spawn pairs and each spawn against representative enemy positions at Crown, wings, portals and loop; check full silhouette, occupied spawn rejection and at least two actual departures.
4. Every pad clears its launch edge and overhead geometry, lands in its reserved square with neutral input, allows an intentional air-steer exit, and does not deliver directly onto a pickup or into a fatal fall.
5. Portal front/back/side behavior, shot blocking, blocked exit, simultaneous travel, preserved movement/cooldown, arrival guard, prediction reset and nearby audible cue all agree offline/online.
6. Measure ground-route and accelerated travel times, health access and re-engagement opportunities from both sides at fixed movement tuning. Paired deterministic routes should be within 5% travel time; investigate every material mismatch rather than averaging it away.
7. Run stationary/moving/combat bots on all difficulty profiles across repeated seeds; record route use, stalls, void deaths, pickup control, spawn deaths and portal/pad use. Treat zero errors as reliability evidence, not tactical quality.
8. Human duel tests must demonstrate recoverable positional disadvantage, meaningful lower-route flanks, no dominant perch and no repeatable spawn trap. Four-contender tests must retain escape choices and clear targeting. Balance requires human play, not an image or static ray count.
9. Record actual rendered 1080p combat on named reference hardware against the 144fps target. Hidden-window timing cannot certify it. Check all costume silhouettes, reduced-flash mode and sound cues before final art signoff.

Authoring targets may be revised through measured layout review, but must not silently change the locked footprint, mode priority, movement rules, portal shot behavior or resource counts. Record each revision with its reason and regenerated matching views.
