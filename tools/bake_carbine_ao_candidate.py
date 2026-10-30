"""Bake a disposable AO atlas from the editable carbine without saving its source blend."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
output = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineAO"
if output.exists():
    raise RuntimeError("AO candidate output already exists")
output.mkdir(parents=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
parts = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if len(parts) != len(atlas["tiles"]) or any(not obj.data.uv_layers.get("SurfaceAtlas") for obj in parts):
    raise RuntimeError("Editable carbine parts or atlas UVs do not match the source manifest")
bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
carbine = bpy.context.object
image = bpy.data.images.new("Carbine AO candidate", width=atlas["size"], height=atlas["size"], alpha=False, float_buffer=False)
image.colorspace_settings.name = "Non-Color"
for slot in carbine.material_slots:
    material = slot.material
    if material is None:
        raise RuntimeError("Missing material on carbine")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    nodes.active = node
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.cycles.device = "CPU"
scene.render.bake.margin = 8
bpy.ops.object.bake(type="AO", use_clear=True, margin=8)
image.filepath_raw = str(output / "Carbine AO.png")
image.file_format = "PNG"
image.save()
(output / "bake.json").write_text(json.dumps({"source": str(source), "parts": len(parts), "atlasSize": atlas["size"], "samples": 16, "type": "AO", "sourceBlendSaved": False}, indent=2) + "\n", encoding="utf-8")
print("CARBINE_AO_BAKED " + str(output / "Carbine AO.png"))
