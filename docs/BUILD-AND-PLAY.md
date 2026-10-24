# Build and play Rail Arena

## Play the released checkpoint

[Download the Windows x64 pre-alpha](https://github.com/Evekiio/rail-arena/releases/tag/v0.1.0-pre-alpha.1). Extract the **entire ZIP** and run `RailArena.exe`, keeping the data folder, DLLs and adjacent files together. Offline play requires no Unity installation, account or internet connection.

This ZIP is an older checkpoint. Its [release notes](PRE-ALPHA.md) describe its features and limitations. New source improvements on `main` are not automatically included in the download.

## Play current source

Current source supports four selectable appearances, 0-3 unique offline bots, untimed practice and timed combat matches. Stationary/moving targets are practice options; timed matches use combat AI. Use [How to Play](QUICK-START.md) for the current menu flow and controls.

The rifle uses click-only fire, a 0.6-second cycle, unlimited ammunition and hold-right-mouse 1.5x zoom. Current damage is 100/50/34; timed matches last ten minutes with tied-leader sudden death. The revised [approved design](DESIGN.md) remains a migration target.

## Prepare a checkout

Install Git LFS and the exact **Unity 6000.6.7f1** Editor with Windows build support. Licensing and initial package restore are required for development.

```powershell
git lfs install
git clone https://github.com/Evekiio/rail-arena.git
cd rail-arena
git lfs pull
```

The Unity project is the `RailArena` subfolder. Its version pin is in `ProjectSettings/ProjectVersion.txt`. Run commands below from the repository root.

## Headless development build

With the Unity CLI installed and the pinned Editor licensed, run:

```powershell
unity run .\RailArena --editor-path 'D:\Unity Editor\6000.6.7f1\Editor\Unity.exe' --timeout 600 --no-tail --format json -- -job-worker-count 4 -executeMethod RailArena.Editor.ArenaBuilder.BuildWindows
```

Replace the Editor path with the actual pinned installation path on your machine. This invokes the existing builder, builds enabled scenes and writes a **Development Build** to `Builds/Windows/RailArena.exe`. A fresh checkout needs its first import before compilation can finish. Inspect the command result; a process starting does not prove a successful build.

Blender and Unity automation must follow [AGENTS.md](../AGENTS.md): headless by default, bounded jobs, sequential heavy work and owned-process cleanup. Visible windows require the operator's permission. Headless validation does not replace physical-input or sound-on acceptance.

## Optional manual playtest build

For an explicitly requested visible Editor session, open `RailArena` in Unity 6000.6.7f1. After imports finish, open `Assets/RailArena/Generated/Foundry.unity`. Use **Rail Arena > Build Windows playtest...** to choose a new executable path. This builds enabled scenes without Development Build. Keep the whole output folder together.

**Build Foundry scene** regenerates scene/material content and is not needed for an ordinary player build. Save intentional edits before building. Local build folders are ignored by Git; do not infer their freshness from their filenames.

## Validate the changed scope

Choose tests relevant to the change and record their results. Avoid routinely adding unrelated fixtures to every check. Development players support the existing mechanics/offline/duel validation runners; use each runner's parameters and current build identity rather than copying historical seven-bot commands or old fingerprints.

Verify source identity after building:

```powershell
python tools/verify_build_identity.py --project RailArena
```

This checks the recorded source identity, not human gameplay quality. Reports in [Validation](../Validation) and the [validation record](VALIDATION.md) identify the conditions tested. Native input, sound, real-internet duels and measured four-contender performance remain separate gates.

## Portable packaging

Use the fingerprint belonging to the build being packaged, not newer edited source:

```powershell
python tools/package_playtest.py --build Builds/Windows --output Builds/Packages/MyPlaytest.zip --fingerprint <recorded-build-fingerprint>
```

Replace the placeholder before running. The packager refuses to overwrite an existing ZIP and verifies archive entries. Packaging does not establish gameplay acceptance or authorize publication. See [hosting](HOSTING.md) for direct-IP setup and [evaluation](EVALUATION.md) for review scenarios; older checkpoint records remain historical evidence.
