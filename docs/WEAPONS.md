# RA-01 induction accelerator

The original Rail Arena weapon replaces the first- and third-person blockout meshes. The open rails, copper induction windings, ceramic standoffs, machined receiver, sight, grip, trigger guard and vent/fastener details are generated from editable geometry. No downloaded meshes or textures are used. Original geometry and source code are MIT licensed.

## Sources and generation

- `ArtSource/Original/Railgun/Railgun.blend`: editable Blender master with individually named parts and editable receiver lettering. Export copies are grouped by surface.
- `tools/build_railgun.py`: deterministic geometry, normal preparation, static mesh grouping, LOD export and studio preview setup for Blender 5.2.
- `ArtSource/Original/Railgun/preparation.json`: actual export counts and provenance.
- `RailArena/Assets/RailArena/Art/Railgun`: two FBX LODs and Unity URP materials.
- `RailArena/Assets/Resources/Weapons/Railgun.prefab`: runtime weapon with muzzle, LOD group, actuator and energy-surface references.

LOD0 contains 45,632 triangles; LOD1 contains 13,683. Each level has six static surface meshes plus one actuator. Static detail is grouped by material to limit draw calls. Audited dimensions are approximately 0.25 x 0.45 x 1.20 metres. The first-person copy forces LOD0; remote copies use distance LOD selection. The prefab has no weapon colliders or damage logic.

The refined RA-01 has tapered rail profiles, faceted ceramic insulators, an octagonal muzzle housing, a skeletal stock, an open sight hood, underside cooling shoes, charge lamps and receiver identification. The existing pistol grip, support cushion, actuator and calibrated grip/muzzle transforms are preserved. Gunmetal, titanium and ceramic surfaces have revised color and roughness. The studio, side and rear PNGs beside the master are Blender review renders, not game screenshots.

From the repository root, regenerate with the installed Blender 5.2 executable, for example:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python tools/build_railgun.py -- (Get-Location).Path
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli -- --code 'RailArena.Editor.RailgunBuilder.Build();'
```

For background import without opening the Editor, use:

```powershell
unity run .\RailArena --editor-version 6000.6.7f1 --timeout 300 -- -nographics -executeMethod RailArena.Editor.RailgunBuilder.Build
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --threads 4 --python tools/validate_railgun.py -- (Get-Location).Path
```

The generator limits rendering to four CPU threads and 24 samples. The geometry audit checks source editability, seven export meshes per LOD, finite coordinates, nondegenerate triangles, reported triangle counts and metre scale. Reports are stored in `Validation/railgun-refinement-geometry.json` and `Validation/railgun-refinement-editmode.xml`. This refinement updates project assets; previously packaged standalone players require a rebuild to include it. Native first-person visual approval remains outstanding.

The Editor builder imports the meshes, assigns materials, creates the weapon prefab and updates only the existing contender weapon attachment. It preserves the character controller and animation graph. A full contender regeneration also uses this weapon prefab, so build the gun before running the contender builder.

## Presentation and invariants

`RailgunPresentation` observes the authoritative/predicted cooldown already owned by gameplay. Its actuator returns over the existing 90-tick cycle and energy surfaces brighten toward ready. Material property blocks avoid creating per-actor material instances. Rail effects start at the model's muzzle marker. The original hand IK targets are retained; the muzzle points forward on Unity Z.

No damage, firing interval, ammunition, movement or scoring rules change. The arm-only first-person rig now shares the weapon's grip markers and cosmetic recoil; see `FIRST-PERSON-ARMS.md`. Finger refinement and authored first-person actions remain unfinished. The studio PNG is a Blender model preview, not an in-game screenshot; the first-person Editor capture and controlled motion previews establish only their stated visual scope. Creative and sound-on approval remain outstanding.

The rail now has pooled fading core/halo, muzzle and impact effects, authoritative hit/elimination confirmation and four original synthesized sound cues. See [combat feedback](COMBAT-FEEDBACK.md) for source, review exports, network semantics and validation limits.
