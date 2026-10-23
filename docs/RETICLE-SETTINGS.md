# Reticle appearance

Open **Settings → Reticle Settings** from the main menu or a paused session. Size and color changes save immediately and apply to the local HUD. The settings page previews the same outlined five-mark reticle against dark and bright backgrounds.

- Size: 18–64 HUD reference units; default 36. The existing CanvasScaler scales these with the viewport, using its 1920 × 1080 reference resolution.
- Colors: cyan, white, lime, magenta and amber. Cyan is the default.
- Reset Reticle restores size 36 and cyan without changing aim, audio or display settings.
- The selected color stays steady during weapon recovery. The rail meter and cycle text retain recovery feedback. Countdown/locked states dim the reticle; death hides it.

The reticle stays centered and retains its dark outline. It does not expand with movement, recoil or zoom, and does not change the shot ray, accuracy, damage or weapon timing. Preferences are local; no network packet change is required. The existing build compatibility identity still updates with the changed runtime assets.

## Review and evidence

[Reticle review gallery](../Validation/Reticle/index.html) contains settings and live-HUD captures at 1280 × 720 and 1920 × 1080. These use actual uGUI meshes/materials rendered by the headless fixture on a neutral background. Settings demonstrate size 64/magenta; the live HUD demonstrates size 48/white during weapon recovery. They are controlled UI evidence, not screenshots of a human playtest or performance measurements.

Tests cover default migration, all palette options, save/load/reset, corrupt values, preview/live agreement, centered aim and persistent selected color during recovery. Built-player mechanics and offline combat checks verify the new source still runs normally. Human readability, physical input and frame-rate qualification remain open. The public pre-alpha ZIP is unchanged by this source update.
