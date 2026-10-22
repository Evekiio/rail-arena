# Rail Arena

An original railgun arena shooter for Windows, inspired by Quake III Arena's movement and Unreal Tournament's readable arenas and bot combat.

**Under development. Offline play and direct-IP duel builds are available locally; this is not the complete game or a published release.**

The target is immediate offline bot matches and practice, plus direct-IP two-human duels. The complete scope and acceptance criteria are in [the objective](docs/OBJECTIVE.md).

Public source: [Evekiio/rail-arena](https://github.com/Evekiio/rail-arena). Tested development milestones are pushed as work progresses; a finished downloadable release is not available yet.

![First-person armored arms and RA-01 railgun in Foundry](RailArena/Assets/Validation/view-arms-first-person.png)

*Actual Unity Editor camera capture with the first arm-only view rig. Finger/action-animation and final arena polish remain unfinished; this is not a standalone or native-input acceptance screenshot.*

## Project

- Unity **6000.6.7f1**, pinned in `RailArena/ProjectSettings/ProjectVersion.txt`.
- Universal Render Pipeline **17.6.0**.
- C# gameplay simulation at **60 ticks per second**.
- Local Git with LFS for binary artwork and audio.

Open the `RailArena` folder in the exact Editor version. Package versions are recorded in the manifest and lockfile. Unity licensing and the first package restore are required for development. The intended offline game will not require an account or external service.

Clone with Git LFS installed so the binary models, textures and animation sources are restored:

```powershell
git lfs install
git clone https://github.com/Evekiio/rail-arena.git
cd rail-arena
git lfs pull
```

## Current implementation

The editable Foundry arena now includes a lower loop, elevated bridge and wings, four ramps, four launch pads, three health pickups and eight spawn positions. Offline bot match and untimed practice use the same local 60 Hz authority, with configurable bot count, three difficulty levels and stationary, moving or combat targets.

The first [arena surface-art pass](docs/ARENA-ART.md) adds original panel textures, beveled covers, vents, warning markings and directional launch fields while preserving the collider/navigation signature.

The first gameplay pass includes location-based hitboxes, a rail cooldown, deaths and respawns, health restoration, countdown, time limit, void scoring and tied-leader sudden death. Menus, a HUD, scoreboard, results, restart and basic settings are implemented. Characters now use a textured armored humanoid, three mesh LODs, four palette variants, retargeted locomotion/jump/landing/hit/death animations and two-hand aiming IK. See [character sources, editing and limitations](docs/CHARACTERS.md). The original [RA-01 railgun](docs/WEAPONS.md) has editable Blender source, two mesh LODs and cooldown-driven feedback. [First-person armored arms](docs/FIRST-PERSON-ARMS.md) now share its grips and recoil. Hand/action-animation, sound and arena polish remain. **The full online acceptance criteria remain incomplete.**

Initial direct-IP duel integration is now available from **ONLINE DUEL // DIRECT IP**. Host and join use the same build and selected UDP port, then both players ready up. The host owns gameplay, while the client predicts/reconciles movement and interpolates the opponent. Bounded 200 ms hit rewind is implemented and covered by historical geometry tests and a two-player standalone smoke test. Complete match/rematch acceptance, impairment testing and remote-network verification remain incomplete. See [networking coverage and remaining integration](docs/NETWORKING.md).

## Validation snapshot

| Evidence | Scope |
| --- | --- |
| 46 Editor-suite tests passing | Shared simulation, model geometry, live player-loop view-arm IK, bot profiles/behavior, protocol, rewind and camera continuity |
| 35 standalone mechanics checks passing | Accelerated real authority with actual hurtboxes; exact combat, pickup, respawn and match-clock boundaries, plus solo movement/fire/respawn and mode switching |
| Full ten-minute network-candidate match and rematch | Two local processes under configured delay/jitter/5% loss; historical fingerprint, not the final art build |
| Latest corrected-hitbox network smoke | Real UDP, 60 seconds, simulated delay/loss, zero application errors |
| Human acceptance still open | Keyboard/mouse feel, settings, visible and sound-on quality, native two-human duel, final build and real internet path |

Reports identify their build and limitations. See [the acceptance matrix](docs/ACCEPTANCE.md), [validation record](docs/VALIDATION.md), [bot checks](docs/BOT-VALIDATION.md) and [direct-IP hosting guide](docs/HOSTING.md).

## Play the local build

Launch `Builds/Windows/RailArena.exe` from the workspace. Choose **OFFLINE BOT MATCH** or **PRACTICE**. Set bot count, difficulty and target behavior before starting. During a session, press Escape to pause; changing the configuration and choosing **RESTART SESSION** applies it.

For movement and arena testing alone, cycle **BOTS** to **0 // SOLO**, then choose **SOLO PRACTICE // UNTIMED**. It has no opponents or network connection. Movement, rail firing, pickups, jump pads, void scoring and unlimited respawns use the normal authority. Timed bot matches require at least one bot. Solo practice can restart with bots after changing the count in the pause menu.

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

The same runner accepts `-Duration 600 -Difficulty 0 -Bots 7` for a ten-minute easy session; difficulty values 1 and 2 select medium and hard. Per-actor route and sampled stall measurements are described in [bot validation](docs/BOT-VALIDATION.md).

Bot reaction, aim and decision profiles are editable in `Assets/Resources/BotTuning.asset`; restart the session to apply changes. See [bot tuning](docs/BOT-TUNING.md) for units, preserved defaults and validation.

Run `tools/Test-Mechanics.ps1` for 35 accelerated standalone authority checks covering actual hurtbox damage, held-fire cooldowns, pickups, respawns, void scoring, match clocks, sudden death, untimed practice and solo-to-bot mode switching. See [scenario scope and the bone-scale regression it discovered](docs/MECHANICS-VALIDATION.md). These checks do not substitute for native playtesting or real-time multiplayer.

Build the current saved scene through the running pinned Editor:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli --timeout 600 -- --code 'RailArena.Editor.ArenaBuilder.BuildWindows();'
```

The scene and editable tuning live under `Assets/RailArena/Generated`. `Rail Arena > Build Foundry scene` regenerates these generated assets from the builder; manual changes to generated assets should be preserved separately before regeneration. See the [direct-IP hosting, firewall and port-forwarding guide](docs/HOSTING.md) for manual connection steps and the remaining remote-network acceptance.

## License

Original project code is MIT licensed. See [LICENSE](LICENSE) and [third-party notices](THIRD-PARTY-NOTICES.md). Inspiration references are documented in [the implementation record](docs/IMPLEMENTATION.md). No Quake or Unreal maps, characters, sounds, branding or code are bundled.
