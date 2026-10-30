"""Export original and proposed UV triangles for a disposable fore-end audit."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
output = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineForeendUVCompare.json"
if output.exists():
    raise RuntimeError("Comparison output already exists")
bpy.ops.wm.open_mainfile(filepath=str(source))
obj = bpy.data.objects["C3 enclosed vented fore-end"]
obj.data.calc_loop_triangles()
triangles = [list(t.loops) for t in obj.data.loop_triangles]

def points(layer):
    return [[[float(layer.data[i].uv.x), float(layer.data[i].uv.y)] for i in tri] for tri in triangles]

original = points(obj.data.uv_layers["SurfaceAtlas"])
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
layer = obj.data.uv_layers.new(name="DisposableCoverageUV")
obj.data.uv_layers.active = layer
layer.active_render = True
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(island_margin=0.01)
bpy.ops.object.mode_set(mode="OBJECT")
layer = obj.data.uv_layers["DisposableCoverageUV"]
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
entry = next(item for item in atlas["tiles"] if item["name"] == obj.name)
grid = 9
gutter = atlas["gutterPixels"] / atlas["size"]
scale = 1 / grid - 2 * gutter
for loop in layer.data:
    loop.uv = (entry["column"] / grid + gutter + loop.uv.x * scale,
               entry["row"] / grid + gutter + loop.uv.y * scale)
output.write_text(json.dumps({"name": obj.name, "tile": entry, "original": original,
                              "candidate": points(layer), "sourceBlendSaved": False}) + "\n", encoding="utf-8")
print("FOREEND_UV_EXPORTED " + str(output))
