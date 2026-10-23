# Optional in-game quick start

**How to Play** is available from the main menu and pause menu. It explains movement, click-only firing, zoom, live standings, practice modes, match rules, health and respawns. It opens on request and does not add a tutorial level or interrupt first launch.

From the main menu, **Start Solo Practice** starts an untimed zero-bot session using the selected character. It preserves the menu's configured bot count, difficulty and practice-target scenario for later sessions. From a running session, the page offers **Resume**, preserving the existing match. Back returns to the appropriate main/pause menu. Results retain their existing rematch flow.

Offline play remains paused while reading the page. The online variant warns that matches continue while menus are open, matching the existing online pause behavior. The menu's rifle summary and the help page derive damage, firing recovery, match length, bot cap, pickup return and respawn timing from current simulation rules. The page describes the implemented build; future design targets are not presented as current gameplay.

## Validation and review

[UI review gallery](../Validation/QuickStart/index.html) covers the menu, main-menu help and paused-session help at 720p/1080p. The captures draw actual uGUI meshes and materials against a neutral background, with controlled menu selections. They are layout reviews, not standalone gameplay screenshots or human-input acceptance.

The full pinned Unity suite checks optional entry, setup preservation, selected-character solo launch, contextual resume, offline clock pause, visible text and button bounds. Windows build and hidden-client mechanics/offline/loopback regressions verify ordinary play. Human onboarding comprehension, physical-input feel, external-peer reliability and measured 1080p/144fps performance remain open. The public pre-alpha ZIP is unchanged by this source update.
