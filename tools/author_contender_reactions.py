"""Author original hit and death actions on the compatible contender skeleton."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Matrix, Quaternion, Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root/'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
folder = root/'ArtSource/Original/ArtPass-v1/Animation/Reactions'; folder.mkdir(parents=True, exist_ok=True)
exports = root/'RailArena/Assets/RailArena/Art/ArtPass/Animation'
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
bpy.context.preferences.filepaths.file_preview_type = 'NONE'
scene = bpy.context.scene; scene.render.fps = 60; scene.render.fps_base = 1
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
body = max((obj for obj in scene.objects if obj.type == 'MESH'), key=lambda obj: len(obj.data.vertices))
rig.animation_data_clear(); rig.animation_data_create(); rig.data.pose_position = 'POSE'

def smooth(value):
    value = max(0, min(1, value)); return value*value*(3-2*value)

def rotate(name, angle):
    bone = rig.pose.bones[name]
    axis = bone.bone.matrix_local.to_quaternion().inverted() @ Vector((1, 0, 0))
    bone.rotation_quaternion = Quaternion(axis, angle)

def turn(bone, delta):
    matrix = bone.matrix.copy(); location = matrix.translation.copy()
    bone.matrix = Matrix.LocRotScale(location, delta @ matrix.to_quaternion(), matrix.to_scale())
    bpy.context.view_layer.update()

def solve_chain(side, suffixes, target, pole):
    upper, lower, wrist = (rig.pose.bones[side + suffix] for suffix in suffixes)
    origin = upper.head.copy(); delta = target-origin
    a, b = (lower.head-origin).length, (wrist.head-lower.head).length
    length = max(abs(a-b)+.0001, min(a+b-.0001, delta.length)); direction = delta.normalized()
    bend = pole-origin; bend -= direction*bend.dot(direction); bend.normalize()
    along = (a*a-b*b+length*length)/(2*length)
    elbow = origin+direction*along+bend*math.sqrt(max(0, a*a-along*along))
    turn(upper, (lower.head-origin).rotation_difference(elbow-origin))
    turn(lower, (wrist.head-lower.head).rotation_difference(target-lower.head))
    return (wrist.head-target).length

def relax_palm(side, amount):
    hand=rig.pose.bones[side+'Hand'];lower=rig.pose.bones[side+'ForeArm'];rest=rig.data.bones
    long=(rest[side+'HandMiddle1'].head_local-rest[side+'Hand'].head_local).normalized()
    across=rest[side+'HandIndex1'].head_local-rest[side+'HandPinky1'].head_local;across-=long*across.dot(long);across.normalize()
    normal=long.cross(across).normalized();basis=Matrix((long,across,normal)).transposed()
    direction=hand.head-lower.head;direction.z=0;direction.normalize();up=Vector((0,0,1));sideways=up.cross(direction).normalized()
    desired=Matrix((direction,sideways,up)).transposed()
    rotation=(desired@basis.inverted()).to_quaternion()@hand.bone.matrix_local.to_quaternion()
    matrix=hand.matrix.copy();hand.matrix=Matrix.LocRotScale(matrix.translation,matrix.to_quaternion().slerp(rotation,amount),matrix.to_scale())
    bpy.context.view_layer.update()

def mesh_floor():
    evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    try:
        return min((evaluated.matrix_world @ vertex.co).z for vertex in mesh.vertices)
    finally:
        evaluated.to_mesh_clear()

records = []; baked = []
for name, frames in [('HitResponse', 18), ('DeathFall', 72)]:
    snapshots = []; measures = []; maximum_wrist_error = 0
    rig.animation_data.action = None
    for frame in range(frames+1):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            bone.location = (0, 0, 0); bone.rotation_mode = 'QUATERNION'; bone.rotation_quaternion = Quaternion(); bone.scale = (1, 1, 1)
        phase = frame/frames
        if name == 'HitResponse':
            pulse = smooth(phase/.22) if phase < .22 else 1-smooth((phase-.22)/.78)
            rotate('Spine', -.11*pulse); rotate('Spine01', -.04*pulse); rotate('Head', .035*pulse)
            rotate('LeftArm', -.65); rotate('LeftForeArm', -.75); rotate('RightArm', -.65); rotate('RightForeArm', -.75)
        else:
            collapse = smooth((phase-.1)/.55); fall = smooth((phase-.22)/.48); settle = smooth((phase-.7)/.3)
            rotate('LeftUpLeg', -.48*collapse); rotate('LeftLeg', .95*collapse); rotate('LeftFoot', -.40*collapse)
            rotate('RightUpLeg', -.68*collapse); rotate('RightLeg', 1.15*collapse); rotate('RightFoot', -.40*collapse)
            rotate('Spine', .06*collapse); rotate('Head', -.03*settle)
            hips = rig.pose.bones['Hips']; rest = hips.bone.matrix_local
            rotation = Quaternion(Vector((1, 0, 0)), -math.pi/2*fall + .045*math.sin(math.pi*settle))
            position = rest.translation + Vector((.065*collapse, .10*collapse, -.84*collapse))
            hips.matrix = Matrix.LocRotScale(position, rotation @ rest.to_quaternion(), Vector((1, 1, 1)))
            bpy.context.view_layer.update()
            relaxation=smooth((phase-.5)/.5)
            for side, sign in [('Left', 1), ('Right', -1)]:
                offset = Vector((sign*.30, -.17-(.035 if side == 'Left' else 0), .22+(.06 if side == 'Right' else 0)))
                target = (hips.head + rotation @ offset).lerp(Vector((hips.head.x+sign*.43,hips.head.y+.25,.065)),relaxation)
                pole = (hips.head + rotation @ Vector((sign*.7, -.3, .15))).lerp(Vector((hips.head.x+sign*.8,hips.head.y+.15,.10)),relaxation)
                maximum_wrist_error = max(maximum_wrist_error, solve_chain(side,('Arm','ForeArm','Hand'),target,pole))
                if relaxation>0:relax_palm(side,relaxation)
                if relaxation>0:
                    foot=rig.pose.bones[side+'Foot'];knee=rig.pose.bones[side+'Leg'];rest=rig.data.bones[side+'Foot']
                    goal=Vector((hips.head.x+rest.head_local.x,hips.head.y-(.63 if side=='Left' else .48),rest.head_local.z))
                    target=foot.head.lerp(goal,relaxation);pole=knee.head.lerp(Vector((hips.head.x+sign*.20,hips.head.y-.35,.85)),relaxation)
                    solve_chain(side,('UpLeg','Leg','Foot'),target,pole)
                    matrix=foot.matrix.copy();goal_rotation=Quaternion(Vector((0,0,1)),sign*.12)@rest.matrix_local.to_quaternion()
                    foot.matrix=Matrix.LocRotScale(matrix.translation,matrix.to_quaternion().slerp(goal_rotation,relaxation),matrix.to_scale());bpy.context.view_layer.update()
            bpy.context.view_layer.update()
            # Geometry-derived authored contact: root remains fixed; pelvis pose clears the floor.
            correction = .015-mesh_floor()
            matrix = hips.matrix.copy(); matrix.translation.z += correction; hips.matrix = matrix
        bpy.context.view_layer.update()
        floor = mesh_floor(); measures.append({'frame': frame, 'minimumGeometryZ': floor, 'hipsBlenderXYZ': list(rig.pose.bones['Hips'].head), 'headBlenderXYZ': list(rig.pose.bones['Head'].head)})
        if name == 'DeathFall': assert abs(floor-.015) < .001, (name, frame, floor)
        snapshots.append({bone.name: bone.matrix.copy() for bone in rig.pose.bones})
    if name == 'DeathFall': assert maximum_wrist_error < .005, maximum_wrist_error
    baked.append((name, frames, snapshots))
    records.append({'name': name, 'frames': frames, 'fps': 60, 'durationSeconds': frames/60, 'loop': False, 'rootMotion': False,
        'maximumAuthoredWristTargetErrorMetres': maximum_wrist_error, 'geometryMeasurements': measures,
        'keyPoses': 'brief chest response and recovery; upper-body runtime mask retains locomotion' if name == 'HitResponse' else 'impact, knee give, backward fall, back contact, settle and held terminal pose'})

for name, frames, snapshots in baked:
    action = bpy.data.actions.new(name); action.use_fake_user = True; rig.animation_data.action = action
    for frame, poses in enumerate(snapshots):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            parent = bone.parent
            bone.matrix_basis = bone.bone.convert_local_to_pose(poses[bone.name], bone.bone.matrix_local, parent_matrix=poses[parent.name], parent_matrix_local=parent.bone.matrix_local, invert=True) if parent else bone.bone.convert_local_to_pose(poses[bone.name], bone.bone.matrix_local, invert=True)
            for prop in ('location', 'rotation_quaternion', 'scale'): bone.keyframe_insert(prop, frame=frame, group=bone.name)
    scene.frame_start = 0; scene.frame_end = frames
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(exports/(name+'.fbx')), use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, use_armature_deform_only=True, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y')
rig.animation_data.action = bpy.data.actions['DeathFall']; scene.frame_start = 0; scene.frame_end = 72; scene.frame_set(72)
scene['motion_authority'] = 'Original authored combat reactions; gameplay owns damage, death, gravity and respawn timing. No ragdoll or controller root motion.'
master = folder/'Contender-Reactions-v1.blend'; bpy.ops.wm.save_as_mainfile(filepath=str(master))
assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash
(folder/'reactions-production.json').write_text(json.dumps({'id':'ANIM-REACTIONS-v1','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'sourceMaster':source.relative_to(root).as_posix(),'sourceMasterSha256':source_hash,'units':'metres','axes':'Blender XY ground / Z up; compatible humanoid import and runtime alignment verified separately','rig':'Unchanged shared contender rest skeleton','provenance':'Original bone poses, two-bone arm contacts and geometry-derived floor clearance; no reversed/retimed third-party action','clips':records,'integration':'Candidate; actual retargeted skin, contacts, trigger/death/respawn and native-rate review required','approval':'Sanders integrated playtest pending'},indent=2)+'\n',encoding='utf-8')
print('AUTHORED_REACTIONS_EXPORTED', [(record['name'], record['maximumAuthoredWristTargetErrorMetres']) for record in records])
