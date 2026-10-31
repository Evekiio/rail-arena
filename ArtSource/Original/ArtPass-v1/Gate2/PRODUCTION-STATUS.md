# Integrated production status

Sanders approved the direction and authorized continuous production without intermediate approval holds. Final playtest is the artistic and user QA gate. Public release remains held for acceptance.

**Working local milestone:** ArtPass Review-20 adds seven measured arena stone/ramp families across nineteen placements, retaining authored slide/crouch and all earlier graphics/gameplay. All 39 mechanics checks, the three-bot match and both 90-second loopback peers pass. All 457 portable file hashes verify. Combat/summit/basement P95 exceed 16.67 ms; GPU/native acceptance remains open. Historical candidates and rejected Review-18 coverage remain recorded. [Validation](../../../../Validation/art-integrated-final-20.json) and [build handoff](../../../../docs/ART-PASS-REVIEW.md) record identity, checksum and scope.

## Implemented

Three original slide actions are source-integrated after Review-17: entry, feet-first gliding hold and compact recovery, driven by authoritative remaining ticks. Native pose/lifecycle review covers 1,176 samples and twelve scenarios; a six-second matching moving comparison checks 1,440 controller trajectories with unchanged root/velocity/ticks/capsule within 0.1 mm. Minimum moving whole-skin clearance is 7.779 mm. Short custom durations retain a direct gliding pose. The affected crouch regression passes. Review-19 verifies the shipping player. The opt-in duel input correction and retained rejected Review-18 evidence are documented in the slide contract; no eligibility or pass criterion was weakened. [Slide source, measured sheets and comparison](../Animation/Slide/index.html) and [contract](../../../../docs/AUTHORED-SLIDE.md).

Five of seven original crouch actions are now integrated in the art-controller source with native foot IK and immediate stance synchronization. Full-body enter/exit remain references. The package retains 252 six-view source images and seven sheets; native evidence adds a ten-second four-character reel, 2,400 installed samples, 544 directional poses, lifecycle/hitbox checks and legacy restoration. Double lowering is prevented, and the initial diagonal contact failure was corrected with native foot IK. Review-17 now packages and validates these motions; native input/sound, moving-world contacts and final artistic acceptance remain open. [Crouch contract](../../../../docs/AUTHORED-CROUCH.md).

Review-16 installs original summit radial paving and compass seal, coping/frieze/underside rays, central cover and four banded columns. Three metre-scaled Blender modules have nine reducing LODs, eighteen orthographic views and controller-scale reference sheets. All six installed placements retain the original collision hulls and return-device destinations. [Summit source contract](../../../../docs/SUMMIT-STONE-KIT.md).

Four refined Blender character masters, original 2K painted atlases, twelve LODs, calibrated arms, selective contours and gameplay-derived portraits. Original two-second magazine/wrist reload follows unchanged simulation timing, with interruption/reset handling. Ivory painted stone, normal/cavity detail, modular covers, faceted tree canopies, floating medical cores, route labels and lower cloud banks are integrated. Revised rifle materials, light-responsive waterfall, baked-GI cel lighting and production Render Graph atmosphere are installed together. Existing gameplay, portal views and basement traversal remain authoritative. The distracting cyan particle cloud is not installed.

Six equal-scale canonical views cover all four characters, stone cover, canopy and medical core. The baseline inventory is supplemented by a hashed production delta and offline gallery. Three integrated Editor tests passed, including reload timing/contact/reset and 180-frame playback. UI flows passed at 1920x1080, 1280x720 and 2560x1080.

## Remaining production and acceptance

Review-15 installs original bridge deck, cover-screen and pier modules with nine LOD exports, eighteen equal-scale orthographic views and three dimensioned sheets. Five installed placements passed exact-envelope, unchanged-collider/device and removal checks; matching bridge/gameplay/underpass/underside captures were inspected after correcting a backing/panel overlap. [Bridge stone evidence](../../../../docs/BRIDGE-STONE-KIT.md).

