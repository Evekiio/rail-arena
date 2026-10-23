"""New 1.0m carbine reference geometry and calibrated views; no live-asset replacement."""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix,Vector

ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
sys.path.insert(0,str(ROOT/'tools'))
from carbine_reference_geometry import Geometry
OUT=ROOT/'ArtSource/Original/PrecisionCarbine-References-v3'
REVIEW=ROOT/'RailArena/Assets/RailArena/Art/CarbineReferenceReview'
for path in [OUT/'Clean',OUT/'Details',REVIEW]:path.mkdir(parents=True,exist_ok=True)
baseline={}
for path in [ROOT/'RailArena/Assets/Resources/Weapons/Railgun.prefab',ROOT/'RailArena/Assets/RailArena/Characters/Hand grip.json']:
    baseline[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
(OUT/'Live-Asset-Baseline.json').write_text(json.dumps(baseline,indent=2)+'\n')
old=ROOT/'ArtSource/Original/ProductionBenchmarks-v1/Production-Benchmarks.blend'
bpy.ops.wm.open_mainfile(filepath=str(old))
for obj in list(bpy.context.scene.objects):
    if obj.type in ('MESH','EMPTY'):bpy.data.objects.remove(obj,do_unlink=True)
g=Geometry(bpy.data.materials['FoundryPalette'],bpy.data.materials['CyanInlay'])
groups={}
def group(identifier):
    for obj in g.parts:
        if 'assemblyId' not in obj:obj['assemblyId']=identifier

# New stock: exact 300mm rear region, with one reinforced opening.
stock=g.raw_profile('C3 reinforced stock',[(-.485,.058),(-.26,.046),(-.2,.025),(-.2,-.02),
    (-.30,-.037),(-.365,-.122),(-.405,-.13),(-.48,-.09),(-.495,-.035)],.068)
g.cut(stock,g.raw_profile('Stock aperture',[(-.445,-.021),(-.365,-.035),(-.405,-.084),(-.448,-.064)],.10))
g.finish(stock,'C3 reinforced stock',0,.0015)
for side in (-1,1):
    g.profile('C3 brace inset',[(-.35,-.042),(-.365,-.049),(-.406,-.113),(-.393,-.116)],.002,2,side*.0345,.0004)
    g.profile('C3 stock seam',[(-.46,.016),(-.28,.009),(-.28,.005),(-.46,.012)],.002,6,side*.035,.0003)
    g.cylinder('C3 sling rim',(-.407,side*.037,-.108),.0075,.003,3,'Y')
    g.cylinder('C3 sling inset',(-.407,side*.039,-.108),.0047,.001,1,'Y')
group('RF-01')
g.profile('C3 shoulder pad',[(-.5,.055),(-.481,.057),(-.475,-.086),(-.462,-.108),(-.48,-.119),(-.495,-.09),(-.5,-.03)],.078,4,bevel=.001)
group('RF-02')
g.profile('C3 cheek rest',[(-.476,.056),(-.267,.046),(-.278,.026),(-.46,.034)],.074,1,bevel=.001)
group('RF-03')
g.profile('C3 receiver core',[(-.20,-.035),(-.20,.046),(-.18,.056),(.085,.056),(.105,.035),(.10,-.039),(-.04,-.049)],.082,1,.0,.0015)
for side in (-1,1):
    g.profile('C3 receiver armor',[(-.184,-.023),(-.179,.048),(.075,.048),(.092,.03),(.085,-.027),(-.035,-.036)],.009,0,side*.043,.001)
    g.profile('C3 teal side panel',[(-.16,.028),(-.035,.028),(-.023,.001),(-.05,-.017),(-.157,-.012)],.002,2,side*.0485,.0005)
    g.box('C3 receiver inlay',(-.09,side*.049,0),(.068,.002,.005),6,.0004)
    for x in (-.17,.074):g.cylinder('C3 receiver screw',(x,side*.048,.035),.0035,.004,3,'Y',8)
    g.box('C3 service detail',(.039,side*.048,-.001),(.035,.004,.018),3,.001)
group('RF-04')
# Faceted enclosed handguard, with real side vents and recessed energy strips.
bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=1,depth=.37,location=(.285,0,.002),rotation=(0,math.pi/2,0))
shroud=bpy.context.object;g.activate(shroud);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
for vertex in shroud.data.vertices:
    vertex.co.y*=.044;vertex.co.z*=.049
bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.030,depth=.41,location=(.285,0,.002),rotation=(0,math.pi/2,0))
g.cut(shroud,bpy.context.object)
for side in (-1,1):
    for row in (-1,1):
        for i in range(8):
            x=.145+i*.040;z=.002+row*.017
            g.cut(shroud,g.raw_profile('Handguard vent',[(x-.012,z-.004),(x+.012,z-.004),(x+.014,z+.004),(x-.010,z+.004)],.030,side*.035))
g.finish(shroud,'C3 enclosed vented fore-end',0,0)
g.cylinder('C3 cosmetic rail bore',(.29,0,.002),.012,.40,1)
for side in (-1,1):g.box('C3 recessed rail liner',(.285,side*.029,.002),(.35,.003,.037),6,.0005)
group('RF-05')
g.ring('C3 muzzle collar',(.484,0,.002),.025,.014,.032,3)
g.ring('C3 muzzle inner lip',(.493,0,.002),.014,.010,.014,1)
g.ring('C3 muzzle inlay',(.491,0,.002),.022,.020,.003,6)
group('RF-06')
for x in (-.068,.038):g.box('C3 optic mount',(x,0,.076),(.027,.048,.032),1,.001)
g.cylinder('C3 compact optic',(-.013,0,.115),.029,.14,1)
for x in (-.091,.065):
    g.ring('C3 optic ceramic rim',(x,0,.115),.035,.026,.016,0)
    g.cylinder('C3 optic glass',(x+(-.001 if x<0 else .001),0,.115),.025,.002,6)
g.cylinder('C3 optic control',(-.01,-.033,.115),.010,.012,3,'Y',8)
group('RF-07')
g.box('C3 continuous top rail',(.12,0,.057),(.68,.033,.008),3,.001)
for i in range(27):g.box('C3 rail tooth',(-.195+i*.025,0,.063),(.011,.040,.008),0,.0006)
group('RF-08')
g.profile('C3 pistol grip',[(-.123,-.035),(-.06,-.038),(-.073,-.13),(-.09,-.18),(-.125,-.178),(-.147,-.15)],.045,4,bevel=.0015)
for side in (-1,1):
    g.profile('C3 grip side',[(-.12,-.053),(-.073,-.054),(-.087,-.14),(-.10,-.165),(-.124,-.153)],.003,0,side*.023,.0007)
group('RF-09')
guard=g.raw_profile('C3 closed guard',[(-.07,-.038),(.01,-.038),(.015,-.085),(-.012,-.104),(-.081,-.090)],.024)
g.cut(guard,g.raw_profile('Guard opening',[(-.060,-.050),(-.002,-.050),(.002,-.078),(-.017,-.091),(-.065,-.081)],.04))
g.finish(guard,'C3 closed trigger guard',0,.001)
g.profile('C3 trigger',[(-.028,-.041),(-.019,-.042),(-.021,-.068),(-.036,-.079),(-.031,-.068)],.009,3,bevel=.0007)
group('RF-10')
g.profile('C3 fixed curved energy cassette',[(.015,-.040),(.068,-.040),(.065,-.085),(.053,-.13),(.037,-.146),(-.004,-.135),(.010,-.087)],.052,1,bevel=.0015)
for side in (-1,1):
    g.profile('C3 cassette armor',[(.021,-.057),(.058,-.057),(.054,-.088),(.042,-.130),(.007,-.122),(.02,-.085)],.003,0,side*.027,.0007)
    g.profile('C3 cassette status',[(.03,-.068),(.036,-.069),(.025,-.116),(.019,-.114)],.001,6,side*.029,.0003)
