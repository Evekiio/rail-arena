# Third-party notices

The new stylized player and three bots use original procedurally modeled visible geometry and palette textures under MIT. Their skeleton is retained from the CC0 Grab3D source described below, including its disclosed AI-assisted provenance; the shared Quaternius motion clips remain CC0 dependencies. The new first-person arms are extracted from that original stylized geometry. The approved concept sheet is AI-generated design-reference artwork. See `docs/STYLIZED-CONTENDERS.md`.

The historical first-person arm mesh is an arm-only CC0 derivative of the Grab3D contender, retaining its disclosed AI-assisted origin. It reuses the original textures and Quaternius CC0 idle motion. The extraction/attachment code is original MIT work; the historical base is not represented as an original Rail Arena sculpture. See `docs/FIRST-PERSON-ARMS.md`.

The added 30-joint finger rig, skin-weight preparation, fitted grip data and procedural trigger/support response are original Rail Arena work on that CC0 base. The modified mesh and textures retain the base's provenance; no third-party paid or restricted hand/animation assets were acquired. See `docs/HAND-ARTICULATION.md`.

The Foundry surface-art generator and its new panel textures, meshes and materials are original Rail Arena work under MIT, not third-party assets. See `docs/ARENA-ART.md` for editable sources and scope.

The RA-01 railgun geometry, Blender generation script, materials and presentation integration are original Rail Arena work under the repository MIT license. Its source and reproduction instructions are recorded in `docs/WEAPONS.md`.

The MIT license applies to original Rail Arena code. It does not relicense Unity, its runtime or its packages.

The Universal 3D template, URP, Input System, uGUI, test framework, Pipeline editor tooling and other Unity dependencies retain their upstream licenses. Their resolved versions and dependency tree are recorded in `RailArena/Packages/packages-lock.json`; package license files are available through Unity Package Manager and the resolved package cache. Unity Editor and player use remain subject to applicable Unity terms.

## Character model and textures

[Sci-Fi Soldier by Grab3D](https://grab3d.com/models/characters/sci-fi-soldier/) is used under CC0 1.0. The upstream license and AI-origin disclosure are preserved at `RailArena/Assets/ThirdParty/Grab3D/LICENSE.txt`. The source is AI-assisted: the publisher identifies Meshy-generated mesh/textures with Blender cleanup. Rail Arena adds topology-reduced LODs, Unity materials, hurtboxes, aiming integration and palette variants. This asset is not represented as hand-modeled original Rail Arena artwork.

The editable Blender master and processing report are in `ArtSource/ThirdParty/Grab3D`; the reproducible preparation script is `tools/prepare_contender.py`. The imported rigged source uses 2K textures. This differs from the publisher's separate static 4K asset.

## Character animation

[Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html) and [Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html), by Quaternius, are CC0 1.0. Only the free Standard downloads were acquired; no Pro or paid Source files were acquired. The original license is included under `RailArena/Assets/ThirdParty/Quaternius/License.txt`. The original free archives are retained under `ArtSource/ThirdParty/Quaternius`.

The character currently uses locomotion, jump, hit and death clips from the first library, retargeted through Unity's humanoid Avatar. Railgun aiming and recoil integration are original project code. No Mixamo files or restricted Asset Store character assets are included.

Files supplied in the local `References` folder are design references and are excluded from publication by default. Rejected character candidates under `ArtSource/Candidates` are also excluded. No third-party audio has been incorporated.

Quake III Arena and Unreal Tournament are named only to identify design inspirations. Their original code and game assets are not part of this project.

## User-supplied weapon shape references

Reference-only photographs under the rifle/carbine reference packages retain their original watermarks and source records. They are supplied design inputs, not original Rail Arena artwork or game textures; the project's software license does not grant rights to third-party photographic content, marks or branding. These photographs are not shipped in the playable client. Original carbine meshes, textures and rendered construction studies are authored separately.

## Interface typography and icons / October 29, 2026

Barlow Regular, Barlow Medium and Barlow Condensed SemiBold are by The Barlow Project Authors and distributed under the SIL Open Font License 1.1. The original copyright and full notices are preserved under `RailArena/Assets/ThirdParty/Barlow/`. The logo remains the original Rail Arena raster artwork; these fonts supply interface typography. Font source URLs and SHA-256 values are recorded in `ArtSource/Original/UI-Brand-v1/Fonts.json`.

The UI icon set is exported from Lucide 1.8.0 under its ISC license. The complete upstream license, including applicable Feather-derived icon notices, is retained at `RailArena/Assets/ThirdParty/Lucide/LICENSE.txt`. Library icon names, export parameters and editable SVG exports are retained in `ArtSource/Original/UI-Brand-v1/`. No proprietary game logos, interface artwork or fonts are bundled.
