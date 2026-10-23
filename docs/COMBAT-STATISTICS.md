# Combat statistics and standings

Current source tracks match-scoped combat totals in the authoritative simulation. Hold Tab for live standings; offline and duel results retain the same table after the match.

| Column | Meaning |
| --- | --- |
| PTS | Existing match score, including minus one for a void death |
| K / D | Opponent eliminations and all deaths, including void deaths |
| ACC | 100 × hits / fired shots, or zero before firing |
| HITS / SHOTS | Successful actor hits and actual fired shots |
| HS | Head hits, including nonlethal hits |

Cooldown clicks do not count as shots. A fired miss counts, including a shot whose historical trace cannot be accepted. The winning shot is recorded before entering results. Void deaths do not award a kill. Starting another match resets all totals.

Standings sort by points. Equal scores share a rank; actor ID only stabilizes their display order. The local row is highlighted, and the winner's name uses amber.

## Duel compatibility

Protocol version 4 transmits complete counters in each actor snapshot. Clients replace their totals from the host, so lost snapshots do not permanently lose statistics. Both participants must use the same updated build. This changes compatibility with earlier builds; it does not add a weapon, service or client-authoritative damage path.

## Evidence and limits

[Review gallery](../Validation/CombatStatistics/index.html) contains live, offline-result and duel-result layouts at 1280 × 720 and 1920 × 1080. These are actual uGUI meshes and materials rendered by the automated layout fixture against a neutral background, using deliberately seeded statistics. They are not records of human match performance or standalone gameplay screenshots.

Simulation tests cover counting, invalid inputs, winning shots, reset, snapshot replacement, malformed packets and version rejection. UI tests cover tied ranks, score penalties, visible text and both result flows. Built-player checks reconcile real shots, hits, kills, deaths and voids in offline combat and a short loopback duel.

These checks do not establish physical-input acceptance, external-network reliability, a complete timed online rematch, competitive balance or 1080p/144fps performance. The downloadable pre-alpha remains its earlier checkpoint; no replacement release is published by this change.
