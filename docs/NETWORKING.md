# Direct-IP duel networking

The networking core is implemented and tested independently of gameplay. Online play is not yet exposed in the game menu. The current runnable build remains an offline milestone.

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

## Remaining integration

The game still needs host/join/ready UI, host session ownership of both actors, client prediction and reconciliation, remote interpolation, validated 200 ms rewind, replicated combat presentation, results/rematch orchestration and disconnect presentation. The same movement and match rules must remain authoritative in offline and online modes. Two standalone clients must then complete matches and rematches under simulated latency/loss. Actual internet play requires a remote machine and network setup; loopback evidence cannot substitute for that test.

Planned default: UDP 7777, configurable by the host. Firewall and port-forwarding instructions will be verified against the integrated player before being presented as usable hosting instructions.
