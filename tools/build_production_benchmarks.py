"""Author independent rifle/low-cover candidates. Run with background Blender 5.2.

Source coordinates: X muzzle-forward, Y weapon width, Z up, in metres.
FBX root rotation is explicitly authored for Unity +Z forward / +Y up.
"""
import bpy
import bmesh
import hashlib
import json
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
BUILDER_SHA = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
OUT = ROOT / 'ArtSource/Original/ProductionBenchmarks-v1'
ASSETS = ROOT / 'RailArena/Assets/RailArena/Art/ProductionBenchmarks'
OUT.mkdir(parents=True, exist_ok=True)
ASSETS.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
PALETTE = [(.83,.81,.71), (.025,.04,.05), (.065,.17,.19),
           (.28,.32,.32), (.018,.024,.026), (.50,.34,.16), (.04,.65,.70), (.26,.34,.37)]

# A deliberately shared palette UV is compact, editable and unambiguous.
atlas = bpy.data.images.new('Foundry palette', width=128, height=16, alpha=True)
atlas.pixels = [v for y in range(16) for x in range(128) for v in (*PALETTE[x//16],1)]
atlas.filepath_raw = str(ASSETS / 'FoundryPalette.png')
atlas.file_format = 'PNG'
atlas.save()
atlas.pack()
surface = bpy.data.materials.new('FoundryPalette'); surface.use_nodes=True
bsdf=surface.node_tree.nodes.get('Principled BSDF')
bsdf.inputs['Metallic'].default_value=.22
bsdf.inputs['Roughness'].default_value=.43
tex=surface.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=atlas; tex.interpolation='Closest'
surface.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
energy=bpy.data.materials.new('CyanInlay'); energy.use_nodes=True
eb=energy.node_tree.nodes.get('Principled BSDF')
eb.inputs['Base Color'].default_value=(.015,.52,.58,1)
eb.inputs['Emission Color'].default_value=(.015,.65,.72,1)
eb.inputs['Emission Strength'].default_value=.65
eb.inputs['Roughness'].default_value=.3
parts=[]

def activate(obj):
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
    bpy.context.view_layer.objects.active=obj

def finish(obj,name,color=0,bevel=.002):
    obj.name=name
    activate(obj)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=obj.modifiers.new('Single facet edge','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    mesh=bmesh.new();mesh.from_mesh(obj.data)
    bmesh.ops.remove_doubles(mesh,verts=list(mesh.verts),dist=1e-7)
    bmesh.ops.triangulate(mesh,faces=list(mesh.faces))
    bmesh.ops.dissolve_degenerate(mesh,edges=list(mesh.edges),dist=1e-7)
    bmesh.ops.recalc_face_normals(mesh,faces=list(mesh.faces));mesh.to_mesh(obj.data);mesh.free()
    obj.data.materials.clear();obj.data.materials.append(energy if color==6 else surface)
    for old_uv in list(obj.data.uv_layers): obj.data.uv_layers.remove(old_uv)
    uv=obj.data.uv_layers.new(name='PaletteUV')
    for poly in obj.data.polygons:
        for idx in poly.loop_indices: uv.data[idx].uv=((color+.5)/8,.5)
    obj['paletteIndex']=color
    parts.append(obj)
    return obj

def box(name,loc,size,color=0,bevel=.002):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    obj=bpy.context.object;obj.dimensions=size
    return finish(obj,name,color,bevel)

def raw_profile(name,points,width,loc=(0,0,0)):
    vs=[(x+loc[0],y*width/2+loc[1],z+loc[2]) for y in (-1,1) for x,z in points]
    n=len(points);fs=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    return obj

def profile(name,points,width,color=0,bevel=.002,loc=(0,0,0)):
    return finish(raw_profile(name,points,width,loc),name,color,bevel)

def cut(obj,cutter):
    activate(obj);mod=obj.modifiers.new('Machined through opening','BOOLEAN')
    mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def cylinder(name,loc,radius,depth,color=0,axis='X',sides=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=radius,depth=depth,location=loc)
    obj=bpy.context.object
    if axis=='X': obj.rotation_euler[1]=math.pi/2
    elif axis=='Y': obj.rotation_euler[0]=math.pi/2
    return finish(obj,name,color,.001)

def ring(name,loc,outer,inner,depth,color=0,axis='X',sides=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=outer,depth=depth,location=loc)
    obj=bpy.context.object
    if axis=='X': obj.rotation_euler[1]=math.pi/2
    elif axis=='Y': obj.rotation_euler[0]=math.pi/2
    activate(obj);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=inner,depth=depth+.02,location=loc)
    cutter=bpy.context.object
    if axis=='X':cutter.rotation_euler[1]=math.pi/2
    elif axis=='Y':cutter.rotation_euler[0]=math.pi/2
    cut(obj,cutter)
    return finish(obj,name,color,.001)

def empty(name,loc):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=loc
    return o

# Rifle: image-guided silhouette, an enclosed rail assembly and open skeletal stock.
stock=raw_profile('Skeletal stock', [(-.56,.015),(-.55,.145),(-.27,.15),(-.16,.11),(-.105,.075),
    (-.17,.015),(-.25,-.11),(-.34,-.11),(-.43,-.035)], .065)
for name,poly in [('Upper', [(-.50,.085),(-.29,.105),(-.20,.07),(-.29,.015),(-.39,.005)]),
                  ('Lower',[(-.385,-.008),(-.285,-.014),(-.255,-.042),(-.30,-.085),(-.335,-.078)])]:
    cut(stock,raw_profile(name,poly,.09))
finish(stock,'Skeletal ceramic stock',0,.004)
box('Rubber shoulder pad',(-.567,0,.03),(.025,.078,.225),4,.006)
for side in (-1,1):
    ring('Stock circular energy recess',(-.51,side*.035,.028),.033,.025,.006,2,'Y')
    cylinder('Stock energy insert',(-.51,side*.039,.028),.024,.003,6,'Y',16)
    profile('Stock light path',[(-.49,.111),(-.28,.127),(-.21,.095),(-.28,.119),(-.49,.104)],.002,6,.0005,(0,side*.034,0))
receiver=profile('Receiver core',[(-.19,.025),(-.18,.14),(.13,.14),(.18,.10),(.16,.015),(-.08,.005)],.082,1,.003)
for side in (-1,1):
    profile('Receiver ivory side shell',[(-.18,.045),(-.16,.137),(.135,.137),(.157,.105),(.13,.042),(-.04,.025)],.009,0,.002,(0,side*.045,0))
    profile('Teal receiver inset',[(-.14,.066),(-.13,.117),(.055,.117),(.068,.07),(.03,.054),(-.10,.054)],.002,2,.001,(0,side*.051,0))
    box('Receiver energy window',(-.04,side*.053,.081),(.12,.002,.009),6,.001)
    box('Receiver serial recess',(.095,side*.051,.086),(.045,.002,.038),3,.001)
    for x in (-.145,.14):cylinder('Receiver fastener',(x,side*.052,.123),.004,.004,3,'Y',8)
profile('Pistol grip',[(-.12,.013),(-.045,.01),(-.075,-.165),(-.126,-.19),(-.155,-.165)],.052,0,.004)
for side in (-1,1):
    profile('Grip textured insert',[(-.12,-.04),(-.082,-.035),(-.10,-.148),(-.128,-.155),(-.139,-.139)],.002,4,.001,(0,side*.027,0))
guard=raw_profile('Trigger guard', [(-.075,.021),(.018,.021),(.018,-.052),(-.015,-.08),(-.085,-.072)], .027)
cut(guard,raw_profile('Guard opening',[(-.062,.006),(.005,.006),(.004,-.045),(-.02,-.062),(-.068,-.056)],.05))
finish(guard,'Closed trigger guard',0,.002)
profile('Trigger',[(-.023,.015),(-.012,.014),(-.016,-.027),(-.036,-.04),(-.032,-.031)],.01,3,.001)
box('Capacitor collar',(.072,0,-.002),(.08,.069,.04),3,.003)
profile('Capacitor cassette',[(.042,-.012),(.109,-.012),(.118,-.115),(.051,-.115)],.057,0,.003)
for side in (-1,1):
    for x in (.06,.078,.096):box('Cassette fluting',(x,side*.029,-.072),(.004,.002,.065),3,.0005)
# Hollow shroud with real openings. Keep all vertices within the silhouette envelope.
shroud=raw_profile('Enclosed shroud',[(.12,.037),(.13,.145),(.56,.145),(.58,.124),(.58,.048),(.55,.033)],.084)
cut(shroud,raw_profile('Barrel cavity',[(.105,.062),(.105,.119),(.60,.119),(.60,.062)],.047))
for side in (-1,1):
    for row in range(2):
        for j in range(7):
            x=.225+j*.043;z=.071+row*.035
            cut(shroud,raw_profile('Vent cutter',[(x-.014,z-.004),(x+.012,z-.004),(x+.015,z+.004),(x-.011,z+.004)],.035,(0,side*.035,0)))
finish(shroud,'Ivory vented barrel enclosure',0,.0015)
cylinder('Graphite bore',(.36,0,.09),.018,.44,1,'X',12)
for side in (-1,1):
    box('Rail energy liner',(.365,side*.026,.087),(.39,.003,.038),6,.001)
    box('Underbarrel graphite seam',(.36,side*.043,.043),(.36,.002,.009),2,.001)
ring('Muzzle ceramic lip',(.58,0,.09),.03,.016,.015,0)
ring('Muzzle dark bore',(.588,0,.09),.016,.010,.009,1)
ring('Muzzle energy collar',(.586,0,.09),.024,.021,.003,6)
box('Lower accessory base',(.33,0,.022),(.27,.055,.025),3,.002)
box('Foregrip ceramic pad',(.28,0,-.001),(.22,.065,.021),0,.003)
for i in range(9):box('Foregrip ribs',(.19+i*.022,0,-.014),(.008,.058,.008),2,.001)
box('Continuous top rail',(.15,0,.154),(.70,.045,.013),3,.001)
for i in range(28):box('Rail teeth',(-.18+i*.024,0,.165),(.012,.05,.012),0,.001)
for x in (-.075,.11):
    box('Optic riser',(x,0,.183),(.033,.045,.035),1,.002)
    box('Optic clamp',(x,0,.174),(.046,.068,.013),3,.002)
cylinder('Scope central body',(.015,0,.225),.027,.25,0)
cylinder('Scope objective',(.159,0,.225),.041,.082,0)
ring('Scope front rim',(.202,0,.225),.041,.032,.012,0)
cylinder('Objective cyan glass',(.208,0,.225),.031,.003,6)
cylinder('Scope rear collar',(-.13,0,.225),.034,.052,0)
ring('Eyepiece rim',(-.158,0,.225),.034,.025,.012,0)
cylinder('Eyepiece dark lens',(-.165,0,.225),.024,.002,2)
cylinder('Elevation dial',(.018,0,.258),.018,.018,3,'Z')
cylinder('Windage dial',(.018,.034,.225),.015,.015,3,'Y')
anchors=[empty('Muzzle',(.598,0,.09)),empty('RightGrip',(-.105,0,-.075)),
         empty('LeftSupport',(.27,0,-.025)),empty('AimLine',(-.16,0,.225)),
         empty('StockContact',(-.58,0,.03))]
rifle=list(parts);parts.clear()

# One low wing cover: exact 2 x 1 x 1.2 bounding envelope, grounded pivot.
cover=profile('Graphite structural core',[(-.97,0),(-.97,1.03),(-.88,1.17),(.88,1.17),(.97,1.03),(.97,0)],.96,1,.008)
for side in (-1,1):
    profile('Ivory upper armor',[(-.86,.43),(-.84,1.10),(-.72,1.17),(.77,1.17),(.88,1.05),(.80,.50),(.24,.38)],.028,0,.008,(0,side*.477,0))
    profile('Teal lower armor',[(-.83,.11),(-.82,.43),(.19,.36),(.72,.49),(.82,.27),(.74,.10)],.025,2,.006,(0,side*.477,0))
    box('Recessed lower bumper',(0,side*.481,.08),(1.61,.025,.10),3,.008)
    box('Cyan lower status strip',(.34,side*.497,.17),(.63,.006,.018),6,.003)
    for x in (-.84,.84):
        profile('End corner brace',[(x-.07,.06),(x-.07,.88),(x-.03,1.12),(x+.05,1.12),(x+.07,.9),(x+.07,.06)],.032,3,.006,(0,side*.48,0))
        cylinder('Brace bolt',(x,side*.498,.97),.018,.004,1,'Y',8)
        box('Amber service indicator',(x,side*.499,.39),(.016,.002,.065),5,.002)
for x in (-.985,.985):
    box('End cap',(math.copysign(.98,x),0,.55),(.028,.88,1.04),3,.008)
    box('End panel',(x,0,.61),(.028,.59,.74),2,.006)
    for y in (-.26,.26):box('End panel seam',(x,y,.61),(.03,.012,.59),1,.001)
box('Top protective rail',(0,0,1.185),(1.65,.88,.03),0,.007)
box('Underside plinth',(0,0,.025),(1.88,.9,.05),1,.006)
module=list(parts);parts.clear()

def geometry_report(objects):
    triangles=0;nonmanifold=0;degenerate=0;corners=[]
    for o in objects:
        o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
        degenerate+=sum(t.area<1e-12 for t in o.data.loop_triangles)
        bm=bmesh.new();bm.from_mesh(o.data)
        nonmanifold+=sum(not e.is_manifold for e in bm.edges);bm.free()
        assert o.data.uv_layers and all(math.isfinite(c) for v in o.data.vertices for c in v.co)
        corners.extend(o.matrix_world@Vector(c) for c in o.bound_box)
    assert not degenerate and not nonmanifold,(degenerate,nonmanifold)
    low=[min(v[i] for v in corners) for i in range(3)];high=[max(v[i] for v in corners) for i in range(3)]
    return dict(triangles=triangles,parts=len(objects),degenerateTriangles=degenerate,nonManifoldEdges=nonmanifold,
                boundsMin=low,boundsMax=high,dimensions=[b-a for a,b in zip(low,high)])

def collection(name,objects):
    c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c)
    for o in objects:
        for old in list(o.users_collection):old.objects.unlink(o)
        c.objects.link(o)
    return c

rc=collection('Rifle editable parts',rifle+anchors)
cc=collection('Low wing cover editable parts',module)
reports={}
for name,objects,extra in [('RailRifle',rifle,anchors),('LowWingCover',module,[])]:
    reports[name]=geometry_report(objects)
    if name=='LowWingCover':
        assert all(abs(a-b)<.0001 for a,b in zip(reports[name]['dimensions'],[2,1,1.2]))
    assert reports[name]['triangles']<18000
    for level in (0,1):
        bpy.ops.object.select_all(action='DESELECT');copies=[]
        for obj in objects:
            dup=obj.copy();dup.data=obj.data.copy();bpy.context.collection.objects.link(dup);copies.append(dup)
            if level==1:
                activate(dup);mod=dup.modifiers.new('LOD silhouette retention','DECIMATE');mod.ratio=.55
                bpy.ops.object.modifier_apply(modifier=mod.name)
        # Join into one mesh with two material slots. Source parts stay editable.
        bpy.ops.object.select_all(action='DESELECT')
        for obj in copies:obj.select_set(True)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object
        joined.name=name+'_LOD'+str(level);bpy.context.scene.cursor.location=(0,0,0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        # Bake axis mapping: Blender (forward,right,up) -> FBX Unity (+Z,+X,+Y).
        # Unity maps Blender -Y to +Z; cover retains its long local X axis.
        joined.rotation_euler[2]=-math.pi/2 if name=='RailRifle' else 0
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        exportObjects=[joined]
        for a in extra:
            dup=a.copy();bpy.context.collection.objects.link(dup)
            dup.location=(a.location.y,-a.location.x,a.location.z);exportObjects.append(dup)
        bpy.ops.object.select_all(action='DESELECT')
        for o in exportObjects:o.select_set(True)
        bpy.context.view_layer.objects.active=joined
        bpy.ops.export_scene.fbx(filepath=str(ASSETS/(name+'_LOD'+str(level)+'.fbx')),use_selection=True,
            object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
            bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
        reports[name]['lod'+str(level)]=geometry_report([joined])
        for o in exportObjects:bpy.data.objects.remove(o,do_unlink=True)

# Render stage is independent of exported sources; no studio geometry enters FBX.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Neutral review world');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.20,.24,.27,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.view_settings.view_transform='AgX'
for name,loc,power,size in [('Key',(-1.0,-2,3),260,2.5),('Fill',(1.4,1.5,1.6),150,2),('Rim',(-1,1,2.5),220,1.5)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc
    o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',camera)
scene.collection.objects.link(cam);scene.camera=cam;camera.type='ORTHO'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Production-Benchmarks.blend'))
views=[]
for name,objects,other,target,scale in [('RailRifle',rifle,module,(0,0,.045),1.42),
                                      ('LowWingCover',module,rifle,(0,0,.6),2.8)]:
    for o in other:o.hide_render=True
    for o in objects:o.hide_render=False
    for view,delta in [('Studio',(1.6,-2.4,1.3)),('Left',(0,-3,0)),('Right',(0,3,0)),
                        ('Top',(0,0,3)),('Bottom',(0,0,-3)),('Front',(3,0,0)),('Rear',(-3,0,0))]:
        cam.location=Vector(target)+Vector(delta);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
        camera.ortho_scale=scale;path=OUT/(name+'-'+view+'.png')
        scene.render.filepath=str(path)
        if '--refresh-exports-cover' not in sys.argv or name=='LowWingCover':
            bpy.ops.render.render(write_still=True)
        views.append(path.name)
    for o in other:o.hide_render=False
reports['referenceImageSha256']=hashlib.sha256((ROOT/'ArtSource/Original/Design-v1/Sources/User-Rifle-Reference.png').read_bytes()).hexdigest()
reports['builderSha256']=BUILDER_SHA
reports['status']='Benchmark candidates; grip fit, motion and creator visual approval pending'
reports['paletteUV']='Overlapping swatch coordinates by design, not lightmap UVs; no baked lighting required'
reports['views']=views
reports['anchorsSourceCoordinates']={a.name:list(a.location) for a in anchors}
(OUT/'geometry.json').write_text(json.dumps(reports,indent=2)+'\n')
print('PRODUCTION_BENCHMARKS_COMPLETE')
