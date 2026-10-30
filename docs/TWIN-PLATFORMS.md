# Twin Platforms traversal layout

This is a separate playable layout requested after the existing Foundry and magazine builds. It does not replace or claim approval of the earlier Foundry-v2 art/design proposal. The original Foundry.unity and earlier local players are retained.

## Layout and controls

Two 32 x 36 metre playable decks face across a 36 metre clear void. Blue is west, red is east. Each has cover, a health pickup, a portal and a super jump pad. Distant islands remain cosmetic backdrop, not extra playable decks. There is no bridge or walkable geometry across the gap.

- Enter the lit front of the blue gate to exit the red gate, or enter red to exit blue. The gates face opposite directions, so world heading and velocity are preserved. A half-second re-entry guard prevents immediate ping-pong. Solid housings protect the back of each gate. Both gate faces are opaque and block shots; bullets are not transported.
- Walk onto a gold SUPER JUMP pad to launch across the gap. No jump press is required. There is a pad in each direction, aiming at a marked landing area on the far deck. The planned ballistic flight is 2.4 seconds over approximately 54 metres. Normal movement/air control still applies, so players can steer away from the safe trajectory.
- The existing 250 RPM rifle, 30-round magazines, R reload, ADS, sprint, walk, crouch and slide remain available. Portal transit retains slide state and magazine/reload state. Pad launches end grounded slides as before.

Portals check the full controller capsule, floor support and both live/interpolated contenders at the destination. An occupied or obstructed exit holds the entrant safely at the front instead of embedding or telefragging it. Bots back away from a blocked gate before rerouting. The HUD briefly identifies the destination platform or a blocked/not-ready gate. How to Play describes the two routes.

## State and networking

Protocol 9 carries portal cooldown, last teleport tick and traversal serial. Authority and prediction share the same swept-plane crossing and exit checks. A traversal serial change resets camera correction and opponent interpolation rather than blending through the void. Historical hurtboxes from an older traversal serial cannot hit an entry-position ghost. The current first-person camera/muzzle moves to the exit immediately on local transit. Reload and weapon cadence are not reset by portals.

Pickup snapshots now carry a bounded count (at most eight) so this symmetric layout can use one health pickup on each island. The preserved Foundry still supports its original three pickups. Both duel peers must use the same build. No relay, account or matchmaking service is introduced.

## Editable sources and local play

- Scene: `RailArena/Assets/RailArena/Generated/TwinPlatforms.unity`
- Repeatable builder: `RailArena/Assets/RailArena/Editor/TwinPlatformsBuilder.cs`
- Portal resolver: `RailArena/Assets/RailArena/Runtime/PortalGate.cs`
- New materials: `RailArena/Assets/RailArena/Generated/TwinPlatformsArt/`
- Evidence: `Validation/TwinPlatforms-2026-10-29/`
- Local player: `Builds/TwinPlatforms-2026-10-29/RailArena.exe`

The new scene is enabled for the local build; the original Foundry scene remains saved and disabled in build settings. Existing art, UI and weapon assets are reused. New geometry is an editable procedural layout, not a final production-art approval. The menu uses its own tighter overview FOV; gameplay continues to use the saved aim/FOV settings.

## Review limits

Headless checks exercise real CharacterController movement through both portals and both ballistic routes, exit occupancy, retrigger protection, connected bot routes, shot occlusion, wire state, rewind discontinuities and interpolation resets. Review renders are controlled Editor captures. They do not replace physical-input/motion review, balance approval, performance profiling or real-internet multiplayer acceptance.

## Qualification record

25 distinct focused checks have passing latest results. Actual CharacterControllers crossed both portals and both pad routes, verified exit blocking against an interpolated opponent with a disabled controller, preserved slide/velocity/ammo state, and respected the retrigger guard. A four-contender moving-bot run covered 3,780 integer ticks (including countdown), used portals twice and pads seven times, and recorded zero void deaths. Route graph reachability, portal shot occlusion, wire compatibility/bounds, hit rewind epochs and snapshot camera/interpolation resets passed. Nine menu/help/HUD captures check 720p, 1080p and ultrawide; four world views were visually reviewed. Initial compile-harness errors and corrected runs are retained in the logs.

The original Foundry scene matches the preserved earlier scene byte-for-byte (SHA-256 `8847038e88fa26e8c1bd67817efe8e99954dfd42eb566350f897aeefe7ea14b2`). No standalone multiplayer or real-internet qualification is claimed for this layout.

The non-development Windows player built successfully and passed hidden menu startup/close with exit code 0. Its 196-file package includes controls, full font/icon notices and SHA-256 provenance. Source identity `bf65b1e7564857434b0365c7d3966a7c7af08fb20fdf39572cbfe29ca11e69ee` independently verifies against the working project. The preceding magazine player's 196-file manifest remains unchanged. No task-owned build/player processes remained, and no visible Editor or game window was opened.

The expanded local build increases each deck to 44×56 m with additional cover and outer routes. See [Expanded platforms](EXPANDED-PLATFORMS.md) for layout, materials, lighting and playtest notes.
