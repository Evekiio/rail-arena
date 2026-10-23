"""Build Rail Arena's original hard-surface rail accelerator with Blender 5.2.
Coordinates in the helpers are Unity metres: X right, Y up, Z muzzle-forward.
"""
import bpy
import bmesh
import json
import math
import os
import sys
from mathutils import Vector

root = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
source = os.path.join(root, 'ArtSource', 'Original', 'Railgun')
assets = os.path.join(root, 'RailArena', 'Assets', 'RailArena', 'Art', 'Railgun')
os.makedirs(source, exist_ok=True)
os.makedirs(assets, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def point(value):
    # Unity's FBX import reflects X when converting the exported handedness.
    return Vector((-value[0], -value[2], value[1]))


def material(name, color, metal, rough, emission=0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metal
    shader.inputs['Roughness'].default_value = rough
    shader.inputs['Emission Color'].default_value = (*color, 1)
    shader.inputs['Emission Strength'].default_value = emission
    return mat


materials = {
    'Gunmetal': material('Gunmetal', (.025, .038, .052), .72, .38),
    'Titanium': material('Titanium', (.24, .30, .35), .8, .32),
    'Ceramic': material('Ceramic', (.48, .53, .51), .1, .4),
    'Copper': material('Copper', (.56, .19, .055), .8, .27),
    'Rubber': material('Rubber', (.014, .022, .028), .05, .62),
    'Energy': material('Energy', (.015, .65, 1), .25, .22, 3),
}
parts = []


def finish(obj, name, surface, bevel=0):
    obj.name = name
    obj.data.materials.append(materials[surface])
    bpy.context.view_layer.objects.active = obj
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
    mesh.to_mesh(obj.data)
    mesh.free()
    if bevel:
        mod = obj.modifiers.new('Machined edge radii', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = True
    normals = obj.modifiers.new('Weighted hard-surface normals', 'WEIGHTED_NORMAL')
    normals.keep_sharp = True
    bpy.ops.object.modifier_apply(modifier=normals.name)
    parts.append((obj, surface, name.startswith('Actuator')))
    return obj


def box(name, location, size, surface, bevel=.004):
    bpy.ops.mesh.primitive_cube_add(size=1, location=point(location))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, surface, min(bevel, min(size) * .3))


def profile(name, polygon, width, surface, bevel=.006):
    vertices = [point((side * width / 2, y, z)) for side in (-1, 1) for z, y in polygon]
    count = len(polygon)
    faces = [tuple(reversed(range(count))), tuple(range(count, count * 2))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count) for i in range(count)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, surface, bevel)


def tube(name, location, outer, inner, length, surface, sides=32):
    vertices = []
    for z in (-length / 2, length / 2):
        for radius in (outer, inner):
            for i in range(sides):
                angle = 2 * math.pi * i / sides
                vertices.append(point((location[0] + radius * math.cos(angle),
                                       location[1] + radius * math.sin(angle), location[2] + z)))
    faces = []
    for i in range(sides):
        j = (i + 1) % sides
        faces += [(i, j, sides * 2 + j, sides * 2 + i),
                  (sides + j, sides + i, sides * 3 + i, sides * 3 + j),
                  (j, i, sides + i, sides + j),
                  (sides * 2 + i, sides * 2 + j, sides * 3 + j, sides * 3 + i)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, surface, .001)


def ring(name, location, radius, thickness, surface):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        major_radius=radius, minor_radius=thickness, location=point(location), rotation=(math.pi / 2, 0, 0))
    return finish(bpy.context.object, name, surface)


profile('Receiver', [(-.28, -.065), (-.28, .045), (-.20, .105), (.12, .075), (.19, .015), (.13, -.075)], .17, 'Gunmetal')
box('Upper receiver armor', (0, .092, -.045), (.135, .027, .24), 'Ceramic')
profile('Stock upper spar', [(-.425, .072), (-.24, .063), (-.24, .026), (-.425, .038)], .075, 'Titanium')
profile('Stock lower spar', [(-.425, -.040), (-.25, -.034), (-.25, -.058), (-.425, -.070)], .06, 'Gunmetal')
box('Stock heel coupling', (0, .004, -.405), (.09, .13, .025), 'Gunmetal')
box('Shoulder pad', (0, .015, -.437), (.13, .17, .032), 'Rubber', .007)
for i in range(6):
    box('Stock traction rib', (0, -.05 + i * .026, -.456), (.12, .008, .007), 'Gunmetal', .001)
