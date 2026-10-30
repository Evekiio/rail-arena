"""Build editable Foundry art candidates from the authoritative v2 layout.

Blender --background --threads 4 --python this.py -- <repository>
Does not modify the playable Unity arena. All coordinates are metres, Z up.
"""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
OUT = ROOT / 'ArtSource/Original/Foundry-Models-v1'
REF = ROOT / 'ArtSource/Original/Foundry-Arena-v2'
for folder in ('Renders', 'Exports'):
    (OUT / folder).mkdir(parents=True, exist_ok=True)
layout = json.loads((REF / 'Map.json').read_text())
kit = json.loads((REF / 'Environment-Kit.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.render.engine = 'CYCLES'
scene.cycles.samples = 6
scene.cycles.use_denoising = True
scene.render.threads_mode = 'FIXED'
scene.render.threads = 4
scene.render.resolution_x = 1440
scene.render.resolution_y = 1080
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Bright cloud-world review lighting')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.55, .69, .8, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .65
palette = {
    'Ceramic': ((.78,.75,.65,1), .12, .43),
    'Graphite': ((.035,.05,.06,1), .55, .55),
    'Teal': ((.065,.19,.21,1), .35, .46),
    'Copper': ((.42,.23,.10,1), .7, .36),
    'Cyan': ((.02,.65,.8,1), .2, .3),
    'Amber': ((1,.40,.015,1), .15, .3),
}
mats = {}
for name, (color, metal, rough) in palette.items():
    mat = bpy.data.materials.new('Foundry-' + name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes['Principled BSDF']
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Metallic'].default_value = metal
    bsdf.inputs['Roughness'].default_value = rough
    if name in ('Cyan', 'Amber'):
        bsdf.inputs['Emission Color'].default_value = color
        bsdf.inputs['Emission Strength'].default_value = .65
    mats[name] = mat
library = bpy.data.collections.new('Editable asset library')
scene.collection.children.link(library)
arena = bpy.data.collections.new('Foundry v2 art assembly')
scene.collection.children.link(arena)
collisions = bpy.data.collections.new('Exact gameplay envelopes - not exported')
scene.collection.children.link(collisions)
assets = {}
current = None


def begin(name):
    global current
    current = bpy.data.collections.new(name)
    library.children.link(current)
    assets[name] = current


def finish(obj, name, material, bevel=.025):
    obj.name = name
    for col in list(obj.users_collection):
        col.objects.unlink(obj)
    current.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Authored single-facet edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-7)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-7)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.materials.append(mats[material])
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj['collision'] = 'none - use exact gameplay envelope'
    return obj


def box(name, p, size, mat='Graphite', bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj = bpy.context.object
    obj.dimensions = size
    return finish(obj, name, mat, min(bevel, min(size)/4))


def cylinder(name, p, radius, depth, mat='Graphite', vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=p)
    return finish(bpy.context.object, name, mat, min(.009,depth/4,radius/4))


def prism(name, points, depth, y, mat):
    # Closed polygon extruded along Y. Used for clipped armor, not opaque holes.
    n = len(points)
    verts = [(x,y+s*depth/2,z) for s in (-1,1) for x,z in points]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    return finish(obj, name, mat, .008)


def bar(name, a, b, width, mat='Graphite'):
    a, b = Vector(a), Vector(b)
    obj = box(name, (a+b)/2, (width,width,(b-a).length), mat, .008)
    obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
    return obj


def deck(name, x, y):
    begin(name)
    box('Closed structural tray', (0,0,-.34), (x-.16,y-.16,.52))
    # Four panels, deliberate recessed joints. Flat deck top remains exactly 0.
    for sx in (-1,1):
        for sy in (-1,1):
            box('Ceramic deck quadrant', (sx*x/4,sy*y/4,-.055),
                (x/2-.035,y/2-.035,.11),'Ceramic',.024)
    cylinder('Central service hub',(0,0,-.004),.15,.018,'Graphite',8)
    for sx in (-1,1):
        box('Recessed central energy seam',(sx*x*.16,0,.001),(x*.24,.015,.006),'Cyan',.001)
    for side in (-1,1):
        box('Teal edge recess', (0,side*(y/2-.025),-.31),(x-.16,.04,.17),'Teal')
        for z in (-.12,-.48):
            box('Armored side rail',(0,side*(y/2-.035),z),(x-.08,.07,.09),'Graphite')
        box('Cyan edge insert', (0,side*(y/2-.002),-.3),(x*.3,.012,.035),'Cyan')
        for sx in (-1,1):
            box('Copper corner latch',(sx*(x/2-.12),side*(y/2-.06),-.28),(.09,.12,.32),'Copper')
    bar('Underside cross brace A',(-x/2+.12,-y/2+.12,-.57),(x/2-.12,y/2-.12,-.57),.06)
    bar('Underside cross brace B',(-x/2+.12,y/2-.12,-.57),(x/2-.12,-y/2+.12,-.57),.06)


deck('FLOOR-4',4,4)
deck('FLOOR-4x2',4,2)
deck('LANDING',4,4)
begin('EDGE')
box('Closed edge cap',(0,0,-.075),(4,.15,.15),'Ceramic')
box('Inlaid safety strip',(0,-.076,-.075),(3.6,.008,.035),'Cyan',.001)
begin('BEAM')
for z in (-.07,-.73):
    box('Flange',(0,0,z),(4,.4,.14),'Graphite')
for x in (-1.9,1.9):
    box('End block',(x,0,-.4),(.2,.4,.8),'Teal')
for i in range(6):
    bar('Diagonal web',(-1.8+i*.6,0,-.66),(-1.2+i*.6,0,-.14),.09,'Ceramic')


def cover(name, size):
    begin(name)
    x,y,z = size
    box('Opaque collision-matching body',(0,0,z/2),(x,y,z),'Graphite',.045)
    for side in (-1,1):
        box('Lower inset',(0,side*(y/2+.004),z*.22),(x-.15,.015,z*.29),'Teal',.004)
        points=[(-x*.42,z*.43),(-x*.42,z*.88),(-x*.34,z*.96),
                (x*.34,z*.96),(x*.42,z*.88),(x*.42,z*.43),(0,z*.30)]
        prism('Faceted ceramic face',points,.018,side*(y/2+.008),'Ceramic')
        for sx in (-1,1):
            box('Structural spine',(sx*x*.45,side*(y/2+.019),z*.52),
                (x*.06,.035,z*.85),'Graphite')
            box('Energy slot',(sx*x*.45,side*(y/2+.04),z*.68),(.027,.009,z*.21),'Cyan',.002)
            for level in (.14,.88):
                box('Copper captive fastener',(sx*x*.36,side*(y/2+.021),z*level),(.045,.012,.045),'Copper',.006)
    box('Top ceramic cap',(0,0,z-.017),(x-.12,y-.08,.05),'Ceramic',.02)


for item in kit:
    if item['id'].startswith('COVER-'):
        cover(item['id'],item['dimensions'])
begin('RAMP')
profile=[(0,0),(6.5,2),(9.5,2),(16,4)]
for i,(a,b) in enumerate(zip(profile,profile[1:])):
    prism('Continuous ramp slab '+str(i),[(a[0],a[1]-.4),(b[0],b[1]-.4),(b[0],b[1]),(a[0],a[1])],4,0,'Graphite')
    # Insets follow the same plane, never create raised steps.
    for j in range(4):
        t0=j/4+.005;t1=(j+1)/4-.005
        p0=Vector((a[0],0,a[1]));p1=Vector((b[0],0,b[1]));v=p1-p0
        q0=p0+v*t0;q1=p0+v*t1
        prism('Flush ceramic traction panel',[(q0.x,q0.z-.035),(q1.x,q1.z-.035),
              (q1.x,q1.z+.002),(q0.x,q0.z+.002)],3.72,0,'Ceramic')
    for side in (-1,1):
        bar('Inset cyan ramp edge',(a[0]+.05,side*1.91,a[1]-.02),
            (b[0]-.05,side*1.91,b[1]-.02),.025,'Cyan')
        bar('Side support chord',(a[0],side*1.88,a[1]-.31),
            (b[0],side*1.88,b[1]-.31),.12,'Teal')
begin('PAD')
cylinder('Recessed base',(0,0,-.19),1,.32)
cylinder('Teal carrier',(0,0,-.10),.96,.15,'Teal')
cylinder('Walkable launch plate',(0,0,-.025),.79,.05,'Graphite',32)
for i in range(12):
    t=i*math.tau/12
    obj=box('Segmented ceramic rim',(.88*math.cos(t),.88*math.sin(t),-.045),(.25,.4,.09),'Ceramic')
    obj.rotation_euler.z=t
    cylinder('Captive copper bolt',(.90*math.cos(t),.90*math.sin(t),.002),.018,.008,'Copper',8)
for side in (-1,1):
    for n in range(2):
        y=side*(.3+n*.16)
        bar('Launch chevron',(-.14,y-.06,.004),(0,y+.04,.004),.024,'Cyan')
        bar('Launch chevron',(0,y+.04,.004),(.14,y-.06,.004),.024,'Cyan')
begin('PORTAL')
# Floor-level aperture is exactly 1.4 x 2.2m. Rear is solid and opaque.
box('Solid rear shield',(0,.32,1.3),(2.4,.16,2.8),'Graphite',.08)
for side in (-1,1):
    prism('Ceramic armored jamb',[(side*.72,-.1),(side*1.2,-.1),(side*1.2,2.28),
          (side*.92,2.7),(side*.72,2.7)],.66,-.07,'Ceramic')
    box('Inner teal liner',(side*.74,-.25,1.1),(.08,.16,2.2),'Teal')
    box('Recessed cyan rim',(side*.705,-.34,1.1),(.016,.016,2.2),'Cyan',.002)
    for z in (.35,1.5,2.3):
        box('Copper latch',(side*.94,-.407,z),(.05,.025,.14),'Copper')
box('Armored lintel',(0,-.04,2.442),(1.9,.65,.5),'Ceramic',.06)
box('Opaque field',(0,-.22,1.1),(1.4,.025,2.2),'Cyan',0)
box('Identity plate',(0,-.38,2.48),(.55,.04,.24),'Graphite')
begin('HEALTH')
box('Closed pedestal base',(0,0,.055),(.8,.8,.11),'Graphite')
box('Beveled ivory housing',(0,0,.16),(.72,.72,.18),'Ceramic',.08)
cylinder('Copper emitter',(0,0,.255),.16,.02,'Copper')
for side in (-1,1):
    box('Amber ready lamp',(0,side*.365,.125),(.3,.018,.04),'Amber')
points=[(-.065,.44),(.065,.44),(.065,.625),(.175,.625),(.175,.755),
        (.065,.755),(.065,.94),(-.065,.94),(-.065,.755),(-.175,.755),
        (-.175,.625),(-.065,.625)]
prism('Separate hovering health marker',points,.075,0,'Amber')

for name,size in [('VENT',(1.4,.2,.8)),('CONDUIT',(1.4,.2,.7)),('FLOODLIGHT',(.65,.65,.12)),
                  ('CRATE',(1.2,1,1)),('CABINET',(1,.6,1.8))]:
    begin(name)
    x,y,z=size
    box('Closed rear casing',(0,0,z/2),size,'Graphite')
    if name=='FLOODLIGHT':
        box('Ceramic bezel',(0,0,z-.02),(x-.025,y-.025,.04),'Ceramic')
        box('Recessed light',(0,0,z+.001),(x-.15,y-.15,.008),'Cyan')
    else:
        for sx in (-1,1):
            box('Armored end frame',(sx*(x/2-.07),-y/2-.012,z/2),(.12,.05,z-.06),'Ceramic')
        for zz in (.06,z-.06):
            box('Armored rail',(0,-y/2-.012,zz),(x-.08,.05,.1),'Ceramic')
        if name in ('VENT','CONDUIT'):
            for i in range(4 if name=='VENT' else 2):
                zz=.18+i*(z-.28)/(3 if name=='VENT' else 1)
                box('Inset louver or conduit',(0,-y/2-.005,zz),(x-.27,.03,.055),'Teal')
                box('Recessed cyan seam',(0,-y/2-.025,zz-.025),(x*.55,.008,.009),'Cyan',.001)
        else:
            box('Ceramic door',(0,-y/2-.008,z*.55),(x-.3,.03,z*.66),'Ceramic')
            box('Graphite service handle',(x*.22,-y/2-.035,z*.55),(.045,.04,.2),'Graphite')


def instance(name, asset, p, angle=0):
    obj=bpy.data.objects.new(name,None)
    obj.instance_type='COLLECTION';obj.instance_collection=assets[asset]
    arena.objects.link(obj);obj.location=p;obj.rotation_euler.z=angle
    obj['asset_id']=asset
    return obj


# Tile every source deck, including six-metre strips, without changing its footprint.
for floor in layout['surfaces']:
    x,y,_=floor['size'];cx,cy,_=floor['center']
    nx=math.ceil(x/4);ny=math.ceil(y/4)
    for ix in range(nx):
        for iy in range(ny):
            w=min(4,x-ix*4);h=min(4,y-iy*4)
            name=floor['id']+f'-bay-{ix}-{iy}'
            obj=instance(name,'FLOOR-4',(cx-x/2+ix*4+w/2,cy-y/2+iy*4+h/2,floor['top']))
            obj.scale=(w/4,h/4,1)
            obj['closure_scale']=[w,h]
for item in layout['cover']:
    match=next(k for k in kit if k['id'].startswith('COVER-') and k['dimensions']==item['size'])
    p=item['center'];instance(item['id'],match['id'],(p[0],p[1],p[2]-item['size'][2]/2))
for item in layout['ramps']:
    p=item['profile'][0]
    instance(item['id'],'RAMP',p,0 if item['profile'][-1][0]>p[0] else math.pi)
for item in layout['jumpPads']:
    p=item['start'];instance(item['id'],'PAD',(p[0],p[1],p[2]+.006))
for item in layout['health']:
    instance(item['id'],'HEALTH',item['position'])
for item in layout['portals']:
    p=item['center'];instance(item['id'],'PORTAL',(p[0],p[1],p[2]-1.3),math.pi if p[1]<0 else 0)
for item in kit:
    if item['id'] in ('VENT','CONDUIT','FLOODLIGHT'):
        for i,p in enumerate(item['placements']):
            # Dressing placements are datum anchors; bury housings in the deck edge.
            instance(item['id']+str(i),item['id'],p,math.pi/2 if abs(p[0])>27 else 0)
# Preserve all source collision boxes unmodified, independently selectable.
current=collisions
for item in layout['surfaces']+layout['cover']:
    obj=box('COLLISION-'+item['id'],item['center'],item['size'],bevel=0)
    obj.display_type='WIRE';obj.hide_render=True
collisions.hide_render=True
library.hide_render=True
library.hide_viewport=True

bpy.ops.object.light_add(type='SUN',location=(-30,-40,60))
sun=bpy.context.object;sun.rotation_euler=(.4,-.5,-.6);sun.data.energy=2.2;sun.data.angle=.12
bpy.ops.object.light_add(type='AREA',location=(0,-10,35))
bpy.context.object.data.energy=4500;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=35
bpy.ops.object.camera_add()
camera=bpy.context.object;scene.camera=camera
renders=[]


def render(name,p,target,scale=70,perspective=False):
    camera.location=p
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='PERSP' if perspective else 'ORTHO';camera.data.ortho_scale=scale;camera.data.lens=22
    scene.render.filepath=str(OUT/'Renders'/f'{name}.png')
    bpy.ops.render.render(write_still=True)
    renders.append({'name':name,'camera':list(p),'target':list(target),'scale':scale,'perspective':perspective})


render('Arena-SW',(70,-75,65),(0,0,1),75)
render('Arena-NE',(-70,75,65),(0,0,1),75)
render('Arena-Top',(0,0,80),(0,0,0),65)
for name,p,t in [('Underpass',(0,-9,1.58),(0,5,1.58)),('Crown',(0,-1.5,5.58),(6,0,5.58)),
                 ('West-Wing',(-14,12,3.58),(-19,8,3.58)),('North-Gate',(0,14,1.58),(0,18,1.3))]:
    render(name,p,t,perspective=True)
arena.hide_render=True
library.hide_render=False
library.hide_viewport=False
for col in assets.values():col.hide_render=True
inventory=[]
for name,col in assets.items():
    col.hide_render=False
    bpy.context.view_layer.update()
    pts=[obj.matrix_world@Vector(p) for obj in col.objects for p in obj.bound_box]
    lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
    center=(lo+hi)/2;size=hi-lo;radius=max(size)*1.7
    render(name+'-Studio',center+Vector((radius,-radius,radius*.72)),center,max(size)*1.85)
    if name in ('FLOOR-4','COVER-01','RAMP','PAD','PORTAL','HEALTH'):
        for view,axis in [('Top',(0,0,1)),('Bottom',(0,0,-1)),('Front',(0,-1,0)),
                          ('Back',(0,1,0)),('Left',(-1,0,0)),('Right',(1,0,0))]:
            render(name+'-'+view,center+Vector(axis)*radius,center,max(size)*1.5)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in col.objects:obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/'Exports'/f'{name}.glb'),use_selection=True,
                              export_format='GLB',export_yup=True)
    triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in col.objects)
    bad=[]
    for obj in col.objects:
        bm=bmesh.new();bm.from_mesh(obj.data)
        if any(not all(math.isfinite(c) for c in v.co) for v in bm.verts):bad.append(obj.name+':nonfinite')
        if any(f.calc_area()<1e-10 for f in bm.faces):bad.append(obj.name+':degenerate')
        if any(not e.is_manifold for e in bm.edges):bad.append(obj.name+':nonmanifold')
        bm.free()
    inventory.append({'id':name,'parts':len(col.objects),'triangles':triangles,'boundsMin':list(lo),
                      'boundsMax':list(hi),'dimensions':list(size),'geometryIssues':bad,
                      'approval':'modeled candidate; creator review pending','units':'metres; export Y up',
                      'materials':sorted({m.name for o in col.objects for m in o.data.materials})})
    col.hide_render=True
