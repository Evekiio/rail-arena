# Rail Arena character references v1

Open `index.html` for the offline reference viewer. Use the files in this `Final` folder. Earlier candidates and the rejected axial attempt outside this folder are not modeling references.

## Coverage

Each of Player, Bot01, Bot02 and Bot03 has four sheets:

| Sheet | Coverage |
| --- | --- |
| Turnaround | Front, front-right 45, right profile, back-right 45, back, back-left 45, left profile, front-left 45 |
| Head | Large front, right, back and left head views, crown inspection and under-jaw inspection |
| Details | Four head views; crown; high and low body inspections; right glove back and palm; right boot outer and inner sides, top and sole; torso front/back; belt and hip panels front/back |
| Axial | Whole-body top and bottom concepts constrained by headless orthographic camera/occlusion guides |

This provides 32 full-body turnaround views, eight full-body axial views, 24 large head inspections and four equipment/detail sheets across sixteen selected PNGs. The large head sheets are the best place to inspect facial anatomy, hair roots and the neck/collar transition. Sole cards define the otherwise unseen tread. High/low body views reveal depth and overlap that front and side silhouettes hide.

These are generated concept illustrations, not calibrated photographs or projections rendered from a finished common mesh. High/low and under-jaw inspections must not be treated as exact 90-degree camera projections. They establish the intended appearance and construction; they do not prove pixel-perfect geometric registration or a perfect finished 3D character. No metre dimensions, orthographic calibration or final model acceptance is implied.

The axial sheets use actual headless Blender top/bottom renders as camera/occlusion guides, with the new turnarounds/details supplying the appearance target. The guide meshes are the older game meshes, not the intended design authority. Their rough shoulders, hair, gloves, blank soles and other visible details were replaced or corrected in the generated concepts. The axial artwork remains a guided reconstruction rather than a calibrated render of the final intended mesh. In both axial rows front/toes point down the sheet; top-view anatomical right is image left, bottom-view anatomical right is image right. The soles rotate relative to the sole detail cards to maintain that convention. The guide renders and intermediate/rejected attempts are retained locally outside `Final`; they are not published as alternate character designs.

## Design authority

`Approved-Concept.png` is the creator-approved starting design. The new sheets expand its coverage; they are review candidates, not a new creator approval. Back surfaces on the bots, glove palms, boot tread and underside transitions that were not visible in the original are proposed continuations of the existing design.

Use the body turnaround for the overall silhouette and garment lengths, the head sheet for facial and hair forms, and the detail sheet for small closures, seams and sole construction. Preserve the original concept's identity, palette and major armor arrangement when resolving small illustrated differences. Do not combine different candidates into another costume. If two drawings imply incompatible geometry, resolve that explicitly in the single 3D master before calling it finished; do not average or silently mirror an asymmetric feature.

## Fixed character features

Left/right always means the character's anatomical side. In a front view, the character's right is on the viewer's left; in a back view it is on the viewer's right. Profile labels in this pack use nose-right for right profile and nose-left for left profile.

| Character | Identity and materials | Features to preserve |
| --- | --- | --- |
| Player | Athletic adult man; warm tan skin; angular face; dark swept polygonal hair; ivory armor, charcoal suit, teal scarf, cyan insets | Compact ivory chest/back shells; two broad ivory-bordered thigh panels; paired belt pouches; fitted dark/ivory boots; no diagonal chest sash or backpack |
| Bot01 | Athletic adult woman; warm tan skin; dark swept bun; terracotta armor; charcoal suit; amber trim and brass hardware | One utility holster on the RIGHT upper thigh only; plain LEFT thigh tab; amber collar/forearm/knee/boot accents; rust upper-back shell; no large glowing chest chevron |
| Bot02 | Athletic adult man; fair warm skin; pale swept hair; dark ash eyebrows; slate/teal suit and chest; pale protective plates | Pale clavicle, shoulder, forearm and knee plates frame teal central surfaces; teal rear cowl; pale-bordered thigh panels; do not substitute the player's solid ivory chest |
| Bot03 | Athletic adult woman; deep warm brown skin; plum armor; charcoal suit; copper hardware | Long locs on RIGHT, cropped/shaved rows on LEFT; longer plain RIGHT thigh tab and shorter copper-edged LEFT flap; no large diagonal copper chest stripe; no teal scarf |

### Small construction decisions

- Player's right boot outer ankle carries the cyan inset; its inner-side detail card uses a plain dark ankle joint. Do not mirror the outer cyan part onto that inner surface.
- Gloves are fingerless, with five fingers per hand. Back-of-hand plates and palm padding are different surfaces of the same glove, not two glove variants.
- Belt pouches are separate from Bot01's single right-thigh holster. A belt pouch visible from a side or rear view does not authorize another thigh holster.
- Shoulder, forearm and knee armor is fitted over dark articulated fabric. Do not model bright gaps through the body or replace the suit with disconnected plates.
- Metal, fabric, skin and emissive insets need separate material behavior. Colors sampled from lit drawings are not linear PBR base-color values.

## Modeling and verification

1. Work from a single editable master for each character. Keep one fixed neutral pose and one scale while establishing the silhouette. Place the floor at the sole baseline; align head, chin, shoulders, belt, knee and ankle landmarks. Do not infer height from the page dimensions or compare cropped close-up scale to full-body scale.
2. Block the front and side volumes together, then inspect the back and both three-quarter directions. Preserve left/right features on the mesh itself instead of mirroring final asymmetric parts.
3. Sculpt the face as a connected anatomical volume, including eye sockets/lids, nose bridge, cheeks, jaw, ears and neck. Hair is layered directional clumps, not a row of identical cones. Match the identity across front, side, rear, crown and under-jaw inspections.
4. Add the layered suit, cloth panels and armor as coherent overlapping construction. Check actual shoulder/hip clearances and boot/sole joins from above and below on the master.
5. Render the master using one camera rig at all eight turnaround directions plus elevated, lowered, top and underside views. Only these shared-mesh renders can establish exact geometric consistency. Compare those real renders against this pack before final artistic approval.
6. After deformation/rigging, inspect locomotion, finger closure, armor collisions, LOD silhouette and first-person wrist continuity separately. Reference quality does not prove animation or game integration quality.

Blender and Unity work remains headless by default. This reference task does not change game meshes, prefabs, hit regions, animation or builds.

## Review and provenance

The sheets were produced using the built-in `image_gen` tool with the original concept supplied as the identity/style reference, then the relevant turnaround/detail sheet supplied for further views. `PromptSet.json` and `Axial-Prompts.json` record prompts, corrections and generated output provenance. `manifest.json` records the final selected PNGs, dimensions and SHA-256 hashes.

The visual pass corrected Bot01's duplicated front-left angle and figure scale, Bot03's swapped profiles/repeated front direction and incorrect head profile, and the player's duplicated boot side and misleading high/low labels. The first unguided full-body axial sheet was excluded because it introduced perspective and changed tread construction. The selected individual axial sheets were regenerated using camera guides, then corrected for closed shoulder shells, palm surfaces, anatomical-side labels, sole orientation and character-specific tread materials. These image checks are separate from future exact shared-mesh verification.

Review the proposed unseen surfaces before consolidating them into the next model revision. The concept's previous approval remains intact; these new reference extensions and the eventual 3D result have not been marked creator-approved.
