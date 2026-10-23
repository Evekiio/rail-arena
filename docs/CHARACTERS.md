# Armored contenders

The chosen direction is detailed armored sci-fi contenders using free assets and original work only.

The new approved direction uses four original low-poly contender meshes with modeled faces, hair, layered armor and clothing, palette textures and three LODs per character. See [the stylized contender sources and regeneration workflow](STYLIZED-CONTENDERS.md). They retain the same animation skeleton and hit rules. The skeleton comes from Grab3D's CC0 Sci-Fi Soldier; its AI-assisted origin and license remain disclosed in the third-party notices. The old textured soldier geometry is retained as historical source, not reused in the new visible meshes.

## Editable assets

- `ArtSource/ThirdParty/Grab3D/Original_Soldier.fbx`: the downloaded rigged source.
- `ArtSource/ThirdParty/Grab3D/ArmoredContender.blend`: packed Blender master with the combat grip adjustment.
- `tools/prepare_contender.py`: reproducible texture extraction, grip shaping and LOD export.
- `ArtSource/Original/StylizedContenders`: four editable Blender masters and their export reports.
- `RailArena/Assets/Resources/Characters/Contender.prefab`: the player prefab.
- `RailArena/Assets/Resources/Characters/Stylized`: three distinct bot prefabs.
- `RailArena/Assets/RailArena/Characters/Contender.controller`: editable animation graph.
- `ContenderBuilder.cs`: generates the prefab, materials, LOD group, weapon attachment and hurtboxes through the Editor.

The historical rigged source has approximately 239k triangles. The original stylized meshes instead use a compact palette texture and modeled surface detail. Actual triangle counts and bounds for each new LOD are in `ArtSource/Original/StylizedContenders/*-Preparation.json`; the original Blender masters retain individual parts and packed palette textures.

## Animation

Quaternius's free CC0 Standard animation library supplies idle, walking, jogging, fast running, jumping, landing, impact and death motion. Backward locomotion clips are derived by reversing the corresponding humanoid motion curves. The graph blends from actual gameplay velocity. The lower body turns toward travel while look-at and hand IK maintain aim. Recoil moves the weapon and hand targets after an authoritative shot.

Root motion is disabled. Animation does not move the character controller or change movement acceleration, damage, health, cooldowns or respawn timing. Hurtboxes follow the skeleton; their hit regions retain the same damage values. Dead bodies remain visible for their death animation while their hurtboxes and controller are disabled, then reset at the normal respawn. Cosmetic gravity and bone clearance settle the visual body onto the deck without changing the authoritative actor position.

The upstream skeleton had wrist bones only. Rail Arena now adds 30 weighted finger joints, an authored RA-01 grip profile and a subtle recoil-driven trigger/support response shared by the contender and view arms. See `HAND-ARTICULATION.md` for source and quality limits. Directional movement uses lower-body turning and backwards clips rather than a complete authored eight-direction strafe library. The arm-only first-person rig is derived from the same base; see `FIRST-PERSON-ARMS.md`.

## Validation and review

The regression suite checks the humanoid Avatar, LOD structure, eleven bone-following hurtboxes, forward/backward foot motion without root displacement, and head descent during death. Current results are in `Validation/character-editmode.json`.

The later scale regression in `Validation/mechanics-editmode.json` also verifies metre-sized world geometry despite the FBX's 100x bone scale. Head/body boxes compensate inherited scale; limb radii are in metres and heights follow world-space bone distances. `ContenderBuilder.UpdateHurtboxes()` regenerates just the eleven hurtboxes. Standalone region/damage checks and the original failure are detailed in `MECHANICS-VALIDATION.md`.

The `CharacterMotionReview.Capture()` Editor helper records a controlled 24 fps LOD0 review through Unity's normal animation and IK loop. Its sequence is idle, run, backwards, jump and death. The jump arc is staged for visual inspection. The resulting `Validation/CharacterMotion/Contender-Motion-Review.mp4` is not gameplay or native-input acceptance evidence. A normal standalone bot soak is recorded separately.

Creative quality still requires review of the actual moving result. The four new meshes are the current modeling pass; close finger contact, authored first-person actions, facial animation and a bespoke eight-direction movement set remain further improvements.

The current motion preview includes the original RA-01 railgun described in `WEAPONS.md`. Its attachment preserves the existing wrist IK targets. Opening an online pause menu now leaves animation and cosmetic corpse settling active, consistent with the continuing online authority; offline pause still freezes them. Editor branch checks are recorded in `Validation/weapon-pause-online.json` and `weapon-pause-offline.json`. These checks use a listening host without a connected peer and do not establish complete two-player pause acceptance.
