"""Compare original and candidate FBX structure without changing source assets."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
original = root / "RailArena/Assets/RailArena/Art/PrecisionCarbine"
candidate = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedModels"
output = candidate / "fbx-comparison.json"
if output.exists():
    raise RuntimeError("Comparison already exists")

def inspect(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
    result = []
    for obj in sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name):
        obj.data.calc_loop_triangles()
        result.append({"name": obj.name, "triangles": len(obj.data.loop_triangles),
                       "vertices": len(obj.data.vertices), "origin": [round(v, 6) for v in obj.location],
                       "dimensions": [round(v, 6) for v in obj.dimensions], "uvLayers": len(obj.data.uv_layers)})
    return result

rows = []
for level in range(3):
    filename = f"Carbine_LOD{level}.fbx"
    before, after = inspect(original / filename), inspect(candidate / filename)
    rows.append({"level": level, "original": before, "candidate": after, "structureMatches": before == after})
output.write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
print("FBX_STRUCTURE " + json.dumps([{ "level": row["level"], "matches": row["structureMatches"], "originalMeshes": len(row["original"]), "candidateMeshes": len(row["candidate"])} for row in rows]))
