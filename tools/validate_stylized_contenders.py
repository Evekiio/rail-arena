"""Background Blender audit of the original contender sources and skinned exports."""
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
source=root/'ArtSource/Original/StylizedContenders'
assets=root/'RailArena/Assets/RailArena/Art/StylizedContenders'
report={'characters':[]}
for name in ('Player','Bot01','Bot02','Bot03'):
    bpy.ops.wm.open_mainfile(filepath=str(source/(name+'.blend')),load_ui=False)
    meshes=[obj for obj in bpy.data.objects if obj.type=='MESH']
    assert len(meshes)>150,'Master must preserve individual editable model parts.'
    entry={'name':name,'editableParts':len(meshes),'lods':[]}
    expected=json.loads((source/(name+'-Preparation.json')).read_text())['lods']
    previous=math.inf
    for level in range(3):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(assets/(name+'_LOD'+str(level)+'.fbx')))
        meshes=[obj for obj in bpy.data.objects if obj.type=='MESH']
        rigs=[obj for obj in bpy.data.objects if obj.type=='ARMATURE']
        assert len(meshes)==1 and len(rigs)==1,'Each LOD requires one skin and one skeleton.'
        mesh,rig=meshes[0],rigs[0]
        assert len(mesh.data.materials)==1,'Each character LOD uses one palette surface.'
        assert len(mesh.data.uv_layers)==1 and mesh.data.uv_layers[0].name=='PaletteUV'
        swatches={tuple(round(value,4) for value in corner.uv) for corner in mesh.data.uv_layers[0].data}
        assert 8<=len(swatches)<=16,'The primary UV channel must contain palette swatches, not primitive UVs.'
        mesh.data.calc_loop_triangles()
        triangles=len(mesh.data.loop_triangles)
        assert triangles==expected[level]['triangles'] and triangles<previous, f"{name} LOD{level}: imported {triangles}, expected {expected[level]['triangles']}, previous {previous}"
        previous=triangles
        zero_area=sum(triangle.area<1e-16 for triangle in mesh.data.loop_triangles)
        assert zero_area==0,f'{name} LOD{level} has {zero_area} degenerate triangles.'
        totals={group.name:0 for group in mesh.vertex_groups}
        for vertex in mesh.data.vertices:
            assert all(math.isfinite(value) for value in vertex.co)
            weights=[group for group in vertex.groups if group.weight>1e-8]
            assert weights and len(weights)<=4,'Each vertex requires one to four skin influences.'
            assert abs(sum(group.weight for group in weights)-1)<.001,'Skin weights must normalize.'
            for group in weights: totals[mesh.vertex_groups[group.group].name]+=group.weight
        fingers=[side+'Hand'+finger+str(segment) for side in ('Left','Right')
                 for finger in ('Thumb','Index','Middle','Ring','Pinky') for segment in (1,2,3)]
        assert all(bone in rig.data.bones for bone in fingers),'All thirty finger joints must survive export.'
        if level==0: assert all(totals.get(bone,0)>.01 for bone in fingers),'Each finger joint must deform visible skin.'
        corners=[mesh.matrix_world@Vector(corner) for corner in mesh.bound_box]
        dimensions=[max(point[i] for point in corners)-min(point[i] for point in corners) for i in range(3)]
        assert 1.7<dimensions[2]<2.05,'Character must remain human-sized in metres.'
        entry['lods'].append({'level':level,'triangles':triangles,'vertices':len(mesh.data.vertices),
                              'bones':len(rig.data.bones),'degenerateTriangles':zero_area,'boundsMetres':dimensions})
    report['characters'].append(entry)
report['passed']=True
output=root/'Validation/StylizedContenders/Geometry.json'
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(report,indent=2)+'\n')
print('STYLIZED_GEOMETRY '+json.dumps(report))
