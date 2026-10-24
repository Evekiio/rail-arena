# Audio mix and rifle preview

Settings > Audio Mix / Sound Test provides master and effects sliders, percentage readouts and a rifle-sound preview. Both levels apply and save immediately. The existing `volume` preference remains the master level, so previous settings are preserved. Effects defaults to 100%, retaining the existing mix.

Master controls the listener. Effects scales the shared local cue source and every pooled positional rifle source, preserving their existing relative gains and distance rolloff. It covers the sounds currently implemented: shots, hit confirmation, eliminations and damage. There is no music or ambience bus claimed by this change.

The preview uses the existing synthesized rifle clip and the selected levels. It does not call the firing simulation or create a beam, muzzle flash, hit, damage or statistic. At zero master or effects level, the preview button is disabled and a mute explanation appears. Raising both levels re-enables it. Invalid/nonfinite levels are normalized to safe defaults; finite levels are clamped to 0-1.

## Validation and review

[Layout gallery](../Validation/AudioMix/index.html) shows normal and muted states at 720p and 1080p. Updated [settings entry at 720p](../Validation/AimSettings/Settings-1280x720.png) and [1080p](../Validation/AimSettings/Settings-1920x1080.png) show where the new page opens. These draw actual uGUI meshes/materials against a neutral background, not human gameplay captures or evidence of audible quality.

Applicable coverage includes the AudioMixHudTests, AimSettingsHudTests and RailFeedbackTests fixtures: save/load, default migration, local/positional gain wiring, zero-level mute, preview isolation, cue sample bounds, pooled effects, settings navigation and text fit. A pinned Unity 6000.6.7f1 headless Windows development build validates player compilation.

Sound-on listening, device/output behavior, the final mix and real combat readability remain human acceptance gates. No unrelated full suite, broad combat soak, new music, networking change or performance qualification is included. The public pre-alpha ZIP remains unchanged.
