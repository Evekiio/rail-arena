# Menu button feedback

Shared menu actions now use distinct normal, hover, selected, pressed and disabled colors, with an 80ms transition. Disabled actions also mute their captions. The existing uGUI Button event, navigation and interactable logic is retained through a small subclass; gameplay and action callbacks are unchanged.

This applies to the shared action-button helper, including main/pause menus, audio, frame pacing and connection actions. The four portrait cards keep their dedicated appearance-selection styling. A disabled bot-match action at zero bots, an inactive FPS limit under VSync and a muted sound preview are now visually distinct from available actions.

## Capture correction

uGUI already assigned the target graphic correctly. The old review helper drew generated mesh/material colors but omitted CanvasRenderer tint, where uGUI ColorTint transitions live. It now multiplies renderer tint into the review mesh colors, matching the [CanvasRenderer color contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CanvasRenderer.SetColor.html).

Historical neutral-background screenshots remain useful layout records, but should not be treated as complete evidence of button-state colors. They remain preserved. New [button-state captures](../Validation/ButtonStates/index.html) include renderer tint and explicitly scripted pointer/focus states. These are actual uGUI geometry/material reviews, not physical mouse/keyboard or native-window acceptance.

## Scoped validation

MenuButtonTests exercises uGUI pointer enter/down/up/click methods and EventSystem selection, verifies distinct state tints, checks that disabled clicks do not launch a match, confirms help actions remain usable and checks dynamic disable/re-enable caption states. Captures cover 720p/1080p. For stable state snapshots, this fixture sets its transition duration to zero; the production default remains 80ms.

The relevant FramePacingHudTests and AudioMixHudTests fixtures also pass, covering their existing dynamically disabled controls. Only these three tests are selected; unrelated menu/gameplay suites are not rerun. A pinned Unity 6000.6.7f1 headless Windows development build checks integration.

Human contrast/interaction review, physical-input acceptance and performance qualification remain open. No new input bindings, gameplay rules or network fields are introduced. The public pre-alpha ZIP remains unchanged.
