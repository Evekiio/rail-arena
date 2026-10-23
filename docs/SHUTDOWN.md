# Player shutdown

The four background players stopped on October 22, 2026 were development players. The paired logs in `Validation/Duel-Evaluation-Full` and `Validation/Duel-Evaluation-Impaired-Full` show two host/client automated runs still advancing through their ten-minute matches and results. They do not show a player hanging after a Quit request. Closing a separate interactive player would not close these independent test players.

The test runners previously relied on the validation coroutine finishing or PowerShell `finally` cleanup. Terminating a runner could bypass that cleanup. Development test players now receive `--validation-owner-pid` and `--validation-timeout`; a main-thread guard checks once per second and quits with code 2 when the owner exits or the runtime limit expires. This does not require a single-instance restriction, so legitimate two-player local testing still works. A stalled main thread still requires the runner's existing forced timeout cleanup.

`MAIN MENU` returns to the menu and `DISCONNECT` leaves the duel. Neither exits the player. `QUIT` is now available in the main menu, pause/results menu and duel ready/results room. Its action releases the network session and rewind resources, saves preferences, releases the cursor and requests application exit. Window close and Alt+F4 release the same session resources through `OnApplicationQuit`. Destruction cleanup remains safe after shutdown cleanup.

## Verification

- `Validation/shutdown-editmode.xml`: all 78 tests passed. The new test exercises the real Quit button in the Editor, checks its availability in main/pause/duel menus, verifies UDP port reuse after quitting a host, and repeats cleanup.
- `tools/Test-Shutdown.ps1`: requires the development Windows player. Sends Windows close messages to menu, automated offline and automated host players, then tests validation deadline and owner termination. Every scenario must exit within 20 seconds with the expected exit code. Owned test processes are cleaned up on failure.
- `Validation/Shutdown/results.json`: all five standalone checks passed. Normal close returned 0; deadline and owner termination returned 2.
- Both Windows and Evaluation players were rebuilt successfully. `Validation/Shutdown-Evaluation/results.json` records a separate successful Evaluation window-close check. No RailArena processes remained after verification.

These are automated lifecycle checks. Menu Quit is exercised in the Editor, where Unity ignores the final application termination request; standalone termination is exercised through Windows close messages and the lifetime guard. They do not claim a manual mouse/keyboard acceptance pass or forced-termination cleanup callbacks.

Reproduce:

```powershell
unity test .\RailArena --mode EditMode --filter RailArena.Tests --output .\Validation\shutdown-editmode.xml
.\tools\Test-Shutdown.ps1
```
