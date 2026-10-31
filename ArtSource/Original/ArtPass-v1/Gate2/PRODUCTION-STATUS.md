# Integrated production status

Sanders approved the direction and authorized continuous production without intermediate approval holds. Final playtest is the artistic and user QA gate. Public release remains held for acceptance.

**Working local milestone:** ArtPass Review-16 adds the measured summit deck, central cover and four column placements to all Review-15 graphics and gameplay. All 39 standalone mechanics checks, the three-bot match and both 90-second loopback peers passed. The archive verified 393 shipped file hashes. Summit P95 and basement mean/P95 exceed the 16.67 ms budget; GPU/native acceptance remains open. [Validation](../../../../Validation/art-integrated-final-16.json), [summit gallery](../Environment/SummitStone/index.html) and [build handoff](../../../../docs/ART-PASS-REVIEW.md) record scope. Historical packages are preserved.

## Implemented

Five of seven original crouch actions are now integrated in the art-controller source with native foot IK and immediate stance synchronization. Full-body enter/exit remain references. The package retains 252 six-view source images and seven sheets; native evidence adds a ten-second four-character reel, 2,400 installed samples, 544 directional poses, lifecycle/hitbox checks and legacy restoration. Double lowering is prevented, and the initial diagonal contact failure was corrected with native foot IK. The next Windows integration package is pending; Review-16 remains current. [Crouch contract](../../../../docs/AUTHORED-CROUCH.md).

Review-16 installs original summit radial paving and compass seal, coping/frieze/underside rays, central cover and four banded columns. Three metre-scaled Blender modules have nine reducing LODs, eighteen orthographic views and controller-scale reference sheets. All six installed placements retain the original collision hulls and return-device destinations. [Summit source contract](../../../../docs/SUMMIT-STONE-KIT.md).

Four refined Blender character masters, original 2K painted atlases, twelve LODs, calibrated arms, selective contours and gameplay-derived portraits. Original two-second magazine/wrist reload follows unchanged simulation timing, with interruption/reset handling. Ivory painted stone, normal/cavity detail, modular covers, faceted tree canopies, floating medical cores, route labels and lower cloud banks are integrated. Revised rifle materials, light-responsive waterfall, baked-GI cel lighting and production Render Graph atmosphere are installed together. Existing gameplay, portal views and basement traversal remain authoritative. The distracting cyan particle cloud is not installed.

Six equal-scale canonical views cover all four characters, stone cover, canopy and medical core. The baseline inventory is supplemented by a hashed production delta and offline gallery. Three integrated Editor tests passed, including reload timing/contact/reset and 180-frame playback. UI flows passed at 1920x1080, 1280x720 and 2560x1080.

## Remaining production and acceptance

Review-15 installs original bridge deck, cover-screen and pier modules with nine LOD exports, eighteen equal-scale orthographic views and three dimensioned sheets. Five installed placements passed exact-envelope, unchanged-collider/device and removal checks; matching bridge/gameplay/underpass/underside captures were inspected after correcting a backing/panel overlap. [Bridge stone evidence](../../../../docs/BRIDGE-STONE-KIT.md).

Seventeen original body clips are installed in the Sky Gardens art-profile source: forward/backward/left/right walk, jog, run, takeoff, airborne hold, landing recovery, hit response and fall/settle. The forward gait fixes measured legacy boot penetration that also affected landing blends. Family reels cover all four identities; three actual-controller jump scenarios cover standing, forward and retreat motion. See [authored locomotion](../../../../docs/AUTHORED-LOCOMOTION.md) and the [current complete action matrix](../animation-production-matrix.csv). Review-09 contains the complete nine-action batch. Complete authored crouch/slide/traversal and directional transitions, complete interrupted hit/death stance combinations and the broader FPS/action reference library remain production work. Wrist reach tests do not certify every palm/finger surface.

Full reference evidence for every baseline entry, complete variant/construction layers, remaining architecture undersides, nature families and device geometry refinement remain unfinished. Older maps and experimental sources are retained. No claim of 100% reference coverage or AAA completion is made.

The eight staging/basement decks now have a source-integrated stone kit with radial paving, inlaid landing markers, friezes and ribbed undersides. Two metre-scaled profiles share a canonical Blender master with original collision reference meshes, twelve equal-scale orthographics, exploded construction views and three LODs each. The focused installed test preserves the original collider set, source mesh data, route/device transforms and removal behavior. [Kit evidence](../../../../docs/BASEMENT-STONE-KIT.md). Review-10 includes this batch. Other map structure and remaining device/nature construction remain unfinished.

The under-bridge pier occlusion and reverse traversal failure are corrected with calculated 17 cm mounting offsets. The existing route generator reproduces the placement. Complete portal family construction/Blender references and native acceptance remain open. See [the verified correction](../../../../docs/UNDERBRIDGE-PORTAL-VISIBILITY.md).

Packaged mechanics, bot-match, targeted multiplayer and 1080p performance evidence remain separate from final visual acceptance. Hidden captures do not establish native input, sound or final artistic acceptance. Unavailable GPU samples remain unavailable.

## Reproduction

Use Blender 5.2 background scripts under tools. Unity 6000.6.7f1: batchmode/nographics ArtSliceBuilder.Prepare, then ArtSliceBuilder.Build from a clean committed checkout. RA_ART_BUILD_OUTPUT selects a new output folder; existing folders are refused. ArtSliceLookCaptures and RifleReloadArtReview are graphics-enabled hidden EditMode tests; check the executed count. Offline galleries do not establish browser rendering.
