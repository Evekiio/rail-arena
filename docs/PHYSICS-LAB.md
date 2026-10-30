# Physics Lab / G-key playtest configuration

Press **G** to open or close the panel; **Escape** also closes it. Opening the panel pauses offline simulation, releases the pointer and suppresses weapon input. Closing restores the previous paused state. All 19 controls fit on one page for screenshots. Drag sliders or enter numeric values; invalid/non-finite input is rejected and finite values are bounded.

The controls cover ground speed/acceleration, air acceleration, friction, stop speed, jump velocity, gravity, sprint/walk/crouch multipliers, slide duration/cooldown/entry speed/deceleration/steering, controller step height/slope limit/skin width, and pad flight-time multiplier. Slide durations are in 60 Hz ticks. Capsule sizes, eye heights and tick rate are displayed read-only because they affect hitboxes and protocol assumptions.

**RESET STANDARD** restores the original tuning. **COPY JSON** copies exact values. **SAVE PNG + JSON** writes a matching timestamped screenshot and JSON under `Application.persistentDataPath/TuningSnapshots` (`%USERPROFILE%/AppData/LocalLow/Evekiio/Rail Arena/TuningSnapshots`). The panel shows the saved file stem. Send both files when possible; the JSON includes exact values, map, UTC time and build identity. The screenshot uses Unity's end-of-frame screen capture and should be checked in the native player.

Changes apply to all offline actors and are **session-only**. They modify a runtime clone, never the source tuning asset or PlayerPrefs. Online duels restore standard values before connecting, and expose the panel read-only; no physics values are added to the wire protocol. Changing gravity, pad time, air steering or controller limits can change the summit landing behavior, so use RESET STANDARD as the comparison baseline.

Validation covers UI bounds/text at 1280x720, 1920x1080 and 2560x1080, exact edit/reset/export behavior, virtual G/Escape input, pointer release, firing suppression, and online tuning isolation. Headless checks do not establish native keyboard/mouse feel or native PNG capture acceptance.

The default ground speed and ground acceleration are 8. Sprint uses the existing 1.35 multiplier (10.8 m/s); the slide entry threshold is 8.8 m/s so a forward sprint can still initiate a slide. Reset Standard returns to these values.
