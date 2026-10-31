# Original crouch motion candidates

Seven original Blender actions now have a measured reference package and valid humanoid imports. They are **candidates**, not installed gameplay. Review 16 and the existing procedural crouch remain the playable baseline. No movement, capsule, hitbox definition, network protocol or live controller has changed in this batch.

| Action | Frames at 60 Hz | Duration | Loop |
|---|---:|---:|---|
| CrouchIdle | 60 | 1.00 s | Yes |
| CrouchForward / Backward | 24 | 0.40 s | Yes |
| CrouchLeft / Right | 15 | 0.25 s | Yes |
| CrouchEnter / Exit | 12 | 0.20 s | No |

The compatible skeleton remains unchanged. The cosmetic pelvis lowers 0.6 m and folds 0.4 m behind the planted ankles; the torso leans forward. Idle and enter/exit retain their ankle positions. Cardinal moving clips have original stance/swing paths. Forward/backward stride is 0.72 m; compact lateral stride is 0.45 m with 0.03 m lane bias. Their nominal stance-foot speed is 3.6 m/s, matching the current 8 × 0.45 movement setting. This is not certification of moving-world foot lock, blends or custom Physics Lab tuning.

## Reference and import evidence

The canonical master is `Animation/Crouch/Contender-Crouch-v1.blend`. The source manifest records source/master hashes, rig, timing and ankle targets for every frame. There are 252 geometry-derived pose images (six views × six poses × seven actions), seven measured sheets and a source gallery. Every view uses a 2.2 m orthographic span. These unarmed bone poses do not certify weapon grips.

The final Blender check evaluates all **216 rig-bound mesh parts** across every sampled action frame. Minimum measured skin height is about 8.408 mm; baked ankle errors remain below 0.1 mm and looping pose matrices close within 0.001. Native imports are valid humanoid clips at 60 Hz with the shared skeleton and root locks; `Validation/motion-crouch-import.json` records actual durations/curves. They are not referenced by the live controller.

The first multipart check exposed up to 73.5 mm of knee-guard penetration in the symmetric deep-crouch stride. The culprit vertex was fully weighted to RightLeg: reaching behind the lowered hips folded the knee below the floor even while the ankle target was correct. The revised pelvis/foot relationship fixes that source defect without lifting the actor or changing the collision capsule. An excessive lateral split was narrowed after inspecting its references. The initial single-largest-part diagnostic was insufficient to establish whole-skin clearance and is superseded by the all-bound-mesh check.

## Reproduction and remaining integration

Run hidden Blender with `tools/author_contender_crouch.py -- "F:/Rail Arena"`; then `render_contender_crouch_refs.py -- "F:/Rail Arena"`. Its `--check-only` mode writes separate numeric evidence and does not claim updated renders. Run bundled Pillow Python with `tools/package_crouch_refs.py`. Run hidden Unity batch with `RailArena.Editor.ContenderMotionArtBuilder.PrepareCrouchCandidates`. Require the success markers, matching master hashes and valid import records; Blender can exit zero after a Python exception.

Before installation, validate all four native character variants, full skinned floor/contact bounds, weapon grips, aim alignment, source hitbox definitions, controller clearance, stance transitions, death/respawn and interruption. The existing CrouchPose already lowers the hips by 0.6 m; simply playing these clips and leaving that pass active would lower them twice. Enter/exit reference timing must never delay the authoritative crouch capsule or expose a standing head above it. Integration needs a stance-aware graph and compatible handling of airborne and slide poses. Native normal-speed playback, moving-world foot contacts and Sanders' final input/sound/artistic acceptance remain open.