profile('Pistol grip', [(-.16, -.05), (-.055, -.07), (-.10, -.275), (-.185, -.255)], .067, 'Rubber', .009)
for i in range(5):
    box('Grip traction', (0, -.105 - i * .03, -.177 - i * .001), (.07, .008, .013), 'Gunmetal', .002)
box('Trigger guard lower', (0, -.165, -.005), (.026, .018, .13), 'Titanium', .005)
box('Trigger guard front', (0, -.119, .065), (.026, .10, .018), 'Titanium', .005)
box('Trigger', (0, -.105, -.025), (.018, .055, .014), 'Copper', .003)
box('Support palm cushion', (-.112, -.075, .005), (.058, .085, .145), 'Rubber', .01)
box('Support mount', (-.095, -.026, .015), (.045, .018, .16), 'Titanium')

tube('Accelerator bore', (0, .015, .385), .032, .022, .66, 'Gunmetal')
for side in (-1, 1):
    rail = profile('Tapered acceleration rail', [(.02, .025), (.02, .066), (.58, .066),
                    (.70, .042), (.70, .022)], .034, 'Titanium', .003)
    rail.location.x = -side * .067
    box('Rail energy seam', (side * .067, .070, .365), (.014, .005, .60), 'Energy', .001)
    box('Ventral brace', (side * .061, -.058, .35), (.026, .024, .60), 'Gunmetal')
    for z in (.13, .35, .57):
        standoff = profile('Faceted ceramic standoff', [(z-.055, -.053), (z-.055, .012),
                            (z-.029, .033), (z+.046, .015), (z+.055, -.035), (z+.03, -.061)],
                            .028, 'Ceramic', .004)
        standoff.location.x = -side * .078
        box('Retaining plate', (side * .096, -.015, z), (.014, .054, .060), 'Titanium', .003)
        for dy in (-.017, .017):
            box('Captive fastener', (side * .105, -.015 + dy, z), (.006, .010, .014), 'Gunmetal', .002)
for station in range(7):
    for turn in range(3):
        ring('Copper induction winding', (0, .015, .11 + station * .07 + turn * .009), .040, .0033, 'Copper')
for z in (.08, .29, .50, .68):
    tube('Flux isolator collar', (0, .015, z), .053, .033, .018, 'Gunmetal')
    ring('Ion pulse band', (0, .015, z + .011), .049, .0025, 'Energy')
tube('Octagonal muzzle armor', (0, .015, .705), .062, .027, .047, 'Titanium', sides=8)
for side in (-1, 1):
    box('Muzzle retaining lug', (side * .055, .015, .690), (.025, .035, .052), 'Gunmetal', .003)
    for station in range(4):
        z = .18 + station * .12
        box('Rail cooling shoe', (side * .071, -.073, z), (.036, .013, .075), 'Titanium', .002)
        for fin in range(3):
            box('Cooling shoe fin', (side * .071, -.083, z-.024+fin*.024), (.043, .009, .006), 'Gunmetal', .001)
tube('Muzzle ceramic lip', (0, .015, .732), .049, .026, .012, 'Ceramic')
ring('Muzzle pulse aperture', (0, .015, .733), .028, .0025, 'Energy')

for side in (-1, 1):
    box('Receiver side armor', (side * .088, .006, -.10), (.014, .090, .23), 'Ceramic', .006)
    box('Receiver inset', (side * .097, .003, -.105), (.007, .052, .16), 'Gunmetal', .002)
    for i in range(6):
        box('Receiver vent', (side * .102, .004, -.17 + i * .026), (.006, .035, .008), 'Titanium', .001)
    box('Power indicator', (side * .102, .044, -.10), (.006, .008, .10), 'Energy', .001)
box('Top sight riser', (0, .122, -.025), (.045, .030, .13), 'Gunmetal')
for side in (-1, 1):
    box('Open sight hood side', (side*.028, .151, -.025), (.009, .042, .10), 'Titanium', .002)
box('Open sight hood roof', (0, .173, -.025), (.065, .009, .10), 'Titanium', .002)
box('Sight aiming bead', (0, .138, .008), (.004, .006, .008), 'Energy', .001)
for side in (-1, 1):
    for index in range(3):
        box('Charge status lamp', (side*.106, -.022, -.060-index*.022), (.004, .008, .012), 'Energy', .001)
    box('Receiver service latch', (side*.094, -.047, -.205), (.018, .016, .026), 'Copper', .002)
    for index in range(4):
        box('Receiver rear heat sink', (side*.065, .060, -.245+index*.015), (.025, .020, .006), 'Gunmetal', .001)

