# Rail Arena implementation record

Target: Unity **6000.6.7f1**, URP **17.6.0**, Windows keyboard and mouse.

The complete objective is the acceptance contract. Work is in progress. Offline play and initial host-authoritative direct-IP duels are implemented. Bounded hit rewind and local two-process smoke tests have passed; full-match, rematch and impaired-network acceptance are being extended. Current evidence and remaining limits are recorded in `docs/VALIDATION.md` and `docs/NETWORKING.md`.

Sequence:
1. Pinned URP project and shared 60 Hz simulation, with deterministic rule tests.
2. Offline practice and bot match, arena traversal, damage and match presentation.
3. Direct-IP duel, prediction, reconciliation, interpolation and bounded hit rewind.
4. Standalone tests, visual inspection, bot soak and simulated network validation.
5. Public source and Windows delivery with measured limitations.

The initial environment check confirmed the exact Editor and an active Unity Personal license. Restricted sandbox discovery reported no Editor and an invalid GitHub login; the same read-only checks outside the sandbox confirmed both were available. Use explicit project paths to avoid touching other Unity projects.

Supplied game references in `F:\Rail Arena\References` were reviewed in `docs/REFERENCE-REVIEW.md`. They are excluded from public source by default except for the folder README. Public source and tested milestones are published at [Evekiio/rail-arena](https://github.com/Evekiio/rail-arena).

## Research

- [id Software's Quake III movement source](https://github.com/id-Software/Quake-III-Arena/blob/master/code/game/bg_pmove.c): reference for directional acceleration, ground friction and airborne momentum. Implementation is original C#; no GPL source is incorporated.
- [Steven Polge's Unreal Tournament AI and Gameplay for Level Designers](https://unrealarchive.org/unreal-tournament/documents/reference/unrealed/unreal-tournament-ai/index.html): original developer guidance preserved by Unreal Archive. Explicit navigation connections, clearance around corners, ramp coverage and item destinations inform the planned arena navigation graph.

These references do not establish gameplay fidelity. Movement feel, combat feedback and sustained bot traversal require playtesting.
