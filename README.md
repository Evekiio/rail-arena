# Rail Arena

An original railgun arena shooter for Windows, inspired by Quake III Arena's movement and Unreal Tournament's readable arenas and bot combat.

**Under development. The offline gameplay milestone is available locally; this is not the complete game or a published release.**

The target is immediate offline bot matches and practice, plus direct-IP two-human duels. The complete scope and acceptance criteria are in [the objective](docs/OBJECTIVE.md).

## Project

- Unity **6000.6.7f1**, pinned in `RailArena/ProjectSettings/ProjectVersion.txt`.
- Universal Render Pipeline **17.6.0**.
- C# gameplay simulation at **60 ticks per second**.
- Local Git with LFS for binary artwork and audio.

Open the `RailArena` folder in the exact Editor version. Package versions are recorded in the manifest and lockfile. Unity licensing and the first package restore are required for development. The intended offline game will not require an account or external service.

## Current implementation

The editable Foundry arena now includes a lower loop, elevated bridge and wings, four ramps, four launch pads, three health pickups and eight spawn positions. Offline bot match and untimed practice use the same local 60 Hz authority, with configurable bot count, three difficulty levels and stationary, moving or combat targets.

The first gameplay pass includes location-based hitboxes, a rail cooldown, deaths and respawns, health restoration, countdown, time limit, void scoring and tied-leader sudden death. Menus, a HUD, scoreboard, results, restart and basic settings are implemented. Characters now use a textured armored humanoid, three mesh LODs, four palette variants, retargeted locomotion/jump/landing/hit/death animations and two-hand aiming IK. See [character sources, editing and limitations](docs/CHARACTERS.md). Weapon, sound and arena presentation still need polish. **Online duel, prediction, reconciliation, interpolation and hit rewind are not implemented yet.**

The direct-IP networking core has separately tested UDP connection setup, compatibility checks, ready state, bounded command/snapshot packets, input deduplication and disconnect handling. It is not connected to the playable session yet. See [networking coverage and remaining integration](docs/NETWORKING.md).

## Play the local build

Launch `Builds/Windows/RailArena.exe` from the workspace. Choose **OFFLINE BOT MATCH** or **PRACTICE**. Set bot count, difficulty and target behavior before starting. During a session, press Escape to pause; changing the configuration and choosing **RESTART SESSION** applies it.

| Control | Action |
| --- | --- |
| W A S D | Move |
| Mouse | Aim |
| Left mouse | Fire railgun |
| Space | Jump; hold to keep hopping |
| Tab | Hold scoreboard |
| Escape | Pause and settings |

No sprint, stamina, armor or ammo pickups are used. Health is 100; headshots deal 100, body hits 50 and limb hits 34. Health pickups restore 25 and return after 20 seconds. The rail cycles every 1.5 seconds.

## Tests

With the Unity CLI installed:

```powershell
unity test .\RailArena --mode EditMode --filter RailArena.Tests --output .\Validation\editmode.xml
```

Passing simulation tests alone does not establish playability, movement fidelity, bot quality or multiplayer reliability. Standalone, visual and simulated-network validation remain required.

For the development Windows build, run `tools/Test-Offline.ps1` to execute a repeatable 45-second local combat smoke test with seven bots and an automated human input driver. It saves counters, errors, void traces and frame timing. This automation does not validate human input feel or menu clicks. Hidden-window frame timing is not a visible-window rendering benchmark.

Build the current saved scene through the running pinned Editor:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli --timeout 600 -- --code 'RailArena.Editor.ArenaBuilder.BuildWindows();'
```

The scene and editable tuning live under `Assets/RailArena/Generated`. `Rail Arena > Build Foundry scene` regenerates these generated assets from the builder; manual changes to generated assets should be preserved separately before regeneration. Hosting and firewall instructions will accompany the online implementation.

## License

Original project code is MIT licensed. See [LICENSE](LICENSE) and [third-party notices](THIRD-PARTY-NOTICES.md). Inspiration references are documented in [the implementation record](docs/IMPLEMENTATION.md). No Quake or Unreal maps, characters, sounds, branding or code are bundled.
