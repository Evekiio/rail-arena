"""Author and sample an original rigid rifle-ready action in Blender metres."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Matrix, Quaternion, Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend'
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady'
folder.mkdir(parents=True, exist_ok=True)
source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False)
bpy.context.preferences.filepaths.file_preview_type = 'NONE'
scene = bpy.context.scene
scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1
scene.render.fps, scene.render.fps_base = 60, 1
scene.frame_start, scene.frame_end = 0, 18
basis = Matrix(((0, -1, 0), (0, 0, 1), (1, 0, 0)))
inverse = basis.inverted()
roots = [obj for obj in scene.objects if obj.parent is None and obj.type in ('MESH', 'EMPTY')]
carrier = bpy.data.objects.new('FirstPerson_Ready_Weapon_Root', None)
scene.collection.objects.link(carrier)
carrier.rotation_mode = 'QUATERNION'
carrier.empty_display_type = 'ARROWS'
carrier.empty_display_size = .12
for obj in roots:
    matrix = obj.matrix_world.copy()
    obj.parent = carrier
    obj.matrix_world = matrix
beats = [(0, (0, -.16, -.035), (8, -4, -4), 'Low ready'),
         (4, (-.006, -.1, -.03), (5, -2, -2), 'Raise'),
         (8, (.004, -.025, -.008), (1, 1, 1), 'Acquire'),
         (12, (0, .004, .002), (-.6, 0, 0), 'Settle'),
         (18, (0, 0, 0), (0, 0, 0), 'Ready')]
for frame, offset, angles, label in beats:
    scene.frame_set(frame)
    pitch, yaw, roll = (math.radians(value) for value in angles)
    rotation = Quaternion((0, 1, 0), yaw) @ Quaternion((1, 0, 0), pitch) @ Quaternion((0, 0, 1), roll)
    carrier.location = inverse @ Vector(offset)
    carrier.rotation_quaternion = (inverse @ rotation.to_matrix() @ basis).to_quaternion()
    carrier.keyframe_insert('location', frame=frame, group='Metre-scaled rifle ready')
    carrier.keyframe_insert('rotation_quaternion', frame=frame, group='Metre-scaled rifle ready')
    scene.timeline_markers.new(label, frame=frame)
carrier.animation_data.action.name = 'RA_FirstPerson_Ready_18f'
scene['action_authority'] = 'Original 0.3-second cosmetic raise; no firing lock, camera change, actor/root motion or gameplay timing.'
scene['axis_conversion'] = 'Rifle Blender(x,y,z) -> Unity(-y,z,x); quaternion basis conversion, no scale.'
scene.frame_set(18)
master = folder / 'Carbine-FirstPerson-Ready-v1.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(master))
samples = []
for frame in range(19):
    scene.frame_set(frame)
    position = basis @ carrier.location
    rotation = (basis @ carrier.rotation_quaternion.to_matrix() @ inverse).to_quaternion().normalized()
    samples.append({'position': dict(zip('xyz', map(float, position))),
                    'rotation': dict(zip('xyzw', (rotation.x, rotation.y, rotation.z, rotation.w)))})
assert Vector(tuple(samples[-1]['position'].values())).length < .000001
assert abs(samples[-1]['rotation']['w'] - 1) < .000001
record = {'schema': 'rail-arena-first-person-ready/1', 'master': master.name,
          'masterSha256': hashlib.sha256(master.read_bytes()).hexdigest(),
          'sourceMaster': source.relative_to(root).as_posix(), 'sourceSha256': source_hash,
          'fps': 60, 'frames': 18, 'durationSeconds': .3,
          'axes': 'Canonical rifle Blender(x,y,z) -> Unity(-y,z,x)',
          'samples': samples, 'markers': [{'frame': frame, 'label': label} for frame, _, _, label in beats],
          'provenance': 'Original authored rigid rifle-root motion on the existing original master; Unity calibrated hands follow unchanged weapon grips.',
          'scope': 'Weapon-only source poses. Native arm/grip/ADS/camera-clearance and interruption review required before shipping.'}
(folder / 'ReadyAction.json').write_bytes((json.dumps(record, indent=2) + '\n').encode('utf-8'))
assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash
print('FIRST_PERSON_READY_AUTHORED', len(samples), record['masterSha256'])

# Geometry-derived references, unsaved: every view shares the same metre span and centre.
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'TEXTURE'
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
camera = bpy.data.objects.new('Ready technical camera', bpy.data.cameras.new('Ready technical camera'))
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type, camera.data.ortho_scale = 'ORTHO', 1.6
scene.frame_set(18)
meshes = [obj for obj in scene.objects if obj.type == 'MESH' and not obj.hide_render]
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
minimum = Vector(tuple(min(point[i] for point in points) for i in range(3)))
maximum = Vector(tuple(max(point[i] for point in points) for i in range(3)))
target = (minimum + maximum) / 2
views = [('front', (1, 0, 0)), ('rear', (-1, 0, 0)), ('left', (0, 1, 0)),
         ('right', (0, -1, 0)), ('top', (0, 0, 1)), ('bottom', (0, 0, -1))]
for view, direction in views:
    camera.location = target + Vector(direction) * 4
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    for frame in (0, 4, 8, 12, 15, 18):
        scene.frame_set(frame)
        path = folder / 'References' / view / f'{frame:02d}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
(folder / 'measured-views.json').write_bytes((json.dumps({'masterSha256': record['masterSha256'],
    'orthographicSpanMetres': 1.6, 'viewAxes': 'Unity front/rear/left/right/top/bottom, converted into canonical rifle Blender axes',
    'restBoundsBlenderMetres': {'minimum': list(minimum), 'maximum': list(maximum)},
    'sharedViewCentreBlenderMetres': list(target), 'frames': [0, 4, 8, 12, 15, 18],
    'scope': 'Canonical weapon geometry and root motion; weapon-only source images, not Unity gameplay captures.'}, indent=2) + '\n').encode('utf-8'))
assert hashlib.sha256(master.read_bytes()).hexdigest() == record['masterSha256']
print('FIRST_PERSON_READY_REFERENCES_RENDERED', 36)
