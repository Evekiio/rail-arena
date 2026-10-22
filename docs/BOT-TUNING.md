# Editable bot profiles

Select `RailArena/Assets/Resources/BotTuning.asset` in Unity's Project window to edit bot behavior. The shipped values preserve the previous implementation:

| Profile | Reaction ticks | Aim error degrees | Turn degrees/second | Decision interval ticks |
| --- | --- | --- | --- | --- |
| Cadet | 48 | 5 | 150 | 39 |
| Veteran | 27 | 2.5 | 240 | 27 |
| Ace | 14 | 1 | 330 | 15 |

Ticks use the shared 60 Hz clock. Shared fields set aim-error refresh interval (12 ticks), health-seeking threshold (50), firing yaw tolerance (9 degrees) and aim offset below the target's eye (0.35 metres). Inspector ranges and runtime validation reject invalid/nonfinite values. Defaults retain seeded random call timing and existing movement/weapon/health rules.

Restart practice or the bot match after editing. A session stores a validated immutable snapshot so further Inspector edits do not change active actors or introduce zero refresh intervals midway through a tick. All bots in a session use the selected profile. Offline automated human drivers use the same configuration for reproducible validation; normal human input and online opponents are not driven by bot profiles.

The asset changes reaction, imperfect aim and decision behavior. It does not expose damage, maximum health, weapon cooldown, movement acceleration, gravity or scoring multipliers. Movement remains separately editable in `Assets/RailArena/Generated/Movement.asset`. Navigation recovery thresholds remain in `BotBrain.cs` to preserve the tested traversal behavior during this migration.

`BotTuningBuilder.EnsureAsset()` creates defaults only if the canonical resource asset is missing. It does not overwrite an edited asset. Full Foundry regeneration invokes it. The build preprocessor rejects missing or invalid canonical tuning before generating the build identity, and sessions also validate before starting. Tuning is included in the source/asset build fingerprint; both duel peers must continue to use the same complete build.

All 43 EditMode tests pass (`Validation/bot-tuning-editmode.json`). Existing reaction/error/occlusion tests still verify the original defaults. A new behavior test edits Veteran reaction to 60 ticks, starts a session, restores the asset, and observes first fire after 60 ticks, proving both application and snapshot isolation. Another test rejects zero refresh intervals, nonfinite settings (including an unselected profile) and unknown difficulty indices.

A direct preprocessor-callback check rejected a temporarily invalid zero refresh interval and restored the original asset (`Validation/bot-tuning-build-guard.json`). This checks callback behavior, not a complete deliberately failing build. The valid Windows build succeeded (`bot-tuning-build.json`), and all 35 standalone mechanics checks passed (`bot-tuning-mechanics.json`). Fresh 45-second seven-bot runs at all three difficulties passed with zero exceptions and zero void deaths (`Validation/Bot-Tuning/`): 170/191/189 shots and 8/21/35 deaths. These are short migration regressions; the earlier ten-minute evidence remains associated with its original build.
