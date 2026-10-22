# Reference review - October 22, 2026

All ten supplied images in the local References folder were visually reviewed. The reference files remain outside public source control.

The Quake screenshots and overhead map images emphasize broad floating decks, exposed gaps, stacked routes, a long bridge, visible launch surfaces and strong silhouettes against empty space. The Facing Worlds images emphasize vertical landmarks and a planetary backdrop. The modern Unreal Tournament interior emphasizes industrial paneling, directional accents, ramp readability and restrained HUD placement. The older enclosed Quake image adds warm material contrast and architectural mass.

## Original Foundry interpretation

An orbital industrial facility uses a rectangular lower loop, two raised wings joined by a narrow elevated bridge, four ramps, four cyan launch coils, three contested health positions and warm perimeter accents. Dark underslung supports and reactor pylons give the suspended decks visual mass. White-gray alloy, blue steel, amber structure lights and cyan energy distinguish geometry, hazards and interactive objects.

No supplied map is traced or reproduced. Current geometry, contender models and weapon are an editable first gameplay pass, not final polished art. The starfield is an original seeded mesh. The rail discharge sound is original procedural audio. Final art and feedback quality remain subject to visual and playtesting iteration.

## Runtime findings so far

Initial routing incorrectly sent every bot to node zero, causing falls. Bots now enter at their nearest node and reset their route after respawn. Launch links are directed toward landing platforms. Launch velocity bypasses grounded integration for two ticks so the previous controller contact does not cancel it.

The initial double-sided star mesh used a lit material with opposing face normals. Its post-processed output produced large white halos. An unlit star material and bounded bloom removed the halos in a subsequent live capture.
