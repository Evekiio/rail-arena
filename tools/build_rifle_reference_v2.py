"""Build an isolated revised-stock reference study and render its construction views.

This does not export a game model or modify the benchmark/Unity assets.
"""
import bmesh
import bpy
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
BASE=ROOT/'ArtSource/Original/ProductionBenchmarks-v1'
OUT=ROOT/'ArtSource/Original/RailRifle-Modeling-References-v2'
(OUT/'Clean').mkdir(parents=True,exist_ok=True)
(OUT/'Details').mkdir(exist_ok=True)
source=BASE/'Production-Benchmarks.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
scene=bpy.context.scene
rifle=bpy.data.collections['Rifle editable parts']
for obj in list(scene.objects):
    if obj.type=='MESH' and (obj not in list(rifle.objects) or obj.name.startswith(('Stock ','Skeletal ','Rubber shoulder'))):
        bpy.data.objects.remove(obj,do_unlink=True)
surface=bpy.data.materials['FoundryPalette']
energy=bpy.data.materials['CyanInlay']

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
    bpy.context.view_layer.objects.active=obj

def finish(obj,name,color,bevel=.001):
    obj.name=name;activate(obj)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    if bevel:
        mod=obj.modifiers.new('Reference edge facets','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    obj.data.materials.clear();obj.data.materials.append(energy if color==6 else surface)
    for uv in list(obj.data.uv_layers):obj.data.uv_layers.remove(uv)
    uv=obj.data.uv_layers.new(name='PaletteUV')
    for loop in uv.data:loop.uv=((color+.5)/8,.5)
    obj['paletteIndex']=color
    for collection in list(obj.users_collection):collection.objects.unlink(obj)
    rifle.objects.link(obj)
    return obj

# Adapt the native stock profile to the unchanged receiver interface and rear extent.
def px(x):return -.5795+(x-65)/695*.4095
def pz(y):return .15-(y-100)/377*.275

def raw_profile(name,points,width,side=0):
    vertices=[(px(x),side+s*width/2,pz(z)) for s in (-1,1) for x,z in points]
    n=len(points);faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj);return obj

def profile(name,points,width,color,side=0,bevel=.001):
    return finish(raw_profile(name,points,width,side),name,color,bevel)

outer=[(94,102),(655,148),(690,170),(744,172),(760,190),(760,227),(699,241),
       (568,253),(442,278),(321,459),(271,477),(136,427),(98,380),(68,151)]
opening=[(194,257),(350,270),(289,349),(260,391),(179,351)]
stock=raw_profile('S2 Shell',outer,.072)
cutter=raw_profile('Single aperture',opening,.11)
activate(stock);mod=stock.modifiers.new('One stock aperture','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
finish(stock,'S2 Shell',0,.0015)
pad=profile('S2 Shoulder pad',[(87,100),(117,103),(147,388),(164,427),(131,447),(96,411),(65,151)],.080,4,bevel=.0012)
profile('S2 Cheek rest',[(133,112),(622,151),(605,195),(176,165),(151,147)],.076,1,bevel=.0012)
for side in (-1,1):
    profile('S2 Neck recess',[(636,182),(741,188),(741,218),(688,229),(566,241),(451,268),(415,275),(439,244),(548,221),(626,211)],.002,2,side*.0368,.0004)
    profile('S2 Brace reinforcement',[(377,267),(420,261),(310,426),(282,449),(245,434),(269,420),(291,395)],.002,1,side*.0368,.0004)
    profile('S2 Lower facet',[(171,371),(251,419),(233,434),(154,403)],.002,2,side*.0368,.0004)
    profile('S2 Cyan seam',[(175,183),(591,211),(591,218),(175,190)],.002,6,side*.037,.0003)
    # A shallow decorative socket, not a second structural stock opening.
    loc=(px(280),side*.039,pz(437))
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.010,depth=.003,location=loc,rotation=(math.pi/2,0,0))
    finish(bpy.context.object,'S2 Sling socket rim',3,.0005)
    loc=(px(280),side*.041,pz(437))
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.0065,depth=.001,location=loc,rotation=(math.pi/2,0,0))
    finish(bpy.context.object,'S2 Sling socket recess',1,.0002)
