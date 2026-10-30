"""Author a two-second magazine/left-hand action on the canonical rifle in Blender.

The exported samples are evaluated from the saved action, not runtime procedural timing.
Unity simulation owns reload completion; these tracks only pose presentation transforms.
"""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Matrix, Quaternion, Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend'
output = root / 'ArtSource/Original/ArtPass-v1/Gate2/RifleActions'
output.mkdir(parents=True, exist_ok=True)
before = hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
scene.render.fps = 60
scene.frame_start, scene.frame_end = 0, 120
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
basis = Matrix(((0, -1, 0), (0, 0, 1), (1, 0, 0)))
inverse = basis.inverted()
profile = json.loads((root / 'RailArena/Assets/RailArena/Characters/Hand grip.json').read_text())
grip = Vector(tuple(profile['leftPosition'][axis] for axis in 'xyz'))
q = profile['leftRotation']
grip_rotation = Quaternion((q['w'], q['x'], q['y'], q['z'])).normalized()
magazines = [obj for obj in scene.objects if obj.type == 'MESH' and obj.get('assemblyId') == 'RF-11']
if not magazines:
    raise RuntimeError('Measured RF-11 cassette assembly not found')
points = [obj.matrix_world @ Vector(corner) for obj in magazines for corner in obj.bound_box]
center = Vector(tuple((min(p[i] for p in points) + max(p[i] for p in points)) / 2 for i in range(3)))
center_unity = basis @ center
carrier = bpy.data.objects.new('Reload_Magazine_Carrier', None)
scene.collection.objects.link(carrier)
carrier.location = center
carrier.rotation_mode = 'QUATERNION'
carrier.empty_display_type = 'ARROWS'
carrier.empty_display_size = .12
for obj in magazines:
    matrix = obj.matrix_world.copy()
    obj.parent = carrier
    obj.matrix_world = matrix
hand = bpy.data.objects.new('Reload_Left_Wrist_Target', None)
scene.collection.objects.link(hand)
hand.rotation_mode = 'QUATERNION'
hand.empty_display_type = 'ARROWS'
hand.empty_display_size = .065
# Explicit authored beats at 60 fps. Pull/replace/seat hand contacts follow the carrier.
beats = [
    (0, (0,0,0), 0, 'support'),
    (9, (0,0,0), 0, 'support'),
    (21, (0,0,0), 0, 'contact'),
    (36, (0,-.16,-.02), -10, 'contact'),
    (49, (-.09,-.22,-.05), -16, 'contact'),
    (65, (-.09,-.22,-.05), -16, 'contact'),
    (78, (-.01,-.12,-.005), -5, 'contact'),
    (91, (0,0,0), 0, 'contact'),
    (98, (0,0,0), 0, 'tap'),
    (105, (0,0,0), 0, 'contact'),
    (118, (0,0,0), 0, 'support'),
    (120, (0,0,0), 0, 'support')]
contact = Vector((-.063, -.105, .033))
for frame, offset, tilt, pose in beats:
    rotation_unity = Quaternion((0, 0, 1), math.radians(tilt))
    rotation_blender = (inverse @ rotation_unity.to_matrix() @ basis).to_quaternion()
    carrier.location = center + inverse @ Vector(offset)
    carrier.rotation_quaternion = rotation_blender
    carrier.keyframe_insert('location', frame=frame, group='Magazine extraction')
    carrier.keyframe_insert('rotation_quaternion', frame=frame, group='Magazine extraction')
    if pose == 'support':
        wrist = grip
        wrist_rotation = grip_rotation
    else:
        wrist = center_unity + Vector(offset) + rotation_unity @ (contact - center_unity)
        if pose == 'tap':
            wrist += Vector((0, -.025, 0))
        wrist_rotation = rotation_unity @ grip_rotation
    hand.location = inverse @ wrist
    hand.rotation_quaternion = (inverse @ wrist_rotation.to_matrix() @ basis).to_quaternion()
    hand.keyframe_insert('location', frame=frame, group='Left wrist contact')
    hand.keyframe_insert('rotation_quaternion', frame=frame, group='Left wrist contact')
carrier.animation_data.action.name = 'RA_Carbine_Magazine_Reload_2s'
hand.animation_data.action.name = 'RA_LeftWrist_Reload_Contact_2s'
for frame, label in [(0,'Ready'), (21,'Grip magazine'), (36,'Withdraw'), (49,'Offscreen replacement'), (78,'Insert'), (91,'Seat'), (98,'Tap heel'), (118,'Support grip')]:
    scene.timeline_markers.new(label, frame=frame)
scene['action_authority'] = '120 ticks / 60 Hz / 2 seconds; visual only; preserve rifle magazine simulation'
scene['axis_conversion'] = 'Canonical rifle Blender(x,y,z) -> Unity(-y,z,x)'
scene['source_sha256'] = before
scene.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=str(output / 'Carbine-Reload-v1.blend'))


def position(v):
    return dict(zip('xyz', [round(float(n), 8) for n in v]))


def rotation(q):
    return dict(zip('xyzw', [round(float(n), 8) for n in (q.x,q.y,q.z,q.w)]))


samples = []
for frame in range(121):
    scene.frame_set(frame)
    samples.append({'magazineOffset': position(basis @ carrier.location - center_unity), 'magazineRotation': rotation((basis @ carrier.rotation_quaternion.to_matrix() @ inverse).to_quaternion()), 'leftPosition': position(basis @ hand.location), 'leftRotation': rotation((basis @ hand.rotation_quaternion.to_matrix() @ inverse).to_quaternion())})
record = {'schema':'rail-arena-authored-reload/1', 'sourceMaster': str(source.relative_to(root)), 'sourceSha256':before, 'canonicalMaster':'Carbine-Reload-v1.blend', 'canonicalMasterSha256':hashlib.sha256((output/'Carbine-Reload-v1.blend').read_bytes()).hexdigest(), 'fps':60, 'durationSeconds':2, 'magazineAssembly':'RF_11_', 'magazinePivot':position(center_unity), 'contactPoint':position(contact), 'samples':samples, 'markers':[{'frame':f,'label':m.name} for m in scene.timeline_markers for f in [m.frame]], 'approval':'Integrated playtest pending', 'provenance':'Original Blender-authored action on original Rail Arena rifle; existing grip calibration retained'}
(output / 'ReloadAction.json').write_text(json.dumps(record, indent=2)+'\n')
assert hashlib.sha256(source.read_bytes()).hexdigest() == before
print('RIFLE_RELOAD_AUTHORED', len(samples), 'samples; original source unchanged')
