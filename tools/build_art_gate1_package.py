"""Build the Gate 1 inventory, review gallery and annotated geometry sheets from audit evidence."""
import argparse
from collections import Counter
import csv
from datetime import datetime, timezone
import hashlib
import html
import json
import math
from pathlib import Path
import re
from urllib.parse import quote

from PIL import Image, ImageDraw, ImageFont

VERSION = "rail-arena-art-production/1"
DISPOSITIONS = {"retain", "refine", "rebuild", "replace", "archive-reference"}
ART_EXTENSIONS = {".blend", ".fbx", ".glb", ".gltf", ".png", ".jpg", ".jpeg", ".tga", ".webp", ".svg", ".wav", ".ogg", ".mp3", ".mp4", ".webm", ".ttf", ".otf", ".json", ".csv", ".md", ".txt", ".html", ".pdf", ".zip"}

# These are authoring associations for review, not assertions of source/export identity.
FAMILIES = [
    ("weapon-current", "Precision carbine, optic, magazine and actuators", "Weapons", "All gameplay", "refine", "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend", "precision-carbine", "Retain measured silhouette and open ADS optic; improve cel surface separation and magazine/action presentation. Repacked runtime candidate parity remains to be certified."),
    ("weapon-legacy", "Original induction railgun", "Weapons", "Legacy", "archive-reference", "ArtSource/Original/Railgun/Railgun.blend", "", "Preserve editable source and earlier exports; not the current precision-carbine art target."),
    ("player", "Player character", "Characters", "All gameplay", "refine", "ArtSource/Original/StylizedContenders/Player.blend", "player", "Retain identity; refine anatomy, face, hair, armor, cloth and deformation against approved direction."),
    ("bot01", "Bot 01 / terracotta and tied hair", "Characters", "All gameplay", "refine", "ArtSource/Original/StylizedContenders/Bot01.blend", "bot01", "Distinct head and armor silhouette, shared compatible skeleton and gameplay bounds."),
    ("bot02", "Bot 02 / slate-teal and pale swept hair", "Characters", "All gameplay", "refine", "ArtSource/Original/StylizedContenders/Bot02.blend", "bot02", "Preserve identity and common gameplay dimensions; refine face, hair flow and readable cel planes."),
    ("bot03", "Bot 03 / plum, copper and locs", "Characters", "All gameplay", "refine", "ArtSource/Original/StylizedContenders/Bot03.blend", "bot03", "Preserve identity and hair silhouette; refine facial anatomy, garment structure and silhouette."),
    ("player-pilot", "Isolated player pilot", "Characters", "Candidate only", "archive-reference", "ArtSource/Original/PlayerPilot-v1/Player-Pilot.blend", "", "A separate candidate, not the installed player. Evaluate useful construction details without silently replacing gameplay assets."),
    ("fps-arms", "First-person arms, hands and grip contacts", "First-person", "First-person camera", "rebuild", "", "", "Derive an intentional arm/hand master from the approved player rig; create action and camera-clearance sheets."),
    ("lower-kit", "Lower arena, ramps, bridge and underpass kit", "Environment", "Lower arena / middle bridge", "refine", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "", "Keep collision footprint and gameplay route geometry. Add intentional trims, edge construction and structural supports."),
    ("summit", "Summit deck, central cover and columns", "Environment", "Summit +22 m", "refine", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "summit", "Retain the four diagonal launch sectors, open exits and existing jump-pad landing clearances."),
    ("basement", "Four staging and basement deck assemblies", "Environment", "Staging +14 m / basement -10 m", "rebuild", "", "basement-deck", "Current decks originate as Unity primitives. The measured sample is a geometry-derived reference copy; author a proper modular Blender master without changing tested jump gaps."),
    ("cover", "Cover, parapets, piers and supports", "Environment", "Combat lanes", "refine", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "", "Readable knee/chest/full cover proportions; preserve sightlines and avoid outline noise against opponents."),
    ("foliage", "Trees, bark, shrubs, grass and vines", "Nature", "Gardens and platform edges", "rebuild", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "", "Shaped leaf clusters, painted accents and controlled wind. Existing texture seams and tangent gaps require review."),
    ("water", "Waterfalls, water lips, foam and spray", "Nature", "Garden edges / basement backdrop", "refine", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "", "A shared light-responsive cel water style with clear layered foam; preserve traversal visibility and restrained spray."),
    ("sky", "Sky, cloud sea and distant ruins", "Nature", "Backdrop", "refine", "", "", "Separate gameplay and noncolliding scenery. Use quiet distant values, distinct levels and restrained atmosphere."),
    ("health", "Health beacon and animated pickup", "Interactive", "Three health positions", "refine", "", "", "Create a canonical assembly and dimension/socket sheet; readable available/depleted states, float and spin."),
    ("portals", "Portal frames, fields and live destination views", "Interactive", "Arena and basement returns", "refine", "", "", "Preserve gate openings, backing, exit capsule clearance and existing teleport rules. Deferred ADS/projection issue is tracked separately."),
    ("pads", "Summit launch pads and basement return bounces", "Interactive", "Four summit launches / two basement returns", "refine", "", "", "Canonical Blender device, clear landing/return destination language and charge/burst/reduced-flash states."),
    ("signage", "Route numbers, takeoff markers and destination signs", "Interactive", "All traversal", "rebuild", "", "", "A consistent measured sign/decal kit, readable without enlarging or obstructing the gameplay surface."),
    ("animation-fps", "First-person weapon and arm action library", "Animation", "First-person camera", "rebuild", "", "", "Authored action references and clips; match current fire, ADS, reload and interrupt timing."),
    ("animation-body", "Shared character locomotion and action library", "Animation", "All characters", "refine", "", "", "Retain compatible licensed foundation where suitable; replace reversed/accelerated substitutes with authored directional actions."),
    ("weapon-vfx", "Muzzle, shot, impact, hit and elimination feedback", "VFX / lighting", "Combat", "refine", "", "", "Short readable cel effects, correct muzzle origin, reduced-flash handling and sound synchronization."),
    ("traversal-vfx", "Portal, pad and pickup effects", "VFX / lighting", "Traversal", "refine", "", "", "Keep the distracting cyan GPU cloud disabled; introduce only readable silhouettes and event-driven accents."),
    ("lighting", "Cel lighting, shadows, grading and atmospheric tiers", "VFX / lighting", "Sky Gardens", "replace", "", "", "Shared URP cel-material response, clean contact shadows and controlled bloom; real performance evidence required."),
    ("menus", "Main menu, setup, characters, pause, results and duel flows", "UI / UX", "Menus", "refine", "", "", "Retain name, core branding and input conventions; polished hierarchy, portraits, spacing and responsive states."),
    ("hud", "HUD, icons, reticle, scoreboard and combat feed", "UI / UX", "Gameplay overlay", "refine", "", "", "Keep spread boundary truthful during animation, readable health/ammo and uncluttered central aiming space."),
    ("settings", "Settings, visual comfort, audio and Physics Lab", "UI / UX", "Configuration", "refine", "", "", "Consistent controls and screenshot/export clarity; preserve exact tuning behavior and saved preferences."),
    ("audio", "Weapon, traversal, pickup and interface cues", "Supporting", "Gameplay and menus", "retain", "", "", "Existing original/licensed cues remain; assess synchronization and polish with the visual actions, no new music project."),
    ("legacy-maps", "Foundry and Twin Platforms", "Environment", "Older maps", "retain", "ArtSource/Original/Foundry-Models-v1/Foundry-Environment.blend", "", "Inventory and preserve, without a complete new production art rebuild in this milestone."),
    ("provenance", "Licenses, fonts, icons, exports, LODs and build art", "Supporting", "Project-wide", "refine", "", "", "Keep source attribution, import identity, export recipes, manifests and release-facing images synchronized."),
]


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def category(path, kind=""):
    value = path.lower()
    if kind == "AnimationClip" or any(t in value for t in ("animation", "animator", "quaternius", "crouchpose")):
        return "Animation"
    if any(t in value for t in ("viewarms", "firstperson", "handgrip", "hand grip", "handarticulation")):
        return "First-person"
    if any(t in value for t in ("precisioncarbine", "precision-carbine", "carbinecandidate", "railgun", "railrifle", "railrifle", "rifle")):
        return "Weapons"
    if any(t in value for t in ("character", "contender", "playerpilot", "grab3d")):
        return "Characters"
    if any(t in value for t in ("/ui/", "arenahud", "branding", "ui-brand", "reticle", "button", "font", ".ttf", ".otf")):
        return "UI / UX"
    if any(t in value for t in ("health", "pickup", "portal", "jumppad", "landing", "launch", "marker", "sign")):
        return "Interactive"
    if any(t in value for t in ("water", "foam", "foliage", "bark", "leaf", "grass", "cloud", "skybox", "distant")):
        return "Nature"
    if kind in {"Shader", "ShaderGraphImporter", "VisualEffectAsset", "UniversalRenderPipelineAsset"} or any(t in value for t in ("shader", "vfx", "effects", "lighting", "grading", "rendering", "ssao", "atmosphere")):
        return "VFX / lighting"
    if any(t in value for t in ("foundry", "skygardens", "sky-gardens", "environment", "twinplatform", "basement", "skyblueprint", "sky_construction")) or kind in {"Mesh", "Material"}:
        return "Environment"
    return "Supporting"