contact=bpy.data.objects.get('StockContact')
if contact:contact.location=(px(79.8),0,pz(275))

def group_id(name):
    if name.startswith('S2 Shoulder'):return 'RF-02'
    if name.startswith('S2 Cheek'):return 'RF-03'
    if name.startswith('S2 '):return 'RF-01'
    if name.startswith(('Muzzle ',)):return 'RF-06'
    if name.startswith(('Scope ','Objective ','Eyepiece ','Windage ','Elevation ','Optic ')):return 'RF-07'
    if name.startswith(('Continuous ','Rail teeth')):return 'RF-08'
    if name.startswith(('Pistol ','Grip ')):return 'RF-09'
    if name.startswith(('Trigger','Closed trigger')):return 'RF-10'
    if name.startswith(('Capacitor ','Cassette ')):return 'RF-11'
    if name.startswith(('Lower accessory','Foregrip ')):return 'RF-12'
    if name.startswith(('Ivory vented','Graphite bore','Rail energy','Underbarrel ')):return 'RF-05'
    assert 'receiver' in name.lower(),name
    return 'RF-04'

objects=[o for o in rifle.objects if o.type=='MESH']
parts=[]
for obj in objects:
    bm=bmesh.new();bm.from_mesh(obj.data)
    assert all(e.is_manifold for e in bm.edges),obj.name;bm.free()
    obj.data.calc_loop_triangles();assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
    points=[obj.matrix_world@Vector(c) for c in obj.bound_box]
    lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
    gid=group_id(obj.name);obj['assemblyId']=gid
    parts.append(dict(name=obj.name,assembly=gid,minimum=lo,maximum=hi,
                      paletteIndex=obj.get('paletteIndex'),triangles=len(obj.data.loop_triangles)))
low=Vector([min(p['minimum'][i] for p in parts) for i in range(3)])
high=Vector([max(p['maximum'][i] for p in parts) for i in range(3)])
center=(low+high)/2
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=2048;scene.render.resolution_y=2048;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.color_mode='RGBA'
# Underfill improves readability of the underside and internal cosmetic surfaces.
light=bpy.data.lights.new('Reference underfill','AREA');light.energy=110;light.size=2
obj=bpy.data.objects.new('Reference underfill',light);scene.collection.objects.link(obj);obj.location=(0,-1,-1.8)
obj.rotation_euler=(Vector((0,0,0))-obj.location).to_track_quat('-Z','Y').to_euler()
camera=scene.camera;camera.data.type='ORTHO';camera.data.clip_start=.01;camera.data.clip_end=20
master=OUT/'Rifle-Reference-Study.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(master))
views=[]
definitions=[('Left',(1,0,0),(0,0,1),(0,-1,0)),('Right',(-1,0,0),(0,0,1),(0,1,0)),
             ('Top',(1,0,0),(0,1,0),(0,0,1)),('Bottom',(1,0,0),(0,-1,0),(0,0,-1)),
             ('Front',(0,1,0),(0,0,1),(1,0,0)),('Back',(0,-1,0),(0,0,1),(-1,0,0))]

def render(name,focus,scale,normal,right=None,up=None,folder='Details',subset=None):
    for o in objects:o.hide_render=subset is not None and o not in subset
    camera.location=Vector(focus)+Vector(normal).normalized()*5
    camera.rotation_euler=(Matrix((right,up,normal)).transposed().to_quaternion().to_euler()
                           if right else (Vector(focus)-camera.location).to_track_quat('-Z','Y').to_euler())
    camera.data.ortho_scale=scale
    file=f'{folder}/RailRifle-{name}.png';scene.render.filepath=str(OUT/file)
    bpy.ops.render.render(write_still=True)
    return dict(name=name,file=file,center=list(focus),orthographicFrameMetres=scale,
                pixelsPerMetre=2048/scale,normal=list(normal),right=right,up=up,resolution=[2048,2048])

