"""Independently audit the saved Blender candidate without changing it."""
import hashlib
import json
import math
import sys
from pathlib import Path

import bmesh
import bpy

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
folder = root / 'ArtSource/Original/Foundry-Models-v1'
bpy.ops.wm.open_mainfile(filepath=str(folder / 'Foundry-Environment.blend'))
report = json.loads((folder / 'Model-Validation.json').read_text())
results = []
for asset in report['assets']:
    collection = bpy.data.collections[asset['id']]
    triangles = 0
    for obj in collection.objects:
        assert obj.type == 'MESH', obj.name
        mesh = obj.data
        assert mesh.uv_layers and len(mesh.materials), obj.name
        assert all(math.isfinite(v) for vertex in mesh.vertices for v in vertex.co), obj.name
        assert all(math.isfinite(v) for uv in mesh.uv_layers.active.data for v in uv.uv), obj.name
        assert all(p.material_index < len(mesh.materials) for p in mesh.polygons), obj.name
        bm = bmesh.new()
        bm.from_mesh(mesh)
        try:
            assert all(edge.is_manifold for edge in bm.edges), obj.name
            assert all(face.calc_area() > 1e-10 for face in bm.faces), obj.name
        finally:
            bm.free()
        triangles += sum(len(p.vertices) - 2 for p in mesh.polygons)
    assert triangles == asset['triangles'], asset['id']
    assert len(collection.objects) == asset['parts'], asset['id']
    results.append({'id': asset['id'], 'triangles': triangles, 'parts': len(collection.objects)})
assert bpy.context.scene.unit_settings.system == 'METRIC'
assert bpy.context.scene.unit_settings.scale_length == 1
receipt = {
    'status': 'pass', 'blender': bpy.app.version_string,
    'masterSHA256': hashlib.sha256((folder / 'Foundry-Environment.blend').read_bytes()).hexdigest(),
    'checks': ['metre scale', 'finite vertices and UVs', 'material assignments',
               'closed manifold parts', 'nondegenerate faces', 'inventory triangle and part counts'],
    'assets': results,
    'limits': 'Does not certify silhouette fidelity, final UV packing, Unity import, collision or performance.'
}
(folder / 'Master-Audit.json').write_text(json.dumps(receipt, indent=2) + '\n')
print('PASS: saved master audited:', len(results), 'asset assemblies')
