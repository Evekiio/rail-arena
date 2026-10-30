"""Read-only inventory of the existing Blender masters, their objects, rigs, actions and image dependencies."""
import hashlib
import json
from pathlib import Path
import sys

import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
output = root / "ArtSource/Original/ArtPass-v1/Gate1/blender-audit.json"
records = []
for path in sorted((root / "ArtSource").rglob("*.blend")):
    if "ArtPass-v1" in path.parts:
        continue
    relative = path.relative_to(root).as_posix()
    record = dict(source=relative, sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(), status="read", objects=[], actions=[], imageDependencies=[])
    try:
        bpy.ops.wm.open_mainfile(filepath=str(path))
        scene = bpy.context.scene
        record["unitScale"] = scene.unit_settings.scale_length
        for obj in bpy.context.scene.objects:
            if obj.type == "ARMATURE":
                obj.data.pose_position = "REST"
        bpy.context.view_layer.update()
        graph = bpy.context.evaluated_depsgraph_get()
        for obj in sorted(scene.objects, key=lambda item: item.name):
            item = dict(name=obj.name, type=obj.type, parent=obj.parent.name if obj.parent else None, collections=[c.name for c in obj.users_collection], location=list(obj.location), scale=list(obj.scale), hiddenRender=obj.hide_render, worldSizeMetres=None, pivotWorld=list(obj.matrix_world.translation), materials=[], bones=[], disposition="retain", approval="unreviewed baseline authoring object")
            if obj.type == "MESH":
                evaluated = obj.evaluated_get(graph)
                points = [evaluated.matrix_world @ Vector(corner) for corner in evaluated.bound_box]
                item["worldSizeMetres"] = [(max(p[i] for p in points) - min(p[i] for p in points)) * record["unitScale"] for i in range(3)]
                item["materials"] = [m.name if m else "MISSING" for m in obj.data.materials]
                item["vertices"] = len(obj.data.vertices)
                item["polygons"] = len(obj.data.polygons)
                item["vertexGroups"] = [g.name for g in obj.vertex_groups]
            elif obj.type == "ARMATURE":
                item["bones"] = [b.name for b in obj.data.bones]
            item["id"] = "blend-object:" + hashlib.sha256((relative + "#" + obj.name).encode()).hexdigest()[:24]
            record["objects"].append(item)
        for action in bpy.data.actions:
            record["actions"].append(dict(name=action.name, frameRange=list(action.frame_range), disposition="retain", approval="not reviewed for final action coverage"))
        for image in bpy.data.images:
            if image.source not in {"FILE", "TILED"}:
                continue
            resolved = Path(bpy.path.abspath(image.filepath))
            record["imageDependencies"].append(dict(name=image.name, authoredPath=image.filepath, packed=bool(image.packed_file), resolvedExists=resolved.exists(), pixels=list(image.size)))
        print("BLENDER_SOURCE_AUDIT", relative, len(record["objects"]), flush=True)
    except Exception as error:
        record["status"] = "ERROR: " + str(error)
    records.append(record)
    output.write_text(json.dumps(dict(schema="rail-arena-blender-audit/1", note="Read-only source inventory in rest pose. World sizes are source-scene AABBs with unit scale applied, not certified runtime dimensions or artistic acceptance. No original master was saved.", sources=records), indent=2), encoding="utf-8")
print("BLENDER_SOURCES_AUDITED", len(records), flush=True)
