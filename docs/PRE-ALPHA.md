# Rail Arena pre-alpha 0.1.0-pre-alpha.1

[Download the Windows x64 client](https://github.com/Evekiio/rail-arena/releases/download/v0.1.0-pre-alpha.1/RailArena-v0.1.0-pre-alpha.1-Windows-x64.zip) | [GitHub release](https://github.com/Evekiio/rail-arena/releases/tag/v0.1.0-pre-alpha.1)

A playable, unfinished FPS / aim-training portfolio project by Sanders Riddle. This pre-alpha introduces the approved precision carbine and the four stylized contenders. It is a playtest checkpoint, not a completed game or performance-certified release.

## Run

1. Download the ZIP and extract **all** files into one folder.
2. Run `RailArena.exe`; keep `RailArena_Data`, DLLs and the other adjacent files together.
3. Choose solo practice or start with three combat bots. Keyboard and mouse are required. No Unity installation, account or internet connection is needed for offline play.

| Control | Action |
| --- | --- |
| WASD | Move |
| Mouse | Aim |
| Left click | Fire one shot |
| Hold right mouse | 1.5x zoom |
| Space, held | Jump / hop |
| Tab, held | Scoreboard |
| Escape | Pause / settings |

The rifle has unlimited energy, no reload and a **0.6-second recovery**. Holding fire does not repeat shots; clicks during recovery are discarded. Current damage is head 100, body 50, limb 34. The broader approved 75/40/30 damage and other design-rule migrations are still pending.

## Included now

- Original 1m ivory/graphite/cyan carbine, editable Blender master, three LODs, 2K surface atlas, fitted hands, shoulder contact and character-matched forearms.
- Player and three stylized bot appearances, offline practice, stationary/moving targets and bot combat. Use three bots for the intended four-contender scope; older menus still expose larger counts.
- Existing playable Foundry arena, momentum movement, ramps, four jump pads, health pickups, respawning, ten-minute timed matches, results and rematch flow.
- Two-human direct-IP duel implementation with host authority, prediction, reconciliation and bounded hit rewind. Both peers must use the same build. Automated evidence covers loopback peers, not acceptance over a real internet connection.
- Windows keyboard/mouse client, pause/settings, scoreboard, shot/hit feedback and dependable exit controls.

## Known limits

The new 56 x 40m competitive-map design, opaque portal pair, regeneration, five-minute/20-kill rules, broader settings and final character/arena polish remain separate design and implementation work. Their reference artwork does not mean those systems are installed in this client.

Fine hand/skin intersections, animation transitions, sound-on quality, physical-input feel and external-peer multiplayer need human acceptance. The 1080p/144fps target is not certified. Headless checks do not establish rendering performance or a finished competitive balance.

## Validation

The pinned Unity version is **6000.6.7f1**. See [release evidence](../Validation/PreAlpha/release-validation.json), [carbine validation](../ArtSource/Original/PrecisionCarbine-Production-v1/manifest.json) and [validation history](VALIDATION.md). The portable archive includes a per-file SHA-256 manifest, licensing notices and launch instructions. The GitHub release includes a separate SHA-256 checksum file.

Report mode, bot count, difficulty, exact steps, expected/actual behavior and the build version with a bug. Player logs are normally under `%USERPROFILE%\AppData\LocalLow\Evekiio\Rail Arena\Player.log`.

[Approved design](DESIGN.md) | [Production carbine comparison gallery](../ArtSource/Original/PrecisionCarbine-Production-v1/index.html) | [Build instructions](BUILD-AND-PLAY.md)
