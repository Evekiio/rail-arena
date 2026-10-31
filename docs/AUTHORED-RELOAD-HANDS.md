# Authored reload hand action

The left fingers now release the support grip, grasp the magazine, follow extraction/seating and return to support. The original Blender-authored wrist/magazine path and two-second timing remain byte-identical. The compatible skeleton, meshes, weights, right-hand grip, existing recoil, gameplay and multiplayer protocol are preserved. The action is installed only by the existing Sky Gardens art presentation.

[Editable master, six measured pose sheets, native motion and contact comparison](../ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1/index.html).

`RifleReloadHandArt` stores 121 Blender-evaluated fifteen-joint quaternion delta frames. `RifleReloadPresentation` samples the existing authoritative reload progress; `HandArticulation` applies the deltas after wrist IK and before its existing recoil offsets. Reset/interruption/comfort-disable clears the live track. A disabled corpse-hand pose keeps ownership until respawn. No animation event moves ammunition or completes reloads.

## Source and native evidence

The canonical weapon-root master retains the original rifle/character geometry and a 54-bone calibrated rig. Six action poses have identical-scale front/rear/left/right/top/bottom views, ruler and schematic human envelope. The original Assembly-v2 and complete rifle masters remain source authorities for other appearances and full weapon construction.

Reopening the saved master checks 468,512 posed vertex samples across all 121 frames against independent native-frame FK and original skin weights. Maximum skin error is below 0.001 mm. Native review passes 484 corrected FPS poses across four appearances plus the 121-pose original-grip reference, 12,100 total sleeve-joint centre samples, 28 third-person contact poses and three interruption points per stage. Maximum wrist error is 0.00295 mm; authored/native fingertip centre difference is 0.00225 mm. The right hand stays in its existing grip. A focused reset test preserves the disabled death pose and restores the live grip.

At the grasp beat, all five fingertip sleeve-ring centres are 9 mm from the magazine surface, versus approximately 32–65 mm in the original static finger pose. This is a geometric fitting metric rather than complete glove/palm collision certification. Fine surface overlap, tap fit and the broader first-person action/transition library remain in production. Clips are silent; 121 inclusive 60 Hz boundary samples cover two seconds, with the final boundary encoded as an additional frame (2.0167 seconds total).

Early diagnostic captures read new targets before a fresh runtime LateUpdate and showed false wrist gaps. The corrected capture waits for a strictly newer `FirstPersonArms.LastPoseFrame`, with a bounded deadline, at every sample. All 121 calibrated samples follow distinct runtime pose frames and pass. The proposed shortened path was left as unpromoted local diagnostics; it is not installed. No runtime wrist/IK correction was needed. An early technical camera at the weapon origin was also discarded in favor of the actual normal hip placement before authoring the final master.

## Reproduce

Use Unity 6000.6.7f1 and Blender 5.2.2 sequentially and hidden. Preserve frozen outputs.

1. For a fresh baseline calibration, set `RA_RELOAD_HAND_EXPORT` to a new folder and run the focused graphics-enabled EditMode `ReloadHandSourceExport` test. It clears only the cosmetic finger track and observes frozen gameplay state. The accepted baseline is preserved compressed in the action's Source folder and linked by its decoded hash.
2. Blender background: `tools/author_reload_hands.py -- <root>` authors the versioned master and samples from that frozen input. It refuses to overwrite a master; preserve/version existing artifacts before a deliberate regeneration.
3. Blender background: `tools/verify_render_reload_hands.py -- <root>` reopens the master and renders views. `--verify-only` runs numerical verification without rerendering.
4. Unity batch/nographics: execute `RailArena.Editor.ReloadHandArtBuilder.Prepare`. ArtSliceBuilder also invokes this importer during normal builds. It verifies master hash, joint order, quaternion validity, endpoints and timing before updating the Unity resource.
5. Graphics-enabled hidden EditMode: run `RailArena.Tests.ReloadHandArtReview` to a fresh evidence folder (the test refuses to overwrite frozen playback). Run `tools/package_reload_hands.py` with the Pillow/ffmpeg runtime to package measured sheets and native clips. It verifies the native XML master hash, saved-master proof and media dimensions.

`Validation/reload-hands-native.xml`, `Validation/reload-hands-saved-master.json` and the action review-evidence.json identify the exact source and limits. Latest verified shipping state is recorded separately in ART-PASS-REVIEW.md. This action family does not complete the full art-production plan or establish final native sound/input/artistic acceptance.
