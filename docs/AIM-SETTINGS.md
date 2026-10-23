# Aim and camera settings

The current source adds a focused controls polish pass after `v0.1.0-pre-alpha.1`. The existing downloadable release does not include these changes; build the current source to use them.

Open **Settings** from the main menu or pause menu. Mouse sensitivity and zoom sensitivity are independently adjustable from 0.020 to 0.500. Vertical camera FOV ranges from 70 to 110 degrees, with the existing 100-degree default. Invert Y reverses vertical look while preserving horizontal look. Master volume and existing display shortcuts remain available alongside the aim controls.

Values apply immediately. Back flushes saved preferences to disk. Returning to play uses the chosen FOV; holding right mouse applies exactly 1.5x magnification at every supported FOV. This changes presentation and controls, not damage, shot timing, movement speed or accuracy.

Existing normal and zoom sensitivity preference keys are retained. If a player has never saved a separate zoom sensitivity, its initial value follows their saved normal sensitivity. Once adjusted, the two remain independent. Missing FOV/inversion preferences retain the old camera and look behavior. Non-finite or out-of-range preferences are sanitized before camera/input use.

The two-column settings layout separates aim/camera from audio/display. Sliders show the selected numeric value, a filled track and inset handle travel. Invert Y updates its label without rebuilding the settings controls.

The current settings header also links to [reticle appearance](RETICLE-SETTINGS.md). The linked settings images have been refreshed for that entry point; the qualification reports below retain the original aim-settings checkpoint. Current reticle tests and build evidence live in `Validation/Reticle`.

## Qualification

The pinned Unity 6000.6.7f1 suite passes 98/98 tests, including preference migration/persistence, invalid values, zoom magnification, vertical-only inversion, live settings interactions and slider endpoints at 720p/1080p. [Test report](../Validation/AimSettings/tests.xml).

Controlled [1080p](../Validation/AimSettings/Settings-1920x1080.png) and [720p](../Validation/AimSettings/Settings-1280x720.png) reviews draw the actual rebuilt uGUI meshes, vertex colors and materials against a neutral background. They are geometry/layout reviews, not standalone screenshots: the batch URP camera request omitted screen-space UI, so the review projects the live UI geometry into pixel coordinates. No visible windows were opened.

The Windows x64 development build succeeds with a verified source compatibility identity. The built executable passes [mechanics checks](../Validation/AimSettings/Standalone/mechanics.json) and [45 seconds of three-bot offline combat](../Validation/AimSettings/Standalone/offline.json). [Build receipt](../Validation/AimSettings/build.json). The existing public client ZIP is unchanged.

These checks do not establish physical-input feel, visual acceptance on every aspect ratio or the game's 1080p/144fps target. Broader rebinding, audio channels, display persistence and accessibility settings remain future work.
