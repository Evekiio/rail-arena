# Recent eliminations and clearer respawn status

Elimination messages now appear in a compact upper-right feed. The upper-centre notice is reserved for countdown, sudden death and respawn status, so the latest elimination no longer remains there indefinitely.

The feed keeps at most three observed events, newest first. Entries expire after six seconds of local real time and fade over their final half-second. Identical consecutive messages remain separate events. Empty history hides the panel; holding Tab hides it while standings are open. Starting a new session or receiving a new duel round clears the history.

Respawn status now takes priority over sudden-death text while the local player is dead. The usual sudden-death notice returns after respawn. Damage, score, weapon timing, respawn timing and match endings are unchanged.

Offline/host authority records actual elimination and void events. A duel client uses observed authoritative death transitions and the existing void-death statistic to distinguish falls from opponent kills. Stale snapshots do not append another message, and a new life/round does not replay an old death. This is a temporary presentation feed, not a guaranteed durable event log: deaths completely skipped between received snapshots are not reconstructed.

## Review and applicable validation

[Review gallery](../Validation/CombatFeed/index.html) shows the feed during sudden death and the higher-priority respawn notice at 720p/1080p. These draw actual uGUI meshes/materials against a neutral background with deliberately seeded four-contender messages and a controlled match clock. They are layout evidence, not records of human combat or standalone gameplay screenshots.

Validation is limited to the feed buffer, combat HUD and snapshot-lifecycle fixtures, plus the Windows build. It covers capacity/expiry/reset, repeated messages, authoritative void identification, duplicate snapshots, new rounds, existing HUD controls, standings visibility and text fit. No unrelated full-game suite or broad combat soak is included. Human readability during combat, external-network delivery and frame-rate acceptance remain open. The public pre-alpha ZIP is unchanged.
