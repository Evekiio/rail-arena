# Player character pilot v1

Creator-review candidate. The installed player, bots, carbine prefab and previous player Blender master are unchanged; their hashes are recorded in `Protected-Assets.json`.

## Review

Open `index.html` locally for the reference/model comparison, actual Unity captures, LODs and five-second motion sequence. Nothing opens automatically.

- `Player-Pilot.blend`: neutral editable master, 288 individually named mesh parts, a connected facial/neck volume, layered hair, suit, armor, fingerless gloves and treaded soles. Native materials and packed reference images are retained.
- `Player-Pilot-Carbine-Fit.blend`: separate editable assembly with the existing production carbine and a measured Unity idle pose keyed on frame 1. The weapon remains separated into editable parts.
- The licensed project skeleton is retained. Thirty finger deformation joints survive runtime export. Optional `CTRL-hand/foot` and pole bones are authoring controls; `Arm IK` and `Leg IK` properties default to zero. Runtime exports use deformation bones only. This is not a facial expression rig or a new animation library.
- Three FBX LODs use one runtime material and unique `SurfaceUV` islands. The 2048px base-color, metallic/smoothness and emission atlases encode authored surface constants. No high-resolution sculpted normal bake or cloth simulation is claimed.
- Unity review prefab and scene: `RailArena/Assets/RailArena/Art/PlayerPilot/Player-Pilot.prefab` and `Player-Pilot-Review.unity`. Presentation scripts are disabled for isolated review. This prefab is not registered as a playable appearance.

## Validation

Blender geometry checks pass: finite coordinates, nondegenerate exported triangles, UV presence, normalized skinning with at most four influences, human scale, thirty finger bones and descending LOD topology.

Unity 6000.6.7f1 sampled five phases at 30 steps each and baked all three LODs at each step. Maximum wrist target error: 0.000 mm. Maximum shoulder anchor error: 0.000 mm. These measure skeletal anchors, not palm/trigger surface clearance. Exact results are in `Unity-Validation.json`. The separate motion video contains 120 fixed-step Unity captures at 24 fps: idle, run, backward, jump and death, one second each. Root translation/jump travel is not simulated. It is an animation review, not gameplay footage.

## Artistic comparison and limits

The approved concept is the primary design authority. The expanded player sheets are generated appearance/construction references, not calibrated views of a shared finished mesh. Source hashes and construction decisions are in `Model.json`.

The pilot preserves ivory/charcoal/teal/cyan identity, compact front/back armor, paired pouches, broad bordered thigh panels, fingerless hands and the specified right outer ankle inset. It adds actual undersides, sole tread and coherent editable construction. It remains an interpretation: the face, swept hair, shoulder-shell profile and scarf drape are visibly more procedural than the illustrations. Do not describe it as an exact reproduction or final artistic approval. Review those areas, finger contact, skin/armor intersections, silhouette under motion and LOD transitions before gameplay replacement.

Headless validation does not establish physical-input acceptance, gameplay feel, audio quality or measured four-contender performance. No release or GitHub publication is performed.

## Reproduce

Run sequentially from the repository root:

```powershell
.\tools\Build-PlayerPilot.ps1 -Stage Model
.\tools\Build-PlayerPilot.ps1 -Stage Audit
.\tools\Build-PlayerPilot.ps1 -Stage Render
.\tools\Build-PlayerPilot.ps1 -Stage Import
.\tools\Build-PlayerPilot.ps1 -Stage Tests
.\tools\Build-PlayerPilot.ps1 -Stage Capture
.\tools\Build-PlayerPilot.ps1 -Stage Fit
```

Blender 5.2.2 and Unity 6000.6.7f1 are used with bounded hidden/background processes and four workers. Capture needs an offscreen graphics device. Encode `Motion/frame-%04d.png` at 24 fps with FFmpeg to `Player-Motion-Review.mp4`, then run `tools/package_player_pilot.py` with Python to recreate the gallery/ZIP. The production carbine and original rig dependencies must already exist in the workspace.

## Methods and provenance

Reference planning, named interfaces and inspection/repair were adapted from powerhouse90/Blender-Superskill. Reference classification, comparison and explicit validation boundaries were informed by RobLe3/cc-blender-skill. These are workflow references, not bundled third-party code or a claim that a Blender MCP backend was installed. The project construction helpers and licensed CC0 skeleton/animation provenance remain applicable. No third-party visible character mesh was reused.