def area(path):
    value = path.lower()
    if "basement" in value or "staging" in value:
        return "Basement / staging"
    if "summit" in value:
        return "Summit"
    if "underpass" in value or "bridge" in value:
        return "Bridge / underpass"
    if "twinplatform" in value or "foundry" in value and "skygardens" not in value:
        return "Legacy / shared Foundry"
    if "skygardens" in value or "sky-gardens" in value:
        return "Sky Gardens"
    return "Shared / project-wide"


def scope(path, raw=None):
    value = path.lower()
    if any(t in value for t in ("/validation/carbinecandidate/", "/validation/skywatercandidate/", "/validation/atmospherecandidate/")):
        return "conditional review-build input"
    if "/validation/" in value or "/candidates/" in value or "playerpilot" in value or "playerpilot-v1" in value:
        return "isolated candidate / validation"
    if raw and "Assets/RailArena/Generated/SkyGardensBasement.unity" in raw.get("sceneDependencies", []):
        return "active scene dependency"
    if raw and raw.get("resourceIncluded"):
        return "Resources included; runtime load unconfirmed"
    if raw and raw.get("sceneDependencies"):
        return "other scene dependency"
    if value.startswith("artsource/"):
        return "authoring / reference source"
    return "no serialized active-use evidence"


def requirements(record):
    kind, path = record["type"], record["path"].lower()
    if kind in {"Mesh", "GameObject"} or path.endswith((".blend", ".fbx", ".glb")):
        return ["canonical master and export parity", "six orthographic views and dimensions", "pivot/socket/collision/LOD specification", "Sanders asset approval"]
    if kind == "AnimationClip" or path.endswith(".anim"):
        return ["pose/timing/contact reference", "motion review and transition coverage", "Sanders animation approval"]
    if path.endswith((".png", ".tga", ".jpg", ".svg", ".mat", ".shader", ".shadergraph", ".vfx")):
        return ["material or state specification", "in-game scale/readability evidence", "Sanders visual approval"]
    return ["owning asset family and provenance review"]


