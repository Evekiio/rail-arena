"""Export original and disposable candidate UV triangles for every rifle part."""
import bpy
import json
import math
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
fix_muzzle = "--muzzle-fix" in sys.argv
output = root / ("Validation/SkyGardens-Playable-2026-10-29/CarbineAllUVMuzzleAngle.json" if fix_muzzle else "Validation/SkyGardens-Playable-2026-10-29/CarbineAllUVCompare.json")
if output.exists():
    raise RuntimeError("Comparison output already exists")
atlas = json.loads((source.parent / "SurfaceAtlas.json").read_text(encoding="utf-8"))
entries = {entry["name"]: entry for entry in atlas["tiles"]}
bpy.ops.wm.open_mainfile(filepath=str(source))
parts = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda obj: obj.name)
if set(entries) != {obj.name for obj in parts}:
    raise RuntimeError("Source parts and atlas manifest differ")
gutter = atlas["gutterPixels"] / atlas["size"]
scale = 1 / 9 - 2 * gutter
rows = []
for obj in parts:
    obj.data.calc_loop_triangles()
    triangles = [list(tri.loops) for tri in obj.data.loop_triangles]
    def coords(layer):
        return [[[float(layer.data[i].uv.x), float(layer.data[i].uv.y)] for i in tri] for tri in triangles]
    original = coords(obj.data.uv_layers["SurfaceAtlas"])
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
        loop.uv = (entry["column"] / 9 + gutter + loop.uv.x * scale,
                   entry["row"] / 9 + gutter + loop.uv.y * scale)
    rows.append({"name": obj.name, "tile": entry, "original": original, "candidate": coords(layer)})
output.write_text(json.dumps({"parts": rows, "sourceBlendSaved": False}) + "\n", encoding="utf-8")
print("ALL_UVS_EXPORTED " + str(output))
