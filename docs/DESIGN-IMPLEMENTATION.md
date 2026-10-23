# Design implementation and release gates

Authority: [design lock](DESIGN.md). The approved rules differ from existing published and local builds. Do not describe those builds as v1 design-compliant until migration tests pass.

## Carbine integration, October 23

The approved precision carbine replaces the shared weapon and all contender presentations. Its [production handoff](../ArtSource/Original/PrecisionCarbine-Production-v1/README.md) records geometry, textures, LODs, fitting, build and validation evidence. Rifle input now latches valid click edges until one simulation sample, discards cooldown clicks, uses a 36-tick cooldown and provides tangent-correct 1.5x hold zoom. The existing 100/50/34 damage remains unchanged; 75/40/30 and the other design migrations below remain pending. Do not infer full design compliance from this asset integration.

## Ordered work

| Stage | Completion evidence |
| --- | --- |
| Design | DESIGN.md and the generated asset register have consistent agreed values |
| Greybox | Editable master, Layout.json, matching orthographic/player views, structural audit |
| Art | Asset-specific views/states and reviewed materials; missing entries explicitly blocked |
| Approval | Creator decisions captured per asset family before detailed modeling |
| Runtime | Rule/input/network/UI/world migration followed by headless regressions |
| Release | Human visual/input/audio approval, reference-machine benchmark and remote duel acceptance |

## Runtime migration checklist

- Simulation: 36-tick fire cycle; 75/40/30 damage; 18000-tick matches; score limit 20; four-player cap; 480-tick damage-free delay and 30-tick health increments up to 50.
- Input: click edges latched until the next simulation sample, consumed once even over multiple fixed ticks; rejected cooldown clicks discarded; redundant UDP delivery cannot duplicate fire. Zoom uses tangent-based magnification and independent sensitivity.
- Roster: selection persistence, unique offline allocation and conflict-resolved two-human ready state. Match first-person palettes to selected cosmetics without changing hitboxes.
- Authority: regeneration, safe spawns, per-actor statistics and hit/death ownership. Time/score ending must agree on both peers. Reset every statistic/timer on restart/rematch.
- Portals: validate full controller exit capsule, transform movement/yaw, retrigger guard, actor/actor blocking and discontinuity identity. Extend prediction, interpolation and rewind semantics; do not rely on cosmetic teleport effects.
- Wire protocol/build identity: add character selection, traversal identity and statistics as needed; version codecs, bound values, reject incompatible builds and retain the two-human limit. No cloud API.
- Presentation: character arms, zoom, authored death/fade, occluded markers, blood-off, reduced-flash, pickup/regen/traversal states and menu/ambient routing.
- Settings/UI: implement approved controls, presets, validation, rebinding conflicts, focus/error screens and storage migration. Use the Unity UI skill before choosing framework changes.
- Existing runners/assertions: migrate old seven-bot, held-fire, lethal-headshot and ten-minute assumptions. Never silently relabel older reports.

## Required tests

Damage combinations (head+limb, head+body, three body, four limb); cooldown boundary and frame-rate/input delivery; regeneration start/reset/cap, death and paused sessions; pickup timing and cap; solo/three-bot caps and unique roster; duel selection conflicts; 20-score finish/time ties/nonleader sudden-death/void; rematch resets; all portal transforms, blocked exit, re-entry and rewind discontinuity; safer spawns and complete bot traversal; per-player statistics; settings persistence and blood-off; clean and impaired local duel; clean process shutdown. Run headless EditMode, standalone mechanics, offline soak and duel scenarios before human acceptance.

## Art blockers and approval policy

The new rifle and combined combat/blood concept requests previously hit image-service filtering. Preserve the original prompts and error state; do not bypass the filter or claim an unavailable sheet exists. Existing current-weapon renders provide historical context, not approval of the new design. Art-family approval and live human acceptance remain external gates. Continuing with approved families does not waive blocked families.
