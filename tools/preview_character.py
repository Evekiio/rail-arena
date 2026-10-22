import bpy
import json
import math
import sys
from mathutils import Vector

args = sys.argv[sys.argv.index('--') + 1:]
source, output = args[:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
if source.lower().endswith('.fbx'):
    bpy.ops.import_scene.fbx(filepath=source)
else:
    bpy.ops.wm.open_mainfile(filepath=source, load_ui=False)
for obj in list(bpy.data.objects):
    if obj.type in {'CAMERA', 'LIGHT'}:
        bpy.data.objects.remove(obj, do_unlink=True)
    elif obj.name in {'Head2', 'Head3', 'Cannon'}:
        obj.hide_render = True
meshes = [o for o in bpy.data.objects if o.type == 'MESH' and not o.hide_render]
points = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
center = (low + high) / 2
size = high - low
report = {'bounds': list(size), 'objects': []}
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        report['objects'].append({'name': obj.name, 'bones': [b.name for b in obj.data.bones]})
    elif obj.type == 'MESH':
        report['objects'].append({'name': obj.name, 'vertices': len(obj.data.vertices),
                                  'triangles': sum(len(p.vertices) - 2 for p in obj.data.polygons),
                                  'materials': [m.name for m in obj.data.materials if m]})
with open(output + '.json', 'w') as handle:
    json.dump(report, handle, indent=2)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.resolution_x = 900
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Neutral studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.045, .055, .075, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45
distance = max(size) * 2.3
up_axis = 2 if size.z > size.y else 1
if up_axis == 2:
    view = Vector((.5, -.85, .2)).normalized()
else:
    view = Vector((.5, .15, 1)).normalized()
bpy.ops.object.camera_add(location=center + view * distance)
camera = bpy.context.object
camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = max(size) * 1.3
scene.camera = camera
for name, offset, energy, color in [
    ('Key', (2, -3, 4), 700, (1, .9, .78)),
    ('Fill', (-3, -1, 2), 400, (.55, .7, 1)),
    ('Rim', (1, 3, 3), 900, (.6, .8, 1))
]:
    bpy.ops.object.light_add(type='AREA', location=center + Vector(offset))
    light = bpy.context.object
    light.name = name
    light.data.energy = energy
    light.data.shape = 'DISK'
    light.data.size = 3
    light.data.color = color
    light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
scene.render.filepath = output + '.png'
bpy.ops.render.render(write_still=True)