def source_matches(path):
    value = path.lower()
    if "playerpilot" in value:
        return ["ArtSource/Original/PlayerPilot-v1/Player-Pilot.blend"]
    if "precisioncarbine" in value or "carbinecandidate" in value:
        return ["ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"]
    if "stylized" in value:
        return [f"ArtSource/Original/StylizedContenders/{name}.blend" for name in ("Player", "Bot01", "Bot02", "Bot03") if name.lower() in value] or ["ArtSource/Original/StylizedContenders/Player.blend"]
    if "skygardensart" in value:
        return ["ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend"]
    if "/art/railgun/" in value:
        return ["ArtSource/Original/Railgun/Railgun.blend"]
    return []


def compose_sheets(root, output, references):
    font_path = Path("C:/Windows/Fonts/arial.ttf")
    font = ImageFont.truetype(str(font_path), 25)
    title = ImageFont.truetype(str(font_path), 40)
    small = ImageFont.truetype(str(font_path), 19)
    for record in references:
        sheet = Image.new("RGB", (2400, 1920), "#101d25")
        draw = ImageDraw.Draw(sheet)
        draw.text((40, 28), "RAIL ARENA / " + record["id"].upper(), font=title, fill="#e8e5dc")
        draw.text((40, 88), "GATE 1 / MEASURED BASELINE GEOMETRY / ART APPROVAL PENDING", font=font, fill="#47d0d4")
        dims = record["dimensionsBlenderXYZMetres"]
        draw.text((40, 133), "Blender XYZ: " + " × ".join(f"{n:.4f}" for n in dims) + " m   |   Same orthographic scale in every view", font=font, fill="#e8e5dc")
        for i, view in enumerate(record["views"]):
            x, y = 20 + i % 3 * 795, 200 + i // 3 * 795
            image = Image.open(root / view["path"]).convert("RGB")
            sheet.paste(image, (x, y + 29))
            draw.text((x + 12, y), view["name"].upper(), font=font, fill="#47d0d4")
            # Exact metre ruler on the unresized 768px render; comparison height uses the same scale.
            ppm = record["pixelsPerMetre"]
            target = record["orthographicSpanMetres"] * .18
            exponent = 10 ** math.floor(math.log10(target))
            length = max(exponent * .1, round(target / exponent, 1) * exponent)
            end = x + 35 + round(length * ppm)
            baseline = y + 760
            draw.line((x + 35, baseline, end, baseline), fill="#ffffff", width=3)
            draw.line((x + 35, baseline - 6, x + 35, baseline + 6), fill="#ffffff", width=3)
            draw.line((end, baseline - 6, end, baseline + 6), fill="#ffffff", width=3)
            draw.text((x + 35, baseline - 30), f"{length:g} m", font=small, fill="#ffffff")
            if i == 0:
                height = 1.8 * ppm
                draw.line((x + 735, baseline, x + 735, baseline - height), fill="#47d0d4", width=3)
                draw.text((x + 615, baseline - height - 24), "1.80 m ruler", font=small, fill="#47d0d4")
        draw.text((40, 1810), "Source: " + record["source"], font=small, fill="#e8e5dc")
        draw.text((40, 1840), "SHA-256: " + record["sourceSha256"], font=small, fill="#99b0bc")
        draw.text((40, 1870), record.get("axisConvention", "Blender (x,y,z) -> Unity (x,z,-y)") + " | Neutral geometry review; not a final material render.", font=small, fill="#99b0bc")
        path = output / "references" / record["id"] / "measured-sheet.png"
        sheet.save(path)
        record["sheet"] = path.relative_to(root).as_posix()
        record["sheetSha256"] = digest(path)


