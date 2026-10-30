"""Compare FBX geometry and UV-driven vertex splits without Unity import."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
original = root / "RailArena/Assets/RailArena/Art/PrecisionCarbine"
candidate = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedModels"
output = candidate / "vertex-splits.json"
if output.exists():
    raise RuntimeError("Audit output already exists")

def inspect(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
    result = {}
    for obj in (o for o in bpy.context.scene.objects if o.type == "MESH"):
        mesh = obj.data
        uv = mesh.uv_layers.active
        keys = {(loop.vertex_index, round(uv.data[loop.index].uv.x, 7), round(uv.data[loop.index].uv.y, 7)) for loop in mesh.loops}
        result[obj.name] = {"vertices": len(mesh.vertices), "uvSplitVertices": len(keys), "loops": len(mesh.loops)}
    return result

rows = []
for level in range(3):
    name = f"Carbine_LOD{level}.fbx"
    a, b = inspect(original / name), inspect(candidate / name)
    if set(a) != set(b):
        raise RuntimeError("FBX mesh groups differ")
    rows.append({"level": level, "changedGroups": [{"name": name, "original": a[name], "candidate": b[name]} for name in a if a[name] != b[name]]})
output.write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
print("VERTEX_SPLIT_AUDIT " + json.dumps([{ "level": r["level"], "changedGroups": len(r["changedGroups"])} for r in rows]))
