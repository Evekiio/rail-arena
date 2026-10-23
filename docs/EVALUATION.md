# Rail Arena evaluation checkpoint

Launch `F:\Rail Arena\Builds\Evaluation\RailArena.exe`. Keep the whole Evaluation folder together. This is a non-development Windows player, built with Unity 6000.6.7f1. It is a working evaluation checkpoint, not final acceptance of the full objective.

## A focused first session (15–20 minutes)

1. **Solo movement (5 minutes):** PRACTICE, BOTS 0 // SOLO. Test mouse aim, WASD acceleration, air strafing, held-Space hopping, ramps, gaps and all four jump pads. Fall into the void and confirm respawn. Judge responsiveness and visibility while moving quickly.
2. **Controlled combat (3 minutes):** Restart with one stationary target, then one strafing target. Try head, torso and limb shots. Check rail recovery, hit/elimination feedback and respawn. Head damage is 100, torso 50, limbs 34; recovery is 1.5 seconds.
3. **Bot combat (5 minutes):** Restart with three combat bots on medium, then seven if desired. Try health pickups when injured. Judge target readability, reaction fairness, navigation, sound and pacing. Pickups restore 25, capped at 100, and return after 20 seconds.
4. **Presentation/settings (2 minutes):** Inspect hands, weapon, armor and animations while aiming, landing and firing. Try sensitivity/volume, resolution and fullscreen. Check pause, restart and return to menu. Tab holds the scoreboard.
5. **Optional timed match:** OFFLINE BOT MATCH runs ten minutes after a three-second countdown. Review scores/results and restart. Internet play is not needed for this evaluation.

## What this checkpoint establishes

| Goal | Current state | What your review can decide |
| --- | --- | --- |
| Immediate solo and 1–7 bot play | Implemented, standalone regression coverage | Movement feel, fun, difficulty and arena flow |
| Shared combat rules and match lifecycle | 37 standalone mechanics checks; 77 Editor tests | Visible aiming/hit alignment and readable feedback |
| Host-authoritative direct-IP duel | Implemented; current build has an impaired smoke; preceding build has clean/impaired full match and rematch evidence | Optional later two-human interaction review |
| Prediction/reconciliation | Receive-before-physics catch-up fix tested; corrections remain measurable | Smoothness remains an engineering/feel goal |
| Armored sci-fi characters and animation | Textured LOD models, locomotion/actions, aiming IK, articulated fingers and fitted railgun grip | Quality direction; cuff seams, intersections and action polish remain unfinished |
| Original arena, rail effects and audio | Implemented with editable original sources | Art/lighting polish, readability and sound-on quality |
| Editable project and Windows build | Pinned Unity project, public Git/LFS; preceding runtime build reproduced from a fresh GitHub checkout | Build workflow and local iteration |
| Full release acceptance | Incomplete | Final art, native control/multiplayer acceptance, real internet path and release packaging still need work |

## Useful feedback

For a bug, record mode, bot count, difficulty, exact steps, expected result and actual result. For quality feedback, identify the top three things that most affect your enjoyment. Separate movement/physics, combat/bots, models/animation, graphics/arena, audio and UI. Screenshots or a short clip help with intersections and readability.

Normal player log: `%USERPROFILE%\AppData\LocalLow\Evekiio\Rail Arena\Player.log`. Copy it after a failure before relaunching, since Unity can replace it.

See [build instructions](BUILD-AND-PLAY.md), [full objective](OBJECTIVE.md), [acceptance matrix](ACCEPTANCE.md) and [build-specific validation](VALIDATION.md). No second PC or internet duel is required from you at this checkpoint.
