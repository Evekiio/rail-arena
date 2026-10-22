# Third-party notices

The first-person arm mesh is an arm-only CC0 derivative of the Grab3D contender described below, retaining its disclosed AI-assisted origin. It reuses the existing textures and Quaternius CC0 idle motion. The extraction/attachment code is original MIT work; this is not a claim that the underlying character was sculpted originally for Rail Arena. See `docs/FIRST-PERSON-ARMS.md`.

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
