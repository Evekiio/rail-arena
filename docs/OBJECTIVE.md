# Historical implementation objective

The approved [design lock v1](DESIGN.md) supersedes conflicting rules below, including firing, damage, match length, roster cap, regeneration and portals. This original objective is retained as milestone history; it does not describe the new target or prove current-build compliance. See [migration and validation gates](DESIGN-IMPLEMENTATION.md).

Build the first complete, working, testable version of Rail Arena in F:\Rail Arena, using Unity 6.6 (6000.6.7f1), URP, and Windows keyboard/mouse controls. Pin this exact Editor version.
Creative direction
Understand Quake III Arena and Unreal Tournament as the primary inspirations. Research their arena combat, movement, map flow, verticality, weapon feedback, pickups, bot matches, and match presentation using authoritative sources before implementing the relevant systems.
Capture their fast, readable, skill-driven arena-shooter experience while giving Rail Arena its own identity. Use Quake III as the primary movement reference and Unreal Tournament as an additional reference for arena readability, combat feedback, and satisfying bot play. Do not automatically add their other weapons or movement abilities. Use original or appropriately licensed assets rather than copying their maps, characters, sounds, branding, or code.
Playable modes
- Offline bot match: Start directly from the main menu without accounts, internet access, or a second player. Support one human against 1–7 configurable bots, with three difficulty levels.
- Practice: Offer an untimed bot session with unlimited respawns so I can run around, shoot bots, and test movement, aiming, damage, pickups, and jump pads. Allow restarting the session and changing bot count or difficulty through the pause menu.
- Solo practice: As requested during development, support zero bots for movement, physics and arena testing without opponents, retaining the same gameplay authority and unlimited respawns.
- Online duel: Two humans can host and join directly by IP and configurable port, ready up, play a complete duel, view results, and rematch.
Gameplay requirements
- Quake-style ground acceleration, strafe jumping, bunny hopping, air acceleration, and momentum preservation. No sprint or stamina.
- Railgun-only combat with unlimited ammunition and a 1.5-second firing interval.
- Shared 100 health: lethal headshots, 50 damage for body hits, and 34 for limb hits. Mixed hits combine.
- Contested health pickups restore 25 health, capped at 100, and respawn after 20 seconds. No regeneration or armor.
- Timed matches last ten minutes, with a three-second starting countdown, two-second respawns, and next-kill sudden death between tied leaders. Void deaths subtract one point.
- One polished, original retro sci-fi floating arena inspired by the supplied reference, with elevated platforms, ramps, gaps, and four jump pads. Make it suitable for both duels and bot practice.
- Readable player models and hitboxes, satisfying railgun effects and sound, health and cooldown displays, crosshair, score, timer, kill feed, scoreboard, results, and rematch controls.
- Settings for mouse sensitivity, volume, resolution, and fullscreen.
Bot requirements
- Bots navigate the arena, traverse ramps and jump pads, acquire targets through line of sight, move during combat, fire the railgun, seek health, die, and respawn.
- Bots obey the same movement, health, damage, weapon cooldown, pickup, and scoring rules as humans.
- Difficulty changes reaction time, aiming error, and decision quality. Bots must not track players through walls, fire with perfect instantaneous aim, or receive hidden damage or health advantages.
- Use the same gameplay simulation for offline and online modes. Offline play must work without connecting to an external service.
- Include repeatable test scenarios for stationary targets, moving targets, and normal combat so mechanics can be checked independently of bot difficulty.
Engineering requirements
- Host-authoritative movement, shooting, damage, pickups, scoring, and match state at 60 ticks per second. Offline sessions run authority locally.
- Client movement prediction, host reconciliation, remote-player interpolation, and validated lag-compensated hits with up to 200 ms rewind.
- Clear connection errors, build compatibility checks, and graceful disconnect handling.
- Maintainable C# architecture, editable gameplay and bot tuning, pinned dependencies, local Git and LFS, and complete Unity asset metadata.
- Prioritize a working offline gameplay loop early, then complete multiplayer and presentation. Neither mode substitutes for the other in the final deliverable.
- No spectators, cloud services, monetization, additional weapons, or online bot support in this first version.
Open-source delivery
Create and publish the public repository Evekiio/rail-arena with an MIT license for original project code and clear notices for third-party assets and dependencies. Include a portfolio-ready README with real screenshots, inspirations, features, architecture, controls, known limitations, and reproducible build, offline-play, and hosting instructions, including firewall and port-forwarding setup.
Completion criteria
Continue through implementation, debugging, automated tests, build verification, visual inspection, offline bot playtesting, and two-client standalone multiplayer testing.
Verify movement, location-based damage, firing cadence, pickups, jump pads, bot navigation and difficulty, respawns, scoring, sudden death, disconnects, and rematches. Confirm bots can repeatedly traverse the arena and sustain combat without routinely becoming stuck or falling into the void. Test multiplayer under simulated latency and packet loss, and record performance on the available machine.
Deliver the editable project, runnable Windows build, published source repository, and an accurate validation report. Clearly distinguish local and simulated-network validation from internet multiplayer, which requires an actual remote-network test. Identify any required credentials, Unity licensing, or network setup precisely.
Do not declare completion based on scaffolding, documentation, or an untested build. Completion means I can launch Rail Arena, immediately run around fighting bots, and also play a complete human duel from hosting through rematch.
