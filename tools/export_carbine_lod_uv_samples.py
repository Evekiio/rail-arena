"""Export candidate FBX triangle UV samples for mip coverage analysis."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedModels"
output = folder / "lod-uv-samples.json"
if output.exists():
    raise RuntimeError("Audit output already exists")
rows = []
for level in range(3):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder / f"Carbine_LOD{level}.fbx"), use_anim=False)
    objects = []
    for obj in sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name):
        obj.data.calc_loop_triangles()
        uv = obj.data.uv_layers.active
        triangles = [[[float(uv.data[i].uv.x), float(uv.data[i].uv.y)] for i in tri.loops] for tri in obj.data.loop_triangles]
        objects.append({"name": obj.name, "triangles": triangles})
    rows.append({"level": level, "objects": objects})
output.write_text(json.dumps(rows) + "\n", encoding="utf-8")
print("LOD_UV_EXPORTED " + str(output))
