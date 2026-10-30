# Portal aperture rendering: controlled profile

The portal destination camera now renders the conservative visible aperture instead of the entire observer frustum. Its oblique exit plane and physical camera projection remain in place. The energy surface remaps screen coordinates into the cropped texture. Resolution buckets preserve at least the previous texture's pixel density inside the visible aperture; near-plane intersections fall back to the full projection. Growth is immediate and shrink hysteresis avoids texture churn. Traversal, collision, weapon accuracy and network messages are unchanged.

## Evidence and limits

The controlled comparison uses the same Review-11 Windows executable and fingerprint `f577ac97b6235a6b15e7b685c78c4e991e80dc6bd444328291263d13ac4a73ac`, RTX 2080 / Direct3D12, 1920×1080 High, 100-degree vertical observer FOV, one automated human and three bots, seed 2147 and fixed 60 Hz simulation. Each view warms for 120 frames and samples 360. The main view is rendered after LateUpdate and synchronizes a one-pixel readback. This measures uncapped wall-clock throughput with readback overhead, not native frame pacing. Full-frame ran first; cropped ran second. No GPU samples were valid in either run.

| View | Draw calls, full → crop | Portal CPU mean, ms | Wall mean, ms | Wall P95, ms |
|---|---:|---:|---:|---:|
| Combat | 4,118 → 1,397 | 5.48 → 5.06 | 16.58 → 17.48 | 22.48 → 36.22 |
| Summit | 3,767 → 1,250 | 4.74 → 3.76 | 16.29 → 15.84 | 34.18 → 30.92 |
| Basement | 3,501 → 1,264 | 4.45 → 4.43 | 15.43 → 17.25 | 20.51 → 39.43 |
| Foliage/waterfall | 3,617 → 1,398 | 4.61 → 3.53 | 16.12 → 15.26 | 36.71 → 22.89 |
| Portal | 1,207 → 345 | 2.81 → 2.83 | 13.06 → 14.27 | 20.53 → 29.36 |

Draw calls fall 61–71% and submitted triangles 62–70%. Overall timing is mixed; this is evidence of reduced rendering work, not proof of a universal frame-time improvement or sustained 60 FPS. Profiler counters are inclusive and must not be summed as independent costs. Readback/scheduling variance and native GPU/frame-pacing measurements remain unresolved. Zero managed GC collections occurred in either run.

Raw records and per-frame CSVs: `Validation/ArtFrameProfile-02/`. The checked comparison is `comparison.json`; reproduce it with:

```powershell
python tools/compare_portal_profiles.py Validation/ArtFrameProfile-02/full.json Validation/ArtFrameProfile-02/cropped.json Validation/ArtFrameProfile-02/comparison.json
```

Run the player hidden with `--profile-art-frame --validation-output <absolute-report-path>` and the standard 1920×1080 launch arguments. Add `--profile-portal-crop-off` for the full-frame reference. Both are opt-in development diagnostics and do not save settings. Use one player at a time and a 150-second watchdog.

## Corrected measurement order

The older art-performance probe staged its observer after portal rendering, while the main player view was reset earlier each frame. The main scene and portals consequently used different observer poses. Measurement version 2 stages the observer between ArenaSession and PortalView, then renders the main view at EndOfFrame. Older milestone timing tables remain historical evidence but are not equivalent portal workloads. The fixed-step attribution probe already uses the corrected order.

## Visual validation and remaining issue

Focused checks cover three physical-camera projection configurations, near-plane fallback, resolution buckets, legacy portal rendering and twelve full/cropped image pairs across six gates with normal and wide ADS views. Visible colour charts, inverse projection mapping and aperture pixel density are checked explicitly. See `Validation/art-portal-crop-focused.xml`, `Validation/art-portal-crop-alignment.xml` and `ArtSource/Original/ArtPass-v1/Performance/PortalCrop/`.

Those alignment checks isolate the aperture. Original-world captures separately exposed existing under-bridge pier occlusion; [the measured-placement follow-up](UNDERBRIDGE-PORTAL-VISIBILITY.md) remains required. Isolated alignment does not establish unobstructed map visibility or Sanders' native acceptance. The older deferred ADS/portal diagnostics remain preserved.

Startup logs also report stripped, unused depth-of-field/Panini post-process shaders in both the prior and new profile builds. These warnings are not recorded exceptions or evidence that those unused effects are installed. Installed effects and native sound/input still require normal playtest review.

The first Review-11 duel attempt ran under restricted networking: both peers stayed at tick zero and the client timed out without game exceptions. Its reports are retained in `Validation/ArtPass-Review-11/Duel-RestrictedNetwork/`. Only the loopback duel was rerun with network access; the mechanics, bot and rendered-view runs were not repeated.

The connected 45-second attempt passed statistics with no exceptions, but failed movement coverage because neither observer sampled a host slide. That report remains in `Duel45-MissingHostSlide/`. A 90-second run extends opportunities under the unchanged criteria; a later pass does not erase the shorter attempt's missing coverage. This smoke test is not a deterministic traversal/stance coverage proof.
