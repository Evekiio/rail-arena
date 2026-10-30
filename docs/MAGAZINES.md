# Rifle magazines and reloads

The rifle now fires at 250 RPM with a 30-round magazine. Hold left mouse to fire; press R for a two-second reload. Reserve ammunition is unlimited. Reloading a full magazine is ignored; a partial reload refills the magazine when it completes. The player reloads manually when empty. Bots request a reload when empty.

Firing cannot consume ammo or produce a shot during reload or with an empty magazine. A repeated reload request cannot extend a reload already in progress. Holding fire through completion can resume fire immediately. Death cancels an unfinished reload; respawn and a new session restore a full magazine. Movement, sprint, crouch and slide remain available while reloading. Damage and recoil strength are unchanged. ADS lowers during reload and returns if right mouse is still held. A small lowered-rifle pose indicates reloading; this is not a full animated magazine-swap asset.

## Cadence

At 60 simulation ticks per second, exact 250 RPM requires 14.4 ticks per shot interval. The shared scheduler uses 14, 14, 15, 14, 15 ticks, repeated: five intervals total 72 ticks (1.2 seconds). It retains the fractional phase across bursts and tactical reloads. This avoids silently rounding to 240 or 257 RPM. The nominal interval is 0.24 seconds; magazine/reload downtime lowers effective sustained output.

## HUD

The lower-right card shows rounds loaded / magazine capacity, with low ammo highlighted, an unlimited-reserve icon, R-reload guidance, a reload countdown/progress bar, and an explicit EMPTY state. The existing firing-recovery bar remains between shots. How to Play includes R and the magazine/reload duration.

## Duel state

Protocol 8 adds one-shot reload intent and authoritative magazine rounds, reload deadline, fractional cadence phase and last interval. The server owns ammunition and firing; clients restore all weapon state before replaying pending input. Reload taps are preserved through a received input burst but are not held or retriggered during packet gaps. Both peers need this same build. Existing protocol-7 players remain separate.

## Local review

The new non-development player is `Builds/Magazines-250RPM-2026-10-29/RailArena.exe`. Keep all adjacent files together. Evidence is under `Validation/Magazines-250RPM-2026-10-29/`.

Checks are focused on magazine exhaustion/refill, exact cadence, reload input, snapshot replay and the ammo HUD at 720p, 1080p and ultrawide. Headless captures and virtual input checks do not replace physical-input, motion or human multiplayer acceptance. No broad regression sweep or real-internet qualification is claimed for this change.

Eight focused cases passed; a follow-up HUD/input check also passed after ensuring WEAPON MOTION OFF immediately clears the reload pose. The checks include exact 250 RPM scheduling, magazine exhaustion, reload boundaries, full/repeated reload handling, reload tap deduplication, wire bounds/state, prediction replay, production R input, blocked firing during reload and refill. Twelve controlled captures cover full, low, empty and reloading states at 1280x720, 1920x1080 and 2560x1080. No standalone multiplayer or real-internet qualification was run for this change.

The non-development Windows build completed successfully. The package contains PLAYTEST.txt, full font/icon notices and a 196-file SHA-256 manifest. Source identity: `a1c69ba36b42d9c774ab2d82fe1047ae627d55bd4be4a11eeed110f37e108344`. No standalone game window was launched for qualification.