def write_csv(path, rows, columns):
    with path.open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        for row in rows:
            writer.writerow({key: json.dumps(row.get(key), ensure_ascii=False) if isinstance(row.get(key), (list, dict)) else row.get(key, "") for key in columns})


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    args = parser.parse_args()
    root = args.root.resolve()
    output = root / "ArtSource/Original/ArtPass-v1/Gate1"
    raw = json.loads((output / "unity-audit.json").read_text(encoding="utf-8-sig"))
    references = json.loads((output / "references/reference-index.json").read_text(encoding="utf-8-sig"))
    baseline_path = output / "baseline-views/index.json"
    baseline = json.loads(baseline_path.read_text(encoding="utf-8-sig")) if baseline_path.exists() else {"views": [], "scope": "not yet captured"}
    blender = json.loads((output / "blender-audit.json").read_text(encoding="utf-8-sig"))
    compose_sheets(root, output, references)
    records = []
    for asset in raw["assets"]:
        path = "RailArena/" + asset["path"]
        status = scope(path, asset)
        disposition = "archive-reference" if status == "isolated candidate / validation" else "retain" if asset["type"] in {"MonoScript", "AssemblyDefinitionAsset", "DefaultAsset", "TextAsset"} else "refine"
        records.append(dict(id=asset["id"], path=path, type=asset["type"], category=category(path, asset["type"]), area=area(path), scope=status, disposition=disposition, usageEvidence=asset["sceneDependencies"], resourceIncluded=asset["resourceIncluded"], dependencies=asset["dependencies"], sourceCandidates=source_matches(path), sourceParity="unverified", dimensions=asset.get("meshes", []), dimensionsEvidence="imported mesh local bounds; not certified metre dimensions", rigClips=asset.get("clips", []), references=[], screenshots=[], provenance="Existing repository input; consult THIRD-PARTY-NOTICES.md and owning authoring records", approval="pending Gate 1 and asset review"))
    for path in sorted((root / "ArtSource").rglob("*")):
        if not path.is_file() or path.suffix.lower() not in ART_EXTENSIONS or "ArtPass-v1" in path.parts:
            continue
        relative = path.relative_to(root).as_posix()
        if path.name.endswith((".blend1", ".blend2")):
            continue
        dimensions = None
        if path.suffix.lower() in {".png", ".jpg", ".jpeg", ".tga", ".webp"}:
            try:
                with Image.open(path) as image:
                    dimensions = {"pixels": list(image.size)}
            except (OSError, ValueError):
                dimensions = {"error": "image dimensions unavailable"}
        records.append(dict(id="source:" + hashlib.sha256(relative.encode()).hexdigest()[:24], path=relative, type="AuthoringSource", category=category(relative), area=area(relative), scope=scope(relative), disposition="archive-reference" if "playerpilot" in relative.lower() or "/Railgun/" in relative or "/Candidates/" in relative else "retain", usageEvidence=[], dependencies=[], sourceCandidates=[relative] if path.suffix.lower() == ".blend" else [], sourceParity="unverified", dimensions=dimensions, dimensionsEvidence="pixel dimensions only" if dimensions else "requires owning master measurement", rigClips=[], references=[], screenshots=[relative] if dimensions else [], provenance="Existing source/reference; upstream license status must be checked before republishing an external reference", approval="not automatically inherited from concept approval", sha256=digest(path), bytes=path.stat().st_size))
    family_records = []
    for identity, name, cat, map_area, disposition, master, ref, notes in FAMILIES:
        reference = next((r for r in references if r["id"] == ref), None)
        family_records.append(dict(id="family:" + identity, name=name, category=cat, area=map_area, disposition=disposition, canonicalMasterCandidate=master or None, sourceExists=bool(master and (root / master).exists()), sourceParity="pending source/export and final art acceptance", measuredReference=reference, approval="pending Sanders Gate 1", notes=notes, requiredEvidence=["canonical master/export parity", "measured sheets and assembly details", "in-game visual and motion review", "Sanders approval"]))
    for record in records:
        record["referenceRequirements"] = requirements(record)
        record["gaps"] = ["approval pending"]
        if record["type"] in {"Mesh", "GameObject"} or record["path"].lower().endswith((".blend", ".fbx", ".glb")):
            record["gaps"] += ["source/export parity not certified", "complete technical construction package not certified"]
        if record["scope"] in {"conditional review-build input", "Resources included; runtime load unconfirmed", "no serialized active-use evidence"}:
            record["gaps"].append("runtime use requires direct evidence")
    # Link samples to their exact source entries, without implying coverage of sibling exports.
    for reference in references:
        for record in records:
            if record["path"] == reference["source"] and reference["sourceKind"] != "unity-export":
                record["references"] = [reference["sheet"], *[v["path"] for v in reference["views"]]]
                record["dimensions"] = {"blenderXYZMetres": reference["dimensionsBlenderXYZMetres"]}
                record["dimensionsEvidence"] = "measured evaluated baseline source geometry in Blender, rest pose"
    components = []
    occurrences = Counter()
    for item in raw["objects"]:
        key = item["container"] + "#" + item["hierarchy"]
        occurrences[key] += 1
        components.append(dict(id="instance:" + hashlib.sha256((key + "#" + str(occurrences[key])).encode()).hexdigest()[:24], container="RailArena/" + item["container"], hierarchy=item["hierarchy"], category=category(item["hierarchy"] + " " + item["container"]), area=area(item["hierarchy"]), disposition="retain" if item["container"].endswith(("Foundry.unity", "TwinPlatforms.unity")) else "refine", dimensionsMetres=item["worldSize"], centerMetres=item["worldCenter"], mesh=item["mesh"], materials=item["materials"], components=item["components"], approval="pending", measurementMethod="Editor renderer world AABB; hierarchy pose and rotation affect bounds"))
    clips = []
    for asset in raw["assets"]:
        for clip in asset.get("clips", []):
            clips.append(dict(id=asset["id"] + ":" + str(clip["localId"]), asset="RailArena/" + asset["path"], disposition="refine", approval="pending action review", **clip))
    authoring_objects = []
    for source in blender["sources"]:
        for item in source["objects"]:
            proposed = "archive-reference" if any(t in source["source"] for t in ("PlayerPilot", "References", "Railgun/", "/Candidates/")) or item["type"] in {"CAMERA", "LIGHT"} else "retain" if item["type"] == "ARMATURE" else "refine"
            authoring_objects.append(dict(**{k: v for k, v in item.items() if k != "disposition"}, source=source["source"], sourceSha256=source["sourceSha256"], unitScale=source["unitScale"], category=category(source["source"] + "/" + item["name"]), area=area(item["name"]), disposition=proposed))
    summary = dict(importedAssets=len(raw["assets"]), sourceFiles=len(records) - len(raw["assets"]), inventoryEntries=len(records), rendererInstances=len(components), inspectedContainers=len(raw["containers"]), importedClips=len(clips), missingScripts=sum(c["missingScripts"] for c in raw["containers"]), measuredSampleFamilies=len(references), measuredSampleViews=sum(len(r["views"]) for r in references), finalApprovedAssets=0, referenceCoverage="not complete; representative measured samples only", categories=dict(Counter(r["category"] for r in records)), scopes=dict(Counter(r["scope"] for r in records)))
    summary.update(blenderMasters=len(blender["sources"]), blenderObjects=len(authoring_objects), blenderActions=sum(len(s["actions"]) for s in blender["sources"]), unresolvedImageBindings=sum(not i["packed"] and not i["resolvedExists"] for s in blender["sources"] for i in s["imageDependencies"]))
    manifest = dict(schema=VERSION, createdUtc=datetime.now(timezone.utc).isoformat(), stage="Gate 1 candidate; Sanders approval pending", performanceTarget="1080p / 60 FPS / RTX 2080 / one player plus three bots; not yet measured for the new art target", scope="Sky Gardens including summit and four basement routes; inventory and preserve older maps", evidenceLimits=raw["boundsNote"], conditionalRuntimeEvidence="CombinedGraphicsReviewBuilder injects ReviewEffects and the atmosphere pipeline. Candidate rifle meshes/AO and water shader are conditional build inputs, not serialized scene bindings. HUD, effects and some materials are built in code. Resources inclusion alone is not proof of runtime use.", summary=summary, families=family_records, assets=records, instances=components, clips=clips, containers=raw["containers"], references=references, baselineCaptures=baseline)
    manifest["blenderObjects"] = authoring_objects
    manifest["blenderSources"] = blender["sources"]
    (output / "asset-manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    write_csv(output / "asset-register.csv", records, ["id", "category", "area", "path", "type", "scope", "disposition", "usageEvidence", "sourceCandidates", "sourceParity", "dimensions", "dimensionsEvidence", "dependencies", "rigClips", "references", "screenshots", "provenance", "approval", "gaps"])
    write_csv(output / "scene-prefab-instances.csv", components, ["id", "container", "hierarchy", "category", "area", "disposition", "dimensionsMetres", "mesh", "materials", "components", "approval", "measurementMethod"])
    actions = {
        "first-person": ["idle", "ready/equip", "fire", "recoil recovery", "ADS enter/exit", "magazine reload", "empty/reload", "walk", "sprint", "crouch", "slide", "jump", "landing", "portal/pad traversal"],
        "character": ["idle", "walk forward", "walk backward", "strafe left", "strafe right", "sprint", "crouch idle", "crouch movement", "crouch enter/exit", "slide entry", "slide loop", "slide exit", "jump start", "fall loop", "landing", "fire", "reload", "hit reaction", "death", "respawn"],
    }
    animation_rows = []
    for group, names in actions.items():
        for action in names:
            terms = {"fire": ["fire", "shoot"], "landing": ["land"], "fall loop": ["jump_loop", "fall"], "walk backward": ["walk", "sprint"], "ready/equip": ["equip", "idle"]}.get(action, [action.split()[0]])
            matches = [c["asset"] + "#" + c["name"] for c in clips if any(t.lower() in c["name"].lower() for t in terms)] if group == "character" else []
            animation_rows.append(dict(id="anim:" + group + ":" + action.replace(" ", "-"), group=group, action=action, existingClipCandidates=matches, currentEvidence="runtime motion code and existing arm rig; authored action coverage unverified" if group == "first-person" else "name-matched imported clips are candidates only; transition quality unverified", requiredViews="FPS hip + ADS + side hand contact" if group == "first-person" else "front/side/rear at gameplay scale + foot/hand contact", requiredDeliverable="Blender poses, timing/contact sheet, 24 FPS review strip, real-time playback, transition and interruption review", authoritativeTiming="preserve current simulation; 250 RPM and current reload/respawn rules", disposition="rebuild" if group == "first-person" or any(t in action for t in ("crouch", "slide", "backward", "strafe")) else "refine", approval="pending"))
    write_csv(output / "animation-matrix.csv", animation_rows, list(animation_rows[0]))
    write_csv(output / "imported-clips.csv", clips, ["id", "asset", "name", "seconds", "framesPerSecond", "loop", "localId", "disposition", "approval"])
    write_csv(output / "blender-object-register.csv", authoring_objects, ["id", "source", "sourceSha256", "name", "type", "parent", "collections", "category", "area", "disposition", "worldSizeMetres", "pivotWorld", "scale", "materials", "bones", "vertexGroups", "approval"])
    schedule = [
        dict(order=1, gate="Gate 1", batch="Inventory, gap report, bible and measured samples", dependsOn="current evidence audit", output="This review package", acceptance="Sanders approves direction, scope and reference standard"),
        dict(order=2, gate="Gate 2", batch="Cel material/lighting prototype and modular garden slice", dependsOn="Gate 1 approval", output="Shared cel settings, measured environment kit and matching gameplay comparison", acceptance="Crisp silhouette, deliberate shadows, playable clearance and measured budget"),
        dict(order=3, gate="Gate 2", batch="Player, rifle, arms and first-person action slice", dependsOn="approved direction and material prototype", output="One refined player plus complete rifle/hands/actions in game", acceptance="Visible improvement in hip/ADS, close-up and motion with preserved contacts/timing"),
        dict(order=4, gate="Gate 2", batch="Device, nature, VFX, HUD and menu slice integration", dependsOn="slice geometry and player integration", output="Complete playable visual slice and before/after reel", acceptance="Sanders approves the shared in-game look before bulk asset rebuilds"),
        dict(order=5, gate="Gate 3", batch="Three bot variants and complete body action library", dependsOn="Gate 2 approval", output="Consistent four-character roster, authored directional/stance/death actions", acceptance="Likeness, deformation, transitions, silhouettes and motion review"),
        dict(order=6, gate="Gate 3", batch="Remaining arena, summit, staging, basement and devices", dependsOn="approved modular kit", output="Full map art, return devices, signs and environment detail", acceptance="Every production family has sources/sheets, readable routes and safe collision clearances"),
        dict(order=7, gate="Gate 3", batch="Nature, effects, all UI flows and presentation closure", dependsOn="complete asset integration", output="Unified water/foliage/sky, feedback, menus, portraits and HUD", acceptance="Complete coverage gallery, state/readability review and licensing record"),
        dict(order=8, gate="Gate 3", batch="Performance, focused final checks and local handoff", dependsOn="integrated art", output="Verified Windows build, checksums, source manifest, comparison reels and exceptions", acceptance="Measured 1080p/60 FPS target, focused gameplay checks and Sanders final approval"),
        dict(order=9, gate="Publication", batch="Publish current v1 alpha package", dependsOn="Sanders final approval", output="New prerelease, download links and preserved release history", acceptance="Published package identity/checksum verified"),
    ]
    write_csv(output / "production-schedule.csv", schedule, list(schedule[0]))
    gaps = "# Gate 1 gap report\n\nThis is a measured baseline inventory, not completed production art or 100% reference acceptance.\n\n"
    gaps += "## Coverage evidence\n\n" + "\n".join(f"- **{k}:** {v}" for k, v in summary.items() if k not in {"categories", "scopes"}) + "\n\n"
    gaps += "## Priority gaps\n\n"
    for family in family_records:
        gaps += f"- **{family['name']}** — {family['disposition']}. {family['notes']} " + ("A measured baseline sheet is supplied; detailed construction and production approval remain open." if family["measuredReference"] else "Complete measured construction/reference package remains open.") + "\n"
    gaps += "\n## Source dependency findings\n\n"
    for source in blender["sources"]:
        for image in source["imageDependencies"]:
            if not image["packed"] and not image["resolvedExists"]:
                gaps += f"- Missing image binding `{image['authoredPath']}` in `{source['source']}`. This is an inventoried source issue; candidate inputs are not approved for production or new redistribution.\n"
    gaps += "\n## Interpretation limits\n\n- All entries have an explicit disposition; dispositions are proposed, not proof of final asset approval.\n- Scene/prefab bounds are world-space AABBs and may include pose, attachments and rotation. Imported mesh bounds are in local units. Do not resize gameplay from these values without canonical-master and export verification.\n- Existing concept approval does not approve generated geometry, new shaders or this proposal. The new illustrative board cannot define exact weapon geometry, dimensions, UI placement or map topology.\n- The four character source heights differ; their measured source sheets do not change the common controller/hitbox rules.\n- The basement deck sample is extracted from actual scene geometry into a reference master. The new modular production kit remains to be authored.\n- Runtime-created objects and conditional review-build material/mesh overrides are flagged separately from serialized scene dependencies.\n- No new artistic performance measurement, visual acceptance or new gameplay build is claimed at Gate 1.\n"
    (output / "GAP-REPORT.md").write_text(gaps, encoding="utf-8")
    (output / "review-data.json").write_text(json.dumps(dict(summary=summary, families=family_records, animations=animation_rows, schedule=schedule), indent=2), encoding="utf-8")
    build_gallery(root, output, manifest, animation_rows, schedule)
    # Verify output schema, link integrity and measured reference geometry without running unrelated gameplay suites.
    assert len({r["id"] for r in records}) == len(records), "Asset IDs must be unique"
    assert len({r["id"] for r in components}) == len(components), "Instance IDs must be unique"
    assert all(r["disposition"] in DISPOSITIONS for r in records + family_records + components)
    assert len({r["id"] for r in authoring_objects}) == len(authoring_objects)
    assert all(r["disposition"] in DISPOSITIONS for r in authoring_objects + clips)
    assert all(s["status"] == "read" and digest(root / s["source"]) == s["sourceSha256"] for s in blender["sources"])
    for reference in references:
        assert len(reference["views"]) == 6
        assert digest(root / reference["source"]) == reference["sourceSha256"], "Reference source changed: " + reference["id"]
        assert all((root / v["path"]).is_file() and digest(root / v["path"]) == v["sha256"] for v in reference["views"])
        assert (root / reference["master"]).is_file() and (root / reference["sheet"]).is_file()
    assert all((root / view["path"]).is_file() for view in baseline["views"])
    verification = dict(schema=VERSION, passed=True, assetIdsUnique=True, instanceIdsUnique=True, dispositionsValid=True, measuredImagesVerified=sum(len(r["views"]) for r in references), baselineCapturesLinked=len(baseline["views"]), sourceCoverage="inventory coverage only; reference and art approval incomplete", summary=summary)
    (output / "package-verification.json").write_text(json.dumps(verification, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


def build_gallery(root, output, manifest, animation_rows, schedule):
    def link(path):
        return "../../../../" + quote(path, safe="/#")
    cards = []
    for family in manifest["families"]:
        reference = family["measuredReference"]
        picture = f'<a href="{link(reference["sheet"])}"><img loading="lazy" src="{link(reference["sheet"])}" alt="Measured six-view baseline for {html.escape(family["name"])}"></a>' if reference else '<div class="missing">Construction package pending</div>'
        master = f'<a href="{link(family["canonicalMasterCandidate"])}">Existing authoring master</a>' if family["sourceExists"] else 'Canonical production master required'
        cards.append(f'<article data-category="{html.escape(family["category"])}"><div class="eyebrow">{html.escape(family["category"])} / {html.escape(family["area"])}</div><h3>{html.escape(family["name"])}</h3><span class="tag">{family["disposition"]}</span>{picture}<p>{html.escape(family["notes"])}</p><small>{master}</small><p class="muted">Approval pending Sanders</p></article>')
    rows = "".join(f'<tr><td>{r["order"]}</td><td>{r["gate"]}</td><td>{html.escape(r["batch"])}</td><td>{html.escape(r["dependsOn"])}</td><td>{html.escape(r["acceptance"])}</td></tr>' for r in schedule)
    animation_table = "".join(f'<tr><td>{r["group"]}</td><td>{html.escape(r["action"])}</td><td>{r["disposition"]}</td><td>{html.escape(r["requiredViews"])}</td><td>{len(r["existingClipCandidates"])}</td></tr>' for r in animation_rows)
    payload = json.dumps(manifest["assets"], ensure_ascii=False).replace("<", "\\u003c")
    page = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / Gate 1 Art Production</title>
<style>*{box-sizing:border-box}body{margin:0;background:#101b23;color:#eeeae0;font:16px/1.6 system-ui,sans-serif}header,main,footer{max-width:1480px;margin:auto;padding:32px}header{padding-top:54px}h1{font-size:clamp(36px,5vw,66px);line-height:1.05;margin:14px 0}h2{font-size:31px;margin-top:52px;border-top:1px solid #37505d;padding-top:28px}h3{line-height:1.3}a{color:#64dce1}a:hover{color:white}.eyebrow{font-size:12px;letter-spacing:.15em;text-transform:uppercase;color:#64dce1}.lede{max-width:900px;font-size:20px;color:#c4cdd0}.status{border-left:4px solid #ecbb71;background:#23313a;padding:18px;max-width:1100px}.metrics{display:flex;gap:16px;flex-wrap:wrap;margin:30px 0}.metric{background:#1b2a34;padding:18px 24px;min-width:170px}.metric strong{display:block;font-size:32px;color:#64dce1}nav{display:flex;gap:18px;flex-wrap:wrap}img{max-width:100%;height:auto}.board{border:1px solid #37505d}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:20px}article{padding:23px;background:#1b2a34;border-top:2px solid #4bc6cd}article img{margin-top:18px}.tag{font-size:12px;background:#354650;border-radius:3px;padding:4px 9px}.muted,small{color:#a9bcc5}.missing{height:130px;margin-top:18px;display:grid;place-items:center;border:1px dashed #4b6370;color:#adc0c9}table{border-collapse:collapse;width:100%;font-size:14px}td,th{padding:13px;text-align:left;border-bottom:1px solid #344957;vertical-align:top}th{color:#64dce1}.scroll{overflow:auto}input,select,button{padding:12px;background:#233641;border:1px solid #47606e;color:#eeeae0;font:inherit;border-radius:3px}input{min-width:240px;flex:1}.filters{display:flex;gap:12px;flex-wrap:wrap;margin:18px 0}button{cursor:pointer}button:disabled{opacity:.4}.path{overflow-wrap:anywhere;font:12px ui-monospace,monospace}footer{border-top:1px solid #37505d;color:#a9bcc5}.palette{display:flex;flex-wrap:wrap;gap:10px}.swatch{padding:16px;min-width:145px;border-top:16px solid var(--c);background:#1b2a34} :focus-visible{outline:3px solid #ecbb71;outline-offset:3px}@media print{body{background:white;color:black}header,main{padding:12px}article{break-inside:avoid}nav,.filters,button{display:none}}</style>
<header><div class="eyebrow">Sanders / v1 alpha production / Gate 1 candidate</div><h1>RAIL ARENA<br><span style="color:#64dce1">ART PRODUCTION REGISTER</span></h1><p class="lede">Measured sources. Clear ownership. A complete route from the current blockout to a cohesive, crisp anime cel-shaded game.</p><div class="status"><strong>Gate 1 approval pending.</strong> This package inventories the baseline and proposes production direction. It does not claim completed game art, exact concept likeness, complete reference coverage or measured 60 FPS performance.</div><div class="metrics">METRICS</div><nav><a href="#direction">Direction</a><a href="#families">Asset families</a><a href="#register">Full inventory</a><a href="#animation">Animation</a><a href="#schedule">Production gates</a><a href="ART-BIBLE.md">Art bible</a><a href="GAP-REPORT.md">Gap report</a><a href="asset-manifest.json">Manifest</a><a href="asset-register.csv">CSV register</a></nav></header>
<main><section id="direction"><h2>01 / The shared visual target</h2><p>Original character identities, low-poly forms, deliberate shadow bands, painted accents and selective outlines. Ivory stone, teal enamel, bronze structure and living gardens remain the environment language. This generated board proposes composition and style only; the measured masters govern geometry, scale and gameplay.</p><a href="concepts/Cel-Direction-Proposal.png"><img class="board" src="concepts/Cel-Direction-Proposal.png" alt="Illustrative cel art-direction proposal, not gameplay or a modeling blueprint"></a><div class="palette"><div class="swatch" style="--c:#e6dfcd">Ivory stone</div><div class="swatch" style="--c:#1c303a">Dark cloth</div><div class="swatch" style="--c:#24626a">Teal enamel</div><div class="swatch" style="--c:#b38748">Bronze</div><div class="swatch" style="--c:#aa4f37">Terracotta</div><div class="swatch" style="--c:#663e57">Plum</div></div><p class="muted">Palette swatches are art-direction starting points, not final shader values. The illustrated weapon and HUD are not approved replacement geometry or UI specifications.</p></section>
<section id="families"><h2>02 / Production families and measured samples</h2><p>The grayscale sheets render current geometry in six orthographic directions at a common per-sheet scale. Rulers and reported dimensions come from geometry, with source hashes and axis conventions. They intentionally expose gaps between present meshes and the art target.</p><div class="grid">CARDS</div></section>
<section id="register"><h2>03 / Complete imported-asset and authoring-file register</h2><p>Imported Unity files and existing ArtSource files are indexed separately from scene/prefab instances. Every entry has a proposed disposition. Conditional review-build inputs and Resources inclusion are evidence classes, not proof that an asset is visible in gameplay.</p><div class="filters"><input id="search" aria-label="Search asset register" placeholder="Search name, category, scope or path"><select id="category" aria-label="Filter category"><option value="">All categories</option></select><select id="disposition" aria-label="Filter disposition"><option value="">All dispositions</option><option>retain</option><option>refine</option><option>rebuild</option><option>replace</option><option>archive-reference</option></select></div><p id="count"></p><div class="scroll"><table><thead><tr><th>Category / disposition</th><th>Asset / source</th><th>Usage evidence</th><th>Reference status</th></tr></thead><tbody id="inventory"></tbody></table></div><p><button id="previous">Previous</button> <button id="next">Next</button> <span id="page"></span></p><p><a href="scene-prefab-instances.csv">Renderer-instance register</a> · <a href="unity-audit.json">Raw Unity evidence</a> · <a href="imported-clips.csv">Imported clip inventory</a> · <a href="package-verification.json">Package verification</a></p></section>
<section id="animation"><h2>04 / Complete action-reference matrix</h2><p>Existing clip-name matches are candidates for inspection, not approved coverage. First-person actions need actual hand-contact, timing and interruption evidence. Every action receives pose sheets, frame strips and normal-speed playback during production.</p><div class="scroll"><table><thead><tr><th>View</th><th>Action</th><th>Disposition</th><th>Required review views</th><th>Existing clip-name candidates</th></tr></thead><tbody>ANIMATIONS</tbody></table></div><p><a href="animation-matrix.csv">Download animation matrix</a></p></section>
<section id="schedule"><h2>05 / Production order and approval gates</h2><p>Headless Blender and Unity jobs are the default. Work stops for Sanders' approval at each major gate. No public package is replaced before final approval.</p><div class="scroll"><table><thead><tr><th>Order</th><th>Gate</th><th>Batch</th><th>Dependency</th><th>Acceptance</th></tr></thead><tbody>SCHEDULE</tbody></table></div><p><a href="production-schedule.csv">Download production schedule</a></p></section></main><footer>Rail Arena / v1 alpha. Approval authority: Sanders. Native input, sound, competitive balance, final visual acceptance and measured target performance remain separate acceptance requirements.</footer>
<script type="application/json" id="asset-data">PAYLOAD</script><script>
const records=JSON.parse(document.getElementById('asset-data').textContent), search=document.getElementById('search'), category=document.getElementById('category'), disposition=document.getElementById('disposition');let page=0;
for(const value of [...new Set(records.map(r=>r.category))].sort()){const option=document.createElement('option');option.textContent=value;category.appendChild(option)}
function draw(){const text=search.value.toLowerCase();const filtered=records.filter(r=>(!category.value||r.category===category.value)&&(!disposition.value||r.disposition===disposition.value)&&(!text||[r.path,r.category,r.scope,r.disposition].join(' ').toLowerCase().includes(text)));const pages=Math.max(1,Math.ceil(filtered.length/80));page=Math.min(page,pages-1);const body=document.getElementById('inventory');body.replaceChildren();for(const r of filtered.slice(page*80,page*80+80)){const row=document.createElement('tr');const a=document.createElement('td');a.textContent=r.category+' / '+r.disposition;const b=document.createElement('td');const anchor=document.createElement('a');anchor.href='../../../../'+r.path.split('/').map(encodeURIComponent).join('/');anchor.textContent=r.path;anchor.className='path';b.appendChild(anchor);const c=document.createElement('td');c.textContent=r.scope;const d=document.createElement('td');d.textContent=r.references.length?'Measured baseline sample; production approval pending':'Construction / visual acceptance pending';row.append(a,b,c,d);body.appendChild(row)}document.getElementById('count').textContent=filtered.length+' matching entries / '+records.length+' total';document.getElementById('page').textContent='Page '+(page+1)+' of '+pages;document.getElementById('previous').disabled=page===0;document.getElementById('next').disabled=page===pages-1;}
for(const control of [search,category,disposition])control.addEventListener('input',()=>{page=0;draw()});document.getElementById('previous').onclick=()=>{page--;draw()};document.getElementById('next').onclick=()=>{page++;draw()};draw();
</script></html>'''
    summary = manifest["summary"]
    metrics = "".join(f'<div class="metric"><strong>{value}</strong>{name}</div>' for name, value in (("Imported assets", summary["importedAssets"]), ("Source files", summary["sourceFiles"]), ("Renderer instances", summary["rendererInstances"]), ("Measured sample views", summary["measuredSampleViews"])))
    page = page.replace("METRICS", metrics).replace("CARDS", "".join(cards)).replace("ANIMATIONS", animation_table).replace("SCHEDULE", rows).replace("PAYLOAD", payload)
    baseline = manifest["baselineCaptures"]
    captures = "".join(f'<article><div class="eyebrow">Current Editor baseline</div><h3>{html.escape(view["id"])}</h3><a href="{link(view["path"])}"><img loading="lazy" src="{link(view["path"])}" alt="Current baseline {html.escape(view["id"])}"></a><p>{html.escape(view["note"])}</p></article>' for view in baseline["views"])
    section = '<section id="baseline"><h2>Current installed art / baseline captures</h2><p>' + html.escape(baseline["scope"]) + '</p><div class="grid">' + captures + '</div></section>'
    page = page.replace('<section id="families">', section + '<section id="families">')
    page = page.replace('<a href="#families">Asset families</a>', '<a href="#baseline">Current baseline</a><a href="#families">Asset families</a>')
    page = page.replace('<a href="scene-prefab-instances.csv">Renderer-instance register</a>', '<a href="blender-object-register.csv">Blender part/rig register</a> · <a href="blender-audit.json">Blender source evidence</a> · <a href="scene-prefab-instances.csv">Renderer-instance register</a>')
    (output / "index.html").write_text(page, encoding="utf-8")


if __name__ == "__main__":
    main()
