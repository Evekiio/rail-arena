"""Bake original feet-first slide poses on the unchanged shared humanoid skeleton."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Quaternion, Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root/'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
folder = root/'ArtSource/Original/ArtPass-v1/Animation/Slide'
folder.mkdir(parents=True, exist_ok=True)
exports = root/'RailArena/Assets/RailArena/Art/ArtPass/Animation'
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
bpy.context.preferences.filepaths.file_preview_type = 'NONE'
rig = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
rig.animation_data_clear()
rig.animation_data_create()
rig.data.pose_position = 'POSE'
scene = bpy.context.scene
scene.render.fps = 60
scene.render.fps_base = 1
goals, constraints, helpers = {}, [], []
for side in ('Left', 'Right'):
    foot = rig.data.bones[side+'Foot']
    lower = rig.pose.bones[side+'Leg']
    goal = bpy.data.objects.new(side+' slide ankle target', None)
    scene.collection.objects.link(goal)
    goal.matrix_world = rig.matrix_world @ foot.matrix_local
    goals[side] = goal
    helpers.append(goal)
    pole = bpy.data.objects.new(side+' slide knee pole', None)
    scene.collection.objects.link(pole)
    pole.location = rig.matrix_world @ Vector((foot.head_local.x, -1, .45))
    helpers.append(pole)
    ik = lower.constraints.new('IK')
    ik.target, ik.pole_target, ik.chain_count, ik.use_stretch = goal, pole, 2, False
    desired = rig.data.bones[side+'Leg'].head_local.copy()
    best = (float('inf'), 0)
    for step in range(128):
        ik.pole_angle = -math.pi + math.tau*step/128
        bpy.context.view_layer.update()
        distance = (lower.head-desired).length
        if distance < best[0]:
            best = distance, ik.pole_angle
    ik.pole_angle = best[1]
    constraints.append((lower, ik))
    rotation = rig.pose.bones[side+'Foot'].constraints.new('COPY_ROTATION')
    rotation.target = goal
    rotation.owner_space = rotation.target_space = 'WORLD'
    constraints.append((rig.pose.bones[side+'Foot'], rotation))

def rotate(name, angle, axis=(1, 0, 0)):
    bone = rig.pose.bones[name]
    local_axis = bone.bone.matrix_local.to_quaternion().inverted() @ Vector(axis)
    bone.rotation_quaternion = Quaternion(local_axis, angle)

specs = [('SlideEnter', 6, False), ('SlideHold', 12, True), ('SlideRecover', 9, False)]
records, baked = [], []
for name, frames, loop in specs:
    rig.animation_data.action = None
    snapshots, targets_by_frame = [], []
    maximum_error = 0
    for frame in range(frames+1):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            bone.location = (0, 0, 0)
            bone.rotation_mode = 'QUATERNION'
            bone.rotation_quaternion = Quaternion()
            bone.scale = (1, 1, 1)
        phase = frame/frames
        smooth = phase*phase*(3-2*phase)
        weight = smooth if name == 'SlideEnter' else 1-smooth if name == 'SlideRecover' else 1
        # Both transition endpoints remain crouched: authority owns the immediate capsule change.
        hips = rig.pose.bones['Hips']
        offset = Vector((.025*weight, .40-.10*weight, -.60-.04*weight))
        hips.location = hips.bone.matrix_local.to_3x3().inverted() @ offset
        rotate('Spine', .45-.53*weight)
        targets = {}
        for side in ('Left', 'Right'):
            # The lead boot extends into travel; the other leg folds underneath the body.
            offset = Vector((.025*weight if side == 'Left' else -.025*weight,
                             (.18 if side == 'Left' else -.40)*weight, 0))
            ankle = rig.data.bones[side+'Foot'].head_local + offset
            goals[side].location = rig.matrix_world @ ankle
            targets[side] = list(ankle)
            rotate(side+'Arm', -.65)
            rotate(side+'ForeArm', -.75)
        bpy.context.view_layer.update()
        for side in ('Left', 'Right'):
            maximum_error = max(maximum_error, (rig.pose.bones[side+'Foot'].head-Vector(targets[side])).length)
        snapshots.append({bone.name: bone.matrix.copy() for bone in rig.pose.bones})
        targets_by_frame.append(targets)
    assert maximum_error < .008, (name, maximum_error)
    seam = max(abs(snapshots[0][bone][row][column]-snapshots[-1][bone][row][column])
               for bone in snapshots[0] for row in range(4) for column in range(4))
    if loop:
        assert seam < .001, (name, seam)
    baked.append((name, frames, snapshots))
    records.append({'name': name, 'fps': 60, 'frames': frames, 'durationSeconds': frames/60, 'loop': loop,
                    'maximumAnkleTargetErrorMetres': maximum_error, 'ankleTargetsBlenderMetres': targets_by_frame,
                    'posePurpose': 'Crouched ready to feet-first slide' if name == 'SlideEnter' else
                                   'Stable feet-first gliding posture; translation follows simulation' if loop else
                                   'Recover into compact crouch; standing remains immediate authority',
                    'authority': 'In-place authored bone animation. No controller motion, physical impulses or hitbox changes.'})
for bone, constraint in constraints:
    bone.constraints.remove(constraint)
for helper in helpers:
    bpy.data.objects.remove(helper, do_unlink=True)
for name, frames, snapshots in baked:
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    for frame, poses in enumerate(snapshots):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            parent = bone.parent
            if parent:
                bone.matrix_basis = bone.bone.convert_local_to_pose(poses[bone.name], bone.bone.matrix_local,
                    parent_matrix=poses[parent.name], parent_matrix_local=parent.bone.matrix_local, invert=True)
            else:
                bone.matrix_basis = bone.bone.convert_local_to_pose(poses[bone.name], bone.bone.matrix_local, invert=True)
            for property_name in ('location', 'rotation_quaternion', 'scale'):
                bone.keyframe_insert(property_name, frame=frame, group=bone.name)
    scene.frame_start, scene.frame_end = 0, frames
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(exports/(name+'.fbx')), use_selection=True, object_types={'ARMATURE'},
        add_leaf_bones=False, use_armature_deform_only=True, bake_anim=True, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y')
rig.animation_data.action = bpy.data.actions['SlideHold']
scene.frame_start, scene.frame_end = 0, 12
scene.frame_set(0)
scene['motion_authority'] = 'Original slide candidates. Movement/steering, stance, hitboxes and duration remain authoritative; no runtime installation.'
master = folder/'Contender-Slide-v1.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(master))
assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash
(folder/'slide-production.json').write_text(json.dumps({'id': 'ANIM-SLIDE-v1', 'master': master.name,
    'masterSha256': hashlib.sha256(master.read_bytes()).hexdigest(), 'sourceMaster': str(source.relative_to(root)),
    'sourceMasterSha256': source_hash, 'axes': 'Blender metres -> Unity (x,z,-y); verify handedness after native alignment',
    'provenance': 'Original feet-first ankle paths, knee solves and baked bone actions on the unchanged shared skeleton',
    'defaultTimingReference': '36 ticks / 0.6 seconds; 6 entry ticks, gliding posture, 9 recovery ticks. Duration is configurable; native integration must account for interruption and shorter slides.',
    'integration': 'Candidate exports only. Validate complete skin, retarget/grips, active phase, steering and interruption before installation.',
    'clips': records, 'approval': 'Final integrated Sanders playtest pending'}, indent=2)+'\n', encoding='utf-8')
print('AUTHORED_SLIDE_EXPORTED', [(item['name'], item['maximumAnkleTargetErrorMetres']) for item in records])
