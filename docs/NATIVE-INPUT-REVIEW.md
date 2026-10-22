# Native input review

Development Windows builds optionally expose game-local input diagnostics. Create an empty `enable-native-input-review.txt` beside `RailArena.exe` before launching. The game then writes `native-input-report.json` in that same directory once per second. Remove the marker before the next launch to disable it. Ordinary non-development builds exclude the component. A dedicated non-development review build can explicitly include it with `RAIL_ARENA_NATIVE_REVIEW`, still requiring the marker.

The report contains the build fingerprint, focus/frame counts, counts of Escape, jump, movement and left-mouse presses observed after Unity's dynamic input update, keyboard availability, aggregate keyboard state-event count, cursor lock, session state, position, sensitivity and volume. It does not record typed text, arbitrary keys, event payloads, other applications or global input. Input counters only observe events while this game has focus. Diagnostic output never drives the session or fabricates controls. Left-mouse counts include menu clicks, not just rail shots.

Press counters establish event receipt only. Movement still needs sustained input and observed displacement; pause needs a visible pause menu and frozen offline tick; settings need save/relaunch comparison. A short injected tap and its released-state screenshot cannot establish a held-key feature such as the scoreboard.

## Build variants

`ArenaBuilder.BuildWindowsTo(output)` retains the development build used by automated mechanics, bot and duel runners. `ArenaBuilder.BuildWindowsPlaytestTo(output)` uses `BuildOptions.None`. Pass `true` as its second argument only for a dedicated native review build with the optional diagnostic component:

```powershell
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli --timeout 600 -- --code 'RailArena.Editor.ArenaBuilder.BuildWindowsPlaytestTo("../Builds/Playtest/RailArena.exe");'
unity command script eval --project-path .\RailArena --caller plugin --skill unity-cli --timeout 600 -- --code 'RailArena.Editor.ArenaBuilder.BuildWindowsPlaytestTo("../Builds/NativeReview/RailArena.exe", true);'
```

Both builders preserve the enabled scene list. Normal playtest builds exclude the development automation harnesses. These are build configurations for an unfinished game, not published releases. Build mode and source fingerprint are recorded separately; the fingerprint does not imply byte-identical binaries across compilation variants. Unity documents [development/release modes](https://docs.unity.com/en-us/engine/6000.6/manual/building-and-publishing/building-introduction) and [per-build additional defines](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/BuildPlayerOptions-extraScriptingDefines.html).

## Native review on the non-development player

The initial non-development `0a0b3766` candidate reached the menu and practice without a firewall dialog in the observed attempt. The dedicated review player `6b469475b9150a92beaa2c3e74ee69cc885da43027fd13207ea37e4f3f8d6ad9` likewise ran without that dialog. No Windows security setting or permission was changed. This observation does not guarantee how another computer will prompt, particularly when hosting a duel.

Native mouse actions verified settings entry, dragging sensitivity from 0.120 to 0.200 and volume from 65% to 40%, saving with Back, quitting and relaunching. Both the UI and a fresh diagnostic report retained the changed values. Settings now display their numeric values live. The original exact float values were then restored (sensitivity 0.1199999973 and volume 0.6499999762), saved, and the window returned to 1280 x 720. Selecting 1920 x 1080 produced a 1922 x 1112 captured window including chrome. Fullscreen removed that chrome and filled a 2560 x 1440 captured window; toggling back restored the 1920 x 1080 window. Display-mode persistence after relaunch and sound-on acceptance are separate, unperformed checks.

The native bot selector cycled 3 through 7 to 0, visibly disabled the timed match button at zero, and launched Solo Practice through its menu button. Mouse aiming/firing showed a rail beam and 6% cooldown. The exported JPEGs in `Validation/NativeReview/` are actual bounded standalone-window captures, without image edits. They are not Editor screenshots.

Automated W, Space and Escape taps did not produce a new counted keyboard state event or press, despite a registered keyboard, focused frames and a locked game cursor. The pawn remained at its starting position and pause did not open. The event count stayed at its initial one. This establishes unsuccessful keyboard automation in this session, not whether physical keys work. Native movement, jump, pause, scoreboard and restart acceptance remain open, as does mouse feel. The opt-in marker was removed and the review player closed after testing. Reports distinguish native observations from automated gameplay drivers; no user playtest is required to continue development.

## Historical development-build attempt

The existing `5a72f123...` standalone launched practice and rendered the first-person arms. An injected Escape tap did not visibly open pause. Its cause remains unresolved; this is not evidence that physical keyboard input fails.

The diagnostic candidate fingerprint is `2a470bd8eaddd48b8426aee6330462c1c53df4b4ce40d0a72284967313c4c556`. It compiled and built successfully. Its initial report is retained at `Validation/native-input-initial.json`; it establishes bootstrap/focus/report writing, but no keys had been exercised. A Windows Firewall permission dialog then blocked the visible candidate. No permission was accepted or changed. The candidate was stopped and its opt-in marker removed before automated regression.

At that checkpoint keyboard, settings and sound-on acceptance were open. The later non-development review above advances mouse/settings evidence without changing Windows permissions. The Windows automation API exposes key taps rather than a supported held-key duration; automated mechanics and bot runs remain separate evidence.
