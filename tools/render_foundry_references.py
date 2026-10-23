"""Headless measured Foundry v2 reference master. No Unity/runtime changes."""
import hashlib
import json
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
pack=root/'ArtSource/Original/Foundry-Arena-v2'
source=pack/'Map.json'
data=json.loads(source.read_text())
out=pack/'References-v1'
renders=out/'Measured'
renders.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.engine='CYCLES'
scene.cycles.samples=4
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=1280;scene.render.resolution_y=960
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Neutral review sky');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.4,.46,.5,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.7
materials={}
for name,c in {'lower':(.25,.34,.38,1),'wing':(.53,.67,.65,1),
               'upper':(.85,.8,.68,1),'ramp':(.36,.54,.59,1),
               'cover':(.12,.18,.23,1),'pad':(.02,.65,.85,1),
               'health':(1,.48,.02,1),'spawn':(.83,.35,.4,1),
               'portal':(.3,.15,.6,1),'drop':(.35,.7,.3,1)}.items():
    m=bpy.data.materials.new(name);m.diffuse_color=c;m.use_nodes=True
    shader=m.node_tree.nodes['Principled BSDF']
    shader.inputs['Base Color'].default_value=c
    shader.inputs['Roughness'].default_value=.8
    materials[name]=m
collections={}
for name in ('Structure','Cover','Markers','ReviewRig'):
    c=bpy.data.collections.new(name);scene.collection.children.link(c);collections[name]=c
records=[]

def finish(obj,name,collection,material):
    obj.name=name
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collections[collection].objects.link(obj)
    obj.data.materials.append(materials[material])
    obj['source']='Foundry-Arena-v2/Map.json';obj['review_only']=True
    points=[obj.matrix_world @ v.co for v in obj.data.vertices]
    records.append(dict(id=name,collection=collection,
                        min=[min(p[i] for p in points) for i in range(3)],
                        max=[max(p[i] for p in points) for i in range(3)]))
    return obj

def box(name,center,size,collection,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    obj=bpy.context.object;obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(obj,name,collection,mat)

for floor in data['surfaces']:
    obj=box(floor['id'],floor['center'],floor['size'],'Structure',floor['kind'])
    obj['deck_height']=floor['top']
for obj in data['cover']:box(obj['id'],obj['center'],obj['size'],'Cover','cover')
# Overlapping authoring screens are one solid collision volume. Exact union
# removes coplanar render seams while preserving the original editable boxes.
cover_sources=bpy.data.collections.new('CoverSourceBoxes')
scene.collection.children.link(cover_sources)
source_boxes=list(collections['Cover'].objects)
for obj in source_boxes:
    collections['Cover'].objects.unlink(obj);cover_sources.objects.link(obj)
union=source_boxes[0].copy();union.data=source_boxes[0].data.copy()
union.name='Cover-Exact-Union';collections['Cover'].objects.link(union)
for obj in source_boxes[1:]:
    bpy.context.view_layer.objects.active=union
    modifier=union.modifiers.new('Exact source-box union','BOOLEAN')
    modifier.operation='UNION';modifier.solver='EXACT';modifier.object=obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
cover_sources.hide_render=True;cover_sources.hide_viewport=True
for ramp in data['ramps']:
    vertices=[];faces=[]
    for a,b in zip(ramp['profile'],ramp['profile'][1:]):
        o=len(vertices);half=ramp['width']/2;t=ramp['thickness']
        vertices.extend([(a[0],a[1]-half,a[2]-t),(a[0],a[1]+half,a[2]-t),
                         (b[0],b[1]+half,b[2]-t),(b[0],b[1]-half,b[2]-t),
                         (a[0],a[1]-half,a[2]),(a[0],a[1]+half,a[2]),
                         (b[0],b[1]+half,b[2]),(b[0],b[1]-half,b[2])])
        faces.extend(tuple(o+i for i in face) for face in
                     ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)))
    mesh=bpy.data.meshes.new(ramp['id']);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(ramp['id'],mesh);scene.collection.objects.link(obj)
    finish(obj,ramp['id'],'Structure','ramp')
for obj in list(collections['Structure'].objects):
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
for item in data['spawns']:
    p=item['position'];box(item['id'],(p[0],p[1],p[2]+.03),(.8,.8,.06),'Markers','spawn')
for item in data['health']:
    p=item['position'];box(item['id'],(p[0],p[1],p[2]+.4),(.5,.5,.8),'Markers','health')
for item in data['jumpPads']:
    p=item['start'];box(item['id'],(p[0],p[1],p[2]+.025),(2,2,.05),'Markers','pad')
    p=item['destination'];box(item['id']+'-Landing',(p[0],p[1],p[2]+.02),(3,3,.04),'Markers','pad')
