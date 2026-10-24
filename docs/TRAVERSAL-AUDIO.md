# Local pickup and jump-pad audio

The local player now gets a short two-note health-pickup chime and a rising electromagnetic pad-launch cue. Both use original deterministic synthesis, the existing local effects source, master volume and effects volume. Their relative gains are .35 for health and .40 for launch; the six existing/new cue clips are created once and released with the feedback component.

Offline/host pickup audio follows the actual authoritative collection, only for the local collector. Live pad simulation/prediction plays the local launch cue. Reconciliation replay updates movement and pad readiness silently, so replaying pending inputs does not repeat the sound or add authority statistics.

A duel client confirms pickups from an observed health increase within the same living actor/round. Duplicate or older snapshots, opponent recovery, death, respawn and a new round do not trigger a local pickup chime. Under current rules, same-life health recovery comes from pickups. This is best-effort presentation: a collection hidden by intervening damage between snapshots may not be heard. A future regeneration implementation must distinguish regeneration from pickup events before reusing this inference.

This pass adds personal feedback only. Positional audio for other players' traversal, footsteps, landing, ambience, music and guaranteed event delivery remain separate work. No damage, health amount, pickup cooldown, pad impulse, movement, input or network schema is changed.

## Review

[Audio and settings review](../Validation/TraversalAudio/index.html) includes [Pickup.wav](../Validation/TraversalAudio/Pickup.wav) and [Launch.wav](../Validation/TraversalAudio/Launch.wav), exported from the tested source samples. These are mono 44.1kHz / 16-bit PCM previews before master/effects and in-game one-shot gains. They do not represent final audible combat mix acceptance.

The current audio-page captures at 720p/1080p update its effects description to include local health/pad sounds. They are actual uGUI geometry/material/tint reviews against a neutral background, not human gameplay recordings. Historical audio settings captures remain preserved.

## Scoped validation

Twelve selected tests cover all six synthesized cues and the fixed visual/audio pool, the existing audio-mix flow, local/remote collector distinction, repeated/stale/new-life snapshots, actual pending-input pad replay and existing pad-readiness synchronization. Cue samples have finite bounded values and faded endpoints. Replay changes motion without emitting another launch cue or incrementing authority pad statistics.

A pinned Unity 6000.6.7f1 headless Windows development build validates integration. No unrelated snapshot/full-game suite, broad combat soak, physical audio-output check, sound-on listening or FPS qualification is included. Human recognition, loudness and combat readability review remain open. The public pre-alpha ZIP is unchanged.
