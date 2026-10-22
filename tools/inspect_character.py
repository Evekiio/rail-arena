import bpy
import json
import sys

args = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.open_mainfile(filepath=args[0], load_ui=False)
report = []
for obj in bpy.data.objects:
    entry = {'name': obj.name, 'type': obj.type, 'dimensions': list(obj.dimensions)}
    if obj.type == 'MESH':
        entry['vertices'] = len(obj.data.vertices)
        entry['polygons'] = len(obj.data.polygons)
        entry['materials'] = [m.name for m in obj.data.materials if m]
        entry['groups'] = [g.name for g in obj.vertex_groups]
    if obj.type == 'ARMATURE':
        entry['bones'] = [b.name for b in obj.data.bones]
    report.append(entry)
with open(args[1], 'w') as handle:
    json.dump({'objects': report, 'actions': [a.name for a in bpy.data.actions]}, handle, indent=2)
