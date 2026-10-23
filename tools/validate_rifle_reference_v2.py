"""Audit reference geometry and prove the non-stock benchmark meshes stayed intact."""
import bpy
import bmesh
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
OUT=ROOT/'ArtSource/Original/RailRifle-Modeling-References-v2'
meta=json.loads((OUT/'Calibration.json').read_text())
source=ROOT/'ArtSource/Original/ProductionBenchmarks-v1/Production-Benchmarks.blend'
assert hashlib.sha256(source.read_bytes()).hexdigest()==meta['sourceBenchmarkSha256']

def fingerprint(obj):
    digest=hashlib.sha256()
    for v in obj.data.vertices:digest.update(struct.pack('<3d',*v.co))
    for p in obj.data.polygons:
        digest.update(struct.pack('<I',len(p.vertices)))
        for i in p.vertices:digest.update(struct.pack('<I',i))
    for row in obj.matrix_world:
        for value in row:digest.update(struct.pack('<d',value))
    return digest.hexdigest()

bpy.ops.wm.open_mainfile(filepath=str(source))
original={o.name:fingerprint(o) for o in bpy.data.collections['Rifle editable parts'].objects
          if o.type=='MESH' and not o.name.startswith(('Stock ','Skeletal ','Rubber shoulder'))}
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Rifle-Reference-Study.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
new={o.name:fingerprint(o) for o in meshes if not o.name.startswith('S2 ')}
assert original==new,'A non-stock mesh changed'
assert len(meshes)==len(meta['assets'][0]['parts'])
triangles=0
for obj in meshes:
    bm=bmesh.new();bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges),obj.name
    if obj.name=='S2 Shell':assert len(bm.verts)-len(bm.edges)+len(bm.faces)==0,'Stock shell must have one through opening'
    bm.free();obj.data.calc_loop_triangles()
    assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
    triangles+=len(obj.data.loop_triangles)
    assert obj.data.uv_layers and 'assemblyId' in obj
assert set(o['assemblyId'] for o in meshes)=={f'RF-{i:02}' for i in range(1,13)}
assert not any(o.name.startswith(('Stock energy','Skeletal','Rubber shoulder')) for o in meshes)
assert hashlib.sha256((OUT/'Rifle-Reference-Study.blend').read_bytes()).hexdigest()==meta['sourceMasterSha256']
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Blender-Reference-Rig.blend'))
assert bpy.context.scene.unit_settings.scale_length==1
empties=[o for o in bpy.context.scene.objects if o.type=='EMPTY']
assert len(empties)==6 and not any(o.type=='MESH' for o in bpy.context.scene.objects)
for obj in empties:
    assert abs(obj.empty_display_size-1.45)<1e-6 and obj.hide_select
    assert obj.data.packed_file is not None
report=dict(passed=True,nonStockMeshesUnchanged=len(new),referenceParts=len(meshes),referenceTriangles=triangles,
            assemblyGroups=12,stockShellThroughOpenings=1,sourceMasterHash=True,
            packedCalibratedImagePlanes=6,productionAssetChanged=False)
(OUT/'Geometry-Validation.json').write_text(json.dumps(report,indent=2)+'\n')
print('RIFLE_REFERENCE_V2_GEOMETRY_PASS '+json.dumps(report))
