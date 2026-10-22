# Development delivery

The public source repository is [Evekiio/rail-arena](https://github.com/Evekiio/rail-arena), with original code under MIT and asset licenses recorded in `THIRD-PARTY-NOTICES.md`.

Push every tested milestone to GitHub as work progresses. Preserve the full project objective; a pushed milestone is not a completed release. Include meaningful implementation changes, associated tests and accurate validation notes in each milestone. Verify that the remote branch contains the new commit after pushing. Keep failed evidence when it explains a corrected regression.

Use Git LFS for the binary patterns in `.gitattributes`. Supplied references, rejected asset candidates, Unity caches, local logs and Windows builds are excluded from source control. Do not add credentials or account tokens. A Windows download/release will be prepared separately from source publication.

Current user direction: handle duel validation through automation without requiring the user to playtest multiplayer now. Prioritize mechanics, physics, graphics, models and bot or solo playtests. Keep the actual remote-network path accurately marked unverified; do not block local enhancement work on arranging a remote player.
