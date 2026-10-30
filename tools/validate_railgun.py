"""Inspect the editable RA-01 master and both exported LODs in background Blender."""
import bpy
import json
import math
from pathlib import Path
import sys
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'ArtSource/Original/Railgun'
assets = root / 'RailArena/Assets/RailArena/Art/Railgun'
bpy.ops.wm.open_mainfile(filepath=str(source / 'Railgun.blend'))
parts = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
assert len(parts) > 100, 'The master must retain separately editable parts.'
assert any(obj.type == 'FONT' and obj.data.body == 'RA-01' for obj in bpy.context.scene.objects)
report = {'masterMeshParts': len(parts), 'editableReceiverText': True, 'lods': []}
expected = json.loads((source / 'preparation.json').read_text())['lods']
for level in (0, 1):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(assets / f'Railgun_LOD{level}.fbx'))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    assert len(meshes) == 7, 'Six material surfaces and one actuator are required.'
    assert sum(obj.name.startswith('Actuator') for obj in meshes) == 1
    triangles = 0
    degenerate = 0
    corners = []
    for obj in meshes:
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
        for vertex in obj.data.vertices:
            assert all(math.isfinite(value) for value in vertex.co), 'Invalid vertex coordinate.'
        degenerate += sum(triangle.area < 1e-14 for triangle in obj.data.loop_triangles)
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    assert triangles == expected[level]['triangles']
    assert degenerate == 0, 'Export contains zero-area triangles.'
    dimensions = [max(point[axis] for point in corners) - min(point[axis] for point in corners) for axis in range(3)]
    assert .9 < max(dimensions) < 1.4, 'Export scale must remain approximately 1.2 metres.'
    report['lods'].append({'level': level, 'meshes': len(meshes), 'triangles': triangles,
                           'degenerateTriangles': degenerate, 'dimensionsBlenderMetres': dimensions})
report['passed'] = True
output = root / 'Validation/railgun-refinement-geometry.json'
output.write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
