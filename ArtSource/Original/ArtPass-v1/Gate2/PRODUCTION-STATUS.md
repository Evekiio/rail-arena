# Integrated production status

Sanders approved the direction and authorized continuous production without intermediate approval holds. Final playtest is the artistic and user QA gate. Public release remains held for acceptance.

**Working local milestone:** ArtPass Review-11 adds aperture-cropped portal views to the complete Review-10 presentation. The controlled same-executable comparison reduces draw calls by 61–71%; overall timing is mixed. All 39 mechanics checks, the three-bot match and both extended 90-second loopback peers passed. Earlier failed duel reports remain preserved. The corrected main/portal benchmark stages one consistent observer pose. Combat P95 still exceeds 16.67 ms, GPU timers are unavailable, and native acceptance remains open. [Measured profile](../../../../docs/PORTAL-RENDER-PERFORMANCE.md), [validation](../../../../Validation/art-integrated-final-11.json) and [build handoff](../../../../docs/ART-PASS-REVIEW.md) record the scope. Historical packages remain preserved.

## Implemented

Four refined Blender character masters, original 2K painted atlases, twelve LODs, calibrated arms, selective contours and gameplay-derived portraits. Original two-second magazine/wrist reload follows unchanged simulation timing, with interruption/reset handling. Ivory painted stone, normal/cavity detail, modular covers, faceted tree canopies, floating medical cores, route labels and lower cloud banks are integrated. Revised rifle materials, light-responsive waterfall, baked-GI cel lighting and production Render Graph atmosphere are installed together. Existing gameplay, portal views and basement traversal remain authoritative. The distracting cyan particle cloud is not installed.

Six equal-scale canonical views cover all four characters, stone cover, canopy and medical core. The baseline inventory is supplemented by a hashed production delta and offline gallery. Three integrated Editor tests passed, including reload timing/contact/reset and 180-frame playback. UI flows passed at 1920x1080, 1280x720 and 2560x1080.

## Remaining production and acceptance

Nine original body actions are installed in the Sky Gardens art-profile source: forward/backward walk, jog, run, takeoff, airborne hold and landing recovery. The forward gait fixes measured legacy boot penetration that also affected landing blends. Family reels cover all four identities; three actual-controller jump scenarios cover standing, forward and retreat motion. See [authored locomotion](../../../../docs/AUTHORED-LOCOMOTION.md) and the [current complete action matrix](../animation-production-matrix.csv). Review-09 contains the complete nine-action batch. Strafe, crouch/slide/traversal combinations, reactions/death and the broader FPS/action reference library remain production work. Wrist reach tests do not certify every palm/finger surface.

Full reference evidence for every baseline entry, complete variant/construction layers, remaining architecture undersides, nature families and device geometry refinement remain unfinished. Older maps and experimental sources are retained. No claim of 100% reference coverage or AAA completion is made.

The eight staging/basement decks now have a source-integrated stone kit with radial paving, inlaid landing markers, friezes and ribbed undersides. Two metre-scaled profiles share a canonical Blender master with original collision reference meshes, twelve equal-scale orthographics, exploded construction views and three LODs each. The focused installed test preserves the original collider set, source mesh data, route/device transforms and removal behavior. [Kit evidence](../../../../docs/BASEMENT-STONE-KIT.md). Review-10 includes this batch. Other map structure and remaining device/nature construction remain unfinished.

The original-world portal review exposed existing under-bridge pier occlusion. Measure and correct the aperture placement, then validate both traversal directions; isolated projection checks do not establish unobstructed map visibility. See [the open finding](../../../../docs/UNDERBRIDGE-PORTAL-VISIBILITY.md).

Packaged mechanics, bot-match, targeted multiplayer and 1080p performance evidence remain separate from final visual acceptance. Hidden captures do not establish native input, sound or final artistic acceptance. Unavailable GPU samples remain unavailable.

## Reproduction

Use Blender 5.2 background scripts under tools. Unity 6000.6.7f1: batchmode/nographics ArtSliceBuilder.Prepare, then ArtSliceBuilder.Build from a clean committed checkout. RA_ART_BUILD_OUTPUT selects a new output folder; existing folders are refused. ArtSliceLookCaptures and RifleReloadArtReview are graphics-enabled hidden EditMode tests; check the executed count. Offline galleries do not establish browser rendering.
