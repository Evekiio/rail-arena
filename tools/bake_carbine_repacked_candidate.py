"""Repack all part UVs into their existing tiles and bake an isolated candidate."""
import bpy
import json
import math
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
quality = "--quality" in sys.argv
fix_muzzle = "--muzzle-fix" in sys.argv
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
output = root / ("Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedAudited" if fix_muzzle else "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedQuality" if quality else "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedCandidate")
if output.exists():
    raise RuntimeError("Candidate output already exists")
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
parts = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda o: o.name)
entries = {entry["name"]: entry for entry in atlas["tiles"]}
if set(entries) != {obj.name for obj in parts}:
    raise RuntimeError("Source parts and atlas manifest differ")
grid = 9
gutter = atlas["gutterPixels"] / atlas["size"]
scale = 1 / grid - 2 * gutter
rows = []
for obj in parts:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(35) if fix_muzzle and obj.name == "C3 muzzle collar" else math.radians(66), island_margin=0.01)
    bpy.ops.object.mode_set(mode="OBJECT")
    layer = obj.data.uv_layers["SurfaceAtlas"]
    entry = entries[obj.name]
    for loop in layer.data:
        loop.uv = (entry["column"] / grid + gutter + loop.uv.x * scale,
                   entry["row"] / grid + gutter + loop.uv.y * scale)
    obj.data.calc_loop_triangles()
    coords = []
    for tri in obj.data.loop_triangles:
        uv = [layer.data[i].uv for i in tri.loops]
        coords.append([sum(p.x for p in uv) / 3, sum(p.y for p in uv) / 3])
    rows.append({"name": obj.name, "centroids": coords})
bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
image = bpy.data.images.new("Repacked candidate", width=4096, height=4096, alpha=False)
image.colorspace_settings.name = "Non-Color"
for slot in bpy.context.object.material_slots:
    if slot.material is None:
        raise RuntimeError("Missing material")
    slot.material.use_nodes = True
    nodes = slot.material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    nodes.active = node
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (1, 1, 1, 1)
    material_output = next(n for n in nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output)
    slot.material.node_tree.links.new(emission.outputs[0], material_output.inputs["Surface"])
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 64 if quality else 16
scene.cycles.device = "CPU"
scene.world.light_settings.distance = 0.03
scene.render.bake.margin = 8
for kind, filename in (("EMIT", "Coverage.png"), ("AO", "AO.png")):
    bpy.ops.object.bake(type=kind, use_clear=True, margin=8)
    image.filepath_raw = str(output / filename)
    image.file_format = "PNG"
    image.save()
(output / "centroids.json").write_text(json.dumps(rows) + "\n", encoding="utf-8")
(output / "bake.json").write_text(json.dumps({"parts": len(parts), "atlasSize": 4096, "aoDistance": 0.03, "samples": scene.cycles.samples, "muzzleUnwrapDegrees": 35 if fix_muzzle else 66, "sourceBlendSaved": False}, indent=2) + "\n", encoding="utf-8")
print("REPACKED_CANDIDATE " + str(output))
