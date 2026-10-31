# Authored contender locomotion

The Sky Gardens art profile source installs original forward/backward and left/right walk, jog and run, plus takeoff, airborne hold and landing recovery on all four contenders. Review-13 contains the lateral batch and all prior graphical/portal work; historical Review-09 through Review-12 remain preserved.

The existing controller used reversed backward clips and accelerated jump/landing states. Its forward gait also penetrated the staged deck by up to 0.121 m before jumping, contaminating the landing-to-locomotion blend. `ContenderMotionArtBuilder` uses an independent art state graph with native-speed airborne states, six authored forward/backward overrides and six original lateral clips. A 13-child directional blend consumes actual velocity in the aim frame. The art locomotion state and its children play at native speed 1; the legacy graph retains its Gait multiplier. Legacy maps retain their original controller. Simulation, root movement, collider dimensions, weapon cadence and network messages are unchanged. The existing live combat-pose solve is reused for review capture.

| Clip | Frames at 60 Hz | Native duration | Authored stride |
|---|---:|---:|---:|
| RetreatWalk | 36 | 0.600 s | 0.53 m |
| RetreatJog | 20 | 0.333 s | 0.74 m |
| RetreatRun | 18 | 0.300 s | 0.82 m |
| AdvanceWalk | 36 | 0.600 s | 0.53 m |
| AdvanceJog | 20 | 0.333 s | 0.74 m |
| AdvanceRun | 18 | 0.300 s | 0.82 m |
| StrafeLeftWalk / StrafeRightWalk | 36 | 0.600 s | 0.40 m |
| StrafeLeftJog / StrafeRightJog | 20 | 0.333 s | 0.50 m |
| StrafeLeftRun / StrafeRightRun | 18 | 0.300 s | 0.60 m |
| Takeoff | 18 | 0.300 s | In-place |
| AirborneHold | 36 | 0.600 s | In-place |
| LandingRecover | 30 | 0.500 s | In-place |

The Blender master retains the shared rest skeleton and authors IK contact paths before baking bone motion. It contains no controller root motion. The runtime Gait parameter remains available to legacy maps; the directional art graph uses MotionX/MotionZ and native authored timing. Runtime presentation caches that capability on controller installation/removal. Directional art faces aim while the legacy controller retains its travel-facing legs. Import preserves the Armature hierarchy: stripping that root makes the copied humanoid description refer to the wrong parent and prevents valid clips.

## Evidence and limits

- [Playback and measured pose gallery](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Construction, source hashes and timing](../ArtSource/Original/ArtPass-v1/Animation/retreat-production.json).
- [Installed playback evidence](../ArtSource/Original/ArtPass-v1/Animation/review-evidence.json).
- [Import evidence](../Validation/motion-retreat-import.json).
- [Latest armed playback test](../Validation/motion-retreat-grip-skinning.xml).
- [Forward gaits and contact playback](../ArtSource/Original/ArtPass-v1/Animation/Forward/index.html).
- [Airborne poses and controller-driven jump reel](../ArtSource/Original/ArtPass-v1/Animation/Airborne/index.html).
- [Forward family and actual movement tests](../Validation/motion-gaits-transitions.xml).
- [Airborne family pose test](../Validation/motion-airborne-installed.xml).
- [Current complete action matrix](../ArtSource/Original/ArtPass-v1/animation-production-matrix.csv).

Three Blender loops close with less than 0.1 mm ankle target error. The Unity review records 540 frames at 60 Hz and 2,160 character samples with live rifle, wrist and finger posing. Minimum foot-surface heights remain approximately 8.4–8.5 mm above the staged deck. Root positions remain fixed; numerical wrist target error stays below 0.025 m. Captures wait a frame after the IK solve so the native skinning buffer reflects the solved pose.

