# Authored combat reactions

The Sky Gardens art graph installs an original 18-frame chest response and 72-frame fall/settle at 60 Hz. Both states play at speed 1. The existing shared skeleton, damage, movement, collider dimensions, weapon cadence, two-second respawn and network protocol remain authoritative. No ragdolls or knockback are introduced. Legacy clip assets and controllers remain preserved.

## Presentation and lifecycle

`HitResponse` uses the existing masked upper-body reaction layer. `DeathFall` has a brief give, backward fall, contact and relaxation into grounded feet and open palms. The terminal pose holds until respawn. Canonical geometry, palm/foot contacts and the pelvis pose are authored in Blender; animation never moves the actor root.

On death, the reaction layer is suppressed and its pending trigger cleared, so an active hit cannot override the corpse torso. Live grip posing stops and authored hands can relax. Dead look-at weighting clears. Respawn restores the prior reaction weight, live hand posing, weapon visibility, hitboxes and standing model offset. Repeated death notifications do not replace the saved reaction weight with zero.

## Measured floor support correction

The initial retargeted pilot exposed a mismatch in the existing six-bone/radius approximation: skin penetrated about 6 cm during entry and the settled body floated about 9 cm above the floor. That failed record remains in `Validation/motion-reactions-installed.xml`, with raw data under `Validation/ReactionPlayback-LegacySupport/`. Those figures describe the early authored pilot using old support, not every pose of the legacy release clip.

Refined characters now cache extremal skin vertices in each dominant bone's bind space at art installation. There are 1,020-1,029 selected points per character versus 19,430-21,548 full skin vertices. Their actual four-weight skin transforms provide cosmetic floor support only. The cache is reused across deaths; contact processing creates no new mesh or physics objects. Legacy characters without the art binding retain their previous support path.

The full-skin review independently evaluates every vertex, rather than validating the sampler against itself. Across the final 1,200 character samples, the minimum skin height is 6.822 mm; settled corpse contact is about 10 mm above the flat reference floor. Cache contact processing measured 0.103-0.117 ms mean per character in a warm, 1,000-call terminal-pose Editor probe. This is not a player frame/GPU result or proof of sustained 60 FPS.

## Source and evidence

- [Canonical Blender master, six-view pose sheets and native-rate reels](../ArtSource/Original/ArtPass-v1/Animation/Reactions/index.html).
- [Construction, rig, timing and source hashes](../ArtSource/Original/ArtPass-v1/Animation/Reactions/reactions-production.json).
- [Full-skin and lifecycle evidence](../ArtSource/Original/ArtPass-v1/Animation/Reactions/review-evidence.json).
- [Final lifecycle check](../Validation/motion-reactions-lifecycle.xml): four contenders, hit active immediately before death, floor contact, body facing, unchanged actor root, repeated death, reaction suppression, hand/weapon/hitbox lifecycle and reset at the existing 120-tick respawn interval.
- [Controlled legacy reference capture](../Validation/motion-reactions-legacy-reference.xml): the unchanged shared `Death01` state at its existing 1.3 speed, prior bone support and static grip. Camera, scale, lighting and 1.5 s elapsed death time match the comparison. It is a source reference, not an older executable recording or a baseline correctness pass.

The two-second dead portion is followed by one second of respawn presentation in the five-second silent reel. Canonical sheets render the saved geometry in front, rear, left, right, top and bottom views at a shared 2.5 m span. Rest geometry, rig and attachment references are inherited from the character family.

## Reproduction and remaining scope

Run Blender 5.2 background jobs with `author_contender_reactions.py`, then `render_contender_reaction_refs.py`, passing the repository root after `--`. Run `ContenderMotionArtBuilder.Prepare` in graphics-disabled Unity batch mode. Run `CombatReactionArtReview.InstalledReactionsRetainContactsDeathAndRespawnLifecycle` in hidden graphics-enabled batch mode, setting `RA_REACTION_REVIEW_OUTPUT` to `Validation/ReactionPlayback-Acceptance`. Run `CaptureSharedLegacyDeathReference`, then `package_reaction_review.py`. Choose new raw output names when revising evidence; earlier failure and pilot records are preserved.

Authored crouch/slide/traversal, all interrupted combat/stance combinations, death on collision edges/slopes, complete LOD contact coverage and remaining first-person actions are still production work. Native input, sound, sustained performance and artistic acceptance remain Sanders' final gate. The staged review is not full gameplay acceptance.
