# Bot behavior and reliability checks

The offline runner supports bounded configuration for repeatable standalone sessions:

```powershell
.\tools\Test-Offline.ps1 -Duration 600 -Difficulty 0 -Bots 7 -Output .\Validation\easy-600s.json
.\tools\Test-Offline.ps1 -Duration 600 -Difficulty 1 -Bots 7 -Output .\Validation\medium-600s.json
.\tools\Test-Offline.ps1 -Duration 600 -Difficulty 2 -Bots 7 -Output .\Validation\hard-600s.json
```

Duration accepts 45 through 1800 seconds, difficulty 0 through 2, and bot count 1 through 7. Defaults preserve the original 45-second seven-bot medium smoke test. The human slot also runs the ordinary bot input driver so traversal can be sustained without injected keyboard/mouse events. Practice mode is used to keep the session active for its requested duration. This does not test a person's input or native menu selection.

The runner records a build fingerprint, configuration, telemetry, errors, void traces and per-actor progress. After five seconds of warm-up it samples progress about once per second. Route-node counts accumulate across observed respawns; deaths and respawn teleports reset the current stall interval. Distance is the sum of sampled position differences, excluding observed life changes; it is not an exact integrated travel distance. Very short life activity between samples can be missed. Stationary time means no displacement of at least half a metre from the last progress anchor. Time without route progress means no newly reached navigation node while alive. These are sampled stall indicators, not proof of complete map coverage or optimal paths.

Acceptance requires no application errors, sufficient authority tick progress, no more than two void deaths, and for every actor at least three reached route nodes, no stationary interval of ten seconds, and no interval without route progress of twenty seconds. Population-dependent minimum activity is `(bots + 1) * 4 - 1` shots, `max(1, bots - 1)` deaths and `bots + 2` pad launches. For seven bots this exactly preserves the original 31 shots, six deaths and nine launches. Minimums are recorded in the final report. These are activity floors, not measured difficulty or accuracy targets. A fresh final report and a successful player exit are required; an in-progress `.tmp` report is not passing evidence. The PowerShell runner imposes a duration-plus-60-second deadline and terminates a hung child.

The first one-bot 45-second sample recorded 50 shots, one death, 15 launches, zero exceptions and zero void deaths, with both actors traversing at least 21 route nodes. It failed the original population-independent six-death floor (`Validation/Bot-Soak-600s/one-bot-smoke.json`). That report remains failed. The runner's new per-population activity minimums correct this diagnostic assumption while leaving the original seven-bot gates unchanged. No gameplay or bot behavior was changed to satisfy it. Fresh one-bot and seven-bot runs passed on fingerprint `e0968521...` (`population-1-corrected.json`, `population-7-corrected.json` in the same folder). The one-bot run recorded 50 shots, one death and 15 launches; the seven-bot run recorded 191 shots, 21 deaths and 54 launches. Both had zero exceptions and void deaths.

## Isolated behavior tests

`BotBrainTests` supplies the real decision code with a small navigation graph, the actual contender actors and a physical world-layer occluder. The clock advances deterministically without moving the actors. These tests establish decision behavior rather than movement quality:

- Easy/medium/hard first-fire reaction delays are exactly 48/27/14 ticks after acquiring an aligned visible target.
- Seeded aiming remains imperfect and within the configured 5/2.5/1 degree yaw and pitch error limits once reaction time has elapsed.
- A new wall immediately prevents firing. A hidden target does not retain its old reaction timer; reacquisition requires a fresh delay. Off-axis reacquisition permits ordinary turn time.
- A low-health bot chooses the graph route toward available health while aiming independently.

All 40 current EditMode tests pass, including these seven new cases (`Validation/bot-brain-editmode.json`). Damage, cooldown, health and scoring invariants remain shared with human gameplay and are separately exercised by the standalone mechanics scenarios. No hidden damage or health multipliers are introduced by this work.

## Measurement limits

The three ten-minute standalone practice runs passed on fingerprint `c2070a17...`, with seven bots and the automated human (`Validation/Bot-Soak-600s/difficulty-0.json` through `difficulty-2.json`). They ran concurrently on the available PC:

| Difficulty | Shots | Deaths | Pickups | Pad launches | Fewest route nodes per actor |
| --- | --- | --- | --- | --- | --- |
| Easy | 2559 | 141 | 26 | 757 | 273 |
| Medium | 2674 | 305 | 27 | 757 | 261 |
| Hard | 2576 | 516 | 26 | 749 | 244 |

Each run completed approximately 600.5 real seconds and more than 36,000 authority ticks, with zero application exceptions and zero void deaths. The longest sampled stationary interval was under 1.1 seconds; the longest sampled interval without route progress was about 3.01 seconds. This covers the recorded ten-minute runs and seed behavior, not indefinite or all-seed reliability. The later `e0968521...` candidate changes only diagnostic population gates and report fields; its fresh one/seven-bot smoke reports are distinct from these long runs.

Hidden-window frame timing is loop timing, not a visible GPU/rendering or input-latency benchmark. Concurrent sessions share the machine and are unsuitable for comparing rendering performance. Difficulty combat totals are observations for these runs, not an aim-accuracy or player-skill benchmark. Bot art/animation, perceived fairness, tactical quality and map flow still require visible play review.
