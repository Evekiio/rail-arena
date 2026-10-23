# RA-01 induction accelerator

The original Rail Arena weapon replaces the first- and third-person blockout meshes. The open rails, copper induction windings, ceramic standoffs, machined receiver, sight, grip, trigger guard and vent/fastener details are generated from editable geometry. No downloaded meshes or textures are used. Original geometry and source code are MIT licensed.

## Sources and generation

- `ArtSource/Original/Railgun/Railgun.blend`: editable Blender master, grouped by surface.
- `tools/build_railgun.py`: deterministic geometry, normal preparation, static mesh grouping, LOD export and studio preview setup for Blender 5.2.
- `ArtSource/Original/Railgun/preparation.json`: actual export counts and provenance.
- `RailArena/Assets/RailArena/Art/Railgun`: two FBX LODs and Unity URP materials.
- `RailArena/Assets/Resources/Weapons/Railgun.prefab`: runtime weapon with muzzle, LOD group, actuator and energy-surface references.

LOD0 contains 34,488 triangles; LOD1 contains 10,342. Each level has six static surface meshes plus one actuator. Static detail is grouped by material to limit draw calls. Dimensions are approximately 0.24 x 0.44 x 1.20 metres. The first-person copy forces LOD0; remote copies use distance LOD selection. The prefab has no weapon colliders or damage logic.

From the repository root, regenerate with the installed Blender 5.2 executable, for example:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python tools/build_railgun.py -- (Get-Location).Path
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.RailgunBuilder.Build();'
```

The Editor builder imports the meshes, assigns materials, creates the weapon prefab and updates only the existing contender weapon attachment. It preserves the character controller and animation graph. A full contender regeneration also uses this weapon prefab, so build the gun before running the contender builder.

## Presentation and invariants

`RailgunPresentation` observes the authoritative/predicted cooldown already owned by gameplay. Its actuator returns over the existing 90-tick cycle and energy surfaces brighten toward ready. Material property blocks avoid creating per-actor material instances. Rail effects start at the model's muzzle marker. The original hand IK targets are retained; the muzzle points forward on Unity Z.

No damage, firing interval, ammunition, movement or scoring rules change. The arm-only first-person rig now shares the weapon's grip markers and cosmetic recoil; see `FIRST-PERSON-ARMS.md`. Finger refinement and authored first-person actions remain unfinished. The studio PNG is a Blender model preview, not an in-game screenshot; the first-person Editor capture and controlled motion previews establish only their stated visual scope. Creative and sound-on approval remain outstanding.

The rail now has pooled fading core/halo, muzzle and impact effects, authoritative hit/elimination confirmation and four original synthesized sound cues. See [combat feedback](COMBAT-FEEDBACK.md) for source, review exports, network semantics and validation limits.
