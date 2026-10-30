"""Render measured six-view baseline sheets from existing geometry; never edits source masters."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("root", type=Path)
    parser.add_argument("--only")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    root = args.root.resolve()
    output = root / "ArtSource/Original/ArtPass-v1/Gate1/references"
    output.mkdir(parents=True, exist_ok=True)
    cases = [(name.lower(), f"ArtSource/Original/StylizedContenders/{name}.blend", "character") for name in ("Player", "Bot01", "Bot02", "Bot03")]
    cases += [("precision-carbine", "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend", "weapon"), ("summit", "ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend", "summit"), ("basement-deck", "ArtSource/Original/ArtPass-v1/Gate1/unity-audit.json", "unity-export")]
    index_path = output / "reference-index.json"
    records = json.loads(index_path.read_text(encoding="utf-8")) if args.only and index_path.exists() else []
    for identity, relative, kind in cases:
        if args.only and identity != args.only:
            continue
        source = root / relative
        if kind == "unity-export":
            bpy.ops.wm.read_factory_settings(use_empty=True)
            example = json.loads(source.read_text(encoding="utf-8"))["representativeGeometry"][0]
            points = [(v["x"], -v["z"], v["y"]) for v in example["worldVertices"]]
            center = Vector(tuple((min(p[i] for p in points) + max(p[i] for p in points)) / 2 for i in range(3)))
            triangles = example["triangles"]
            mesh = bpy.data.meshes.new("Measured Unity basement deck")
            mesh.from_pydata([Vector(p) - center for p in points], [], [triangles[i:i + 3] for i in range(0, len(triangles), 3)])
            mesh.update()
            obj = bpy.data.objects.new("Basement deck geometry reference", mesh)
            bpy.context.scene.collection.objects.link(obj)
            obj["unity_world_center"] = tuple((center.x, center.z, -center.y))
            meshes = [obj]
        else:
            bpy.ops.wm.open_mainfile(filepath=str(source))
            for obj in bpy.context.scene.objects:
                if obj.type == "ARMATURE":
                    obj.data.pose_position = "REST"
            meshes = [o for o in bpy.context.scene.objects if o.type == "MESH" and not o.hide_render]
            if kind == "summit":
                meshes = [o for o in meshes if o.name.startswith("SUMMIT-")]
            else:
                meshes = [o for o in meshes if not any(token in o.name.lower() for token in ("studio floor", "ground plane", "backdrop", "reference plane"))]
        if not meshes:
            raise RuntimeError("No geometry found for " + identity)
        selected = set(meshes)
        for obj in bpy.context.scene.objects:
            if obj.type in {"MESH", "FONT", "CURVE", "SURFACE", "META"}:
                obj.hide_render = obj not in selected
        bpy.context.view_layer.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()
        points = [o.evaluated_get(depsgraph).matrix_world @ Vector(v) for o in meshes for v in o.evaluated_get(depsgraph).bound_box]
        low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
        high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
        center = (low + high) / 2
        size = high - low
        scene = bpy.context.scene
        unit_scale = scene.unit_settings.scale_length
        if abs(unit_scale - 1) > 0.00001:
            raise RuntimeError(f"Unresolved unit scale for {identity}: {unit_scale}")
        scene.unit_settings.system = "METRIC"
        scene.render.engine = "BLENDER_WORKBENCH"
        scene.render.resolution_x = scene.render.resolution_y = 768
        scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = "PNG"
        scene.render.film_transparent = False
        scene.display.shading.light = "STUDIO"
        scene.display.shading.color_type = "MATERIAL"
        scene.display.shading.show_shadows = True
        scene.display.shading.show_cavity = True
        scene.display.shading.cavity_type = "BOTH"
        scene.display.shading.background_type = "WORLD"
        if scene.world is None:
            scene.world = bpy.data.worlds.new("Technical reference world")
        scene.world.color = (.19, .22, .25)
        scene.display.shading.show_object_outline = True
        scene.display.shading.object_outline_color = (.04, .05, .07)
        camera_data = bpy.data.cameras.new("Gate 1 orthographic camera")
        camera = bpy.data.objects.new("Gate 1 orthographic camera", camera_data)
        scene.collection.objects.link(camera)
        scene.camera = camera
        camera_data.type = "ORTHO"
        span = max(2.2, max(size) * 1.3)
        camera_data.ortho_scale = span
        camera_data.clip_start = .001
        camera_data.clip_end = max(1000, span * 10)
        folder = output / identity
        folder.mkdir(exist_ok=True)
        views = []
        axes = (("front", (0, -1, 0)), ("rear", (0, 1, 0)), ("left", (-1, 0, 0)), ("right", (1, 0, 0)), ("top", (0, 0, 1)), ("bottom", (0, 0, -1)))
        if kind == "weapon":
            axes = (("front", (1, 0, 0)), ("rear", (-1, 0, 0)), ("left", (0, 1, 0)), ("right", (0, -1, 0)), ("top", (0, 0, 1)), ("bottom", (0, 0, -1)))
        for name, axis in axes:
            camera.location = center + Vector(axis) * span * 3
            camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
            path = folder / (name + ".png")
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            views.append(dict(name=name, path=path.relative_to(root).as_posix(), cameraPosition=list(camera.location), cameraEuler=list(camera.rotation_euler), sha256=sha(path)))
        copy = folder / (identity + "-reference.blend")
        bpy.ops.wm.save_as_mainfile(filepath=str(copy))
        record = dict(id=identity, source=relative, sourceSha256=sha(source), master=copy.relative_to(root).as_posix(), sourceKind=kind, axisConvention="Blender (x,y,z) -> Unity (-y,z,x) for this weapon master" if kind == "weapon" else "Blender (x,y,z) -> Unity (x,z,-y)", meshes=len(meshes), dimensionsBlenderXYZMetres=list(size), centerBlenderXYZ=list(center), orthographicSpanMetres=span, pixelsPerMetre=768 / span, resolution=768, views=views, status="Measured baseline geometry, not approved production art", limitations="Existing source likeness and Unity parity are not certified by these sheets. Rest-pose renderer AABB; no artistic edits. Unity-export example is a reference copy, not a new runtime asset.")
        (folder / "measurements.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
        records = [r for r in records if r["id"] != identity] + [record]
        (output / "reference-index.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
        print("ART_GATE1_REFERENCE", identity, list(size), flush=True)


if __name__ == "__main__":
    main()
