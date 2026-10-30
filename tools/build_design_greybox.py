"""Build a review-only metre-scale arena master; never modify Unity assets."""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
out = root / 'ArtSource/Original/Design-v1/Greybox'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.engine = 'CYCLES'
scene.cycles.samples = 8
scene.render.threads_mode = 'FIXED'
scene.render.threads = 4
scene.render.resolution_x = 1400
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.world = bpy.data.worlds.new('Neutral review world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.34, .40, .46, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
materials = {}
for name, color in {'Lower':(.22,.28,.31,1), 'Wing':(.55,.62,.62,1),
                    'Upper':(.82,.78,.65,1), 'Ramp':(.38,.53,.57,1),
                    'Cover':(.13,.17,.21,1), 'Pad':(.05,.7,.85,1),
                    'Health':(1,.52,.05,1), 'Spawn':(.75,.27,.34,1),
                    'Portal':(.35,.18,.75,1)}.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .8
    materials[name] = mat
collections = {}
for name in ('GameplayGeometry', 'GameplayMarkers', 'ReviewRig'):
    collection = bpy.data.collections.new(name)
    scene.collection.children.link(collection)
    collections[name] = collection
records = []


def register(obj, name, role, material, marker=False):
    obj.name = name
    target = collections['GameplayMarkers' if marker else 'GameplayGeometry']
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    target.objects.link(obj)
    obj.data.materials.append(materials[material])
    obj['role'] = role
    obj['review_only'] = True
    points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
    lower = [min(point[i] for point in points) for i in range(3)]
    upper = [max(point[i] for point in points) for i in range(3)]
    records.append({'id': name, 'role': role,
                    'center_blender': [(a+b)/2 for a,b in zip(lower,upper)],
                    'bounds_min': lower, 'bounds_max': upper,
                    'dimensions': [b-a for a,b in zip(lower,upper)], 'material': material,
                    'collision': not marker})
    return obj


def box(name, center, size, material, role='walkable', marker=False):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return register(obj, name, role, material, marker)


def floor(name, x, y, width, depth, top, material):
    return box(name, (x,y,top-.3), (width,depth,.6), material)


for s in (-1,1):
    floor(f'Lower_Edge_X{s}', s*26, 0, 4, 40, 0, 'Lower')
    floor(f'Lower_Edge_Y{s}', 0, s*18, 48, 4, 0, 'Lower')
    for t in (-1,1):
        floor(f'Wing_{s}_{t}',s*18,t*14,12,8,2,'Wing')
        # Each ramp assembly has a level 2m turnout joining the wing deck.
        # A continuously sloping slab intersecting a flat wing would create a ledge.
        verts=[]
        faces=[]
        for a,b,za,zb in [(26,19,0,2),(19,17,2,2),(17,10,2,4)]:
            xa,xb=s*a,s*b
            offset=len(verts)
            verts.extend([(xa,t*8-2,za-.4),(xa,t*8+2,za-.4),
                          (xb,t*8+2,zb-.4),(xb,t*8-2,zb-.4),
                          (xa,t*8-2,za),(xa,t*8+2,za),
                          (xb,t*8+2,zb),(xb,t*8-2,zb)])
            faces.extend([tuple(offset+i for i in f) for f in
                          [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]])
        mesh=bpy.data.meshes.new(f'RampMesh_{s}_{t}')
        mesh.from_pydata(verts,[],faces)
        mesh.update()
        obj=bpy.data.objects.new(f'Ramp_{s}_{t}',mesh)
        scene.collection.objects.link(obj)
        register(obj,obj.name,'ramp','Ramp')
        # The upper end touches this landing along x=+/-10.
        floor(f'Upper_Landing_{s}_{t}',s*8,t*8,4,4,4,'Upper')
    floor(f'Upper_Spine_{s}',s*8,0,4,12,4,'Upper')
floor('Central_Bridge',0,0,12,6,4,'Upper')
floor('Lower_Crossing',0,0,8,32,0,'Lower')
for s in (-1,1):
    box(f'Central_SightBlocker_{s}',(s*3.8,0,1.7),(1.6,4,3.4),'Cover','hard_cover')
    for t in (-1,1):
        box(f'Wing_Cover_{s}_{t}',(s*20,t*13,2.6),(3,1.2,1.2),'Cover','hard_cover')
        box(f'Spawn_Blocker_{s}_{t}',(s*23,t*15,.9),(1,3,1.8),'Cover','hard_cover')

pads=[]
for s in (-1,1):
    for t in (-1,1):
        name=f'Pad_{len(pads)+1}'
        start=(s*26,t*12,.03); destination=(s*17,t*16,2)
        box(name,start,(2,2,.06),'Pad','pad_trigger',True)
        box(name+'_Landing',destination,(2.5,2.5,.04),'Pad','pad_destination',True)
        pads.append({'id':name,'start':start,'destination':destination,
                     'trajectory':'Requires actual-controller launch calibration'})
health=[(-18,16,2),(0,0,4),(18,-16,2)]
for i,p in enumerate(health):
    box(f'Health_{i+1}',(p[0],p[1],p[2]+.4),(.7,.7,.8),'Health','health',True)
spawns=[(-26,-17,0),(26,17,0),(-26,17,0),(26,-17,0),
        (-12,-18,0),(12,18,0),(12,-18,0),(-12,18,0)]
for i,p in enumerate(spawns):
    box(f'Spawn_{i+1}',(p[0],p[1],p[2]+.04),(1,1,.08),'Spawn','spawn_candidate',True)
portals=[]
for s in (-1,1):
    p=(0,s*18,1.3)
    box(f'Portal_{s}',p,(2.4,.25,2.6),'Portal','portal_aperture',True)
    # Offset cover breaks the through-gate sightline without blocking both exits.
    box(f'Portal_Cover_{s}',(s*3,s*16.8,1),(1.2,2,2),'Cover','hard_cover')
    portals.append({'id':f'Portal_{s}','center':p,'normal':[0,-s,0],
                    'exit_floor':[0,s*16.7,0], 'clearance_radius':.5,
                    'clearance_height':2,'status':'controller clearance and exposure untested'})

# Lights and cameras belong to a separate non-gameplay collection.
import bmesh
for obj in collections['GameplayGeometry'].objects:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
for location, energy, size in [((0,-10,40),4500,35),((20,25,25),2500,25)]:
    bpy.ops.object.light_add(type='AREA',location=location)
    light=bpy.context.object
    light.data.energy=energy;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(65,-70,65))
