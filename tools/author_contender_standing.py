"""Author a four-second planted standing-ready loop on the original shared rig."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Quaternion, Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/Standing'
folder.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
bpy.context.preferences.filepaths.file_preview_type = 'NONE'
scene = bpy.context.scene
scene.render.fps = 60
scene.render.fps_base = 1
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
rig.animation_data_clear()
rig.animation_data_create()
rig.data.pose_position = 'POSE'
constraints, helpers, goals = [], [], {}
for side in ('Left', 'Right'):
    foot = rig.data.bones[side + 'Foot']
    leg = rig.pose.bones[side + 'Leg']
    goal = bpy.data.objects.new(side + ' planted ready ankle', None)
    scene.collection.objects.link(goal)
    goal.matrix_world = rig.matrix_world @ foot.matrix_local
    goals[side] = goal
    helpers.append(goal)
    pole = bpy.data.objects.new(side + ' ready knee pole', None)
    scene.collection.objects.link(pole)
    pole.location = rig.matrix_world @ Vector((foot.head_local.x, -1, .55))
    helpers.append(pole)
    ik = leg.constraints.new('IK')
    ik.target, ik.pole_target, ik.chain_count, ik.use_stretch = goal, pole, 2, False
    desired = leg.head.copy()
    best = (float('inf'), 0)
    for step in range(128):
        ik.pole_angle = -math.pi + math.tau * step / 128
        bpy.context.view_layer.update()
        error = (leg.head - desired).length
        if error < best[0]:
            best = error, ik.pole_angle
    ik.pole_angle = best[1]
    constraints.append((leg, ik))
    rotation = rig.pose.bones[side + 'Foot'].constraints.new('COPY_ROTATION')
    rotation.target, rotation.owner_space, rotation.target_space = goal, 'WORLD', 'WORLD'
    constraints.append((rig.pose.bones[side + 'Foot'], rotation))

def rotate(name, angle):
    bone = rig.pose.bones[name]
    axis = bone.bone.matrix_local.to_quaternion().inverted() @ Vector((1, 0, 0))
    bone.rotation_quaternion = Quaternion(axis, angle)

snapshots, ankle_targets = [], []
maximum_error = 0
for frame in range(241):
    scene.frame_set(frame)
    phase = frame / 240
    breath = math.sin(math.tau * phase)
    for bone in rig.pose.bones:
        bone.location, bone.scale = (0, 0, 0), (1, 1, 1)
        bone.rotation_mode = 'QUATERNION'
        bone.rotation_quaternion = Quaternion()
    hips = rig.pose.bones['Hips']
    hips.location = hips.bone.matrix_local.to_3x3().inverted() @ Vector((0, .01, -.035 + .002 * breath))
    rotate('Spine', .035 + .004 * breath)
    for side in ('Left', 'Right'):
        rotate(side + 'Arm', -.65)
        rotate(side + 'ForeArm', -.75)
    bpy.context.view_layer.update()
    targets = {side: list(rig.data.bones[side + 'Foot'].head_local) for side in goals}
    for side in goals:
        maximum_error = max(maximum_error, (rig.pose.bones[side + 'Foot'].head - Vector(targets[side])).length)
    ankle_targets.append(targets)
    snapshots.append({bone.name: bone.matrix.copy() for bone in rig.pose.bones})
assert maximum_error < .001, maximum_error
seam = max(abs(snapshots[0][name][row][column] - snapshots[-1][name][row][column])
           for name in snapshots[0] for row in range(4) for column in range(4))
assert seam < .00001, seam
for bone, constraint in constraints:
    bone.constraints.remove(constraint)
for helper in helpers:
    bpy.data.objects.remove(helper, do_unlink=True)
action = bpy.data.actions.new('StandingReady')
action.use_fake_user = True
rig.animation_data.action = action
for frame, poses in enumerate(snapshots):
    scene.frame_set(frame)
    for bone in rig.pose.bones:
        parent = bone.parent
        arguments = dict(parent_matrix=poses[parent.name], parent_matrix_local=parent.bone.matrix_local) if parent else {}
        bone.matrix_basis = bone.bone.convert_local_to_pose(poses[bone.name], bone.bone.matrix_local, invert=True, **arguments)
        for channel in ('location', 'rotation_quaternion', 'scale'):
            bone.keyframe_insert(channel, frame=frame, group=bone.name)
scene.frame_start, scene.frame_end = 0, 240
scene.frame_set(0)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
export = root / 'RailArena/Assets/RailArena/Art/ArtPass/Animation/StandingReady.fbx'
bpy.ops.export_scene.fbx(filepath=str(export), use_selection=True, object_types={'ARMATURE'},
                        add_leaf_bones=False, use_armature_deform_only=True, bake_anim=True,
                        bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                        bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y')
master = folder / 'Contender-Standing-v1.blend'
scene['motion_authority'] = 'Original in-place standing-ready candidate; simulation, hitboxes, grips and root remain authoritative.'
bpy.ops.wm.save_as_mainfile(filepath=str(master))
assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash
record = {'id': 'ANIM-STANDING-v1', 'master': master.name,
          'masterSha256': hashlib.sha256(master.read_bytes()).hexdigest(),
          'sourceMaster': source.relative_to(root).as_posix(), 'sourceMasterSha256': source_hash,
          'axes': 'Blender metres -> Unity (x,z,-y); shared original skeleton',
          'integration': 'Candidate; native retarget, contact and transition review required.',
          'clips': [{'name': 'StandingReady', 'fps': 60, 'frames': 240, 'durationSeconds': 4,
                     'loop': True, 'maximumAnkleTargetErrorMetres': maximum_error,
                     'loopSeamMatrixDifference': seam, 'ankleTargetsBlenderMetres': ankle_targets,
                     'export': export.relative_to(root).as_posix(),
                     'exportSha256': hashlib.sha256(export.read_bytes()).hexdigest()}]}
(folder / 'standing-production.json').write_bytes((json.dumps(record, indent=2) + '\n').encode('utf-8'))
print('AUTHORED_STANDING_EXPORTED', maximum_error, seam)
