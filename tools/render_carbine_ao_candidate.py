"""Render the disposable repacked AO as an unlit inspection image."""
import bpy
import json
import sys
from mathutils import Vector
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
folder = root / ("Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedQuality" if "--quality" in sys.argv else "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedCandidate")
angled = "--angled" in sys.argv
output = folder / ("AO-inspection-angled.png" if angled else "AO-inspection.png")
if output.exists():
    raise RuntimeError("Inspection render already exists")
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
parts = sorted((o for o in scene.objects if o.type == "MESH"), key=lambda o: o.name)
entries = {entry["name"]: entry for entry in atlas["tiles"]}
gutter = atlas["gutterPixels"] / atlas["size"]
scale = 1 / 9 - 2 * gutter
for obj in parts:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(island_margin=0.01)
    bpy.ops.object.mode_set(mode="OBJECT")
    layer = obj.data.uv_layers["SurfaceAtlas"]
    entry = entries[obj.name]
    for loop in layer.data:
        loop.uv = (entry["column"] / 9 + gutter + loop.uv.x * scale,
                   entry["row"] / 9 + gutter + loop.uv.y * scale)
material = bpy.data.materials.new("AO inspection only")
material.use_nodes = True
nodes = material.node_tree.nodes
nodes.clear()
texture = nodes.new("ShaderNodeTexImage")
texture.image = bpy.data.images.load(str(folder / "AO.png"))
texture.image.colorspace_settings.name = "Non-Color"
emission = nodes.new("ShaderNodeEmission")
output_node = nodes.new("ShaderNodeOutputMaterial")
material.node_tree.links.new(texture.outputs["Color"], emission.inputs["Color"])
material.node_tree.links.new(emission.outputs[0], output_node.inputs["Surface"])
for obj in parts:
    obj.data.materials.clear()
    obj.data.materials.append(material)
camera = scene.camera
camera.data.type = "ORTHO"
camera.data.ortho_scale = 1.2
camera.location = (1.2, -3, 1.2) if angled else (0, -3, 0)
camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.engine = "CYCLES"
scene.cycles.samples = 8
scene.cycles.device = "CPU"
scene.render.resolution_x = 1600
scene.render.resolution_y = 700
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.view_settings.view_transform = "Standard"
scene.render.filepath = str(output)
bpy.ops.render.render(write_still=True)
print("AO_INSPECTION " + str(output))
