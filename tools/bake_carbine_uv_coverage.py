"""Bake a white emission coverage mask using the same joined carbine UVs."""
import bpy
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineAO"
size = int(sys.argv[sys.argv.index("--") + 2]) if len(sys.argv) > sys.argv.index("--") + 2 else 2048
if size not in (2048, 4096):
    raise ValueError("Coverage size must be 2048 or 4096")
output = folder / ("CoverageWhite.png" if size == 2048 else "CoverageWhite-4096.png")
if output.exists():
    raise RuntimeError("Coverage image already exists")
bpy.ops.wm.open_mainfile(filepath=str(root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"))
parts = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
obj = bpy.context.object
image = bpy.data.images.new("Carbine coverage", width=size, height=size, alpha=False)
image.colorspace_settings.name = "Non-Color"
material = bpy.data.materials.new("Carbine coverage material")
material.use_nodes = True
nodes = material.node_tree.nodes
nodes.clear()
output_node = nodes.new("ShaderNodeOutputMaterial")
emission = nodes.new("ShaderNodeEmission")
emission.inputs["Color"].default_value = (1, 1, 1, 1)
material.node_tree.links.new(emission.outputs[0], output_node.inputs["Surface"])
node = nodes.new("ShaderNodeTexImage")
node.image = image
nodes.active = node
obj.data.materials.clear()
obj.data.materials.append(material)
for poly in obj.data.polygons:
    poly.material_index = 0
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 1
scene.cycles.device = "CPU"
bpy.ops.object.bake(type="EMIT", use_clear=True, margin=8)
image.filepath_raw = str(output)
image.file_format = "PNG"
image.save()
print("CARBINE_UV_COVERAGE_BAKED " + str(output))