for item in data['portals']:
    box(item['id'],item['center'],item['housing'],'Markers','portal')
for item in data['safeDrops']:
    p=item['edge'];box(item['id'],(p[0],p[1],p[2]+.02),(3,.15,.04),'Markers','drop')

for location,energy,size in [((0,-15,40),5000,35),((20,20,25),3000,25)]:
    bpy.ops.object.light_add(type='AREA',location=location)
    obj=bpy.context.object;obj.data.energy=energy;obj.data.size=size
    obj.rotation_euler=(-obj.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(70,-75,65))
camera=bpy.context.object;scene.camera=camera
for obj in list(scene.objects):
    if obj.type in ('LIGHT','CAMERA'):
        for c in list(obj.users_collection):c.objects.unlink(obj)
        collections['ReviewRig'].objects.link(obj)

def camera_at(position,target,scale=75,perspective=False):
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='PERSP' if perspective else 'ORTHO'
    camera.data.ortho_scale=scale;camera.data.lens=22

camera_at((70,-75,65),(0,0,1))
bpy.context.preferences.filepaths.file_preview_type='NONE'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Foundry-v2-Reference-Master.blend'))
render_records=[]
def render(name,position,target,scale=75,perspective=False):
    camera_at(position,target,scale,perspective)
    scene.render.filepath=str(renders/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    render_records.append(dict(file=name+'.png',position=position,target=target,
                               projection='perspective' if perspective else 'orthographic',scale=scale))

for args in [('Overview-SW',(70,-75,65),(0,0,1),75),
             ('Overview-NE',(-70,75,65),(0,0,1),75),
             ('Top',(0,0,80),(0,0,0),65),('Bottom',(0,0,-80),(0,0,0),65),
             ('North',(0,75,3),(0,0,3),65),('South',(0,-75,3),(0,0,3),65),
             ('East',(75,0,3),(0,0,3),55),('West',(-75,0,3),(0,0,3),55)]:render(*args)
for level,name in [(0,'Lower-Plan'),(2,'Wing-Plan'),(4,'Upper-Plan')]:
    collections['Markers'].hide_render=True;collections['Cover'].hide_render=True
    for obj in collections['Structure'].objects:obj.hide_render=obj.get('deck_height',-1)!=level
    render(name,(0,0,80),(0,0,0),65)
for c in ('Markers','Cover'):collections[c].hide_render=False
for obj in collections['Structure'].objects:obj.hide_render=False
for name,position,target in [('Crown',(0,-1.5,5.58),(6,0,5.58)),
                             ('Underpass',(0,-9,1.58),(0,5,1.58)),
                             ('West-Wing',(-14,12,3.58),(-19,8,3.58)),
                             ('East-Wing',(14,-12,3.58),(19,-8,3.58)),
                             ('Outer-Loop',(-25.5,-13,1.58),(-25.5,0,1.58)),
                             ('North-Gate',(0,14,1.58),(0,18,1.3)),
                             ('South-Gate',(0,-14,1.58),(0,-18,1.3)),
                             ('Spawn-S1',(-25.5,-17.5,1.58),(-15,-12,1.58)),
                             ('Spawn-S5',(-9,-17,1.58),(0,-12,1.58))]:
    render('Player-'+name,position,target,perspective=True)

for name,axis,value,position,target in [('Section-NS-X0',0,0,(-75,0,3),(0,0,3)),
                                       ('Section-EW-N8',1,8,(0,-75,3),(0,8,3))]:
    originals=list(collections['Structure'].objects)+list(collections['Cover'].objects)
    copies=[];normal=Vector((1,0,0) if axis==0 else (0,1,0));point=Vector((value,0,0) if axis==0 else (0,value,0))
    for obj in originals:
        obj.hide_render=True;dup=obj.copy();dup.data=obj.data.copy()
        collections['Structure'].objects.link(dup);dup.hide_render=False;copies.append(dup)
        bm=bmesh.new();bm.from_mesh(dup.data)
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                              plane_co=dup.matrix_world.inverted() @ point,
                              plane_no=dup.matrix_world.to_3x3().transposed() @ normal,clear_inner=True)
        bm.to_mesh(dup.data);bm.free()
    collections['Markers'].hide_render=True
    render(name,position,target,65)
    for obj in copies:bpy.data.objects.remove(obj,do_unlink=True)
    for obj in originals:obj.hide_render=False
    collections['Markers'].hide_render=False

(out/'Measured-Views.json').write_text(json.dumps(dict(
    mapSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    status='same-master measured review renders; no runtime validation',
    coverRendering='Exact Boolean union; original thirty source boxes retained in hidden CoverSourceBoxes collection',
    geometry=records,views=render_records),indent=2)+'\n',encoding='utf-8')
print('FOUNDRY_REFERENCES_COMPLETE '+str(out))