camera=bpy.context.object
camera.data.type='ORTHO';camera.data.ortho_scale=75
scene.camera=camera
for obj in list(scene.objects):
    if obj.type in ('CAMERA','LIGHT'):
        for collection in list(obj.users_collection): collection.objects.unlink(obj)
        collections['ReviewRig'].objects.link(obj)

def point_camera(location,target,scale):
    camera.location=location
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=scale

point_camera((65,-70,65),(0,0,1),75)
layout={'version':1,'status':'review-only greybox; not runtime-integrated or playtested',
        'units':'metres','coordinate_system':'Blender Z-up; Unity (x,z,-y)',
        'envelope':[56,40],'deck_heights':[0,2,4], 'objects':records,
        'pads':pads,'health':health,'spawns':spawns,'portals':portals}
(out/'Layout.json').write_text(json.dumps(layout,indent=2)+'\n',encoding='utf-8')
bpy.context.preferences.filepaths.file_preview_type='NONE'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Foundry-Design-v1.blend'))
views=[('Overview',(65,-70,65),(0,0,1),75),('Top',(0,0,80),(0,0,0),64),
       ('North',(0,75,2),(0,0,2),64),('South',(0,-75,2),(0,0,2),64),
       ('East',(75,0,2),(0,0,2),55),('West',(-75,0,2),(0,0,2),55)]
if '--elevations-only' in sys.argv:
    views=[view for view in views if view[0] in ('North','South','East','West')]
for name,location,target,scale in views:
    point_camera(location,target,scale)
    scene.render.filepath=str(out/(name+'.png'))
    bpy.ops.render.render(write_still=True)
if '--elevations-only' in sys.argv:
    print('DESIGN_GREYBOX_COMPLETE '+str(out))
    sys.exit(0)
camera.data.type = 'PERSP'
camera.data.lens = 22
for name,location,target in [('Player-Lower',(0,-17,1.6),(0,0,1.6)),
                             ('Player-Bridge',(0,0,5.6),(10,0,5.6)),
                             ('Player-Wing',(18,14,3.6),(0,0,3.6))]:
    point_camera(location,target,64)
    scene.render.filepath=str(out/(name+'.png'))
    bpy.ops.render.render(write_still=True)
camera.data.type = 'ORTHO'
for name,predicate in [('LowerPlan',lambda obj: obj['role']=='walkable' and obj.location.z<0),
                       ('UpperPlan',lambda obj: obj['role']=='walkable' and obj.location.z>1)]:
    for obj in collections['GameplayGeometry'].objects: obj.hide_render=not predicate(obj)
    collections['GameplayMarkers'].hide_render=True
    point_camera((0,0,80),(0,0,0),64)
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
for obj in collections['GameplayGeometry'].objects:obj.hide_render=False
collections['GameplayMarkers'].hide_render=False
# True section: clip temporary mesh copies at Y=0, render north half, then discard.
import bmesh
originals=list(collections['GameplayGeometry'].objects)
copies=[]
for obj in originals:
    obj.hide_render=True
    duplicate=obj.copy();duplicate.data=obj.data.copy()
    collections['GameplayGeometry'].objects.link(duplicate)
    duplicate.hide_render=False;copies.append(duplicate)
    bm=bmesh.new();bm.from_mesh(duplicate.data)
    local_point=duplicate.matrix_world.inverted() @ Vector((0,0,0))
    local_normal=duplicate.matrix_world.to_3x3().transposed() @ Vector((0,1,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                          plane_co=local_point,plane_no=local_normal,clear_inner=True)
    bm.to_mesh(duplicate.data);bm.free()
collections['GameplayMarkers'].hide_render=True
point_camera((0,-75,2),(0,0,2),64)
scene.render.filepath=str(out/'Section-Y0.png');bpy.ops.render.render(write_still=True)
for obj in copies:bpy.data.objects.remove(obj,do_unlink=True)
for obj in originals:obj.hide_render=False
collections['GameplayMarkers'].hide_render=False
print('DESIGN_GREYBOX_COMPLETE '+str(out))
