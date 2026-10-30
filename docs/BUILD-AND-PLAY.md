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

For the integrated Sky Gardens art version, use the production art builder. It prepares the versioned art assets, installs the presentation into the built basement scene, uses the production atmosphere pipeline and includes the opt-in mechanics, bots, duel and art-performance runners:

```powershell
$env:RA_ART_BUILD_OUTPUT = (Join-Path $PWD 'Builds/RailArena-ArtReview-New')
unity run .\RailArena --editor-path 'D:\Unity Editor\6000.6.7f1\Editor\Unity.exe' --timeout 1800 --no-tail --format json -- -job-worker-count 4 -executeMethod RailArena.Editor.ArtSliceBuilder.Build
```

Choose a new output directory each time. The builder refuses to overwrite an existing folder and restores temporary render-pipeline settings and source identity after building. The package records its actual pipeline configuration and exact input list. A clean source checkout may reuse derived shader-cache artifacts from the same Unity version. Final public release remains held for Sanders' playtest acceptance.

The following baseline builder retains the earlier presentation for comparison; it does not install the integrated art pass.

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
python tools/verify_build_identity.py --project RailArena --inputs Validation/build-identity-inputs.txt
```

Unity writes the exact dependency manifest during identity generation. Active scene dependencies under `Assets/Validation` must be included; inactive pilots are excluded. For an art review build whose source identity is restored after building, use the package's `BuildInputs.txt` with `--inputs` and `BuildIdentity.txt` with `--identity`, against its clean source checkout. This checks source compatibility, not human gameplay quality. Reports in [Validation](../Validation) identify the conditions tested. Native input, sound, real-internet duels and measured four-contender performance remain separate gates.

## Portable packaging

Use the fingerprint belonging to the build being packaged, not newer edited source:

```powershell
python tools/package_playtest.py --build Builds/Windows --output Builds/Packages/MyPlaytest.zip --fingerprint <recorded-build-fingerprint>
```

Replace the placeholder before running. The packager refuses to overwrite an existing ZIP and verifies archive entries. Packaging does not establish gameplay acceptance or authorize publication. See [hosting](HOSTING.md) for direct-IP setup and [evaluation](EVALUATION.md) for review scenarios; older checkpoint records remain historical evidence.

## October 29 ADS and movement playtest

The local physical ADS/sprint/walk/crouch player is `Builds/Competitive-ADS-2026-10-29/RailArena.exe`. Keep all adjacent files together. Hold right mouse for ADS, left Shift to sprint forward, left Alt to walk and left Ctrl to crouch. See [behavior, compatibility and validation](ADS-AND-MOVEMENT.md). This separate local build does not update the public download.

## Branded menu and HUD review build

Launch `Builds/Branded-UI-2026-10-29/RailArena.exe` for the new menu/HUD pass. Existing ADS and baseline builds remain separate. The [interface specification](branding/INTERFACE.md) records controls, assets, accessibility/validation scope and known acceptance limits. This local player does not update the public download.

## Sprint-slide local review

The new Windows review player is `F:\Rail Arena\Builds\Sprint-Slide-2026-10-29\RailArena.exe`. Run it with all adjacent files intact. Hold Shift while moving forward, then press Ctrl to slide. Hold Ctrl to remain crouched. The new protocol 6 requires this same build on both duel peers. Earlier baseline, ADS and branded UI build folders remain separate. See [SLIDE.md](SLIDE.md) for mechanics and validation.

## Automatic-rifle local review

Run `F:\Rail Arena\Builds\Assault-Rifle-2026-10-29\RailArena.exe` with all adjacent files intact. Hold left mouse for 600 RPM automatic fire; release to stop. ADS remains on right mouse, with reduced visual recoil. This protocol 7 build must be used on both duel peers. Prior sprint-slide and branded UI builds remain separate. See [AUTOMATIC-RIFLE.md](AUTOMATIC-RIFLE.md) for mechanics and evidence.

The slower 300 RPM player is `F:\Rail Arena\Builds\Assault-Rifle-300RPM-2026-10-29\RailArena.exe`. This changes the shared interval to 0.2 seconds and retains the recoil animation. No tests/player checks were run for this rate-only adjustment.

Current magazine build: 250 RPM, 30 rounds, R reload (2 seconds), unlimited reserve, protocol 8. See [magazine controls and state](MAGAZINES.md). Local player: `F:\Rail Arena\Builds\Magazines-250RPM-2026-10-29\RailArena.exe`. Earlier fire-rate/build qualification remains specific to those preceding players.

## Twin Platforms local review

Run `F:\Rail Arena\Builds\TwinPlatforms-2026-10-29\RailArena.exe` with all adjacent files intact. Both colored gates work in both directions; walk onto a gold pad to jump the gap. Use the same protocol-9 build on both duel peers. See [TWIN-PLATFORMS.md](TWIN-PLATFORMS.md). Earlier builds remain separate.

## Sky Gardens / Physics Lab playtest

Run `Builds/SkyGardens-PhysicsLab-2026-10-29/RailArena.exe`. The approved Foundry v2 map includes four summit pads and live portals. Press **G** for the offline movement/physics lab; use **SAVE PNG + JSON** to share exact settings. See [Physics Lab](PHYSICS-LAB.md) and [Sky Gardens](SKY-GARDENS-PLAYABLE.md).
