"""Author original in-place crouch actions on the unchanged contender skeleton."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector, Quaternion

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
source=root/'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Crouch';folder.mkdir(parents=True,exist_ok=True)
exports=root/'RailArena/Assets/RailArena/Art/ArtPass/Animation'
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False)
bpy.context.preferences.filepaths.file_preview_type='NONE'
rig=next(obj for obj in bpy.context.scene.objects if obj.type=='ARMATURE')
rig.animation_data_clear();rig.animation_data_create();rig.data.pose_position='POSE'
scene=bpy.context.scene;scene.render.fps=60;scene.render.fps_base=1
goals={};constraints=[];helpers=[]
for side in ('Left','Right'):
    foot=rig.data.bones[side+'Foot'];lower=rig.pose.bones[side+'Leg']
    goal=bpy.data.objects.new(side+' crouch ankle target',None);scene.collection.objects.link(goal)
    goal.matrix_world=rig.matrix_world@foot.matrix_local;goals[side]=goal;helpers.append(goal)
    pole=bpy.data.objects.new(side+' forward crouch knee pole',None);scene.collection.objects.link(pole)
    pole.location=rig.matrix_world@Vector((foot.head_local.x,-1,.45));helpers.append(pole)
    ik=lower.constraints.new('IK');ik.target=goal;ik.pole_target=pole;ik.chain_count=2;ik.use_stretch=False
    desired=rig.data.bones[side+'Leg'].head_local.copy();best=(float('inf'),0)
    for step in range(128):
        ik.pole_angle=-math.pi+math.tau*step/128;bpy.context.view_layer.update()
        distance=(lower.head-desired).length
        if distance<best[0]:best=(distance,ik.pole_angle)
    ik.pole_angle=best[1];constraints.append((lower,ik))
    rotation=rig.pose.bones[side+'Foot'].constraints.new('COPY_ROTATION');rotation.target=goal
    rotation.owner_space='WORLD';rotation.target_space='WORLD';constraints.append((rig.pose.bones[side+'Foot'],rotation))

def rotate(name,angle):
    bone=rig.pose.bones[name]
    axis=bone.bone.matrix_local.to_quaternion().inverted()@Vector((1,0,0))
    bone.rotation_quaternion=Quaternion(axis,angle)

specs=[('CrouchIdle',60,True,(0,0),0,0),
       ('CrouchForward',24,True,(0,-1),.72,0),('CrouchBackward',24,True,(0,1),.72,0),
       ('CrouchLeft',15,True,(1,0),.45,.03),('CrouchRight',15,True,(-1,0),.45,.03),
       ('CrouchEnter',12,False,(0,0),0,0),('CrouchExit',12,False,(0,0),0,0)]
records=[];baked=[]
for name,frames,loop,axis,stride,bias in specs:
    rig.animation_data.action=None;snapshots=[];maximum_error=0;ankles=[]
    for frame in range(frames+1):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            bone.location=(0,0,0);bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.scale=(1,1,1)
        phase=frame/frames;smooth=phase*phase*(3-2*phase)
        stance=smooth if name=='CrouchEnter' else 1-smooth if name=='CrouchExit' else 1
        breath=.003*math.sin(math.tau*phase) if name=='CrouchIdle' else 0
        bob=.008*math.sin(math.tau*phase*2) if stride else breath
        hips=rig.pose.bones['Hips'];hips.location=hips.bone.matrix_local.to_3x3().inverted()@Vector((0,.40*stance,-.6*stance+bob))
        rotate('Spine',.45*stance)
        targets={}
        for side,shift in [('Left',0),('Right',.5)]:
            p=(phase+shift)%1
            if p<.5:travel=stride*(.5-2*p);lift=0
            else:
                t=(p-.5)*2;travel=stride*(-.5+t*t*(3-2*t));lift=.065*math.sin(math.pi*t) if stride else 0
            # The pelvis folds behind the heels; idle/enter/exit retain their planted ankle positions.
            ankle=rig.data.bones[side+'Foot'].head_local+Vector((axis[0]*travel+(bias if side=='Left' else -bias),axis[1]*travel,lift))
            goals[side].location=rig.matrix_world@ankle;targets[side]=list(ankle)
            rotate(side+'Arm',-.65);rotate(side+'ForeArm',-.75)
        bpy.context.view_layer.update()
        for side in ('Left','Right'):
            maximum_error=max(maximum_error,(rig.pose.bones[side+'Foot'].head-Vector(targets[side])).length)
        snapshots.append({bone.name:bone.matrix.copy() for bone in rig.pose.bones});ankles.append(targets)
    assert maximum_error<.008,(name,maximum_error)
    if loop:
        seam=max(abs(snapshots[0][bone][row][column]-snapshots[-1][bone][row][column]) for bone in snapshots[0] for row in range(4) for column in range(4))
        assert seam<.001,(name,seam)
    baked.append((name,frames,snapshots))
    records.append({'name':name,'fps':60,'frames':frames,'durationSeconds':frames/60,'loop':loop,
        'hipLoweringMetres':.6,'hipBackwardOffsetMetres':.40,'spineLeanRadians':.45,'strideMetres':stride,'lateralBiasMetres':bias,'travelAxisBlenderXY':list(axis),
        'nominalStanceFootSpeedMetresPerSecond':stride/(frames/60*.5) if stride else 0,
        'maximumAnkleTargetErrorMetres':maximum_error,'ankleTargetsBlenderMetres':ankles,
        'authority':'In-place reference. Enter/exit are cosmetic timing references; capsule and live stance remain authoritative. No root motion.'})
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
rig.animation_data.action=bpy.data.actions['CrouchIdle'];scene.frame_start=0;scene.frame_end=60;scene.frame_set(0)
scene['motion_authority']='Original in-place crouch candidates. Existing capsule, hitboxes and movement remain authoritative; no automatic runtime installation.'
master=folder/'Contender-Crouch-v1.blend';bpy.ops.wm.save_as_mainfile(filepath=str(master))
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash
(folder/'crouch-production.json').write_text(json.dumps({'id':'ANIM-CROUCH-v1','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),
    'sourceMaster':str(source.relative_to(root)),'sourceMasterSha256':source_hash,'axes':'Blender metres -> Unity (x,z,-y); anatomical handedness verified after humanoid/body alignment',
    'provenance':'Original authored ankle paths, knee solves and baked bone actions on the unchanged shared skeleton; no reversed or retimed licensed clips',
    'defaultMovementReference':'8 m/s ground speed × 0.45 crouch multiplier = 3.6 m/s; this is not certification of world-space foot lock or Physics Lab variants',
    'integration':'Candidate exports only. Validate humanoid import, body/floor/grip contacts and stance transitions before installation; prevent double lowering by the existing procedural CrouchPose.',
    'clips':records,'approval':'Final integrated Sanders playtest pending'},indent=2)+'\n',encoding='utf-8')
print('AUTHORED_CROUCH_EXPORTED',[(record['name'],record['maximumAnkleTargetErrorMetres']) for record in records])
