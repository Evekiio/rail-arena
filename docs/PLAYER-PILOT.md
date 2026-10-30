# Player character pilot

The separate player pilot is a creator-review candidate based on the approved starting concept and the selected `References-v1/Final` player sheets. It is not installed as the playable character.

- [Offline review gallery](../ArtSource/Original/PlayerPilot-v1/index.html)
- [Editable sources, validation and limits](../ArtSource/Original/PlayerPilot-v1/README.md)
- Neutral Blender master: `ArtSource/Original/PlayerPilot-v1/Player-Pilot.blend`
- Fitted Blender assembly: `ArtSource/Original/PlayerPilot-v1/Player-Pilot-Carbine-Fit.blend`
- Isolated Unity prefab and review scene: `RailArena/Assets/RailArena/Art/PlayerPilot`

The pilot retains the licensed compatible deformation skeleton and existing locomotion library. It adds separately authored player surface work, optional Blender authoring IK controls, paintable UV islands, PBR surface atlases and three LODs. The carbine pose is measured in Unity and transferred back to a separate editable Blender assembly.

The original player/bot prefabs, production weapon prefab and previous player Blender master are hash-checked against the baseline recorded for this task. Model generation, import and capture write only the pilot folders and their own reports. The isolated prefab has gameplay presentation scripts disabled.

Run `tools/Build-PlayerPilot.ps1` stages sequentially: `Model`, `Audit`, `Render`, `Import`, `Tests`, `Capture`, `Fit`. Blender jobs use background mode, four threads, a ten-minute timeout and owned-process cleanup. Unity jobs use the pinned `6000.6.7f1` batch runner with bounded lifetimes. Import disables graphics; offscreen capture requires a graphics device and opens no visible window.

Technical checks establish geometry, rig/export integrity, sampled deformation scale and skeletal contact error. They do not establish exact concept likeness, collision-free clothing, palm/finger surface fit, gameplay acceptance or measured frame-rate performance. Review the face, hair, shoulder silhouette, scarf, fine contacts and moving LOD transitions before replacing gameplay assets.
