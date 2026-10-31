# Original crouch motion and grounded integration

Seven original Blender actions have measured references and valid humanoid imports. Five grounded motions are now integrated in the Sky Gardens art-controller source. Full-body enter/exit actions remain reference-only. Review 16 remains the packaged baseline until the new Windows integration pass. Movement, capsule dimensions, hitbox definitions and the network protocol are retained; hitbox world positions follow the new grounded poses in the authoritative stance setter.

| Action | Frames at 60 Hz | Duration | Loop |
|---|---:|---:|---|
| CrouchIdle | 60 | 1.00 s | Yes |
| CrouchForward / Backward | 24 | 0.40 s | Yes |
| CrouchLeft / Right | 15 | 0.25 s | Yes |
| CrouchEnter / Exit | 12 | 0.20 s | No |

The compatible skeleton remains unchanged. The cosmetic pelvis lowers 0.6 m and folds 0.4 m behind the planted ankles; the torso leans forward. Idle and enter/exit retain their ankle positions. Cardinal moving clips have original stance/swing paths. Forward/backward stride is 0.72 m; compact lateral stride is 0.45 m with 0.03 m lane bias. Their nominal stance-foot speed is 3.6 m/s, matching the current 8 × 0.45 movement setting. This is not certification of moving-world foot lock, blends or custom Physics Lab tuning.

## Reference and import evidence

The canonical master is `Animation/Crouch/Contender-Crouch-v1.blend`. The source manifest records source/master hashes, rig, timing and ankle targets for every frame. There are 252 geometry-derived pose images (six views × six poses × seven actions), seven measured sheets and a source gallery. Every view uses a 2.2 m orthographic span. These unarmed bone poses do not certify weapon grips.

The final Blender check evaluates all **216 rig-bound mesh parts** across every sampled action frame. Minimum measured skin height is about 8.408 mm; baked ankle errors remain below 0.1 mm and looping pose matrices close within 0.001. Native imports are valid humanoid clips at 60 Hz with the shared skeleton and root locks; `Validation/motion-crouch-import.json` records actual durations/curves.

The first multipart check exposed up to 73.5 mm of knee-guard penetration in the symmetric deep-crouch stride. The culprit vertex was fully weighted to RightLeg: reaching behind the lowered hips folded the knee below the floor even while the ankle target was correct. The revised pelvis/foot relationship fixes that source defect without lifting the actor or changing the collision capsule. An excessive lateral split was narrowed after inspecting its references. The initial single-largest-part diagnostic was insufficient to establish whole-skin clearance and is superseded by the all-bound-mesh check.

## Reproduction and remaining integration

Run hidden Blender with `tools/author_contender_crouch.py -- "F:/Rail Arena"`; then `render_contender_crouch_refs.py -- "F:/Rail Arena"`. Its `--check-only` mode writes separate numeric evidence and does not claim updated renders. Run bundled Pillow Python with `tools/package_crouch_refs.py`. Run hidden Unity batch with `RailArena.Editor.ContenderMotionArtBuilder.PrepareCrouchCandidates`. Require the success markers, matching master hashes and valid import records; Blender can exit zero after a Python exception.

Run `ContenderMotionArtBuilder.Prepare` to reproduce the grounded state, or `AuditGraphRepeatability` to verify byte-stable graph preparation. The live graph plays five grounded clips at speed 1. Native foot IK is required: the initial unassisted diagonal blends lost skin floor contact even though isolated clips passed. The sixteen-direction review now passes with native IK.

`ContenderPresentation` synchronizes crouch/stand poses within the stance setter, including rifle grips. Fully authored grounded poses skip the old 0.6 m procedural lowering, preventing double lowering. Airborne and slide conditions return to their existing procedural treatment. Legacy controllers retain the original lowering path. Source hitbox collider definitions and local attachments are unchanged. The full-body enter/exit references are not played by the live graph because their timing must not delay the authoritative capsule or head pose. A richer cosmetic transition solution remains production work.

Three focused reviews passed: 676 isolated candidate samples, 2,400 installed-controller samples across all four characters, 544 directional poses, immediate stance synchronization, jump/slide fallbacks, death/respawn, unchanged hitbox definitions/attachments and legacy lowering. Minimum installed skin height is 8.424 mm; directional floor heights range from 8.423 to 13.705 mm. Maximum wrist error is 0.014 mm. The ten-second 720p/60 reel is staged through the installed controller at native clip cadence, with simulation held for geometry inspection. It is not native input/audio, moving-world foot-lock or final player acceptance. The existing directional/legacy regression also passes. Records: `motion-crouch-final-focused.xml`, `motion-crouch-directional-regression.xml` and `Animation/Crouch/review-evidence.json`.

Wrist reach does not certify every palm/finger surface contact. Windows integration, complete interruption/transition coverage, moving-world contacts, custom Physics Lab variants and Sanders' final input/sound/artistic acceptance remain open.
