# Native input review

Development Windows builds optionally expose game-local input diagnostics. Create an empty `enable-native-input-review.txt` beside `RailArena.exe` before launching. The game then writes `native-input-report.json` in that same directory once per second. Remove the marker before the next launch to disable it. Release builds exclude the component.

The report contains the build fingerprint, focus/frame counts, counts of Escape, jump, movement and fire presses observed after Unity's dynamic input update, session state, position, sensitivity and volume. It does not record typed text, arbitrary keys, other applications or global input. Counters only observe input while this game has focus. Diagnostic output never drives the session or fabricates controls.

Press counters establish event receipt only. Movement still needs sustained input and observed displacement; pause needs a visible pause menu and frozen offline tick; settings need save/relaunch comparison. A short injected tap and its released-state screenshot cannot establish a held-key feature such as the scoreboard.

## Current attempt

The existing `5a72f123...` standalone launched practice and rendered the first-person arms. An injected Escape tap did not visibly open pause. Its cause remains unresolved; this is not evidence that physical keyboard input fails.

The diagnostic candidate fingerprint is `2a470bd8eaddd48b8426aee6330462c1c53df4b4ce40d0a72284967313c4c556`. It compiled and built successfully. Its initial report is retained at `Validation/native-input-initial.json`; it establishes bootstrap/focus/report writing, but no keys had been exercised. A Windows Firewall permission dialog then blocked the visible candidate. No permission was accepted or changed. The candidate was stopped and its opt-in marker removed before automated regression.

Native keyboard, mouse feel, settings save/relaunch and sound-on acceptance remain open. The Windows automation API exposes key taps rather than a supported held-key duration. Automated mechanics and bot runs remain separate evidence. Resume native review when the Windows permission prompt is resolved through the user's normal security workflow; local game development does not depend on this check.
