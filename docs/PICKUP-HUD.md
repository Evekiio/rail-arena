# Health-pickup HUD readout

Local health pickups now show a restrained amber `+amount` inside the health card. A normal collection can show `+25`; a capped collection from 98 to 100 shows `+2`. The current three-digit health value and meter remain authoritative, and low-health red is preserved even when a pickup occurs.

The readout lasts 1.25 seconds of local real time and fades over the final 0.25 seconds. Damage cancels it immediately; death hides it and feedback reset clears its amount/timer. Nonpositive gain notifications are ignored. The latest gain replaces the previous readout rather than accumulating a total. Pausing hides the HUD while real-time expiry continues.

This is text in the existing health panel, with no full-screen flash or aiming-area overlay. It remains visible with effects volume at zero and reduced-flash mode enabled. Pickup audio, health restoration, availability timing, damage, movement, scoring and the network schema are unchanged.

Offline/host authority supplies the actual restored difference. A duel client supplies its observed positive same-life health difference under the [current local pickup feedback rules](TRAVERSAL-AUDIO.md). That is a net observed gain, not a durable per-pickup event log; damage between snapshots can reduce or hide a confirmation. New-life and round transitions do not create a pickup notice.

## Review and scoped validation

[Review gallery](../Validation/PickupHud/index.html) shows +25, capped +2 and a pickup that still leaves low health, at 720p/1080p. Captures use the actual authority collection path with controlled starting health, muted effects and reduced flash. They draw actual uGUI meshes/materials/tint against a neutral background, not human gameplay recordings.

Five selected tests cover the new HUD flow, the existing combat HUD, local snapshot confirmation and authoritative collection at 50/98 health. Checks include exact amounts, muted visibility, text fit, preserved low-health warning, expiry, damage cancellation, death hiding, reset and nonpositive suppression. Expiry is forced in the fixture rather than using a real-time sleep.

A pinned Unity 6000.6.7f1 headless Windows development build checks integration. No unrelated synthesis/full-snapshot/full-game suite, broad combat soak, human readability or performance qualification is included. The public pre-alpha ZIP is unchanged.
