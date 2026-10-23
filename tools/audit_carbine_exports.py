"""Inspect every runtime LOD independently of the authoring scene."""
import bpy,bmesh,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]);out=root/'ArtSource/Original/PrecisionCarbine-Production-v1';asset=root/'RailArena/Assets/RailArena/Art/PrecisionCarbine'
rows=[]
for level,budget in enumerate((12000,6000,3000)):
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(asset/f'Carbine_LOD{level}.fbx'))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];triangles=0;points=[]
    for obj in meshes:
        obj.data.calc_loop_triangles();triangles+=len(obj.data.loop_triangles)
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.normal)
        assert all(abs(v.normal.length-1)<.001 for v in obj.data.vertices),obj.name
        assert obj.data.uv_layers and all(0<=c<=1 for uv in obj.data.uv_layers.active.data for c in uv.uv)
        bm=bmesh.new();bm.from_mesh(obj.data)
        assert all(e.is_manifold for e in bm.edges),obj.name
        bm.free();points.extend(obj.matrix_world@v.co for v in obj.data.vertices)
    dims=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert triangles<=budget and abs(max(dims)-1)<.002
    rows.append(dict(level=level,triangles=triangles,dimensionsMetres=dims,finite=True,normalsUnitLength=True,uvInBounds=True,manifold=True,degenerates=0))
(out/'Export-Validation.json').write_text(json.dumps(dict(passed=True,lods=rows),indent=2)+'\n');print(json.dumps(rows))
