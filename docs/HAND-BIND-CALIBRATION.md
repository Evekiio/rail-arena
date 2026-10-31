# Authored finger binding correction

The art importer previously assigned legacy inverse bind matrices to every bone. The original gloves contain rigid finger sleeves authored around curled finger rest frames. Replacing those frames with the legacy rest frames moved distal segments about the wrong joint centres, producing detached-looking fingertips. This was a calibration error rather than a missing mesh or weapon attachment.

`ContenderArtBuilder` now retains the imported source inverse bind for finger descendants of LeftHand/RightHand and keeps the established legacy calibration for wrists and all other bones. The twelve calibrated character LOD skins and four arm skins were regenerated. Original Blender character masters, FBXs, vertices, normals, UVs, topology, weights, skeleton transforms, Avatar and asset identities are preserved. No runtime simulation or network protocol changes are required.

## Evidence

- `Validation/fps-hand-bind-before.json` records the previous calibration and source/legacy joint offsets.
- `Validation/fps-hand-binding-contract.xml`: 360 authored finger matrices, 288 unchanged legacy body matrices, 36 byte-identical vertex streams and unchanged topology across four appearances and three LODs. All four arm skins use their corrected LOD0 bind data.
- `Validation/fps-hand-source-after.xml`: native hip/ADS captures of all four appearances, independent linear-skin proof and actor/accuracy/ADS invariants.
- `Validation/fps-hand-saved-master.json`: reopening the actual Assembly-v2 Blender master verifies all eight held poses, 31,012 arm vertices, 432 bone heads, rigid rifle parts, weights, hierarchy, UVs and camera/socket frames.
- `Validation/hand-seam-motion.xml` and `Validation/HandBindingMotion-01/contacts.csv`: 180 native frames before and after, with twenty sleeve-joint centre pairs each frame. Previous maximum gap 50.4365 mm; corrected maximum 0.00274 mm across 3,600 corrected samples. The review asserts actor position/velocity, capsule, ammo, next shot time and simulation tick remain unchanged.

[Measured current assembly and native motion comparison](../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2/index.html). Assembly-v1 is retained as the historical baseline. The before motion uses an owned clone with the previous inverse bindings; the after uses installed corrected binding. Geometry, actor/Animator state, camera and cosmetic timing match. The sweep includes equip, ADS/recoil articulation, the authored two-second reload and settling. It does not fire authoritative bullets or establish cadence, native input, audio, artistic acceptance or complete animation coverage.

## Reproduction and limits

Use Unity 6000.6.7f1 hidden batch mode. Run `ContenderArtBuilder.Prepare` headlessly, then focused EditMode `HandBindingReview.PreserveGeometryAndAuthoredFingerBindings`. Graphics-enabled hidden EditMode `FirstPersonAssemblyExport` captures the held assembly; set `RA_FPS_ASSEMBLY_EXPORT` to a new versioned RuntimeSource directory, and preserve the passing XML with the compressed source hash. Run graphics-enabled `HandSeamMotionReview` for native movement of the hand/art assembly. Never overwrite frozen raw evidence intentionally without first versioning it.

All assembly Blender tools accept `--family Assembly-v2` after the root positional argument (package tool takes only the option). The author tool additionally accepts `--native-result Validation/fps-hand-source-after.xml --saved-master-result Validation/fps-hand-saved-master.json`. Follow FIRST-PERSON-ASSEMBLY.md for the sequential author/render/reopen/package workflow. Clip hashes and encoding are recorded in motion-review.json; clips are silent 720p/60 reference, not audio acceptance.

Fine contact fit against the rifle and magazine, complete first-person actions, transitions, replacement Avatar import and final native playtest remain open. Joint-centre continuity is a narrow deformation measurement, not proof that every modeled surface or finger contact is artistically complete. The latest verified Windows artifact is identified separately in ART-PASS-REVIEW.md.

To reproduce encoded media from frozen frames, run `python tools/package_hand_binding_motion.py --frames Validation/HandBindingMotion-01 --output Validation/HandBindingMotion-Encoded-02`. It refuses existing outputs, verifies all 360 source images, bounds each ffmpeg job to 120 seconds and probes 720p/60, 180-frame output. The checked-in assembly media retain their original hashes.
