"""Validate baked motion targets and render geometry-derived equal-scale six-view pose references."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
check_only='--check-only' in sys.argv
family=sys.argv[sys.argv.index('--family')+1] if '--family' in sys.argv else 'Crouch'
assert family in ('Crouch','Slide','Standing'),family
folder=root/'ArtSource/Original/ArtPass-v1/Animation'/family
spec=json.loads((folder/(family.lower()+'-production.json')).read_text(encoding='utf-8'))
master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
scene=bpy.context.scene;rig=next(obj for obj in scene.objects if obj.type=='ARMATURE')
bodies=[obj for obj in scene.objects if obj.type=='MESH' and obj.find_armature()==rig]
assert bodies,'The canonical reference must include every mesh bound to the contender rig'
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Measured '+family.lower()+' camera',bpy.data.cameras.new('Measured '+family.lower()+' camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=2.2
target=Vector((0,0,.9));evidence=[]
for clip in spec['clips']:
    name=clip['name'];rig.animation_data.action=bpy.data.actions[name];first=None;maximum_error=0;minimum=float('inf');maximum_height=-float('inf');maximum_width=0
    for frame,targets in enumerate(clip['ankleTargetsBlenderMetres']):
        scene.frame_set(frame);bpy.context.view_layer.update()
        for side in ('Left','Right'):maximum_error=max(maximum_error,(rig.pose.bones[side+'Foot'].head-Vector(targets[side])).length)
        matrices={bone.name:bone.matrix.copy() for bone in rig.pose.bones}
        if frame==0:first=matrices
        points=[]
        for body in bodies:
            evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
            try:points.extend(evaluated.matrix_world@vertex.co for vertex in mesh.vertices)
            finally:evaluated.to_mesh_clear()
        minimum=min(minimum,min(point.z for point in points));maximum_height=max(maximum_height,max(point.z for point in points))
        maximum_width=max(maximum_width,max(point.x for point in points)-min(point.x for point in points))
    seam=max(abs(matrices[bone][row][column]-first[bone][row][column]) for bone in matrices for row in range(4) for column in range(4))
    assert maximum_error<.001,(name,maximum_error)
    if clip['loop']:assert seam<.001,(name,seam)
    assert minimum>=-.005,(name,'skin penetrates floor',minimum)
    phases=(0,.125,.25,.375,.5,.75) if clip['loop'] else (0,.2,.4,.6,.8,1)
    frames=[round(clip['frames']*phase) for phase in phases]
    destination=folder/'References'/name;destination.mkdir(parents=True,exist_ok=True)
    for view,direction in [('front',(0,-1,0)),('rear',(0,1,0)),('left',(-1,0,0)),('right',(1,0,0)),('top',(0,0,1)),('bottom',(0,0,-1))]:
        if check_only:continue
        camera.location=target+Vector(direction)*4;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        for index,frame in enumerate(frames):
            scene.frame_set(frame);scene.render.filepath=str(destination/(view+'-'+str(index)+'.png'));bpy.ops.render.render(write_still=True)
    evidence.append({'clip':name,'poseFrames':frames,'orthographicSpanMetres':2.2,'views':['front','rear','left','right','top','bottom'],'maximumBakedAnkleTargetErrorMetres':maximum_error,
        'loop':clip['loop'],'loopSeamMatrixDifference':seam if clip['loop'] else None,'minimumWholeSkinHeightMetres':minimum,'maximumWholeSkinHeightMetres':maximum_height,
        'maximumWholeSkinWidthMetres':maximum_width,'boundMeshCount':len(bodies),'scope':'All canonical Blender meshes bound to this rig and baked target checks; not Unity retarget, grip, capsule clearance or installed stance certification'})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/('baked-contact-check.json' if check_only else 'baked-contact-review.json')).write_bytes((json.dumps({'masterSha256':digest,'rendersUpdated':not check_only,'clips':evidence},indent=2)+'\n').encode('utf-8'))
print(family.upper()+'_REFERENCE_CONTACTS_VERIFIED',[(entry['clip'],entry['minimumWholeSkinHeightMetres']) for entry in evidence])
