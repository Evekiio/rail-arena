# Authored contender locomotion

The Sky Gardens art profile now installs original backward walk, jog and run on all four contenders. Review-07 remains the current verified portable build and predates this batch. The next build must include this committed source before its handoff advances.

The existing controller used reversed forward clips. `ContenderMotionArtBuilder` creates a separate override with exactly three replacements; legacy maps retain their original controller. Simulation, root movement, hitboxes, weapon cadence and network messages are unchanged. `ContenderPresentation.ApplyCombatPose` exposes the existing pose solve for review capture without duplicating its formulas.

| Clip | Frames at 60 Hz | Native duration | Authored stride |
|---|---:|---:|---:|
| RetreatWalk | 36 | 0.600 s | 0.53 m |
| RetreatJog | 20 | 0.333 s | 0.74 m |
| RetreatRun | 18 | 0.300 s | 0.82 m |

The Blender master retains the shared rest skeleton and authors IK contact paths before baking bone motion. It contains no controller root motion. The existing runtime Gait parameter continues to follow velocity. Import preserves the Armature hierarchy: stripping that root makes the copied humanoid description refer to the wrong parent and prevents valid clips.

## Evidence and limits

- [Playback and measured pose gallery](../ArtSource/Original/ArtPass-v1/Animation/index.html).
- [Construction, source hashes and timing](../ArtSource/Original/ArtPass-v1/Animation/retreat-production.json).
- [Installed playback evidence](../ArtSource/Original/ArtPass-v1/Animation/review-evidence.json).
- [Import evidence](../Validation/motion-retreat-import.json).
- [Latest armed playback test](../Validation/motion-retreat-grip-skinning.xml).

Three Blender loops close with less than 0.1 mm ankle target error. The Unity review records 540 frames at 60 Hz and 2,160 character samples with live rifle, wrist and finger posing. Minimum foot-surface heights remain approximately 8.4–8.5 mm above the staged deck. Root positions remain fixed; numerical wrist target error stays below 0.025 m. Captures wait a frame after the IK solve so the native skinning buffer reflects the solved pose.

These are in-place reviews at Gait=1, not moving-world foot-slip certification, rendering benchmarks or native input acceptance. Numerical wrists do not certify every palm or finger surface. Jump, landing, stance transitions and the complete action library remain production work. Sanders retains final artistic and native playtest acceptance.

## Reproduction

Run Blender 5.2 headlessly with `--python tools/author_contender_retreat.py -- <repository-root>`, then `--python tools/render_contender_retreat_refs.py -- <repository-root>`. The first command writes new authored assets; the second checks and renders the saved master without changing it.

Run pinned Unity 6000.6.7f1 in batch mode: execute `RailArena.Editor.ArtSliceBuilder.Prepare` to import and install the profile. Run the focused EditMode fixtures `RailArena.Tests.RetreatMotionArtReview` and `RailArena.Tests.RetreatPlaybackReview` in a hidden graphics-enabled session. Check executed test counts and results. Run `tools/package_retreat_review.py` with the bundled Pillow-enabled Python to create the nine-second silent reel, measured sheets and evidence. Silent review does not establish sound acceptance.
