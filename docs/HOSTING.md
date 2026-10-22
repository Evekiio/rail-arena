# Hosting a direct-IP duel

Rail Arena supports exactly two humans with one player hosting authority. There are no accounts, matchmaking servers or relays. Both players need the same complete Windows build, including `RailArena_Data` and the accompanying DLLs. Matching an executable filename is insufficient: the embedded source/asset fingerprint must agree.

The game menu defaults to **UDP 7777**. Automated validation scripts use different default ports to avoid colliding with manual play.

## Same computer or LAN

1. Launch the host, choose **ONLINE DUEL // DIRECT IP**, enter a port and select **HOST DUEL**. Keep it running at the waiting screen.
2. On a second instance on the same PC, use host address `127.0.0.1`. On a second PC on the LAN, use the host PC's LAN IPv4 address instead. Enter the same port and select **JOIN HOST**.
3. Both players select **READY / CANCEL READY**. The host starts the three-second countdown once both are ready.
4. Play the ten-minute match. Tied leaders continue to next-kill sudden death. On results, both select **READY / CANCEL REMATCH** for another round. **DISCONNECT** returns to the menu.

On the host, `ipconfig` displays adapter IPv4 addresses and default gateways. Use the active Ethernet or Wi-Fi adapter's IPv4 address, not a disconnected adapter or an unrelated VPN. Microsoft's [ipconfig reference](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/ipconfig) describes these fields.

## Windows Firewall

The host must permit inbound UDP for the selected executable and local port. If Windows prompts when hosting, allow the trusted network profile on which you intend to host. The following is an optional explicit rule for a **Private** network. Run it yourself in an elevated PowerShell from the extracted build directory:

```powershell
$railExe = (Resolve-Path -LiteralPath '.\RailArena.exe').Path
$railPort = 7777
New-NetFirewallRule -Name "RailArena-UDP-$railPort" -DisplayName "Rail Arena UDP $railPort" `
    -Direction Inbound -Action Allow -Protocol UDP -LocalPort $railPort `
    -Program $railExe -Profile Private
```

`Get-NetConnectionProfile` shows the active network category. A rule limited to Private does not apply on Public. Choose the intended profile deliberately rather than disabling the firewall or changing every interface. If the game is moved, recreate its program-path rule for the new location. Managed devices may require an administrator's policy change. The game's application code does not create rules or elevate itself. Rule parameters follow Microsoft's [New-NetFirewallRule reference](https://learn.microsoft.com/en-us/powershell/module/netsecurity/new-netfirewallrule).

Remove the example rule when it is no longer needed:

```powershell
Remove-NetFirewallRule -Name 'RailArena-UDP-7777'
```

## Internet hosting and router forwarding

First verify that two LAN players can connect. Then use the router's own administration interface and model-specific manual to reserve the host PC's LAN address and add a single forwarding entry:

| Field | Example |
| --- | --- |
| Protocol | UDP |
| External/WAN port | 7777 |
| Internal/LAN destination | The host PC's reserved LAN IPv4 address |
| Internal port | 7777 |

The joining player on another network enters the host router's current **public IPv4 address**, with the external port. The host still listens on its internal port. Keep both ports identical for this first setup. A public IPv4 address is shared with the intended challenger privately; it is not an account credential. No router username, password or account token belongs in the game. Forwarding field names vary by model; [TP-Link's manufacturer guide](https://www.tp-link.com/us/support/faq/1379/) illustrates the external-port, internal-address, internal-port and protocol mapping.

Double NAT may require configuration at both routers. Carrier-grade NAT or an ISP without inbound public IPv4 may prevent this setup; contact the network operator about inbound connectivity or host on a connection that provides it. A router WAN address in `100.64.0.0/10` is within the provider shared range defined by [RFC 6598](https://www.rfc-editor.org/info/rfc6598/), and is a useful diagnostic clue. Private WAN addresses or a WAN address different from the connection's public address are also reasons to investigate upstream NAT. Rail Arena has no automatic NAT traversal, UPnP, IPv6 or DNS-name join support in this version.

Test from a genuinely different connection. A second PC behind the same router using the public address may depend on router loopback support and does not prove an internet path.

## Troubleshooting

| Observation | Check |
| --- | --- |
| Invalid address | Enter a literal IPv4 address. Hostnames and IPv6 are not accepted. |
| Invalid port | Use an integer from 1 through 65535. |
| Could not open UDP socket / address in use | Stop the other host on that port or select a free port. |
| Build mismatch | Transfer the same complete build to both PCs and restart both. |
| Host is full | Only one challenger may occupy the host's second slot. |
| Join times out | Host still listening; address and port correct; correct firewall profile/program path; UDP forwarding and upstream NAT checked. |
| Disconnect / timeout in play | Verify both applications and the host connection remain running. A lost leave packet is handled by the connection timeout. |
| Local port appears open in a TCP test | Rail Arena uses UDP. A TCP port probe does not establish a game handshake. |

## Evidence and limits

Real two-process localhost matches, full ten-minute match/rematch runs and simulated delay/jitter/loss are documented in `NETWORKING.md` and `VALIDATION.md`, each tied to its build. **A human duel across two actual remote networks is still unperformed.** These instructions have been checked against the implementation and official command documentation; no firewall/router settings were changed to prepare this guide. Native two-human lobby, results and rematch interaction still require acceptance. Record build fingerprint, endpoint roles, router/firewall setup, complete match result and successful rematch when performing that test. Keep private network details out of the public repository.
