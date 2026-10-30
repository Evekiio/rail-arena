"""Validate baked contact tracks and render equal-scale pose strips without editing the motion master."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Animation'
forward='--forward' in sys.argv
if forward:folder=folder/'Forward'
master=folder/('Contender-Advance-v1.blend' if forward else 'Contender-Retreat-v1.blend');digest=hashlib.sha256(master.read_bytes()).hexdigest()
record=json.loads((folder/('forward-production.json' if forward else 'retreat-production.json')).read_text())
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.data.pose_position='POSE'
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Measured motion review camera',bpy.data.cameras.new('Measured motion review camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=2.2
evidence=[]
for spec in record['clips']:
    name=spec['name'];frames=spec['frames'];rig.animation_data.action=bpy.data.actions[name];error=0;first=None
    for frame in range(frames+1):
        scene.frame_set(frame);bpy.context.view_layer.update()
        for side,shift in [('Left',0),('Right',.5)]:
            p=(frame/frames+shift)%1
            if p<.5:travel=spec['strideMetres']*(.5-2*p);height=0
            else:
                import math
                t=(p-.5)*2;travel=spec['strideMetres']*(-.5+t*t*(3-2*t));height=spec['swingAnkleLiftMetres']*math.sin(math.pi*t)
            target=rig.data.bones[side+'Foot'].head_local+Vector((0,-travel if forward else travel,height))
            error=max(error,(rig.pose.bones[side+'Foot'].head-target).length)
        matrices={bone.name:bone.matrix.copy() for bone in rig.pose.bones}
        if frame==0:first=matrices
    seam=max(abs(matrices[name][row][column]-first[name][row][column]) for name in matrices for row in range(4) for column in range(4))
    assert error<.001,(name,error)
    assert seam<.001,(name,seam)
    destination=folder/'References'/name;destination.mkdir(parents=True,exist_ok=True)
    for angle,position in [('front',(0,-4,.9)),('side',(4,0,.9))]:
        camera.location=position;camera.rotation_euler=(Vector((0,0,.9))-camera.location).to_track_quat('-Z','Y').to_euler()
        for index,phase in enumerate((0,.125,.25,.375,.5,.75)):
            scene.frame_set(round(frames*phase));scene.render.filepath=str(destination/(angle+'-'+str(index)+'.png'));bpy.ops.render.render(write_still=True)
    pose_frames=[round(frames*phase) for phase in (0,.125,.25,.375,.5,.75)]
    evidence.append({'clip':name,'maximumBakedAnkleTargetErrorMetres':error,'maximumLoopSeamMatrixDifference':seam,'orthographicSpanMetres':2.2,'views':['front','side'],'poseFrames':pose_frames,'posePhases':[frame/frames for frame in pose_frames],'scope':'Baked Blender poses; in-place animation, not Unity/controller contact certification'})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'baked-contact-review.json').write_text(json.dumps({'masterSha256':digest,'clips':evidence},indent=2)+'\n')
print('BAKED_RETREAT_CONTACTS_VERIFIED',[(entry['clip'],entry['maximumBakedAnkleTargetErrorMetres']) for entry in evidence])
