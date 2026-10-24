# Frame pacing settings

Settings > Frame Pacing now provides saved VSync and FPS-limit controls. New installations retain the existing default: VSync off and a 144 FPS target. Available limits cycle through 60, 120, 144, 240 and unlimited.

Enabling VSync sets one display refresh per frame. The FPS button is disabled and the active-status line explains that display sync takes priority. The selected limit stays saved and returns when VSync is disabled. Invalid saved limits fall back to 144. Changes apply and save immediately, including across application restarts.

This follows [Unity desktop VSync behavior](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html): a nonzero vSyncCount overrides Application.targetFrameRate. Actual timing depends on hardware, drivers and workload. This adds controls, not a performance certification. Fixed simulation remains 60 ticks per second; movement and combat rules are unchanged.

## Evidence

[Review gallery](../Validation/FramePacing/index.html) includes the settings entry point, FPS-limit state and VSync state at 720p and 1080p. These render actual uGUI meshes/materials against a neutral background; they are not human gameplay captures.

The scoped HUD fixture exercises cycling every limit, runtime application, persistence, disabled-button state, returning from the settings page, invalid-value recovery and text fit at both sizes. It restores test preferences and runtime pacing values. A pinned Unity 6000.6.7f1 Windows development build checks compilation and player packaging.

No unrelated full suite, broad combat soak, physical display-sync/tearing assessment or four-contender FPS qualification is claimed. The downloadable public pre-alpha remains unchanged.
