"""Original in-place retreat cycles on the unchanged shared skeleton, baked from explicit IK contacts."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector, Quaternion

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
forward='--forward' in sys.argv
strafe='--strafe' in sys.argv
prefix='Strafe' if strafe else 'Advance' if forward else 'Retreat'
source=root/'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
folder=root/'ArtSource/Original/ArtPass-v1/Animation'
if forward:folder=folder/'Forward'
if strafe:folder=folder/'Strafe'
folder.mkdir(parents=True,exist_ok=True)
exports=root/'RailArena/Assets/RailArena/Art/ArtPass/Animation';exports.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False)
bpy.context.preferences.filepaths.file_preview_type='NONE'
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rig.animation_data_clear();rig.animation_data_create();rig.data.pose_position='POSE'
scene=bpy.context.scene;scene.render.fps=60;scene.render.fps_base=1
goals={};constraints=[];helpers=[]
for side in ('Left','Right'):
    foot=rig.data.bones[side+'Foot'];lower=rig.pose.bones[side+'Leg']
    goal=bpy.data.objects.new(side+' ankle contact target',None);scene.collection.objects.link(goal)
    goal.matrix_world=rig.matrix_world@foot.matrix_local;helpers.append(goal);goals[side]=goal
    pole=bpy.data.objects.new(side+' forward knee pole',None);scene.collection.objects.link(pole)
    pole.location=rig.matrix_world@Vector((foot.head_local.x,-1,.55));helpers.append(pole)
    ik=lower.constraints.new('IK');ik.target=goal;ik.pole_target=pole;ik.chain_count=2;ik.use_stretch=False;constraints.append((lower,ik))
    # The existing rig has nontrivial bone rolls. Calibrate the pole around its actual rest knee.
    desired=rig.data.bones[side+'Leg'].head_local.copy();best=(float('inf'),0)
    for step in range(128):
        angle=-math.pi+2*math.pi*step/128;ik.pole_angle=angle;bpy.context.view_layer.update()
        distance=(lower.head-desired).length
        if distance<best[0]:best=(distance,angle)
    ik.pole_angle=best[1]
    rotation=rig.pose.bones[side+'Foot'].constraints.new('COPY_ROTATION');rotation.target=goal;rotation.owner_space='WORLD';rotation.target_space='WORLD';constraints.append((rig.pose.bones[side+'Foot'],rotation))

def rotate_world(name,angle):
    bone=rig.pose.bones[name];bone.rotation_mode='QUATERNION'
    axis=bone.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0))
    bone.rotation_quaternion=Quaternion(axis,angle)

def reset_pose():
    for bone in rig.pose.bones:
        bone.location=(0,0,0);bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.scale=(1,1,1)

specs=[(prefix+'Walk',36,.53,.075,.11,0,0),(prefix+'Jog',20,.74,.105,.17,0,0),(prefix+'Run',18,.82,.13,.22,0,0)]
if strafe:
    specs=[('Strafe'+direction+gait,frames,stride,lift,lowering,bias,sign)
        for direction,sign in [('Left',1),('Right',-1)]
        for gait,frames,stride,lift,lowering,bias in [('Walk',36,.4,.065,.11,.08),('Jog',20,.5,.09,.18,.14),('Run',18,.6,.12,.27,.2)]]
records=[];baked=[]
for name,frames,stride,lift,lowering,bias,direction in specs:
    rig.animation_data.action=None;snapshots=[];maximum_error=0
    for frame in range(frames+1):
        scene.frame_set(frame);reset_pose();phase=frame/frames
        hips=rig.pose.bones['Hips'];offset=Vector((0,0,-lowering+.012*math.cos(4*math.pi*phase)))
        hips.location=hips.bone.matrix_local.to_3x3().inverted()@offset
        rotate_world('Spine',.06 if name.endswith('Walk') else .11)
        for side,shift in [('Left',0),('Right',.5)]:
            p=(phase+shift)%1
            if p<.5:travel=stride*(.5-2*p);height=0
            else:
                t=(p-.5)*2;ease=t*t*(3-2*t);travel=stride*(-.5+ease);height=lift*math.sin(math.pi*t)
            if strafe:
                # Feet retain distinct lateral lanes throughout the side-step cycle.
                # Left is +X anatomically in this master; the compatible humanoid import maps it to gameplay left.
                ankle=rig.data.bones[side+'Foot'].head_local+Vector((direction*travel+(bias if side=='Left' else -bias),0,height))
            else:ankle=rig.data.bones[side+'Foot'].head_local+Vector((0,-travel if forward else travel,height))
            goals[side].location=rig.matrix_world@ankle
            rotate_world(side+'Arm',-.65);rotate_world(side+'ForeArm',-.75)
        bpy.context.view_layer.update()
        for side in ('Left','Right'):
            maximum_error=max(maximum_error,(rig.pose.bones[side+'Foot'].head-(rig.matrix_world.inverted()@goals[side].location)).length)
        snapshots.append({bone.name:bone.matrix.copy() for bone in rig.pose.bones})
    assert maximum_error<.008,(name,maximum_error)
    baked.append((name,frames,snapshots))
    records.append({'name':name,'fps':60,'frames':frames,'durationSeconds':frames/60,'loop':True,'strideMetres':stride,'swingAnkleLiftMetres':lift,'hipLoweringMetres':lowering,'maximumAnkleTargetErrorMetres':maximum_error,'lateralBiasMetres':bias,'anatomicalLeftSign':direction,'contact':'Each foot stance: phase 0–0.5, swing: 0.5–1; right offset half a cycle. Controller supplies translation; root motion is absent.'})

for bone,constraint in constraints:bone.constraints.remove(constraint)
for helper in helpers:bpy.data.objects.remove(helper,do_unlink=True)
for name,frames,snapshots in baked:
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    for frame,poses in enumerate(snapshots):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            parent=bone.parent
            bone.matrix_basis=bone.bone.convert_local_to_pose(poses[bone.name],bone.bone.matrix_local,parent_matrix=poses[parent.name] if parent else None,parent_matrix_local=parent.bone.matrix_local if parent else None,invert=True) if parent else bone.bone.convert_local_to_pose(poses[bone.name],bone.bone.matrix_local,invert=True)
            bone.keyframe_insert('location',frame=frame,group=bone.name);bone.keyframe_insert('rotation_quaternion',frame=frame,group=bone.name);bone.keyframe_insert('scale',frame=frame,group=bone.name)
    scene.frame_start=0;scene.frame_end=frames
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(exports/(name+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
rig.animation_data.action=bpy.data.actions['StrafeLeftWalk' if strafe else prefix+'Walk'];scene.frame_start=0;scene.frame_end=36;scene.frame_set(0)
scene['motion_authority']='Original authored in-place '+prefix+'; existing gameplay/controller and shared rest skeleton remain authoritative'
master=folder/('Contender-'+prefix+'-v1.blend');bpy.ops.wm.save_as_mainfile(filepath=str(master))
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash
(folder/('strafe-production.json' if strafe else 'forward-production.json' if forward else 'retreat-production.json')).write_text(json.dumps({'id':'ANIM-'+prefix.upper()+'-v1','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'sourceMaster':str(source.relative_to(root)),'sourceMasterSha256':source_hash,'axes':'Blender metres -> Unity (x,z,-y); anatomical handedness is verified after the existing humanoid/body alignment','rig':'Unchanged shared contender rest skeleton','provenance':'Original authored IK contact paths and baked bone motion, not reversed or retimed clips','clips':records,'integration':'Candidate exports; humanoid import and normal-speed contact review required before installation','approval':'Final integrated Sanders playtest pending'},indent=2)+'\n',encoding='utf-8')
print('AUTHORED_RETREAT_EXPORTED',[(r['name'],r['maximumAnkleTargetErrorMetres']) for r in records])