arena.hide_render=False
library.hide_render=True;library.hide_viewport=True
for col in assets.values():col.hide_render=False
camera.location=(70,-75,65);camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=75
bpy.context.preferences.filepaths.file_preview_type='NONE'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Foundry-Environment.blend'))
report={'status':'modeled review candidates; not installed in gameplay','blenderVersion':bpy.app.version_string,
        'renderSettings':{'engine':'Cycles CPU','threads':4,'samples':6,'resolution':[1440,1080]},
        'sourceSHA256':hashlib.sha256((REF/'Map.json').read_bytes()).hexdigest(),
        'builderSHA256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'assets':inventory,'renders':renders,
        'layoutCounts':{k:len(layout[k]) for k in ('surfaces','cover','ramps','jumpPads','portals','health','spawns')},
        'limitations':['No runtime traversal or balance qualification','No LOD or draw-call qualification',
                      'Cloud sky, distant islands and landmarks remain concept references',
                      'Crate and cabinet are unplaced proposals; no gameplay cover added',
                      'Detailed effect animation and Unity import qualification pending',
                      'Device top has 6mm art offset to prevent coplanar deck overlap; collision authority unchanged']}
(OUT/'Model-Validation.json').write_text(json.dumps(report,indent=2)+'\n')
assert not any(a['geometryIssues'] for a in inventory), 'Geometry audit failed'
print('FOUNDRY MODEL PACKAGE COMPLETE',len(inventory),'assets',len(renders),'renders')
