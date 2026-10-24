# Visual offline character picker

Offline Setup > Character now opens four portrait cards instead of cycling a text label. All four installed appearances are selectable by the human: ivory, ember, teal and plum. The selected card has a darker teal frame, a SELECTED label and a saved-choice readout. Selection applies immediately to the saved next-session choice; Back to Setup returns to the launch/pause menu.

During a paused session, the heading explicitly says NEXT SESSION APPEARANCE. Choosing another card does not swap the live body or hand palette. Restarting the session uses the new choice. Movement, hitbox dimensions, bot allocation, difficulty, population limits and direct-IP duel identities are unchanged. Selected cards receive UI focus on entry and selection; native keyboard/mouse acceptance remains separate from callback tests.

## Portrait sources

Four 512 x 640 portraits are rendered headlessly from the installed contender prefabs and their actual palettes. They use consistent front key/fill lighting and a fixed three-quarter camera. The weapon is hidden for the portrait; no gameplay mesh, rig or material is edited. These are model renders, not concept artwork or a promise of final character-art quality.

`ContenderPortraitBuilder.Build` regenerates the PNG textures in `Assets/Resources/UI/ContenderPortraits`. The [source manifest](../Validation/CharacterPicker/Portraits.json) records each prefab and its Unity dependency hash. Regenerate portraits and the manifest when their source prefabs change. Texture import uses clamped bilinear sampling, no mipmaps and a fixed 4:5 aspect in the cards.

## Review and validation

[Picker review gallery](../Validation/CharacterPicker/index.html) shows main-menu selection and paused next-session selection at 720p/1080p, alongside the four source portraits. UI captures draw actual uGUI meshes/materials against a neutral background. They are not human gameplay recordings.

Applicable fixtures cover portrait dependency consistency, resource dimensions, explicit choices, save persistence, exactly one selected label, UI focus, no live-body swap, next-session application, text fit and the existing unique-roster/hand-palette flow. The initial three-test scope passed; a two-test picker-only refresh verifies the final contrast/focus adjustment. A pinned Unity 6000.6.7f1 Windows development build validates integration.

Historical roster captures/reports remain separate checkpoints. No unrelated full suite, broad combat soak, real-input acceptance or performance qualification is claimed. Final character appearance/animation approval and human interface review remain open. The public pre-alpha ZIP is unchanged.
