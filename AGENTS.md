# Project instructions

## Unity execution

- Run Unity work headless by default, including builds, automated tests, imports and validation. Use batch mode and disable graphics when the task supports it.
- Ask the user before opening any visible windows, including the Unity Editor, game players, terminals and review tools. Explain why a visible window is needed and wait for permission before opening it.
- If validation requires a player process, keep its window hidden unless the user has authorized a visible session. Hidden-window checks do not establish visual quality or physical input acceptance.
- Track every process launched for the task, apply bounded timeouts and clean up owned processes when the task completes, fails or is cancelled. Preserve unrelated user processes.
- Avoid concurrent heavy Unity jobs so the user can keep working. Report any validation that cannot be completed headlessly and why.