group('RF-11')
g.profile('C3 integrated angled hand stop',[(.13,-.036),(.225,-.036),(.214,-.055),(.181,-.077),(.159,-.073),(.175,-.052),(.13,-.050)],.046,2,bevel=.001)
g.box('C3 support contact strip',(.16,0,-.048),(.095,.048,.009),4,.001)
group('RF-12')
objects=g.parts
parts=[]
for obj in objects:
    bm=bmesh.new();bm.from_mesh(obj.data);assert all(e.is_manifold for e in bm.edges),obj.name;bm.free()
    obj.data.calc_loop_triangles();assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
    points=[obj.matrix_world@Vector(c) for c in obj.bound_box]
    parts.append(dict(name=obj.name,assembly=obj['assemblyId'],paletteIndex=obj['paletteIndex'],
        minimum=[min(p[i] for p in points) for i in range(3)],maximum=[max(p[i] for p in points) for i in range(3)],triangles=len(obj.data.loop_triangles)))
low=Vector([min(p['minimum'][i] for p in parts) for i in range(3)])
high=Vector([max(p['maximum'][i] for p in parts) for i in range(3)])
assert abs((high-low).x-1)<.001 and (high-low).y<=.101
center=(low+high)/2
anchors={'Muzzle':(.501,0,.002),'RightGrip':(-.103,-.065,-.075),'LeftSupport':(.10,.055,-.070),
         'AimLine':(-.013,0,.115),'StockContact':(-.497,0,-.025)}
for name,location in anchors.items():
    obj=bpy.data.objects.new(name,None);bpy.context.scene.collection.objects.link(obj);obj.location=location
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=1600;scene.render.resolution_y=1600;scene.render.resolution_percentage=100
scene.render.film_transparent=True;scene.render.image_settings.color_mode='RGBA'
light=bpy.data.lights.new('Carbine underfill','AREA');light.energy=100;light.size=2
obj=bpy.data.objects.new('Carbine underfill',light);scene.collection.objects.link(obj);obj.location=(0,-1,-1.5)
obj.rotation_euler=(-obj.location).to_track_quat('-Z','Y').to_euler()
camera=scene.camera;camera.data.type='ORTHO';camera.data.clip_start=.01;camera.data.clip_end=20
master=OUT/'Carbine-Reference-Study.blend';bpy.ops.wm.save_as_mainfile(filepath=str(master))
# One temporary preview export, not a production LOD or replacement resource.
copies=[]
for obj in objects:
    duplicate=obj.copy();duplicate.data=obj.data.copy();scene.collection.objects.link(duplicate);copies.append(duplicate)
