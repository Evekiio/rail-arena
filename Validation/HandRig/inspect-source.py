import bpy, json, os, sys
from mathutils import Vector

source, output = sys.argv[sys.argv.index('--') + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
armature = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
report = {}
for name in ('LeftHand', 'RightHand'):
    transform = armature.matrix_world @ armature.data.bones[name].matrix_local
    points = []
    for mesh in (o for o in bpy.data.objects if o.type == 'MESH'):
        group = mesh.vertex_groups.get(name)
        if group is None: continue
        for vertex in mesh.data.vertices:
            if any(w.group == group.index and w.weight > .6 for w in vertex.groups):
                points.append(list(transform.inverted() @ mesh.matrix_world @ vertex.co))
    report[name] = {'points': points, 'wrist': list(transform.translation),
        'boneLength': armature.data.bones[name].length}
os.makedirs(os.path.dirname(output), exist_ok=True)
with open(output, 'w') as handle: json.dump(report, handle)
print(json.dumps({name: {'vertices': len(data['points']), 'boneLength': data['boneLength'],
    'minimum': [min(p[i] for p in data['points']) for i in range(3)],
    'maximum': [max(p[i] for p in data['points']) for i in range(3)]} for name, data in report.items()}))
