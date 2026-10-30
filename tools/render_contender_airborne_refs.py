"""Render the saved airborne master's key poses at a shared metre scale, without changing it."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Airborne'
master=folder/'Contender-Airborne-v1.blend';digest=hashlib.sha256(master.read_bytes()).hexdigest()
spec=json.loads((folder/'airborne-production.json').read_text());assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rig.data.pose_position='POSE'
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Airborne measured camera',bpy.data.cameras.new('Airborne measured camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=2.2
poses={'Takeoff':[0,3,6,9,15,18],'AirborneHold':[0,6,12,18,24,30],'LandingRecover':[0,5,8,15,22,30]}
evidence=[]
for clip in spec['clips']:
    name=clip['name'];rig.animation_data.action=bpy.data.actions[name]
    measurements=[]
    for frame in range(clip['frames']+1):
        scene.frame_set(frame);bpy.context.view_layer.update()
        measurements.append({'frame':frame,'hipsMetresBlenderXYZ':list(rig.pose.bones['Hips'].head),'leftAnkleMetresBlenderXYZ':list(rig.pose.bones['LeftFoot'].head),'rightAnkleMetresBlenderXYZ':list(rig.pose.bones['RightFoot'].head)})
    if clip['loop']:
        for key in ('hipsMetresBlenderXYZ','leftAnkleMetresBlenderXYZ','rightAnkleMetresBlenderXYZ'):
            assert (Vector(measurements[0][key])-Vector(measurements[-1][key])).length<.001,(name,key)
    directory=folder/'References'/name;directory.mkdir(parents=True,exist_ok=True)
    for view,position in [('front',(0,-4,.9)),('side',(4,0,.9))]:
        camera.location=position;camera.rotation_euler=(Vector((0,0,.9))-camera.location).to_track_quat('-Z','Y').to_euler()
        for index,frame in enumerate(poses[name]):
            scene.frame_set(frame);scene.render.filepath=str(directory/(view+'-'+str(index)+'.png'));bpy.ops.render.render(write_still=True)
    evidence.append({'clip':name,'fps':60,'poseFrames':poses[name],'orthographicSpanMetres':2.2,'views':['front','side'],'measurements':measurements})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'measured-pose-review.json').write_text(json.dumps({'masterSha256':digest,'scope':'Canonical baked Blender geometry; shared character orthographics define the rest mesh. Runtime retargeting and controller motion require separate evidence.','clips':evidence},indent=2)+'\n')
print('AIRBORNE_MEASURED_REFERENCES_RENDERED 36 poses / 2.2 metre shared view span')