bpy.ops.object.select_all(action='DESELECT')
for obj in copies:obj.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object
joined.name='CarbineReferenceMesh';scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
joined.rotation_euler[2]=-math.pi/2;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bpy.ops.export_scene.fbx(filepath=str(REVIEW/'Carbine-Reference.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
bpy.data.objects.remove(joined,do_unlink=True)
palette=bpy.data.images['Foundry palette'];palette.filepath_raw=str(REVIEW/'FoundryPalette.png');palette.save()
views=[];details=[]
def render(name,focus,scale,normal,right=None,up=None,folder='Details',subset=None,resolution=1600):
    for obj in objects:obj.hide_render=subset is not None and obj not in subset
    camera.location=Vector(focus)+Vector(normal).normalized()*4
    camera.rotation_euler=Matrix((right,up,normal)).transposed().to_quaternion().to_euler() if right else (Vector(focus)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=scale;scene.render.resolution_x=scene.render.resolution_y=resolution
    file=f'{folder}/RailRifle-{name}.png';scene.render.filepath=str(OUT/file);bpy.ops.render.render(write_still=True)
    return dict(name=name,file=file,center=list(focus),orthographicFrameMetres=scale,pixelsPerMetre=resolution/scale,
                normal=normal,right=right,up=up,resolution=[resolution,resolution])
for name,r,u,n in [('Left',(1,0,0),(0,0,1),(0,-1,0)),('Right',(-1,0,0),(0,0,1),(0,1,0)),
    ('Top',(1,0,0),(0,1,0),(0,0,1)),('Bottom',(1,0,0),(0,-1,0),(0,0,-1)),
    ('Front',(0,1,0),(0,0,1),(1,0,0)),('Back',(0,-1,0),(0,0,1),(-1,0,0))]:
    view=render(name,center,1.25,n,r,u,'Clean',resolution=2048);view['note']=f'View from {n}; X forward, Y width, Z up';views.append(view)
for name,n,r in [('Front-Detail',(1,0,0),(0,1,0)),('Back-Detail',(-1,0,0),(0,-1,0))]:details.append(render(name,center,.40,n,r,(0,0,1)))
stockparts=[o for o in objects if o['assemblyId'] in ('RF-01','RF-02','RF-03')]
for name,n,r,u in [('Stock-Side',(0,-1,0),(1,0,0),(0,0,1)),('Stock-Top',(0,0,1),(1,0,0),(0,1,0)),('Stock-Rear',(-1,0,0),(0,-1,0),(0,0,1))]:
    details.append(render(name,(-.35,0,-.035),.36,n,r,u,subset=stockparts))
details.append(render('Grip-Contacts',(-.05,0,-.07),.32,(0,-1,0),(1,0,0),(0,0,1)))
details.append(render('Muzzle-Construction',(.48,0,.002),.12,(1,-.7,.4),subset=[o for o in objects if o['assemblyId'] in ('RF-05','RF-06')]))
details.append(render('Hero',center,1.25,(1.5,-2.5,1.1)))
offsets={'RF-01':(-.10,0,0),'RF-02':(-.16,0,0),'RF-03':(-.08,0,.12),'RF-04':(0,-.07,0),
         'RF-05':(.11,0,0),'RF-06':(.2,0,0),'RF-07':(0,0,.18),'RF-08':(0,0,.08),
         'RF-09':(0,0,-.12),'RF-10':(0,-.13,0),'RF-11':(.08,0,-.11),'RF-12':(.11,0,-.07)}
for obj in objects:obj.location+=Vector(offsets[obj['assemblyId']])
details.append(render('Exploded',center,1.7,(1.3,-2.8,1.4)))
for obj in objects:obj.location-=Vector(offsets[obj['assemblyId']])
bpy.context.view_layer.update()
for obj in objects:
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,
        plane_co=obj.matrix_world.inverted()@Vector((0,0,0)),plane_no=obj.matrix_world.to_3x3().transposed()@Vector((0,1,0)),clear_inner=True)
    bm.to_mesh(obj.data);bm.free()
details.append(render('Centre-Section',center,1.25,(0,-1,0),(1,0,0),(0,0,1)))
meta=dict(status='Precision carbine design proposal; approval required before production/replacement',designName='Precision Carbine',
    units='metres',sourceAxes='X forward, Y width, Z up',sourceMasterSha256=hashlib.sha256(master.read_bytes()).hexdigest(),
    builderSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),runtimeChanged=False,
    assets=[dict(asset='RailRifle',center=list(center),minimum=list(low),maximum=list(high),dimensions=list(high-low),
                 orthographicFrameMetres=1.25,pixelsPerMetre=2048/1.25,views=views,parts=parts)],
    details=details,anchors=anchors,explodedOffsets=offsets)
(OUT/'Calibration.json').write_text(json.dumps(meta,indent=2)+'\n')
print('CARBINE_REFERENCE_V3_COMPLETE')
