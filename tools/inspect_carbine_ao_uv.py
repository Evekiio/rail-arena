"""Inspect UV-layer selection before and after the disposable AO join."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineAO"
bpy.ops.wm.open_mainfile(filepath=str(root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"))
parts = sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name)
before = []
bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
    obj.data.uv_layers.active = obj.data.uv_layers["SurfaceAtlas"]
    before.append({"name": obj.name, "active": obj.data.uv_layers.active.name, "render": [u.name for u in obj.data.uv_layers if u.active_render], "layers": [u.name for u in obj.data.uv_layers]})
bpy.context.view_layer.objects.active = parts[0]
selected = list(bpy.context.selected_objects)
source_faces = sum(len(o.data.polygons) for o in parts)
bpy.ops.object.join()
mesh = bpy.context.object.data
report = {"before": before, "selection": {"sourceParts": len(parts), "selectedParts": len(selected), "sourceFaces": source_faces}, "joined": {"active": mesh.uv_layers.active.name, "render": [u.name for u in mesh.uv_layers if u.active_render], "layers": [u.name for u in mesh.uv_layers], "faces": len(mesh.polygons)}}
(folder / "uv-join-diagnostic.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print("CARBINE_AO_UV_JOIN " + json.dumps({"selection": report["selection"], "joined": report["joined"]}))
