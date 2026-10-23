# Build and play Rail Arena

This is the stable local playtest handoff. Offline solo and bot play are ready for your own testing. Final art quality and full human/internet multiplayer acceptance remain open.

## Play now on this PC

Launch `F:\Rail Arena\Builds\Evaluation\RailArena.exe` for the non-development player. Keep its entire folder together; the EXE depends on the adjacent data folder and DLLs. No account, server or internet connection is needed for offline play.

1. Choose **PRACTICE**. Set **BOTS** to **0 // SOLO** for movement testing, or choose 1–7 bots and a difficulty for combat.
2. Start untimed practice. Use WASD to move, mouse to aim, left mouse to fire, Space to jump/keep hopping, Tab for scores and Escape for pause/settings.
3. Try the ramps, gaps and four jump pads. Fire while moving, fall off and respawn, then restart with bots from the pause menu.
4. With bots, test head/body/limb hits, health pickups, deaths and respawns. Try stationary and strafing target scenarios before normal combat.
5. Use **OFFLINE BOT MATCH** for the timed match and results flow. The starting countdown lasts three seconds; rail recovery is 1.5 seconds.

The development player at `Builds/Windows/RailArena.exe` is the version used by automated validation. Both local handoff players use fingerprint `fa2cb0af271399bb89d693dc5768b378a1aa832c50f6720cf21953329edb7bc4`.

## Open and build the editable project

Install **Unity 6000.6.7f1** with Windows build support through Unity Hub. Use the exact version pinned in `RailArena/ProjectSettings/ProjectVersion.txt`. Unity licensing and initial package download are needed to develop; they are not needed to run the existing offline player.

For a fresh clone, install Git LFS, clone the public repository and run `git lfs pull` before opening the project. Open **the `RailArena` subfolder**, not the repository root, through Unity Hub. Wait for imports and compilation to finish.

Open `Assets/RailArena/Generated/Foundry.unity`, then press Play to test in the Editor. Click the Game view to give it input focus.

For a Windows build, leave Play mode and choose **Rail Arena > Build Windows playtest...**. Save intentional scene changes when prompted, then choose `RailArena.exe` in a new folder such as `Builds/MyPlaytest`. This uses the existing enabled scenes and creates a non-development Windows player, then reveals the EXE. Run it with all adjacent files present. Cancelling either dialog leaves the existing build intact.

You can also use **File > Build Profiles**, select Windows, retain the enabled Foundry scene and choose **Build** with Development Build disabled.

Alternatively, with this project's Editor running and the Unity CLI installed, run from the repository root in PowerShell:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli --timeout 600 -- --code 'RailArena.Editor.ArenaBuilder.BuildWindowsPlaytestTo("../Builds/MyPlaytest/RailArena.exe");'
```

This compiles the saved enabled scenes. Save intentional scene edits first. **Rail Arena > Build Foundry scene** regenerates the arena and can replace generated scene/material edits; it is not required for an ordinary player build.

Builds are local and ignored by Git. Source, Unity metadata and licensed binary assets are published through Git/LFS. Keep your own build folder separate from the supplied players.

To make a portable ZIP after building, use Python 3.9+ and the fingerprint recorded by that build (not an identity from newer edited source):

```powershell
python tools/package_playtest.py --build Builds/Evaluation --output Builds/Packages/MyEvaluation.zip --fingerprint fa2cb0af271399bb89d693dc5768b378a1aa832c50f6720cf21953329edb7bc4
```

This example identifies the supplied evaluation build. Choose a new output filename; the packager refuses to overwrite files, excludes Unity's backup folder, includes notices and verifies every ZIP entry. Extract the entire ZIP before running the EXE. Build identity is supplied from the build record; the packager verifies file integrity, not embedded identity or gameplay acceptance.

## Repeat the automated checks

The validation runners require a Development Build. The existing `Builds/Windows` player is suitable. Run from the repository root:

```powershell
.\tools\Test-Mechanics.ps1
.\tools\Test-Offline.ps1 -Duration 45 -Bots 7 -Difficulty 1
```

These exercise actual standalone authority. They do not establish physical keyboard/mouse feel, audible mix quality or a real internet connection. See [validation](VALIDATION.md) for build-specific evidence and [hosting](HOSTING.md) for direct-IP setup.

## Known limits for this handoff

- Character fingers and weapon grips are articulated, but glove/cuff seams, some intersections and richer action animation still need polish. The unfinished sleeve experiment is preserved locally under ignored `ArtSource/Candidates/ArmLiner-Handoff`, outside the shipping project.
- Automated mechanics, offline combat and local simulated-network duels have passed. Physical keyboard acceptance, movement feel, sound-on review and native two-human host-to-rematch acceptance remain for human testing.
- A different-internet-connection duel has not been verified. The preceding ba7f4cc8 build has full ten-minute local match/rematch coverage, clean and impaired, with large client corrections under concurrent import/build load. The current fa2cb0af build adds a tested receive-before-physics fix and has a 60-second impaired-duel regression. This does not establish smooth movement or native human interaction.
- This is a playable development checkpoint, not a finished release or final visual-quality signoff.

If something fails, record the mode, bot count/difficulty, steps, expected result and actual result. The normal Windows player log is `%USERPROFILE%\AppData\LocalLow\Evekiio\Rail Arena\Player.log`; check the company/product names in Project Settings if you change them.
