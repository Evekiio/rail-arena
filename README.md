![Rail Arena brand banner with four contender concepts](docs/branding/rail-arena-banner.png)

# Rail Arena

### One railgun. Every shot matters.

**An arena shooter built to make aim practice feel like play.**

Rail Arena is a Windows game and AI engineering portfolio project by **Sanders Riddle**, built with modern AI agentic workflows. The vision is a complete, focused aim trainer and arena game: sharpen your precision, learn to shoot while moving, and turn another practice session into one more match.

[Explore the vision](#the-game)  Ã‚Â·  [Build and play](docs/BUILD-AND-PLAY.md)  Ã‚Â·  [Character designs](docs/CHARACTER-REFERENCES.md)  Ã‚Â·  [Arena references](ArtSource/Original/Foundry-Arena-v2/References-v1/README.md)  Ã‚Â·  [Engineering evidence](docs/VALIDATION.md)

> **Pre-alpha client:** [Download for Windows x64](https://github.com/Evekiio/rail-arena/releases/download/v0.1.0-pre-alpha.1/RailArena-v0.1.0-pre-alpha.1-Windows-x64.zip) Ã‚Â· [Release notes and limitations](docs/PRE-ALPHA.md). Extract the whole ZIP, then run `RailArena.exe`. This is an unfinished portfolio build for playtesting; concept artwork and planned arena features are distinguished from the current client below.

## Approved design / next production milestone

The [design lock v1](docs/DESIGN.md) sets the next target: four contenders offline, two-human online duels, a 0.6-second semi-auto rifle, 75/40/30 location damage, five-minute or 20-kill matches, slow partial regeneration and one fixed portal pair. **These are approved requirements, not claims about the existing builds.**

The [design review package](ArtSource/Original/Design-v1/README.md) contains the shared headless Blender greybox, matching views, new concept sheets and a per-asset register. Detailed modeling requires creator approval; runtime migration and playtesting follow the [implementation gates](docs/DESIGN-IMPLEMENTATION.md). Existing gameplay summaries below describe prior milestones where they differ.

## The game

The experience below describes the approved design target. Existing builds still use earlier rules while migration is pending.

Aim is more interesting when the target shoots back.

Rail Arena puts precision inside a compact sci-fi arena: elevated platforms, momentum-driven movement, jump pads and a railgun that rewards a well-timed shot. Practice at your own pace, fight bots, or work toward a head-to-head duel where positioning matters as much as your crosshair.

The design draws on Quake III Arena's movement and Unreal Tournament's arena readability, with original characters, weapons and environments. Its identity is deliberately focused: **one weapon, a readable battlefield, and skill you can feel improving.**

| Experience | What it is for |
| --- | --- |
| **Solo practice** | Explore the arena, learn routes and work on movement with unlimited respawns and no opponents. |
| **Target practice** | Use stationary, moving or combat bot behavior to isolate precision or practice under pressure. |
| **Bot matches** | Approved target: face 1ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬ÃƒÂ¢Ã¢â€šÂ¬Ã…â€œ3 distinct bots across three difficulty levels, with scoring, results and restart. |
| **Direct-IP duel** | Host or join a two-human match, ready up, compete and rematch. Integration is implemented; full human and remote-network acceptance is still open. |

### Precision with consequences

- **Make the shot count.** Approved target: unlimited ammunition, one shot per click, a 0.6-second rail cycle and 75/40/30 head/body/limb damage.
- **Keep moving.** Ground acceleration, air control and momentum preservation support a movement-driven practice loop, without sprint or stamina.
- **Play the arena.** Ramps, gaps, four jump pads and contested health pickups create routes and decisions beyond holding an angle.
- **Stay in the loop.** Untimed practice supports repetition; five-minute or 20-kill matches add a countdown, scoreboard, results and tied-leader sudden death.

The goal is enjoyable, repeatable practice inside a real game. Claims about measurable aim improvement require player studies; this project does not yet establish training outcomes.

### A complete game with a deliberate boundary

The first-version target is **one polished original arena, one satisfying railgun, offline practice and bot combat with four total contenders, one fixed portal pair, and a complete two-human direct-IP duel loop**. Responsive controls, readable feedback, dependable shutdown and a clean path from launch to rematch are part of that target.

Cloud accounts, matchmaking services, progression economies, monetization, extra weapons, spectators and online bots are outside this version's scope. See the [approved gameplay and completion contract](docs/DESIGN.md).

## Designed to be recognizable

Ivory armor. Graphite silhouettes. Cyan energy. Distinct contenders built around a high-detail, low-poly visual direction.

![Approved concept designs for the player and three bots](ArtSource/Original/StylizedContenders/References-v1/Final/Approved-Concept.png)

| Contender | Visual identity | Reference sheet |
| --- | --- | --- |
| **Player** | Ivory armor, teal cowl and cyan accents | [Turnaround](ArtSource/Original/StylizedContenders/References-v1/Final/Player-Turnaround.png) |
| **Bot 01** | Terracotta armor and amber energy | [Turnaround](ArtSource/Original/StylizedContenders/References-v1/Final/Bot01-Turnaround.png) |
| **Bot 02** | Slate, ivory and cool teal | [Turnaround](ArtSource/Original/StylizedContenders/References-v1/Final/Bot02-Turnaround.png) |
| **Bot 03** | Plum armor and copper detailing | [Turnaround](ArtSource/Original/StylizedContenders/References-v1/Final/Bot03-Turnaround.png) |

[Explore all sixteen character sheets](docs/CHARACTER-REFERENCES.md), including head studies, equipment details and top/bottom concepts. These guide modeling; they are not screenshots or a promise of final runtime appearance. Newly extended hidden surfaces remain review candidates, and geometric consistency must be resolved in the shared 3D models.

The [brand kit](docs/branding/README.md) includes the transparent logo, banner, palette and usage notes.

## Building the world of Rail Arena

The current [Foundry arena specification](docs/ARENA-DESIGN.md) defines the duel-first layout, named zones, cover, spawn pockets, health, pad trajectories, opaque shot-blocking portals and environment kit. [Measured deck plans and placement data](ArtSource/Original/Foundry-Arena-v2/README.md) supersede the earlier blockout proposal. Layout review and live controller testing remain open.

A floating industrial arena above an ocean of clouds. Ivory structures, graphite frames, teal recesses and restrained cyan energy tie the battlefield to its contenders.

![Foundry arena environment concept with elevated wings and lower routes](ArtSource/Original/ArenaReferences-v1/01-Arena-Hero.png)

*Environment concept for review, not a gameplay screenshot or a validated competitive layout.*

The new **six-sheet environment pack** translates that direction into a Blender modeling brief:

| Reference | Design focus |
| --- | --- |
| [Arena overview](ArtSource/Original/ArenaReferences-v1/01-Arena-Hero.png) | Floating architecture, elevated wings and player-height atmosphere |
| [Layout proposal](ArtSource/Original/ArenaReferences-v1/02-Arena-Layout.png) | Routes, cover, deck levels, spawn candidates and sightlines |
| [Modular architecture](ArtSource/Original/ArenaReferences-v1/03-Modular-Architecture.png) | Floors, walls, ramps, bridge beams, cover and landing modules |
| [Environment objects](ArtSource/Original/ArenaReferences-v1/04-Environment-Props.png) | Equipment cabinets, crates, vents, lighting and health pedestals |
| [Jump pads and portal study](ArtSource/Original/ArenaReferences-v1/07-Traversal-Devices.png) | Pad construction and activation states; a historical portal study |
| [World and sky](ArtSource/Original/ArenaReferences-v1/08-World-Sky.png) | Distant floating islands, atmosphere and directional lighting studies |

The proposed blockout explores a 56 ÃƒÆ’Ã†â€™:  40 metre envelope, three height levels, paired routes, four jump pads, three health locations and eight spawn candidates. These are design targets for review. The images contain cross-view inconsistencies; the [modeling handoff](ArtSource/Original/ArenaReferences-v1/README.md) records them and defines which written constraints take precedence.

The [Foundry v2 reference pack](ArtSource/Original/Foundry-Arena-v2/References-v1/README.md) now provides an editable measured Blender master, 22 geometry views and eight environment concept sheets. These are review candidates. The next production step is actual-controller tests of movement, spawn exposure, bot navigation and sightlines before detailed art. One opaque, shot-blocking portal pair is now part of the approved target; it is not yet integrated into current gameplay. The sky studies are mood references, not seamless skybox textures.

The new rifle and combat/blood-effects reference sheets remain undelivered after image-service filtering. Existing weapon and feedback systems are documented separately; this pack adds no new runtime assets or mechanics. [Browse the full pack, prompts and validation manifest](ArtSource/Original/ArenaReferences-v1/README.md).

The approved [1.0m precision carbine](ArtSource/Original/PrecisionCarbine-Production-v1/README.md) now replaces the shared game weapon, including all four contenders and character-matched first-person forearms. The [production review package](ArtSource/Original/PrecisionCarbine-Production-v1/README.md) puts approved construction views beside final renders and installed Unity captures. It includes three LODs, a 2K surface atlas and reproducible headless builders. Fine visual acceptance and measured combat performance remain separate gates. [V3 references](ArtSource/Original/PrecisionCarbine-References-v3/README.md), [rifle v2](ArtSource/Original/RailRifle-Modeling-References-v2/README.md) and the [older benchmarks](ArtSource/Original/ProductionBenchmarks-v1/README.md) remain available as history.

## AI engineering, applied to a playable product

**Created by Sanders Riddle as a portfolio project in AI Solutions and AI Engineering.** Rail Arena explores how modern AI agents can help turn a creative brief into working software, editable assets and inspectable evidence.

The engineering challenge spans gameplay rules, real-time networking, asset pipelines, process lifecycle management and validation. The value of the workflow is in connecting those pieces into a maintainable product, then checking that the result behaves as intended.

| Workflow | How it appears in this project |
| --- | --- |
| **Define the contract** | Explicit gameplay rules, version pins, scope boundaries and completion criteria give agents concrete constraints. |
| **Implement and integrate** | Agent-assisted C# development and scripted asset pipelines connect simulation, bots, networking and presentation. |
| **Create and refine assets** | AI-generated concept references support visual exploration; editable Blender sources and Unity assets support implementation. Concept approval and model quality remain separate decisions. |
| **Verify with evidence** | Automated mechanics checks, standalone runners, network scenarios and build-specific reports make results reviewable. |
| **Keep the operator in control** | Project instructions require headless Unity work by default, permission before visible windows, bounded processes and cleanup. |

AI assistance is part of the development process. In-game opponents use configurable gameplay logic; they are not LLM-driven agents. Generated output is reviewed and tested, and automated checks do not substitute for human judgment about fun, feel or visual quality.

## Built for inspection

**Unity 6000.6.7f1  Ã‚Â·  URP 17.6.0  Ã‚Â·  C#  Ã‚Â·  Windows keyboard and mouse  Ã‚Â·  Git LFS**

The gameplay core runs at **60 ticks per second**. Offline play uses local authority; online duels use host authority with client prediction, reconciliation, opponent interpolation and bounded hit rewind up to 200 ms.

```mermaid
flowchart LR
    Human[Player input] --> Simulation[Shared gameplay simulation]
    Bots[Offline bot decisions] --> Simulation
    Network[Direct-IP duel inputs] --> Simulation
    Simulation --> Rules[Movement, hits, health and scoring]
    Rules --> State[Authoritative state]
    State --> View[Characters, weapon, HUD and audio]
    State --> Client[Client reconciliation and interpolation]
```

Gameplay authority determines damage and movement. Presentation observes state. Keeping those responsibilities separate makes rules easier to test and allows practice and competitive modes to share the same foundation.

Explore the [implementation record](docs/IMPLEMENTATION.md), [networking](docs/NETWORKING.md), [bot tuning](docs/BOT-TUNING.md), [weapon pipeline](docs/WEAPONS.md) and [combat feedback](docs/COMBAT-FEEDBACK.md).

### What the evidence establishes

Automated coverage includes simulation rules, standalone combat mechanics, bot scenarios, replicated respawn behavior, local duel/rematch scenarios and simulated network impairment. Reports identify the builds and conditions they cover.

The remaining quality gates include physical keyboard/mouse feel, sound-on review, final art and animation polish, native two-human play from hosting through rematch, and a duel across real internet connections. Some impaired-network runs exposed large client corrections under concurrent import/build load; smooth multiplayer remains a quality target.

**Passing tests is evidence of specific behavior, not a finished-game claim.** Read the [validation record](docs/VALIDATION.md), [acceptance matrix](docs/ACCEPTANCE.md) and [native input review](docs/NATIVE-INPUT-REVIEW.md) for results and limits.

## Build it. Play it. Inspect it.

The repository contains the editable Unity project and source assets. Windows builds are currently local and are not included as public release downloads.

```powershell
git lfs install
git clone https://github.com/Evekiio/rail-arena.git
cd rail-arena
git lfs pull
```

Use **Unity 6000.6.7f1** with Windows build support and open the `RailArena` subfolder. Unity licensing and the initial package restore are required for development. Follow the [build and play guide](docs/BUILD-AND-PLAY.md) for exact steps, or the [evaluation guide](docs/EVALUATION.md) for a focused playtest. Automation should follow the repository's [headless execution instructions](AGENTS.md).

On the development PC, the non-development evaluation player is `Builds/Evaluation/RailArena.exe`; keep its adjacent data and DLLs together. Choose **PRACTICE** for solo or bot training, **OFFLINE BOT MATCH** for a timed session, or **ONLINE DUEL // DIRECT IP** for host/join. Offline play needs no account or external service. [Direct-IP setup](docs/HOSTING.md) covers ports, firewall and hosting requirements.

| Control | Action |
| --- | --- |
| W A S D | Move |
| Mouse | Aim |
| Left mouse | Fire |
| Space | Jump; hold to keep hopping |
| Tab | Hold scoreboard |
| Escape | Pause and settings |

Choose **QUIT** to exit. **MAIN MENU** and **DISCONNECT** return to menus while keeping the application open. See [shutdown behavior](docs/SHUTDOWN.md) for lifecycle checks and runner safeguards.

## Project notes

- **Creator:** Sanders Riddle
- **Portfolio focus:** AI Solutions / AI Engineering, agentic development workflows, gameplay systems and reproducible validation
- **Status:** Active development toward a complete Windows aim trainer and arena game
- **Source:** [Evekiio/rail-arena](https://github.com/Evekiio/rail-arena)
- **License:** Original project code is [MIT licensed](LICENSE); see [third-party notices](THIRD-PARTY-NOTICES.md) for assets and dependencies.

Rail Arena uses original project assets and documented dependencies. No Quake or Unreal maps, characters, sounds, branding or code are bundled.
