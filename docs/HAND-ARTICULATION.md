# Articulated armored hands

The CC0 contender now has 30 added finger joints: three links for each thumb and finger on both hands. The original upstream wrist-only mesh, textures and AI-assisted provenance remain disclosed. This is original rigging and skinning work on that base, not a newly sculpted character.

`tools/hand_rig.py` contains the wrist-local joint guides, smooth neighboring-finger weights and generic combat bind pose. It preserves at most four normalized influences per vertex. `tools/prepare_contender.py` invokes it before texture packing and the three LOD exports. The editable Blender master and full skin report remain in `ArtSource/ThirdParty/Grab3D`. All 30 finger groups contain weighted geometry; the source report's maximum weight-sum error is below 0.000001.

![Isolated articulated support hand, Blender inspection](../Validation/HandRig/Review/Articulated-Left-Side.png)

This is an isolated Blender geometry inspection, not a gameplay screenshot. Earlier candidates in `Validation/HandRig/` retain the visible knuckle stretching that prompted smoother weights. Unchanged outer finger vertices and hard nearest-finger assignments were corrected before integration.

## Weapon fit and animation

`Assets/RailArena/Characters/Hand grip.json` stores the fitted wrist markers and joint rotations for the RA-01. `HandGripAuthoring` applies that data when rebuilding the weapon, contender and view-arm prefabs. First- and third-person hands use full wrist rotation IK, preserving a consistent palm orientation through animation and aim. The old targets placed excessive hand geometry inside the trigger guard; the new pose places the index finger by the trigger and curls the other digits around their grips. A constrained Editor-only fitting pass was used for nine virtual fingertip guides; the support thumb retains its natural pose. Those guides are authoring aids, not proof that every visible glove surface contacts the weapon perfectly.

Runtime `HandArticulation` applies cached rotations after Animator and wrist IK. The right index adds up to 2/4/2 degrees across its links during shot recoil; other fingers add one degree and thumbs half a degree. Every frame starts from the authored grip, avoiding accumulated rotation. It uses the existing recoil envelope, creates no gameplay components, and adds no reload, equip restriction or firing delay. No runtime finger solver, new package or paid asset is added.

Rebuild from the repository's existing licensed inputs:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python tools/prepare_contender.py -- "$PWD\ArtSource\ThirdParty\Grab3D\Original_Soldier.fbx" "$PWD\RailArena\Assets\ThirdParty\Grab3D" "$PWD\ArtSource\ThirdParty\Grab3D"
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.HandGripAuthoring.ApplyToWeaponPrefab(); RailArena.Editor.ContenderBuilder.Build();'
```

The source hand profile is separate from the generic Blender bind pose. It survives prefab regeneration. The main railgun generator also reads it. Inspect individual joints and the serialized grip rotations in Unity when authoring a revision; the calibration snippets in `Validation/HandRig/` record this fitting session and are not general-purpose gameplay commands.

## Evidence and limits

All 76 Editor-suite cases passed. Coverage includes weighted geometry on every finger joint, normalized skin, actual baked skin deformation under recoil, stable wrists, repeatable poses, exact return to the grip, and palm orientation through live first-person extreme aiming and the live third-person pose. Existing humanoid, locomotion, death, hurtbox, input and network cases also pass.

The initial two skin-displacement failures were in the measurement: the FBX renderer's 100x scale was applied twice. The corrected test uses explicit scale compensation before converting deltas to world space, following Unity 6.6's [BakeMesh contract](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html). The failed report is retained; no deformation bound was loosened.

The 192-frame, 24 fps [controlled motion preview](../Validation/ViewHandMotion/Articulated-Hand-Motion.mp4) covers equip/run/airborne/landing/look and two recoil pulses through the ordinary animation loop. Maximum measured wrist errors were 0.0059 mm left and 0.0052 mm right; maximum finger offset was approximately four degrees. Speeds, jump/landing and recoil are staged, so this is not native input or actual combat footage. Close ready/pull captures and the report accompany it.

Close views still expose glove texture/topology seams, cuff/arm extraction edges and imperfect intersections. Surface cleanup, first-person clipping review, richer authored action clips, final balancing and creative quality remain open. This milestone establishes articulated geometry and a reproducible fitted pose, not final high-quality character acceptance.
