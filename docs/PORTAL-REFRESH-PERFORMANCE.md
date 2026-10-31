# Portal refresh performance investigation

Review 27 remains the verified playable package. The shipping probe's combat P95 was 44.061 ms. A focused profile on the unchanged executable reproduced a combat 39.082 ms P95 (18.404 ms mean), with no garbage collections during combat. Portal submission averaged 5.266 ms with 15.125 ms P95; scripts and physics also slowed during affected frames. Inclusive timings are not additive, synchronous readback is included and GPU samples are unavailable. This identifies substantial portal work, not a proven single root cause of every spike.

Four combat-view destination cameras each rendered every frame although their apertures were only about 11–58 pixels tall. The opt-in candidate reuses distant small views for two or four frames, with a 1/15-second age limit. Close (within 12 m), large (96 pixels on the longest side) and scoped views refresh every frame. Re-entry, view/projection/layer changes, aperture escape from cached crop, resizing and invalid targets force a refresh. Cached texture and shader crop coordinates stay paired. No movement, teleport, collision, damage or multiplayer authority changes.

The controlled candidate kept normal gameplay defaults unchanged and enabled the feature only with --profile-portal-refresh in the attribution harness. After measurement, the integrated Sky Gardens art profile enables it and restores prior defaults on removal. Older maps retain full-rate rendering. The profiling flag still selects its controlled on mode; no flag is needed for normal Review 28 play. Native frozen-map checks cover all six directions: non-black destination images, zero pixel differences between full-rate and cached images, close/scoped full cadence, projection/layer/movement/re-entry gates and actor invariants. Four pure policy checks cover thresholds, screen-edge crop containment and phase distribution. A test setup that reset a physical camera's projection also needed its physical-lens mode restored before checking focal-length-based ADS; that diagnostic is not a game projection fix.

Source: PortalView.cs and PortalRefreshPolicy.cs. Reproduce focused checks through PortalRefreshPolicyTests and PortalRefreshRenderingReview. Run tools/Profile-PortalRefresh.ps1 with a new output folder in both orders against one unchanged candidate. It hashes nine runtime payload files before and after each run and verifies embedded build identity/mode. tools/compare_portal_refresh.py validates the workload and keeps nearby portal 0 at full cadence. Both measurement orders completed and support promotion in Sky Gardens. This does not establish sustained native 60 FPS or artistic acceptance.


## Controlled result

Candidate source: `57e94e9a180dbe462bb2c78f1a193745f83508fe`; embedded identity `6567dab7bf432598240956e666932fc885f42639dfe310658e83cd9422025923`. Each ordered pair verifies the same nine runtime payload hashes and fixed profile configuration. The integrated portal/policy/profile implementation hashes match that candidate; only the art-profile activation and evidence delivery change afterward.

| Order | Combat mean full → adaptive | Combat P95 full → adaptive | Mean reduction |
|---|---:|---:|---:|
| Off then on | 17.435 → 12.183 ms | 31.200 → 14.386 ms | 30.1% |
| On then off | 16.064 → 13.072 ms | 19.153 → 16.325 ms | 18.6% |

Combat destination renders fall about 62%; the portal submission marker mean falls about 54–64%. All five views have lower means in both pairs, while some P95 misses remain and the first pair's near-portal P95 is slightly higher. Portal 0 at the close inspection view retains all 360 refreshes with zero skips. Do not add inclusive counter times or claim an isolated GPU saving. Raw pair data and exact values are in the browsable package rather than this rounded table.

The final integrated native check verifies six non-black destination pairs with zero changed pixels, close/scoped full cadence, changed-projection and layer gates, re-entry and crop ownership. Removing the Sky Gardens art profile restores prior settings; the older Twin Platforms map keeps its original full-rate defaults. The failed physical-lens test setup and earlier diagnostics remain outside promoted evidence.

[Browsable performance evidence](../ArtSource/Original/ArtPass-v1/Performance/PortalRefresh/index.html). No geometry, collider, animation source or Blender master is changed by this optimization. Review 28 shipping identity, build checks and remaining acceptance are recorded in ART-PASS-REVIEW.md. Production will pause after the verified Review 28 handoff at Sanders' request; the full art-production goal remains incomplete.
