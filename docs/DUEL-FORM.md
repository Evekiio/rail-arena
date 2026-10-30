# Recoverable duel connection errors

Invalid connection attempts now keep the player on the host/join form. A dedicated error card sits beneath the connection instructions, with address and port entries retained for correction or retry. Previously the generic refresh returned to the main menu and placed the error over its controls area.

Editing either field clears the stale message without rebuilding the form. Back dismisses the message and returns to the main menu; the draft remains available if the player opens Online Duel again during the same app session. The instructions also clarify that 127.0.0.1 connects to the same computer, while another PC needs the host's IPv4 address and matching UDP port.

Existing connection validation remains authoritative. Hosting still ignores the address field and validates the selected port. Socket, connection and compatibility failures use the same form error route after returning out of a session. Successful hosting retains the existing ready room and disconnect behavior. No transport, wire format, match rule, firewall configuration or saved network preference is changed.

Socket failures use actionable English: an occupied port asks for a different port, access denial suggests another port or checking network permissions, and other socket failures suggest checking the port/network settings. The original socket error code remains in the diagnostic log.

## Focused validation

The scoped UI flow checks invalid ports, IPv6 rejection for the IPv4 join field, retained drafts, clearing errors on edit/back, a real occupied UDP port, successful retry after releasing that port, and listener cleanup through Disconnect. It also checks visible text and graphic bounds at 720p and 1080p. [Test report](../Validation/DuelForm/tests.xml).

[Review gallery](../Validation/DuelForm/index.html) contains actual uGUI mesh/material captures on a neutral background for the normal form, invalid address and occupied-port states. These are controlled UI reviews, not human gameplay screenshots.

The Windows build verifies compilation and asset packaging. Combat and protocol suites were not rerun because their implementation is unchanged. External networking, physical text entry, human usability and performance acceptance remain separate. The public pre-alpha ZIP remains unchanged.
