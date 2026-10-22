# First-person armored arms

The view model now includes textured armored forearms and fixed-grip hands derived from the existing CC0 Grab3D contender. This is an arm-only derivative of the disclosed AI-assisted base, not a newly sculpted original character. It reuses the existing licensed textures, humanoid skeleton and Quaternius CC0 idle animation. No paid assets or additional downloaded artwork are introduced.

## Editable assets and generation

- `Assets/Resources/Characters/ViewArms.prefab`: arm-only view rig and editable mount pose relative to the weapon.
- `Assets/RailArena/Characters/View arms.asset`: remapped mesh with 18,333 vertices and 15,962 triangles.
- `Assets/RailArena/Characters/View arms.controller`: looping ready pose with the normal humanoid IK pass.
- `FirstPersonArmsBuilder.cs`: extracts triangles whose three vertices each have at least 60% combined weight on upper-arm, lower-arm or hand bones, then removes unused vertices while retaining UVs, normals, tangents, weights and bind poses.
- `FirstPersonArms.cs`: cosmetic grip IK, elbow hints and shared attachment setup.

Use **Rail Arena > Build first-person arms**, or:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.FirstPersonArmsBuilder.Build();'
```

Build the contender and detailed railgun first when regenerating everything from source. Full contender regeneration now also rebuilds the view arms. Both character and view presentations reference the canonical grip transforms on `Resources/Weapons/Railgun.prefab`, preserving the previous third-person positions and wrist orientations.

## Runtime behavior

The arm rig is parented beneath the first-person weapon, so both grips and arms share camera motion and recoil. The prefab's translated mount keeps the shoulder cuts below the view. Idle motion evaluates through Unity's ordinary Animator and `OnAnimatorIK`; elbow hints keep the forearms below the firing line. First-person materials match the local contender's selected palette when a session starts, including the challenger slot.

The new rig has no actor, collider or hurtbox components and renders on the existing view-model layer. It cannot change damage, movement, pickups, scoring, authority or hit rewind. It hides with the weapon during death/pause and returns on resume/respawn. Cosmetic backward recoil travel is now 5.5 cm instead of the earlier 15 cm, which brought the stock too close to the camera during review. The camera aim and authoritative shot ray remain unchanged; no ammo or reload mechanism is added.

The source has wrist bones but no articulated finger rig. Fingers remain in the previously authored fixed combat shape. Wrist contact is measured; it does not prove perfect finger-to-trigger/palm fit. Finger refinement, dedicated authored equip/fire/landing motion, a complete directional character set and final creative quality remain open. World-surface view-model clipping and native mouse feel are not certified by this pass.

## Verification and preview

The initial three manually sampled EditMode IK cases failed because they did not exercise the normal `OnAnimatorIK` lifecycle (`Validation/view-arms-editmode-initial.json`). They were replaced with one real player-loop test covering neutral and the full +/-89-degree camera pitch range with recoil movement. The suite includes that live test plus arm-weight-only topology, valid Avatar, absence of gameplay components and shared third-person grip references (`Validation/view-arms-editmode.json`). The new test enters/exits Play mode inside the Editor suite; it is not native keyboard/mouse input acceptance.

`FirstPersonMotionReview.Capture()` requires Play mode at the main menu. It records 96 frames at 24 fps from a temporary review camera, using the same prefab attachment and normal animation loop. Idle, two recoil pulses 1.5 seconds apart and charge recovery are staged visually. The MP4 in `Validation/ViewArmsMotion/` is controlled presentation evidence, not gameplay, sound-on, shot-cadence or network acceptance. `review.json` records maximum wrist errors. The generated frame PNGs are ignored; the encoded preview, poster and report are retained.
