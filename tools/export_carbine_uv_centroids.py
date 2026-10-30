"""Export triangle UV centroids for coverage comparison without modifying the blend."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineAO"
bpy.ops.wm.open_mainfile(filepath=str(root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"))
rows = []
for obj in sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name):
    uv = obj.data.uv_layers["SurfaceAtlas"]
    obj.data.calc_loop_triangles()
    for tri in obj.data.loop_triangles:
        p = [uv.data[i].uv for i in tri.loops]
        area = abs((p[1].x-p[0].x)*(p[2].y-p[0].y)-(p[1].y-p[0].y)*(p[2].x-p[0].x)) * .5
        rows.append([sum(v.x for v in p)/3, sum(v.y for v in p)/3, area])
(folder / "triangle-uv-centroids.json").write_text(json.dumps(rows) + "\n", encoding="utf-8")
print("CARBINE_UV_CENTROIDS " + str(len(rows)))