# Raised identification remains editable text in the master and becomes mesh in the exports.
bpy.ops.object.text_add(location=point((-.108, .027, -.035)), rotation=(math.pi/2, 0, math.pi/2))
label = bpy.context.object
label.data.body = 'RA-01'
label.data.size = .016
label.data.extrude = .00025
label.data.space_character = 1.15
label.name = 'RA-01 receiver identification'
label.data.materials.append(materials['Titanium'])
box('Actuator_Gunmetal', (0, -.055, .015), (.075, .032, .13), 'Gunmetal')

# Save individually named parts, not only the joined export surfaces.
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source, 'Railgun.blend'))
bpy.context.view_layer.objects.active = label
label.select_set(True)
bpy.ops.object.convert(target='MESH')
parts.append((bpy.context.object, 'Titanium', False))

# Collapse static details by surface so the high-detail mesh costs six material draws.
groups = []
for surface in materials:
    members = [obj for obj, mat, actuator in parts if mat == surface and not actuator]
    if not members:
        continue
    bpy.ops.object.select_all(action='DESELECT')
    for obj in members:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = members[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = 'Surface_' + surface
    # A shared origin makes static LOD exports and Unity bounds predictable.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    groups.append(joined)
actuator = next(obj for obj, _, moving in parts if moving)
groups.append(actuator)

report = {'creator': 'Evekiio / Rail Arena', 'license': 'MIT', 'units': 'metres',
          'design': 'Refined RA-01: tapered rails, skeletal stock, faceted insulators, open sight and cooling shoes',
          'master': 'Individually named editable mesh parts and receiver text; no downloaded meshes or textures',
          'muzzleUnity': [0, .015, .742], 'gripGeometryPreserved': True, 'lods': []}
for level, ratio in ((0, 1), (1, .30)):
    copies = []
    bpy.ops.object.select_all(action='DESELECT')
    for original in groups:
        obj = original.copy(); obj.data = original.data.copy()
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        if ratio < 1:
            mod = obj.modifiers.new('Distance topology', 'DECIMATE'); mod.ratio = ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.name = original.name + '_LOD' + str(level)
        copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in copies:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(assets, 'Railgun_LOD' + str(level) + '.fbx'),
        use_selection=True, object_types={'MESH'}, bake_anim=False, axis_forward='-Z', axis_up='Y',
        use_mesh_modifiers=True, apply_unit_scale=True)
    report['lods'].append({'level': level, 'triangles': sum(sum(len(p.vertices) - 2 for p in obj.data.polygons) for obj in copies)})
    for obj in copies:
        bpy.data.objects.remove(obj, do_unlink=True)
with open(os.path.join(source, 'preparation.json'), 'w') as stream:
    json.dump(report, stream, indent=2)

# A separate studio render is review material, not an in-game screenshot.
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.30))
floor = bpy.context.object
floor.data.materials.append(material('Studio floor', (.055, .065, .08), .15, .45))
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.render.threads_mode = 'FIXED'
scene.render.threads = 4
scene.cycles.use_denoising = True
scene.render.resolution_x = 1400; scene.render.resolution_y = 1000; scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Studio world'); scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.08, .10, .14, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .4
for name, location, power, color, size in (
    ('Key', (1.0, -.9, 1.8), 180, (.8, .9, 1), 1.6),
    ('Fill', (-1.1, -.4, .9), 100, (.35, .7, 1), 1.3),
    ('Rim', (0, 1.2, 1.4), 240, (1, .55, .27), 1.1)):
    light = bpy.data.lights.new(name, 'AREA'); light.energy = power; light.color = color; light.shape = 'DISK'; light.size = size
    obj = bpy.data.objects.new(name, light); bpy.context.collection.objects.link(obj); obj.location = location
    obj.rotation_euler = (Vector((0, -.1, 0)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
camera = bpy.data.cameras.new('Studio camera'); obj = bpy.data.objects.new('Studio camera', camera)
bpy.context.collection.objects.link(obj); obj.location = (1.50, -1.85, 1.05)
obj.rotation_euler = (Vector((0, -.12, -.02)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
camera.lens = 55; scene.camera = obj
scene.render.filepath = os.path.join(source, 'Railgun-Studio.png')
bpy.ops.render.render(write_still=True)
for name, location in (('Side', (1.95, -.1, .28)), ('Rear', (1.2, 1.7, .95))):
    obj.location = location
    obj.rotation_euler = (Vector((0, -.12, -.02)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = os.path.join(source, 'Railgun-' + name + '.png')
    bpy.ops.render.render(write_still=True)
print(json.dumps(report))
