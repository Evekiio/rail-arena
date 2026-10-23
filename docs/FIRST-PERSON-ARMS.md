# First-person armored arms

The view model includes textured armored forearms and articulated hands derived from the existing CC0 Grab3D contender, with 30 added finger joints and a fitted RA-01 grip. This is an arm-only derivative of the disclosed AI-assisted base, not a newly sculpted original character. It reuses the licensed textures, humanoid skeleton and Quaternius CC0 idle animation. No paid assets or additional downloaded artwork are introduced. See [hand articulation](HAND-ARTICULATION.md) for source, animation, fitting and remaining quality limits.

## Editable assets and generation

- `Assets/Resources/Characters/ViewArms.prefab`: arm-only view rig and editable mount pose relative to the weapon.
- `Assets/RailArena/Characters/View arms.asset`: remapped mesh with 18,377 vertices and 16,116 triangles.
- `Assets/RailArena/Characters/View arms.controller`: looping ready pose with the normal humanoid IK pass.
- `FirstPersonArmsBuilder.cs`: extracts triangles whose three vertices each have at least 60% combined weight on upper-arm, lower-arm, hand or finger-descendant bones, then removes unused vertices while retaining UVs, normals, tangents, weights and bind poses.
- `FirstPersonArms.cs`: cosmetic grip IK, elbow hints and shared attachment setup.

Use **Rail Arena > Build first-person arms**, or:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.FirstPersonArmsBuilder.Build();'
```

Build the contender and detailed railgun first when regenerating everything from source. Full contender regeneration now also rebuilds the view arms. Both character and view presentations reference the canonical grip transforms on `Resources/Weapons/Railgun.prefab`, preserving the previous third-person positions and wrist orientations.

## Runtime behavior

The arm rig is parented beneath the first-person weapon, so both grips and arms share camera motion and recoil. The prefab's translated mount keeps the shoulder cuts below the view. Idle motion evaluates through Unity's ordinary Animator and `OnAnimatorIK`; elbow hints keep the forearms below the firing line. First-person materials match the local contender's selected palette when a session starts, including the challenger slot.

The new rig has no actor, collider or hurtbox components and renders on the existing view-model layer. It cannot change damage, movement, pickups, scoring, authority or hit rewind. It hides with the weapon during death/pause and returns on resume/respawn. Cosmetic backward recoil travel is now 5.5 cm instead of the earlier 15 cm, which brought the stock too close to the camera during review. The camera aim and authoritative shot ray remain unchanged; no ammo or reload mechanism is added.

The original source had wrist bones only. Added finger joints now provide a fitted grip and a subtle trigger/support response after wrist IK. Measured wrist and virtual-tip positions do not prove perfect glove-surface contact. Glove/cuff cleanup, bespoke action clips, a complete directional character set and final creative quality remain open. World-surface view-model clipping and native mouse feel are not certified by this pass.

### Movement and landing feedback

`FirstPersonWeaponMotion` adds original procedural motion to the shared weapon/arms transform. Ground travel produces up to 1.2 cm sideways and 0.9 cm vertical bob, fading out in the air or at rest. Smoothed look sway is capped at 1.5 degrees per axis before its small yaw/roll composition. Landing compression uses the greatest downward speed observed during the airborne interval, caps at 4 cm and recovers with a damped impulse envelope. A 0.28-second raise lowers the weapon by up to 16 cm when a session starts or the local player respawns. Recoil adds up to 2.25 degrees of weapon pitch to the existing 5.5 cm travel.

These effects move neither the camera nor the actor. The crosshair and gameplay aim ray remain stable. Session/life resets clear motion history; hiding on death or pause clears pending landing/sway state. The motion class reads velocity, grounded state and look deltas; it has no access to authority or networking. It adds no equip delay, fire restriction, sprint or reload. This is procedural presentation, not a replacement for bespoke finger/action animation.

Online clients now receive the host's life identity and explicitly reset view motion/recoil on respawn and rematch, including a respawn whose death-state snapshots were lost. The authoritative snapshot handler also clears old-life prediction and camera correction. This corrects lifecycle signaling; it does not add new skeletal action clips.

The updated controlled review uses the runtime motion class for equip, running, airborne fade, landing, looking and two recoil/charge pulses. Its 192 frames at 24 fps are saved in `Validation/ViewWeaponMotion/`. The camera remains fixed and velocities/look deltas are staged. This is not native input, actual jumping/landing footage or firing-cadence acceptance. The earlier idle/recoil-only preview in `Validation/ViewArmsMotion/` remains historical evidence.

## Verification and preview

The initial three manually sampled EditMode IK cases failed because they did not exercise the normal `OnAnimatorIK` lifecycle (`Validation/view-arms-editmode-initial.json`). They were replaced with one real player-loop test covering neutral and the full +/-89-degree camera pitch range with recoil movement. The suite includes that live test plus arm-weight-only topology, valid Avatar, absence of gameplay components and shared third-person grip references (`Validation/view-arms-editmode.json`). The new test enters/exits Play mode inside the Editor suite; it is not native keyboard/mouse input acceptance.

`FirstPersonMotionReview.Capture()` requires Play mode at the main menu. The original 96-frame idle/recoil capture is preserved in `Validation/ViewArmsMotion/`. The preceding recorder produced the 192-frame sequence in `Validation/ViewWeaponMotion/` described above, using the same runtime motion, prefab attachment and normal animation loop. Two recoil pulses 1.5 seconds apart and charge recovery are staged visually. `review.json` records maximum wrist errors and transform travel. Generated frame PNGs are ignored; the encoded preview, poster, contact sheet and report are retained. Neither preview establishes gameplay, sound-on, shot-cadence or network acceptance.

The current recorder now writes `Validation/ViewHandMotion/`, including the 30-joint grip response and separate contact closeups. The preceding folders remain historical. Its latest measured wrist errors stay below 0.006 mm, with a four-degree maximum finger response; see `HAND-ARTICULATION.md` for the precise scope and exposed surface defects.
