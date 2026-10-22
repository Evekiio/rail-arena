# Direct-IP duel networking

The development player now exposes **ONLINE DUEL // DIRECT IP** alongside offline play. Initial host-authoritative gameplay integration has passed a two-process local smoke test. The complete online acceptance criteria are still unproven; this is not a finished release.

## Session integration

The host owns the shared 60 Hz `ArenaSession` authority loop for both human slots, including movement, firing, health, pickups, scoring and respawns. Offline play uses that same loop. Online matches contain no bot participants. An opt-in development validation driver can supply automated input to each process's own human slot.

The client predicts its movement with the shared controller integration and pad logic, retains up to 128 unacknowledged commands, restores the acknowledged host state, then replays remaining commands. Commands are redundantly batched, normalized before encoding and consumed no more than once per host tick. The host sends snapshots every three simulation ticks. The opponent is interpolated six ticks behind the estimated host time. Client-predicted fire produces effects but cannot apply damage; host shot and hit serials replicate combat presentation. Pausing opens local controls while the online authority continues.

Host/join controls accept an IPv4 address and configurable UDP port (default 7777). Both peers must ready up before the countdown. Results use the host winner and offer ready-up for a new round. These paths are implemented; complete timed results/rematch and native-input interaction still need acceptance testing.

## Implemented core

- `DuelSocket`: nonblocking IPv4 UDP with explicit port binding, deterministic disposal and datagrams limited to 1,200 bytes. Receive work is bounded to 64 datagrams per link update; malformed traffic consumes that budget too.
- `DuelProtocol`: versioned binary packets with exact lengths, finite/ranged input validation, two-actor authoritative snapshots and three pickup timers. Inputs contain movement, aim, jump/fire intent, sequence and viewed tick. They cannot carry damage, health or scores.
- `DuelLink`: one remote peer, build-identity comparison, cryptographically random client nonce, address-bound HMAC return-path challenge, session token, retries, ready-state repetition, monotonically newer packet acceptance and eight-second timeout. Advancing the round clears ready state. The application supplies the same nonzero build fingerprint to both peers.
- `DuelInputQueue`: bounded reordering/deduplication of redundant input batches, at most one consumed command per authority tick, and short motion continuation on loss. It never repeats fire/jump intent without a new command. Input sequences start at one and a new queue is created each round.
- `BuildIdentityGenerator`: automatically hashes runtime assets/code, package manifests and project settings before a player build. Text line endings are normalized so Git checkout conventions do not produce false incompatibility. Editor and test code, validation captures and the generated identity itself are excluded. The generated SHA-256 resource exposes a 64-bit wire token through `BuildIdentity`. Verification confirmed repeatability, line-ending independence, content-change detection and restoration of the original hash after removing a temporary probe (`Validation/network-build-identity.json`).

The transport follows [.NET's documented nonblocking UDP behavior](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receivefrom). No service accounts, discovery servers or cloud relays are involved. The challenge verifies return-path access; it does not encrypt traffic or provide account identity or protection against an on-path attacker.

## Evidence

`Validation/network-core-editmode.json` records 20 passing cases: the previous 11 simulation/character cases and nine networking cases. Networking checks exercise real loopback UDP sockets: handshake, build mismatch, ready state, command and snapshot delivery, round reset, graceful leave, missing-peer timeout, port rebinding, oversized packets, wrong-session and reordered traffic, forged challenge responses and a full host. Codec tests also reject every prefix of a valid input datagram, trailing bytes, invalid values and 2,000 random datagrams. The input queue verifies deduplication, ordering, bounds and the per-tick consumption limit.

These are transport and protocol tests, not two-client standalone match acceptance, simulated internet impairment, or remote-network verification.

`Validation/online-session-editmode.json` records 22 passing cases after integration, including authoritative replica state and outbound normalization regressions. `Validation/online-session-build.json` records the successful Windows build.

`Validation/Duel-Corrected/host.json` and `client.json` record two separate hidden Windows player processes connected over real loopback UDP with automated inputs. The host recorded 30 shots, 13 deaths and 10 pad launches. The client received 898 snapshots, acknowledged 2,690 inputs and measured a maximum continuous-life prediction correction of 0.0449 m. Both reported zero errors and passed. Reports are sampled separately, so their final ticks and shot counts need not match. This 45-second smoke test does not reach the ten-minute results boundary or prove rematch, impairment tolerance or internet connectivity.

The initial run is retained under `Validation/Duel-Initial`: client serialization failed because rotated unit movement occasionally produced a component of `1.00000012`. Sender normalization fixes this without relaxing the receiver's bounds. The corrected run observed 99 such normalizations and no serialization errors. Reusing the shared authority also passed a fresh seven-bot offline soak (`Validation/online-change-offline-regression.json`): 143 shots, 62 deaths, four pickups, 45 pad launches, zero errors and zero void deaths.

The connection panel was visually inspected in a live, advancing Editor session. Its button action was invoked through Unity APIs; native keyboard entry and mouse interaction are not established by that capture.

## Remaining integration

Validated 200 ms hit rewind is not implemented; online shots currently query live host hitboxes. Prediction/interpolation and replicated presentation need impairment and visual acceptance. Two standalone clients must complete full matches, results, rematches and disconnect/error scenarios, including simulated latency and loss. Native connection-menu interaction remains unverified. Actual internet play requires a remote machine and network setup; loopback evidence cannot substitute for that test.

For a local development smoke test, run `tools/Test-Duel.ps1`; it uses UDP 17777 by default and launches two hidden players. Manual LAN/internet hosting instructions and remote-network verification remain part of final delivery.
