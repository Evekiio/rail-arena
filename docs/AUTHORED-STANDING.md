# Original standing-ready motion

The Sky Gardens art graph now uses `StandingReady` at zero movement speed. The original legacy graph is preserved. This is source integration after Windows Review22; that verified portable build remains current until the next shipping milestone. The new loop does not change gameplay movement, weapon timing, controller dimensions, stance setters or multiplayer messages.

The canonical source is `ArtSource/Original/ArtPass-v1/Animation/Standing/Contender-Standing-v1.blend`. It retains the original shared skeleton and body parts, metre units and a 60 Hz, four-second loop. Slight knee flex and restrained breathing avoid a rigid standing silhouette. Original ankle targets stay planted. Arm bone poses are construction references; Unity's existing weapon contact solution remains authoritative for the live hands.

The geometry-derived package includes six orthographic views at six time samples, all at a 2.2 m view span, one measured pose sheet and an editable master. All 216 bound source meshes are checked across 241 samples including the loop endpoint. Minimum source skin height is 8.470 mm; maximum baked ankle error is 0.031 mm and loop matrix difference is zero.

Candidate and installed native reviews each cover 964 character samples, four identities and 241 time samples. Installed skin-floor heights are 8.470–8.490 mm, with maximum recorded wrist error 0.001 mm. Actor positions and standing capsule heights are unchanged. The installed controller selects the authored idle at full weight; the shared graph remains unmodified by capture. Directional and crouch integration regressions pass. These reviews hold the simulation for geometry inspection and do not establish every idle-to-movement blend, moving-world foot contact, native input, sound or final artistic acceptance.

The native reel contains 240 frames at 1280×720/60 Hz, exactly four seconds, silent. The duplicate source loop endpoint is retained in the numeric CSV and excluded from the movie. The reference gallery records exact master and reel hashes and separate source/shipping status.

Reproduce with hidden Blender 5.2 background jobs: `tools/author_contender_standing.py -- "F:/Rail Arena"`, then `tools/render_contender_motion_refs.py -- "F:/Rail Arena" --family Standing`. Require success markers and the matching source hashes. Compose the sheet using `tools/package_standing_refs.py`.

Use hidden Unity 6000.6.7f1 batch `ContenderMotionArtBuilder.PrepareStandingCandidates` for the isolated import or `Prepare` for the independent art graph. Native review requires graphics-enabled hidden batch EditMode tests: `StandingCandidateArtReview`, plus the affected `DirectionalMotionArtReview` and `CrouchIntegrationReview`. Package the verified normal-speed reel and CSV using `tools/package_standing_playback.py`. No root motion, simulation callback or new network field is introduced.

Remaining work includes cosmetic crouch transitions that preserve immediate head/capsule timing, broader FPS/body actions, combat interruption combinations, complete terrain contacts and Sanders' native/artistic review. Do not treat this one action as complete animation production.

A subsequent low-speed review passes 5,184 stationary native blend snapshots: four characters, eight travel directions, two facing angles, nine speeds (including 0–1.5 m/s) and nine cycle phases. It preserves actor root, velocity and capsule at each sample, checks complete skin-floor height and wrist contacts, and requires full idle weight at zero velocity. This evidence does not establish temporal start/stop behavior or moving-world foot locking. See `Validation/standing-blends.xml` and the family `BlendChecks.csv`.