Forward gaits additionally passed 540 frames / 2,160 character samples. Three actual-controller scenarios cover standing, forward and retreat jumps: all visit Locomotion, Jump, Air and Land, then recover. Minimum sole heights stay above 0.005 m. The root rises from 0.080 m to 1.434 m under unchanged discrete jump physics. Landing soles remain approximately 8.3–8.9 mm above the deck while the hips absorb impact. The moving recovery returns to locomotion after its initial compression.

These collision-deck and in-place reviews are not full foot-slip certification, rendering benchmarks or native input acceptance. Numerical wrists do not certify every palm/finger surface. Full crouch/slide/traversal and directional transitions, authored death/reactions, remaining first-person actions and complete action references remain production work. The existing procedural 0.6 m crouch solve is retained; this lateral batch is not a complete authored crouch library. Sanders retains final acceptance.

## Reproduction

Run Blender 5.2 headlessly with `--python tools/author_contender_retreat.py -- <repository-root>`, then `--python tools/render_contender_retreat_refs.py -- <repository-root>`. The first command writes new authored assets; the second checks and renders the saved master without changing it.

Run pinned Unity 6000.6.7f1 in batch mode: execute `RailArena.Editor.ArtSliceBuilder.Prepare` to import and install the profile. Run the focused EditMode fixtures `RailArena.Tests.RetreatMotionArtReview` and `RailArena.Tests.RetreatPlaybackReview` in a hidden graphics-enabled session. Check executed test counts and results. Run `tools/package_retreat_review.py` with the bundled Pillow-enabled Python to create the nine-second silent reel, measured sheets and evidence. Silent review does not establish sound acceptance.

For forward gaits, append `--forward` to both Blender retreat scripts and the Python retreat review packager. For airborne actions, run `author_contender_airborne.py`, then `render_contender_airborne_refs.py`. Run `AirborneMotionArtReview`, `AirborneTransitionReview` and the forward method in `RetreatPlaybackReview`, then `package_airborne_review.py`. The authoring jobs preserve the canonical character master. Run `build_animation_production_matrix.py` to refresh current action evidence while retaining Gate 1's original matrix.

## Authored lateral batch

Six new clips retain the shared rest skeleton. Equal-scale front/side/top sheets cover six sampled poses per clip; the geometry-derived ankle target errors stay below 0.1 mm and loop seams stay within the checked tolerance. Lateral foot lanes are widened for side-step clearance rather than reversing a forward cycle. Blender anatomical handedness is checked after humanoid import and the existing body alignment.

[Master, measured sheets and native-rate family/controller reels](../ArtSource/Original/ArtPass-v1/Animation/Strafe/index.html). [Installed test](../Validation/motion-strafe-installed.xml): 360 captured frames, 1,440 character samples, 256 sampled diagonal/crouched/yaw poses, planted-foot handedness, root/velocity/controller-height preservation and restoration of legacy travel-facing presentation. Actual boot soles stay about 8.4-8.5 mm above the staged floor; wrist targets are within the checked 2.5 cm tolerance. This is not moving-world foot-lock or full palm/finger surface certification.

[Controller transition review](../Validation/motion-strafe-airborne.xml) independently repeats standing, forward and retreat jumps against the changed graph, requiring Locomotion, Jump, Air and Land plus the unchanged discrete ballistic height. Older raw evidence is preserved using `RA_AIRBORNE_REVIEW_OUTPUT`.

Append `--strafe` to `author_contender_retreat.py`, `render_contender_retreat_refs.py` and `package_retreat_review.py`. Import with `ContenderMotionArtBuilder.Prepare`. Run `DirectionalMotionArtReview` and `RetreatPlaybackReview.CaptureInstalledStrafeForAllFourIdentities` in hidden graphics-enabled batch mode. Then run `AirborneTransitionReview` with output `Validation/StrafeAirborneTransitions` before packaging. Source import and builds remain graphics-disabled batch jobs.

The separate [authored combat-reaction pass](AUTHORED-COMBAT-REACTIONS.md) adds HitResponse and DeathFall in Review-14, preserving these locomotion and airborne clips.
