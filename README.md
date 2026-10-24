![Rail Arena brand banner with contender concepts](docs/branding/rail-arena-banner.png)

# Rail Arena

### One railgun. Every shot matters.

**Aim practice with a reason to play one more match.**

Rail Arena is a compact Windows FPS by **Sanders Riddle**, built as an **AI Solutions / AI Engineering portfolio project**. Land a precise shot, keep your momentum, and find a better angle. One weapon and a readable arena keep the focus on aim, movement and positioning.

[Download pre-alpha](https://github.com/Evekiio/rail-arena/releases/tag/v0.1.0-pre-alpha.1) | [Build and play](docs/BUILD-AND-PLAY.md) | [Design](docs/DESIGN.md) | [Validation](docs/VALIDATION.md)

> **Active pre-alpha development.** The public Windows ZIP is an older playable checkpoint. `main` contains newer improvements. Approved concepts describe the production target; they do not establish completed integration, competitive balance or release readiness.

## Pick your practice

| Mode in current source | The loop |
| --- | --- |
| **Solo practice** | Explore, learn routes and repeat movement without a timer or opponents. |
| **Target practice** | Work on stationary or moving targets, or practice against combat bots. |
| **Offline bot match** | Choose an appearance and face up to three distinct bots across three difficulty profiles. Compete, review your results and rematch. |
| **Direct-IP duel** | Host or join a two-human ready-room match. Both peers need the same build; human and real-internet acceptance remain open. |

The current rifle fires one instant-hit shot per click, with **0.6-second recovery**, unlimited energy and **1.5x hold-to-zoom**. Momentum movement, hold-to-hop, ramps, four jump pads and contested health pickups give every shot a positioning decision. There is no reload, sprint or stamina system.

Current source uses 100 health, 100/50/34 head/body/limb damage, two-second respawns and ten-minute matches with tied-leader sudden death. The approved next rules differ; see the migration boundary below.

## Recent polish on main

| Area | What changed | Evidence and details |
| --- | --- | --- |
| **Control and visibility** | Independent zoom sensitivity, vertical FOV, invert Y, saved reticle size/color, reduced flashes, optional steady weapon and saved VSync/FPS limits | [Aim settings](docs/AIM-SETTINGS.md), [reticle](docs/RETICLE-SETTINGS.md), [comfort](docs/VISUAL-COMFORT.md), [frame pacing](docs/FRAME-PACING.md) |
| **Combat feedback** | Live/final statistics, a temporary corner elimination feed and readable respawn status during sudden death | [Statistics](docs/COMBAT-STATISTICS.md), [recent events](docs/COMBAT-FEED.md) |
| **Getting into a match** | Four selectable appearances, unique offline rosters, optional quick-start help and recoverable duel-form errors | [Roster](docs/OFFLINE-ROSTER.md), [help](docs/QUICK-START.md), [duel setup](docs/DUEL-FORM.md) |
| **Arena readability** | Daylight/cloud presentation, amber health beacons, corrected pad arrows and marked landing areas | [Lighting](docs/FOUNDRY-LIGHTING.md), [health](docs/HEALTH-BEACON.md), [traversal](docs/TRAVERSAL-CUES.md) |

These links include scoped validation and visual review captures. Controlled headless UI captures are labeled separately from gameplay renders. The public ZIP has not been rebuilt for these source updates.

## An original visual identity

Ivory ceramic, graphite frames, teal cloth and restrained cyan energy connect the contenders, carbine and floating Foundry.

![Approved contender concept artwork, not a runtime screenshot](ArtSource/Original/StylizedContenders/References-v1/Final/Approved-Concept.png)

*Approved character concepts. Current models and final visual acceptance are separate from the artwork.*

| Asset family | Current status | Review material |
| --- | --- | --- |
| **Four contenders** | Stylized appearances integrated; detailed art, animation and silhouette review remain open | [Character references](docs/CHARACTER-REFERENCES.md), [model notes](docs/STYLIZED-CONTENDERS.md) |
| **Precision carbine** | Shared replacement integrated with character-matched forearms, hand/shoulder fitting, three LODs and a 2K atlas | [Production review](ArtSource/Original/PrecisionCarbine-Production-v1/README.md) |
| **Playable Foundry** | Existing gameplay geometry with lighting, pickup and traversal presentation improvements | [Current lighting](docs/FOUNDRY-LIGHTING.md) |
| **Foundry v2** | Duel-first 56 x 40 m proposal with measured plans and concept references; runtime replacement and route qualification pending | [Arena specification](docs/ARENA-DESIGN.md), [reference package](ArtSource/Original/Foundry-Arena-v2/References-v1/README.md) |
| **Brand** | Editable project identity and usage guidance | [Brand kit](docs/branding/README.md) |

Historical artwork remains available for provenance. The [arena specification](docs/ARENA-DESIGN.md) and its measured placement data take precedence over conflicting early concept layouts.

## A focused production target

The intended first version is **one polished original arena, one satisfying rifle, offline practice and bot combat, and two-human direct-IP duels**. The Foundry is a floating industrial arena above bright clouds, with lower circulation, elevated wings and a central bridge. Its layout favors flowing routes with deliberate exposure.

The [approved design](docs/DESIGN.md) calls for five-minute or 20-kill matches, 75/40/30 damage, delayed partial regeneration and exactly one opaque, shot-blocking portal pair. **Those rule changes, portals and the new arena are not yet integrated into current gameplay.** [Implementation gates](docs/DESIGN-IMPLEMENTATION.md) separate design lock, layout resolution, concept approval, modeling and qualification.

Cloud accounts, matchmaking services, progression economies, monetization, extra weapons, spectators and online bots are outside this version. Measurable aim improvement requires player studies; automated tests cannot establish training outcomes.

## AI engineering through a playable product

Rail Arena demonstrates Sanders Riddle's work across gameplay, real-time networking, editable asset pipelines and reproducible validation using modern AI agentic workflows.

- **Explicit contracts:** pinned tools, shared gameplay rules and documented approval gates.
- **Inspectable implementation:** C# simulation, configurable bot logic and host-authoritative duels with prediction, reconciliation, interpolation and bounded hit rewind.
- **Editable creative work:** AI-assisted concepts support exploration; Blender masters, export scripts and Unity assets support production.
- **Scoped verification:** relevant tests and build-specific reports support each change, with screenshots for visual work and documented limits.
- **Operator control:** Blender and Unity automation stays headless, with bounded jobs, owned-process cleanup and permission before visible windows.

The in-game bots use gameplay logic, not LLM calls. Human judgment still decides whether movement, sound, art and combat feel good.

## Build, play and inspect

**Unity 6000.6.7f1 | URP 17.6.0 | C# | Windows x64 | Keyboard and mouse | Git LFS**

```powershell
git lfs install
git clone https://github.com/Evekiio/rail-arena.git
cd rail-arena
git lfs pull
```

For the existing client, [download the pre-alpha ZIP](https://github.com/Evekiio/rail-arena/releases/tag/v0.1.0-pre-alpha.1), extract every file and run `RailArena.exe`. Offline play needs no Unity installation, account or network connection. Read its [checkpoint-specific limitations](docs/PRE-ALPHA.md).

For current source, follow the [build guide](docs/BUILD-AND-PLAY.md). Keep the executable, data folder and DLLs together. [Direct-IP hosting](docs/HOSTING.md) covers ports and firewall setup.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / aim |
| Left click | Fire once |
| Hold right mouse | 1.5x zoom |
| Space, held | Jump / keep hopping |
| Tab, held | Standings |
| Escape | Pause / settings |

Choose **QUIT** to exit; **MAIN MENU** and **DISCONNECT** keep the application open. [Shutdown notes](docs/SHUTDOWN.md) explain lifecycle safeguards.

## What remains to prove

Physical keyboard/mouse feel, sound-on review, final art and animation acceptance, human host-to-rematch play and real-internet duels remain open. The **1080p / 144 fps goal** needs documented reference-PC measurements during four-contender combat. Hidden-window checks and render captures do not certify it.

[Validation record](docs/VALIDATION.md) | [Acceptance matrix](docs/ACCEPTANCE.md) | [Networking](docs/NETWORKING.md) | [Weapon pipeline](docs/WEAPONS.md) | [Implementation](docs/IMPLEMENTATION.md)

Original project code is [MIT licensed](LICENSE). See [third-party notices](THIRD-PARTY-NOTICES.md) for asset/dependency terms. Quake III Arena and Unreal Tournament inform the genre direction; their maps, characters, sounds, branding and code are not bundled.
