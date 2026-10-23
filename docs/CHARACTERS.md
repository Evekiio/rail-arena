# Armored contenders

The chosen direction is detailed armored sci-fi contenders using free assets and original work only.

The game now uses one textured humanoid base with four material variants. These share the same silhouette and hit rules; they are not four separately sculpted characters. The CC0 base is Grab3D's Sci-Fi Soldier. Its AI-assisted origin and license are preserved in the third-party notices.

## Editable assets

- `ArtSource/ThirdParty/Grab3D/Original_Soldier.fbx`: the downloaded rigged source.
- `ArtSource/ThirdParty/Grab3D/ArmoredContender.blend`: packed Blender master with the combat grip adjustment.
- `tools/prepare_contender.py`: reproducible texture extraction, grip shaping and LOD export.
- `RailArena/Assets/Resources/Characters/Contender.prefab`: the game-ready prefab.
- `RailArena/Assets/RailArena/Characters/Contender.controller`: editable animation graph.
- `ContenderBuilder.cs`: generates the prefab, materials, LOD group, weapon attachment and hurtboxes through the Editor.

The original rigged source has approximately 239k triangles. Its three exported LODs target approximately 60k, 24k and 8k triangles, with 2K base color, normal, metallic and roughness textures. Metallic and inverted roughness are packed into Unity's metallic/smoothness map. The Blender master has packed textures for portability.

## Animation

Quaternius's free CC0 Standard animation library supplies idle, walking, jogging, fast running, jumping, landing, impact and death motion. Backward locomotion clips are derived by reversing the corresponding humanoid motion curves. The graph blends from actual gameplay velocity. The lower body turns toward travel while look-at and hand IK maintain aim. Recoil moves the weapon and hand targets after an authoritative shot.

Root motion is disabled. Animation does not move the character controller or change movement acceleration, damage, health, cooldowns or respawn timing. Hurtboxes follow the skeleton; their hit regions retain the same damage values. Dead bodies remain visible for their death animation while their hurtboxes and controller are disabled, then reset at the normal respawn. Cosmetic gravity and bone clearance settle the visual body onto the deck without changing the authoritative actor position.

The upstream skeleton had wrist bones only. Rail Arena now adds 30 weighted finger joints, an authored RA-01 grip profile and a subtle recoil-driven trigger/support response shared by the contender and view arms. See `HAND-ARTICULATION.md` for source and quality limits. Directional movement uses lower-body turning and backwards clips rather than a complete authored eight-direction strafe library. The arm-only first-person rig is derived from the same base; see `FIRST-PERSON-ARMS.md`.

## Validation and review

The regression suite checks the humanoid Avatar, LOD structure, eleven bone-following hurtboxes, forward/backward foot motion without root displacement, and head descent during death. Current results are in `Validation/character-editmode.json`.

The later scale regression in `Validation/mechanics-editmode.json` also verifies metre-sized world geometry despite the FBX's 100x bone scale. Head/body boxes compensate inherited scale; limb radii are in metres and heights follow world-space bone distances. `ContenderBuilder.UpdateHurtboxes()` regenerates just the eleven hurtboxes. Standalone region/damage checks and the original failure are detailed in `MECHANICS-VALIDATION.md`.

The `CharacterMotionReview.Capture()` Editor helper records a controlled 24 fps LOD0 review through Unity's normal animation and IK loop. Its sequence is idle, run, backwards, jump and death. The jump arc is staged for visual inspection. The resulting `Validation/CharacterMotion/Contender-Motion-Review.mp4` is not gameplay or native-input acceptance evidence. A normal standalone bot soak is recorded separately.

Creative quality still requires review of the actual moving result. Close finger detail, authored first-person actions, more individually modeled contenders and a bespoke eight-direction movement set remain further improvements.

The current motion preview includes the original RA-01 railgun described in `WEAPONS.md`. Its attachment preserves the existing wrist IK targets. Opening an online pause menu now leaves animation and cosmetic corpse settling active, consistent with the continuing online authority; offline pause still freezes them. Editor branch checks are recorded in `Validation/weapon-pause-online.json` and `weapon-pause-offline.json`. These checks use a listening host without a connected peer and do not establish complete two-player pause acceptance.
