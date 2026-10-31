"""Geometry-derived six-view pose evidence from the saved reaction master."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Reactions'
spec=json.loads((folder/'reactions-production.json').read_text(encoding='utf-8'))
master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
scene=bpy.context.scene;rig=next(obj for obj in scene.objects if obj.type=='ARMATURE')
body=max((obj for obj in scene.objects if obj.type=='MESH'),key=lambda obj:len(obj.data.vertices))
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Measured reaction camera',bpy.data.cameras.new('Measured reaction camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=2.5
views=[('front',(0,-4,.85)),('rear',(0,4,.85)),('left',(4,.05,.85)),('right',(-4,.05,.85)),('top',(0,.05,4)),('bottom',(0,.05,-4))]
records=[]
for clip in spec['clips']:
    name=clip['name'];rig.animation_data.action=bpy.data.actions[name]
    frames=[0,3,6,10,14,18] if name=='HitResponse' else [0,12,24,36,54,72]
    measurements=[]
    for frame in range(clip['frames']+1):
        scene.frame_set(frame);bpy.context.view_layer.update();evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
        try:
            points=[evaluated.matrix_world@vertex.co for vertex in mesh.vertices]
            low=[min(point[axis] for point in points) for axis in range(3)];high=[max(point[axis] for point in points) for axis in range(3)]
        finally:evaluated.to_mesh_clear()
        if name=='DeathFall':assert abs(low[2]-.015)<.001,(frame,low)
        measurements.append({'frame':frame,'minimumBlenderXYZ':low,'maximumBlenderXYZ':high})
    destination=folder/'References'/name;destination.mkdir(parents=True,exist_ok=True)
    for view,position in views:
        camera.location=position;camera.rotation_euler=(Vector((0,.05,.85))-camera.location).to_track_quat('-Z','Y').to_euler()
        for index,frame in enumerate(frames):
            scene.frame_set(frame);scene.render.filepath=str(destination/(view+'-'+str(index)+'.png'));bpy.ops.render.render(write_still=True)
    records.append({'clip':name,'fps':60,'poseFrames':frames,'orthographicSpanMetres':2.5,'views':[view[0] for view in views],'measurements':measurements})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'measured-pose-review.json').write_text(json.dumps({'masterSha256':digest,'scope':'Canonical Blender geometry at identical six-view scale. Runtime retargeting, corpse floor support and lifecycle need separate validation.','clips':records},indent=2)+'\n',encoding='utf-8')
print('REACTION_MEASURED_REFERENCES_RENDERED 72 views at 2.5 metre shared span')