for name,right,up,normal in definitions:
    v=render(name,center,1.45,normal,right,up,'Clean')
    v['note']=f'View from {normal}; source X forward, Y width, Z up';views.append(v)
details=[]
for name,normal,right in [('Front-Detail',(1,0,0),(0,1,0)),('Back-Detail',(-1,0,0),(0,-1,0))]:
    details.append(render(name,center,.55,normal,right,(0,0,1)))
stock_objects=[o for o in objects if o.name.startswith('S2 ')]
stock_center=(px(410),0,pz(280))
for name,normal,right,up in [('Stock-Side',(0,-1,0),(1,0,0),(0,0,1)),
    ('Stock-Top',(0,0,1),(1,0,0),(0,1,0)),('Stock-Rear',(-1,0,0),(0,-1,0),(0,0,1))]:
    details.append(render(name,stock_center,.52,normal,right,up,subset=stock_objects))
details.append(render('Grip-Contacts',(-.055,0,-.065),.40,(0,-1,0),(1,0,0),(0,0,1)))
details.append(render('Muzzle-Construction',(.575,0,.09),.14,(1,-.8,.5),subset=[o for o in objects if o['assemblyId'] in ('RF-05','RF-06')]))
details.append(render('Hero',center,1.45,(1.5,-2.5,1.2)))
# Exploded art assembly: temporary offsets only; source study stays assembled.
offsets={'RF-01':(-.12,0,0),'RF-02':(-.19,0,0),'RF-03':(-.10,0,.14),
         'RF-04':(0,-.08,0),'RF-05':(.13,0,0),'RF-06':(.25,0,0),
         'RF-07':(0,0,.22),'RF-08':(0,0,.09),'RF-09':(0,0,-.15),
         'RF-10':(0,-.16,0),'RF-11':(.09,0,-.13),'RF-12':(.13,0,-.08)}
for o in objects:o.location+=Vector(offsets[o['assemblyId']])
details.append(render('Exploded',center,1.95,(1.3,-2.8,1.4)))
for o in objects:o.location-=Vector(offsets[o['assemblyId']])
for o in objects:
    activate(o);bm=bmesh.new();bm.from_mesh(o.data)
    # Geometry is temporarily cut at Y=0; the assembled master was saved before this operation.
    inv=o.matrix_world.inverted();plane=inv@Vector((0,0,0))
    normal=o.matrix_world.to_3x3().transposed()@Vector((0,1,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,
                          plane_co=plane,plane_no=normal,clear_inner=True)
    bm.to_mesh(o.data);bm.free()
details.append(render('Centre-Section',(center.x,0,center.z),1.45,(0,-1,0),(1,0,0),(0,0,1)))
anchors={o.name:list(o.location) for o in rifle.objects if o.type=='EMPTY'}
asset=dict(asset='RailRifle',center=list(center),minimum=list(low),maximum=list(high),dimensions=list(high-low),
           orthographicFrameMetres=1.45,pixelsPerMetre=2048/1.45,views=views,parts=parts)
metadata=dict(status='PROPOSED revised-stock modeling reference; creator approval pending',
    units='metres',sourceAxes='X muzzle-forward, Y width, Z up',
    sourceBenchmarkSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    sourceMasterSha256=hashlib.sha256(master.read_bytes()).hexdigest(),
    builderSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    assets=[asset],details=details,anchors=anchors,explodedOffsets=offsets,
    section='Y=0, negative Y half removed; uncapped visual cutaway; not mechanical internals',
    runtimeChanged=False)
(OUT/'Calibration.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('RIFLE_REFERENCE_V2_COMPLETE')