Twenty-five original body clips are installed in the Sky Gardens art-profile source: twelve cardinal gaits, five grounded crouch loops, three slide phases, three airborne actions and two combat reactions. The forward gait fixes measured legacy boot penetration that also affected landing blends. Family reels cover all four identities; three actual-controller jump scenarios cover standing, forward and retreat motion. See [authored locomotion](../../../../docs/AUTHORED-LOCOMOTION.md) and the [current complete action matrix](../animation-production-matrix.csv). Full cosmetic crouch/standing transitions, remaining traversal, complete interrupted hit/death stance combinations and the broader FPS/action reference library remain production work. Wrist reach tests do not certify every palm/finger surface.

Full reference evidence for every baseline entry, complete variant/construction layers, remaining architecture undersides, nature families and device geometry refinement remain unfinished. Older maps and experimental sources are retained. No claim of 100% reference coverage or AAA completion is made.

The eight staging/basement decks now have a source-integrated stone kit with radial paving, inlaid landing markers, friezes and ribbed undersides. Two metre-scaled profiles share a canonical Blender master with original collision reference meshes, twelve equal-scale orthographics, exploded construction views and three LODs each. The focused installed test preserves the original collider set, source mesh data, route/device transforms and removal behavior. [Kit evidence](../../../../docs/BASEMENT-STONE-KIT.md). Review-10 includes this batch. Other map structure and remaining device/nature construction remain unfinished.

The under-bridge pier occlusion and reverse traversal failure are corrected with calculated 17 cm mounting offsets. The existing route generator reproduces the placement. Complete portal family construction/Blender references and native acceptance remain open. See [the verified correction](../../../../docs/UNDERBRIDGE-PORTAL-VISIBILITY.md).

Packaged mechanics, bot-match, targeted multiplayer and 1080p performance evidence remain separate from final visual acceptance. Hidden captures do not establish native input, sound or final artistic acceptance. Unavailable GPU samples remain unavailable.

## Reproduction

Use Blender 5.2 background scripts under tools. Unity 6000.6.7f1: batchmode/nographics ArtSliceBuilder.Prepare, then ArtSliceBuilder.Build from a clean committed checkout. RA_ART_BUILD_OUTPUT selects a new output folder; existing folders are refused. ArtSliceLookCaptures and RifleReloadArtReview are graphics-enabled hidden EditMode tests; check the executed count. Offline galleries do not establish browser rendering.

## Lower arena stone source milestone

Seven original measured Blender families now cover nineteen lower-deck/ramp placements with three detail levels each. Focused native verification passes 27 original-floor rays, 704 material-region comparisons across LODs, all collider/navigation/device preservation and cleanup checks. Forty-two equal-scale orthographic views, seven measured sheets and matching installed comparisons are supplied. Revised LODs preserve the original near geometry and weathered colour positions. Review 20 now validates and packages this source milestone with the retained graphics, animation and traversal work. Wider nature/device, animation and full-reference production remain open.

## Rendering-budget research / unpromoted candidate

Review 20 attribution recorded approximately 1,966 combat draw calls across main/portal views. A rigid rifle-shell batching prototype passes native geometry, matched visual, reload and candidate gameplay checks, but reduces full-workload draw calls only 1.1–3.8% and has worse wall-time means in all five controlled views. It is archived as reference material, with production runtime restored and Review 20 still current. Research candidate Review 21 is not a promoted portable package. The archive explicitly records source, disposition and evidence; see [findings](../../../../docs/RIFLE-RENDER-BUDGET.md) and [research gallery](../Experiments/RifleBatch/index.html). The existing low-LOD barrel simplification remains an art gap.

## Garden nature / source milestone

Eleven original unit-frame modules and 33 reducing LOD exports now cover the proposed trunks, crown variants, shrubs, grass and paired vines for all four existing planting sites. The read-only native source audit records 24 noncolliding renderings and exact installed frames. The package includes 66 equal-scale orthographic images, eleven dimensioned sheets and four installed-scale Blender diagrams. It is not installed in Review 20. Flat shrub masses and some branch/crown contacts are being refined before native integration; see [source contract](../../../../docs/GARDEN-NATURE.md) and [candidate gallery](../Nature/GardenNature/index.html).
