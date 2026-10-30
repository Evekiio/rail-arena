"""Compare world-space oriented triangles in the original and candidate FBXs."""
import bpy
import json
import sys
from collections import Counter
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
include_normals = "--normals" in sys.argv
original = root / "RailArena/Assets/RailArena/Art/PrecisionCarbine"
candidate = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedModels"
output = candidate / ("triangle-normals.json" if include_normals else "triangle-geometry.json")
if output.exists():
    raise RuntimeError("Audit output already exists")

def inspect(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
    result = {}
    for obj in (o for o in bpy.context.scene.objects if o.type == "MESH"):
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles = Counter()
        for tri in mesh.loop_triangles:
            points = [tuple(round(float(c), 6) for c in (obj.matrix_world @ mesh.vertices[i].co)) for i in tri.vertices]
            if include_normals:
                normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
                normals = [tuple(round(float(c), 5) for c in (normal_matrix @ mesh.corner_normals[i].vector).normalized()) for i in tri.loops]
                points = list(zip(points, normals))
            oriented = min(tuple(points[i:] + points[:i]) for i in range(3))
            triangles[oriented] += 1
        result[obj.name] = triangles
    return result

rows = []
for level in range(3):
    name = f"Carbine_LOD{level}.fbx"
    a, b = inspect(original / name), inspect(candidate / name)
    if set(a) != set(b):
        raise RuntimeError("FBX mesh groups differ")
    mismatches = []
    for group in sorted(a):
        removed = sum((a[group] - b[group]).values())
        added = sum((b[group] - a[group]).values())
        if removed or added:
            mismatches.append({"name": group, "originalOnly": removed, "candidateOnly": added})
    rows.append({"level": level, "triangles": sum(sum(c.values()) for c in a.values()), "mismatchedGroups": mismatches})
output.write_text(json.dumps({"precisionMetres": 0.000001, "levels": rows}, indent=2) + "\n", encoding="utf-8")
print("TRIANGLE_GEOMETRY " + json.dumps([{ "level": r["level"], "triangles": r["triangles"], "mismatchedGroups": len(r["mismatchedGroups"])} for r in rows]))
