"""Read-only worst deformed vertex evidence for crouch authoring corrections."""
import json
from pathlib import Path
import sys
import bpy

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Crouch'
spec=json.loads((folder/'crouch-production.json').read_text(encoding='utf-8'))
bpy.ops.wm.open_mainfile(filepath=str(folder/spec['master']),load_ui=False)
rig=next(obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE')
bodies=[obj for obj in bpy.context.scene.objects if obj.type=='MESH' and obj.find_armature()==rig]
records=[]
for clip in spec['clips']:
    rig.animation_data.action=bpy.data.actions[clip['name']];worst={'height':float('inf')}
    for frame in range(clip['frames']+1):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        for body in bodies:
            evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
            try:
                vertex=min(mesh.vertices,key=lambda vertex:(evaluated.matrix_world@vertex.co).z)
                point=evaluated.matrix_world@vertex.co
                if point.z<worst['height']:
                    original=body.data.vertices[vertex.index]
                    worst={'clip':clip['name'],'frame':frame,'mesh':body.name,'vertex':vertex.index,'height':point.z,'world':list(point),
                        'weights':[{ 'bone':body.vertex_groups[group.group].name,'weight':group.weight} for group in original.groups],
                        'bones':{name:list(rig.matrix_world@rig.pose.bones[name].head) for name in ('Hips','LeftLeg','RightLeg','LeftFoot','RightFoot')}}
            finally:evaluated.to_mesh_clear()
    records.append(worst)
(root/'Validation/motion-crouch-floor-diagnostic.json').write_text(json.dumps({'masterSha256':spec['masterSha256'],'scope':'Read-only all-bound-mesh Blender diagnostic; not installed gameplay evidence','meshCount':len(bodies),'clips':records},indent=2)+'\n',encoding='utf-8')
print('CROUCH_FLOOR_DIAGNOSED',[(entry['clip'],entry['mesh'],entry['frame'],entry['height']) for entry in records])
