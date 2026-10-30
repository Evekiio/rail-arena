# Rail Arena interface system

October 29, 2026. The interface extends the existing Rail Arena identity into a complete menu and combat-HUD system. It retains the original wordmark, current contenders, gameplay and local/direct-IP modes. This is a local review build toward the requested production-quality presentation, not final creative or AAA-quality certification.

## Identity and components

| Token | Value / use |
| --- | --- |
| Graphite | `#121A22`, menu background |
| Warm ivory | `#EFE9D9`, primary interface text |
| Electric cyan | `#48DAE8`, focus rails, primary icons and readiness |
| Muted text | `#8CA3A8`, descriptions and supporting labels |
| Button surface | `#1C2B36`, interactive surfaces |
| Focus surface | `#14525C`, selected controls, paired with a visible cyan rail |
| Display typography | Barlow Condensed SemiBold, page/mode titles and major HUD numbers |
| Body typography | Barlow Regular, descriptions, labels and tables |
| Control typography | Barlow Medium, buttons and persistent actions |
| Iconography | Lucide 1.8.0, 30 icons, 1.75-unit strokes on a 24-unit view box, exported at 96 px |

Colors are converted from the brand's sRGB values for the project's linear render pipeline. Logo clear space, native proportions and the graphite edge are retained. The runtime logo is fitted at 3:1, not stretched. Supporting UI uses thin rules and restrained opaque/translucent surfaces instead of effects over aiming space.

The wordmark remains the original raster artwork. Barlow supplies interface typography, not a replacement wordmark. Font OFL notices and the complete Lucide ISC/Feather notices are included in source and the local build. Editable icon exports, font source URLs and content hashes are in `ArtSource/Original/UI-Brand-v1/`.

## Menus and navigation

- Main menu: persistent section navigation, three descriptive launch cards and a separate session-setup panel with the selected contender portrait. Zero bots disables the timed match with explicit recovery guidance while leaving solo practice available.
- Settings: grouped aim/camera and audio/display controls, plus reticle and comfort pages. Existing saved values, live application, display controls and rifle preview are retained.
- Contenders: four actual installed-asset portraits and a persistent explicit selection. Appearance selection does not change an active session.
- Direct-IP: labeled address/port fields, retained drafts, recoverable inline errors and clearer host/join instructions. Waiting cannot ready up; the action becomes ready/cancel-ready or rematch/cancel-rematch according to real connection and local-ready state.
- Pause and results: the shared brand shell, clear resume/restart/rematch/menu/quit actions and authoritative standings with tie-aware ranks and per-player counters.
- Help: current movement/ADS bindings and actual match rules, without a mandatory tutorial.

Keyboard arrows and Enter use native uGUI navigation. Focus has a cyan rail as well as a color change. Setup cycling retains the corresponding control's focus after rebuilding the page. Escape backs out of a submenu before resuming a paused match. Footer hints reflect whether Back or Resume is available. Pointer hover, selected, pressed and disabled states remain distinct.

A short original synthesized interface click respects master/effects volume. Effects changes mute the interface source immediately. Slider changes do not emit repeated click sounds. Audible quality and native-input acceptance still need human review.

## Combat HUD

The center remains clear for the existing reticle/ADS sight. Health sits at the lower left with a heart/pulse icon, number, bar and pickup gain. A compact stance badge observes move/walk/sprint/crouch. Weapon readiness and unlimited energy sit at the lower right with lightning/infinity icons and the unchanged cooldown meter. Score/speed and match/time use trophy/gauge/clock icons near the top. Elimination events, hit confirmation, damage edges, countdown, respawn notice, standings and result statistics retain their real gameplay data.

Icons supplement labels and values; they do not imply armor, inventory, ranked progression, matchmaking or other unavailable systems. Normal aiming, 1.5x ADS, shot accuracy, movement tuning, damage, match length and networking rules are unchanged by this UI pass.

## Implementation and review

`ArenaHud.Brand.cs` owns the shared shell, design tokens, asset placement, launch cards, focus/back handling and click audio. Existing partial HUD files retain their feature logic. `MenuButton` owns native selectable-state feedback. `UiBrandImporter.Configure` imports the new images as 2D textures without NPOT distortion or mipmaps, and embeds dynamic font data for standalone players.

`tools/build_ui_brand_assets.cjs` exports actual Lucide paths using the installed library and Sharp; `tools/fetch_ui_fonts.py` records upstream font URLs and SHA-256 values. The source scene, models, portraits and existing branded/ADS builds are retained. Validation runs from an isolated project copy.

The new flow review covers main, contenders, settings, audio, reticle, comfort, display, help, duel form/error, ready room, recovery HUD, ADS HUD, pause and results at 1280x720, 1920x1080 and 2560x1080. It checks visible text and selectable bounds and exercises setup focus, back navigation and disabled readiness. Existing targeted HUD/settings/input/error/statistics tests run alongside it. Initial importer, audio-mute and ultrawide clipping failures are retained with their corrected rechecks.

Evidence and captures: `Validation/UI-Brand-2026-10-29/`. The final local non-development player is `Builds/Branded-UI-2026-10-29/RailArena.exe`; keep its adjacent data folder and DLLs together. Build/runtime qualification is recorded after completion. No visible Editor or player window is opened by automation. Human motion/sound/input review, final creative approval, real-internet play and rendering-performance certification remain separate gates.

## Qualification record

- The complete 15-case UI suite passed in one final invocation. The separate standings recheck and elimination-feed case passed, for 16 distinct affected cases with passing latest results. The flow review produced 45 controlled captures across the three viewport sizes. Failures and corrected rechecks remain in the evidence folder.
- All 37 standalone mechanics checks passed with zero recorded errors.
- The 45-second four-contender offline run passed with 214 shots, 11 deaths, two pickups, 27 pad launches, no exceptions or void deaths and reconciled statistics.
- A 45-second two-process loopback duel passed with movement-mode cycling, zero application errors on both peers and live crouch observed on both sides. Client maximum active correction was 0.356 m; this short run does not establish competitive smoothness or an improvement over preceding builds. No new impaired/full-match/internet qualification is claimed for the UI-only change.
- Generated source identity `bf907ba715989647e431e1b3a49ec3d5dcbaef54e762d67e00a5afeb470b3166` independently verifies against the actual project. The preceding ADS player's full 191-file manifest still verifies unchanged.

![Menu review capture](interface-menu.png)

![HUD and ADS review capture](interface-hud.png)

- Final non-development build completed successfully and passed hidden menu startup/close with exit code 0. Its 196-file package manifest verifies, full font/icon notices are included, and development validation drivers are absent. No task-owned Unity/player processes remained after qualification.
