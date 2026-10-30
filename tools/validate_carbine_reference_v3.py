"""Headless audit of the original carbine reference study, not a production certification."""
import bpy,bmesh,hashlib,json,math,sys
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);out=root/'ArtSource/Original/PrecisionCarbine-References-v3'
meta=json.loads((out/'Calibration.json').read_text())
master=out/'Carbine-Reference-Study.blend'
assert hashlib.sha256(master.read_bytes()).hexdigest()==meta['sourceMasterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];triangles=0
for obj in meshes:
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co),obj.name
    bm=bmesh.new();bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges),obj.name
    if obj.name=='C3 reinforced stock':assert len(bm.verts)-len(bm.edges)+len(bm.faces)==0
    bm.free();obj.data.calc_loop_triangles()
    assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
    assert obj.data.uv_layers and all(math.isfinite(c) for uv in obj.data.uv_layers.active.data for c in uv.uv)
    assert all(p.material_index<len(obj.data.materials) for p in obj.data.polygons)
    triangles+=len(obj.data.loop_triangles)
assert set(o['assemblyId'] for o in meshes)=={f'RF-{i:02}' for i in range(1,13)}
dims=meta['assets'][0]['dimensions'];assert abs(dims[0]-1)<.001 and dims[1]<=.101 and .32<dims[2]<.34
baseline=json.loads((out/'Live-Asset-Baseline.json').read_text())
assert all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in baseline.items())
bpy.ops.wm.open_mainfile(filepath=str(out/'Blender-Reference-Rig.blend'))
planes=list(bpy.context.scene.objects);assert len(planes)==6
assert all(o.type=='EMPTY' and o.data.packed_file and abs(o.empty_display_size-1.25)<1e-6 for o in planes)
report=dict(passed=True,referenceParts=len(meshes),triangles=triangles,assemblyGroups=12,finiteGeometry=True,manifold=True,nondegenerate=True,uvPresent=True,materialAssignments=True,dimensionsMetres=dims,packedImagePlanes=6,liveAssetsUnchanged=True,productionCertified=False)
(out/'Geometry-Validation.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
