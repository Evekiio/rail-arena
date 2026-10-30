"""Original low-poly contender geometry on the existing CC0 animation skeleton.

Run in Blender 5.2 background mode. All dimensions are metres, Blender Z up,
with faces looking toward -Y. The old soldier's mesh is never reused.
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Vector

parser = argparse.ArgumentParser()
parser.add_argument('root', type=Path)
parser.add_argument('--only', choices=['Player', 'Bot01', 'Bot02', 'Bot03'])
parser.add_argument('--render', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = args.root.resolve()
source = root / 'ArtSource/Original/StylizedContenders'
assets = root / 'RailArena/Assets/RailArena/Art/StylizedContenders'
review = root / 'Validation/StylizedContenders'
for directory in (source, assets, review):
    directory.mkdir(parents=True, exist_ok=True)

DESIGNS = {
    'Player': dict(armor=(.70, .66, .53), cloth=(.025, .037, .047), scarf=(.035, .11, .13),
                   skin=(.48, .26, .135), hair=(.025, .018, .014), accent=(.025, .65, .8), metal=(.27, .30, .29),
                   face=1.0, shoulder=1.0, style='quiff'),
    'Bot01': dict(armor=(.43, .095, .042), cloth=(.031, .027, .029), scarf=(.052, .045, .039),
                  skin=(.48, .235, .105), hair=(.018, .014, .015), accent=(1, .39, .018), metal=(.30, .24, .17),
                  face=.92, shoulder=.92, style='bun'),
    'Bot02': dict(armor=(.14, .27, .30), cloth=(.025, .039, .047), scarf=(.035, .09, .11),
                  skin=(.65, .43, .27), hair=(.71, .61, .43), accent=(.32, .66, .69), metal=(.43, .46, .43),
                  face=1.02, shoulder=1.04, style='swept'),
    'Bot03': dict(armor=(.19, .075, .14), cloth=(.029, .022, .034), scarf=(.052, .025, .045),
                  skin=(.21, .092, .049), hair=(.018, .012, .019), accent=(.62, .27, .11), metal=(.36, .22, .15),
                  face=.98, shoulder=.98, style='locs'),
}
ORDER = ['armor', 'cloth', 'scarf', 'skin', 'hair', 'accent', 'metal', 'rubber',
         'eye', 'iris', 'lip', 'hairLight', 'armorShade', 'skinShade', 'stitch', 'white']
parts = []
rig = None
materials = {}


def material(name, color, metallic=0, roughness=.68):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Metallic'].default_value = metallic
    shader.inputs['Roughness'].default_value = roughness
    if name.endswith('_accent'):
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = .35
    return mat


def finish(obj, name, surface, bone, bevel=0):
    obj.name = name
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    if bevel:
        modifier = obj.modifiers.new('Small modeled edge bevel', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.materials.clear()
    obj.data.materials.append(materials[surface])
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    if bone:
        obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
        modifier = obj.modifiers.new('Humanoid deformation', 'ARMATURE')
        modifier.object = rig
        obj.parent = rig
    obj['surface'] = surface
    parts.append(obj)
    return obj


def mesh(name, vertices, faces, surface, bone, bevel=0):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, surface, bone, bevel)


def box(name, location, size, surface, bone, bevel=.004, direction=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if direction is not None:
        obj.rotation_euler = Vector(direction).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, surface, bone, bevel)


def ico(name, location, scale, surface, bone, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=location)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, surface, bone)


def loft(name, rings, surface, bone, count=12):
    # Ring entries: height, half-width, anterior depth, posterior depth, Y centre.
    vertices = []
    for z, width, front, back, cy in rings:
        for i in range(count):
            angle = 2*math.pi*i/count
            cosine = math.cos(angle)
            vertices.append((width*math.sin(angle), cy-cosine*(front if cosine >= 0 else back), z))
    faces = [tuple(reversed(range(count)))]
    for row in range(len(rings)-1):
        for i in range(count):
            j = (i+1) % count
            faces.append((row*count+i, row*count+j, (row+1)*count+j, (row+1)*count+i))
    faces.append(tuple((len(rings)-1)*count+i for i in range(count)))
    return mesh(name, vertices, faces, surface, bone)


def plate(name, outline, depth, surface, bone, bevel=.003):
    # Outline consists of XYZ points on the front surface; extrusion goes toward +Y.
    vertices = list(outline) + [(x, y+depth, z) for x, y, z in outline]
    n = len(outline)
    faces = [tuple(range(n)), tuple(reversed(range(n, 2*n)))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, vertices, faces, surface, bone, bevel)


def head(bone):
    return rig.matrix_world @ rig.data.bones[bone].head_local


def tail(bone):
    return rig.matrix_world @ rig.data.bones[bone].tail_local


def sleeve(name, a, b, radii, surface, bone, count=8):
    a, b = Vector(a), Vector(b)
    axis = (b-a).normalized()
    u = axis.cross(Vector((0, -1, 0))).normalized()
    v = u.cross(axis).normalized()
    vertices = []
    for fraction, side, front in radii:
        center = a.lerp(b, fraction)
        for i in range(count):
            angle = 2*math.pi*i/count
            vertices.append(center + u*math.sin(angle)*side + v*math.cos(angle)*front)
    faces = [tuple(reversed(range(count)))]
    for row in range(len(radii)-1):
        for i in range(count):
            j = (i+1)%count
            faces.append((row*count+i, row*count+j, (row+1)*count+j, (row+1)*count+i))
    faces.append(tuple((len(radii)-1)*count+i for i in range(count)))
    return mesh(name, vertices, faces, surface, bone)


def face(design):
    width = design['face']
    skull = loft('Sculpted jaw cheek brow and cranium', [
        (1.599, .035*width, .034, .038, .008),
        (1.621, .059*width, .075, .050, .008),
        (1.648, .076*width, .086, .061, .009),
        (1.687, .096*width, .091, .078, .009),
        (1.724, .094*width, .101, .084, .009),
        (1.759, .093*width, .083, .084, .012),
        (1.795, .081*width, .070, .077, .013),
        (1.821, .048*width, .043, .045, .014),
        (1.830, .012, .012, .012, .014)], 'skin', 'Head', count=16)
    for side in (-1, 1):
        x = side*.043*width
        # Cut an actual orbital recess so the eye sits within the face rather than on a floating plane.
        def facial_surface(px, z):
            depth = .091 + max(0,min(1,(z-1.687)/.037))*.010 if z<1.724 else .101-(z-1.724)*.514
            return .009-depth*math.sqrt(max(.01,1-(px/(.095*width))**2))
        surface_y=facial_surface(x,1.719)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=(x,surface_y,1.719))
        cutter=bpy.context.object
        cutter.scale=(.020,.018,.010)
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        bpy.context.view_layer.objects.active=skull
        recess=skull.modifiers.new('Modeled orbital recess','BOOLEAN')
        recess.operation='DIFFERENCE'
        recess.object=cutter
        bpy.ops.object.modifier_apply(modifier=recess.name)
        bpy.data.objects.remove(cutter,do_unlink=True)
        skull.vertex_groups['Head'].add(list(range(len(skull.data.vertices))),1,'REPLACE')
        ico('Inset faceted eye globe',(x,surface_y+.006,1.719),(.017,.014,.008),'eye','Head',2)
        eye_y=surface_y-.009
        ico('Iris', (x, eye_y, 1.719), (.005, .0015, .0055), 'iris', 'Head', 2)
        ico('Pupil', (x, eye_y-.0015, 1.719), (.0027, .0007, .004), 'rubber', 'Head', 1)
        ico('Eye catchlight', (x-.0015, eye_y-.0025, 1.721), (.0011, .0005, .0011), 'white', 'Head')
        brow = [(side*.020*width, -.092, 1.737), (side*.025*width, -.094, 1.745),
                (side*.068*width, -.079, 1.743), (side*.073*width, -.076, 1.735),
                (side*.045*width, -.089, 1.738)]
        brow=[(px,facial_surface(px,z)-.003,z) for px,_,z in brow]
        plate('Sculpted expressive eyebrow', brow, .004, 'hair', 'Head', .0005)
        ico('Ear outer helix', (side*.098*width, .002, 1.706), (.014, .017, .026), 'skin', 'Head', 2)
        ico('Ear concha', (side*.107*width, -.009, 1.705), (.005, .008, .014), 'skinShade', 'Head')
    mesh('Faceted nose bridge tip and nostrils', [(-.011, -.084, 1.735), (.011, -.084, 1.735),
         (-.015, -.090, 1.685), (.015, -.090, 1.685), (-.008, -.119, 1.696), (.008, -.119, 1.696),
         (0, -.117, 1.683), (0, -.098, 1.726)],
         [(0, 7, 4, 2), (7, 1, 3, 5), (4, 7, 5), (4, 5, 6), (2, 4, 6), (6, 5, 3), (2, 6, 3)], 'skin', 'Head')
    plate('Upper lip sculpt', [(-.027, -.083, 1.664), (-.010, -.090, 1.670),
          (0, -.091, 1.668), (.010, -.090, 1.670), (.027, -.083, 1.664), (0, -.093, 1.663)],
          .005, 'lip', 'Head', .0005)
    plate('Lower lip sculpt', [(-.022, -.085, 1.662), (.022, -.085, 1.662),
          (.012, -.090, 1.656), (-.012, -.090, 1.656)], .004, 'skinShade', 'Head', .0005)
    return skull


def hair_lock(name, start, bend, end, width, surface='hair'):
    start, bend, end = Vector(start), Vector(bend), Vector(end)
    axis = (end-start).normalized()
    u = axis.cross(Vector((0, 1, 0))).normalized()
    if u.length < .01:
        u = Vector((1, 0, 0))
    v = axis.cross(u).normalized()
    vertices = []
    for center, radius in ((start, width), (bend, width*.70), (end, .001)):
        for i in range(5):
            angle = i*2*math.pi/5
            vertices.append(center+u*math.cos(angle)*radius+v*math.sin(angle)*radius*.63)
    faces = [tuple(reversed(range(5)))]
    for row in range(2):
        faces += [(row*5+i, row*5+(i+1)%5, (row+1)*5+(i+1)%5, (row+1)*5+i) for i in range(5)]
    faces.append(tuple(range(10, 15)))
    return mesh(name, vertices, faces, surface, 'Head')


def hairstyle(design):
    w = design['face']
    loft('Faceted fitted hair cap', [(1.760, .095*w, .081, .086, .014),
          (1.800, .089*w, .079, .083, .014), (1.836, .055*w, .047, .049, .014),
          (1.850, .012, .012, .012, .014)], 'hair', 'Head', count=12)
    style = design['style']
    if style in ('quiff', 'swept'):
        crown=[(-.065,-.010,-.087,-.042,.060),(-.031,-.022,-.062,-.067,.071),
               (.006,-.025,-.020,-.076,.055),(.044,-.014,.026,-.071,.077),
               (.072,.012,.086,-.025,.039),(-.060,.038,-.084,.014,.042),
               (-.021,.031,-.047,.004,.085),(.022,.029,.013,-.014,.093),
               (.058,.048,.075,.031,.056),(-.037,.074,-.061,.084,.043),
               (.003,.074,-.011,.076,.063),(.041,.077,.054,.088,.036)]
        for i,(x,y,tx,ty,rise) in enumerate(crown):
            extra=.009 if style=='swept' else 0
            hair_lock('Sculpted overlapping crown tuft',(x,y,1.794),
                      (x*.65+tx*.35,y*.65+ty*.35,1.825+rise*.27),
                      (tx-.012,ty-.008,1.812+rise*.57+extra),.029+(i%3)*.003,
                      'hairLight' if i%5==0 else 'hair')
        for side in (-1, 1):
            for i in range(3):
                hair_lock('Temple and nape lock', (side*.076, .012+i*.019, 1.787),
                          (side*.096, .025+i*.017, 1.779), (side*.100, .047+i*.015, 1.750+i*.006), .013)
        for i,(x,drop,tx) in enumerate(((-.064,.024,-.083),(-.034,.052,-.061),
                                      (.002,.035,-.019),(.039,.067,.016),(.071,.027,.063))):
            hair_lock('Asymmetric sweeping fringe',(x,-.047,1.811),
                      ((x+tx)/2,-.077,1.790),(tx,-.091,1.810-drop),.027)
    elif style == 'bun':
        ico('Gathered sculpted hair bun', (0, .088, 1.845), (.062, .057, .053), 'hair', 'Head', 2)
        for i in range(7):
            hair_lock('Swept tied-back hair section', ((i-3)*.024, -.043, 1.79),
                      ((i-3)*.023, .024, 1.85), ((i-3)*.009, .10, 1.855), .020,
                      'hairLight' if i%3 == 0 else 'hair')
        for side in (-1, 1):
            hair_lock('Face framing fringe', (side*.066, -.03, 1.81), (side*.081, -.074, 1.77),
                      (side*.080, -.070, 1.703), .022)
        box('Hair tie', (0, .104, 1.818), (.063, .014, .016), 'metal', 'Head', .002)
    else:
        for i in range(11):
            angle = i*2*math.pi/11
            start = (math.sin(angle)*.058, .015+math.cos(angle)*.048, 1.825)
            bend = (math.sin(angle)*.107-.023, .013+math.cos(angle)*.09, 1.81)
            end = (math.sin(angle)*.112-.037, .017+math.cos(angle)*.10, 1.73-(i%3)*.026)
            # Thick polygonal locs retain their own blunt silhouette.
            sleeve('Sculpted crown loc', start, bend, [(0,.014,.013), (1,.013,.012)], 'hair', 'Head', 6)
            sleeve('Sculpted falling loc', bend, end, [(0,.014,.013), (1,.010,.010)],
                   'hairLight' if i%4 == 0 else 'hair', 'Head', 6)
        for i in range(3):
            box('Cropped temple hair row', (.093, .022+i*.019, 1.772), (.006, .009, .022), 'hairLight', 'Head', .001)


def torso(design):
    loft('Continuous fitted torso suit', [(1.00,.132,.083,.086,0), (1.12,.129,.085,.083,0),
         (1.25,.142,.095,.085,.004), (1.39,.183,.105,.093,.011),
         (1.46,.183,.089,.082,.019), (1.51,.080,.051,.056,.026)], 'cloth', 'Spine01')
    # Soft torso vertices blend the existing spine sections; armor pieces remain rigid.
    body = parts[-1]
    for group in list(body.vertex_groups): body.vertex_groups.remove(group)
    names = [('Hips',1.04), ('Spine02',1.19), ('Spine01',1.32), ('Spine',1.46)]
    groups = {name:body.vertex_groups.new(name=name) for name, _ in names}
    for vertex in body.data.vertices:
        z = vertex.co.z
        weights = [max(0, 1-abs(z-center)/.16) for _,center in names]
        total = sum(weights)
        if total == 0: weights[-1] = total = 1
        for (name,_),weight in zip(names,weights):
            if weight: groups[name].add([vertex.index],weight/total,'REPLACE')
    for side in (-1,1):
        x = lambda value: side*value
        plate('Beveled pectoral shell', [(x(.010),-.106,1.442), (x(.120),-.112,1.453),
              (x(.172),-.097,1.401), (x(.153),-.119,1.335), (x(.047),-.129,1.302),
              (x(.011),-.129,1.327)], .028, 'armor', 'Spine', .005)
        plate('Pectoral lower inset', [(x(.044),-.135,1.333),(x(.142),-.127,1.352),
              (x(.125),-.130,1.322),(x(.063),-.138,1.309)], .006, 'armorShade','Spine',.001)
        plate('Oblique layered armor', [(x(.118),-.097,1.313),(x(.153),-.081,1.286),
              (x(.130),-.099,1.207),(x(.080),-.107,1.220)], .020,'armorShade','Spine01')
        box('Chest cyan inset', (x(.142),-.116,1.405), (.012,.006,.045),'accent','Spine',.001)
        box('Shoulder strap', (x(.139),-.052,1.465), (.032,.09,.022),'metal','Spine',.002)
        box('Chest strap rivet', (x(.143),-.078,1.468), (.014,.007,.012),'stitch','Spine',.001)
        # Shaped hip tabs maintain a layered cloth silhouette without a long coat.
        plate('Split hip cloth panel', [(x(.065),-.106,1.102),(x(.145),-.091,1.100),
              (x(.183),-.085,.948),(x(.111),-.121,.933)], .016,'armorShade','Hips',.002)
        box('Hip panel accent seam',(x(.144),-.118,.997),(.012,.005,.080),'metal','Hips',.001)
        box('Belt utility pouch',(x(.147),-.106,1.081),(.068,.058,.096),'cloth','Hips',.005)
        box('Pouch flap',(x(.147),-.139,1.108),(.064,.010,.026),'metal','Hips',.002)
        box('Pouch clasp',(x(.147),-.146,1.097),(.012,.005,.018),'stitch','Hips',.001)
        plate('Back scapular armor', [(x(.01),.106,1.439),(x(.137),.100,1.448),
              (x(.157),.101,1.350),(x(.059),.117,1.305),(x(.01),.119,1.328)],
              -.021,'armor','Spine',.004)
    for i in range(3):
        box('Segmented abdominal insert',(0,-.099,1.247-i*.043),(.11,.023,.033),
            'armorShade' if i%2==0 else 'cloth','Spine01',.003)
    loft('Utility belt',[(1.065,.150,.101,.099,0),(1.115,.151,.101,.099,0)],'rubber','Hips')
    box('Belt buckle',(0,-.111,1.09),(.070,.021,.049),'metal','Hips',.004)
    box('Belt buckle inset',(0,-.124,1.09),(.037,.006,.024),'rubber','Hips',.001)
    box('Buckle pin',(.014,-.129,1.09),(.010,.006,.019),'stitch','Hips',.001)
    loft('Neck',[(1.49,.046,.045,.043,.025),(1.625,.043,.039,.043,.024)],'skin','neck')
    loft('Layered scarf upper collar',[(1.478,.112,.101,.088,.024),
         (1.522,.105,.083,.084,.027),(1.565,.082,.063,.068,.026)],'scarf','Spine')
    plate('Scarf folded front', [(-.102,-.066,1.532),(.103,-.068,1.530),(.072,-.111,1.487),
          (.008,-.123,1.453),(-.069,-.105,1.486)], .014,'scarf','Spine',.002)
    plate('Scarf diagonal fold', [(-.092,-.082,1.516),(.075,-.095,1.506),(.012,-.127,1.473),
          (-.046,-.113,1.489)],.005,'scarf','Spine',.001)
    box('Back harness spine',(0,.118,1.367),(.035,.017,.130),'metal','Spine',.002)
    box('Back harness charge line',(0,.129,1.365),(.013,.005,.070),'accent','Spine',.001)


def limbs(design):
    for side, sign in (('Left',1),('Right',-1)):
        upper, lower, hand = side+'Arm',side+'ForeArm',side+'Hand'
        a,b,c = head(upper),head(lower),head(hand)
        sleeve('Upper arm tailored suit',a,b,[(0,.079,.072),(.22,.082,.075),(.80,.060,.058),(1,.054,.052)],'cloth',upper)
        sleeve('Upper arm cuff',a,b,[(.66,.066,.062),(.83,.065,.061)],'armorShade',upper)
        shoulder = a.lerp(b,.19)
        shoulder.y -= .009
        shoulder.z += .008
        shell = sleeve('Sculpted shoulder shell',a,b,[(.03,.090*design['shoulder'],.095),
                       (.20,.103*design['shoulder'],.094),(.43,.082,.078)],'armor',upper,8)
        front = shoulder+Vector((0,-.096,.004))
        box('Shoulder recessed edge',front,(.087,.012,.017),'metal',upper,.002)
        box('Shoulder accent insert',front+Vector((sign*.031,-.008,.013)),(.018,.005,.024),'accent',upper,.001)
        ico('Elbow articulated suit joint',b,(.059,.057,.062),'rubber',lower,2)
        sleeve('Forearm undersuit',b,c,[(0,.055,.052),(.4,.061,.054),(1,.040,.038)],'cloth',lower)
        sleeve('Segmented forearm bracer',b,c,[(.20,.068,.064),(.40,.070,.068),(.83,.049,.047)],'armor',lower)
        sleeve('Bracer wrist seal',b,c,[(.82,.052,.05),(.94,.049,.046)],'rubber',lower)
        center=b.lerp(c,.57)+Vector((0,-.062,0))
        box('Forearm inset panel',center,(.039,.012,.10),'armorShade',lower,.003,direction=c-b)
        center=b.lerp(c,.77)+Vector((0,-.048,0))
        box('Forearm luminous trim',center,(.044,.008,.013),'accent',lower,.001)
        finger_names=[hand+finger+'1' for finger in ('Index','Middle','Ring','Pinky')]
        knuckles=sum((head(name) for name in finger_names),Vector())/4
        sleeve('Fitted glove palm',c,knuckles,[(0,.039,.029),(.45,.046,.034),(1,.046,.027)],'cloth',hand)
        for finger in ('Thumb','Index','Middle','Ring','Pinky'):
            for segment in range(1,4):
                bone=hand+finger+str(segment)
                radius=.010 if finger!='Pinky' else .0085
                sleeve('Articulated glove '+finger+' '+str(segment),head(bone),tail(bone),
                       [(0,radius,radius),(.4,radius*1.04,radius),(1,radius*.82,radius*.83)],
                       'cloth' if segment<3 else 'skin',bone,6)
                if segment==1:
                    ico('Glove knuckle armor',head(bone)+Vector((0,.005,.007)),
                        (radius*1.12,radius*.9,radius*.8),'metal',bone)
        hip,knee,ankle=head(side+'UpLeg'),head(side+'Leg'),head(side+'Foot')
        thigh,shin,foot=side+'UpLeg',side+'Leg',side+'Foot'
        sleeve('Tailored trouser thigh',hip,knee,[(0,.101,.095),(.24,.116,.100),(.68,.091,.078),(1,.073,.066)],'cloth',thigh)
        tx,ty,tz=hip.lerp(knee,.36)
        plate('Sculpted thigh side reinforcement',[(tx-.046,ty-.096,tz+.076),
              (tx+.048,ty-.096,tz+.076),(tx+.057,ty-.104,tz-.025),
              (tx+.026,ty-.108,tz-.090),(tx-.041,ty-.108,tz-.071)],.014,'armorShade',thigh,.003)
        box('Thigh inset seam',(tx+sign*.032,ty-.114,tz-.011),(.008,.005,.080),'metal',thigh,.001)
        ico('Knee fabric articulation',knee,(.080,.074,.074),'rubber',shin,2)
        sleeve('Lower trouser leg',knee,ankle,[(0,.074,.069),(.4,.078,.065),(1,.054,.05)],'cloth',shin)
        kx,ky,kz=knee
        plate('Angular knee guard',[(kx-.071,ky-.072,kz+.052),(kx+.064,ky-.072,kz+.052),
              (kx+.072,ky-.096,kz+.003),(kx+.033,ky-.103,kz-.063),
              (kx-.042,ky-.103,kz-.063),(kx-.076,ky-.091,kz-.002)],.032,'armor',shin,.005)
        plate('Knee dark inset',[(kx-.040,ky-.106,kz+.025),(kx+.034,ky-.106,kz+.025),
              (kx+.018,ky-.119,kz-.035),(kx-.023,ky-.119,kz-.035)],.005,'armorShade',shin,.001)
        center=knee.lerp(ankle,.55)
        x,y,z=center
        plate('Shin segmented guard',[(x-.060,y-.063,z+.108),(x+.055,y-.063,z+.108),
              (x+.058,y-.079,z-.083),(x+.025,y-.089,z-.125),
              (x-.039,y-.089,z-.125),(x-.065,y-.072,z-.052)],.025,'armorShade',shin,.004)
        box('Shin vent inset',(x,y-.092,z),(.026,.008,.105),'rubber',shin,.002)
        sleeve('Boot ankle cuff',knee,ankle,[(.83,.065,.060),(1.08,.067,.066)],'armor',foot)
        x=ankle.x
        sole=loft('Shaped boot outsole',[(.012,.070,.195,.106,.015),(.045,.077,.196,.106,.015)],'rubber',foot,12)
        sole.location.x=x
        shoe=loft('Anatomical sculpted boot upper',[(.047,.074,.188,.100,.015),
                  (.092,.072,.177,.095,.015),(.134,.061,.144,.083,.022),
                  (.185,.047,.051,.065,.038)],'armor',foot,12)
        shoe.location.x=x
        box('Toe reinforcement',(x,-.151,.092),(.112,.056,.021),'armorShade',foot,.006)
        box('Boot instep panel',(x,-.040,.159),(.099,.135,.029),'cloth',foot,.005)
        box('Boot lateral accent',(x+sign*.080,-.022,.074),(.008,.082,.013),'accent',foot,.001)
        for i in range(3):
            box('Boot tread division',(x,-.15+i*.075,.013),(.143,.008,.012),'metal',foot,.001)


def variant_details(design):
    style=design['style']
    if style=='bun':
        for obj in parts:
            if obj.name.startswith(('Beveled pectoral shell','Pectoral lower inset')):
                for vertex in obj.data.vertices: vertex.co.x*=.92
        plate('Bot01 asymmetric upper thigh shield',[(.080,-.127,1.059),(.164,-.113,1.054),
              (.197,-.120,.893),(.110,-.140,.873)],.018,'armor','LeftUpLeg',.003)
        box('Bot01 thigh shield signal',(.148,-.145,.958),(.012,.006,.109),'accent','LeftUpLeg',.001)
        plate('Bot01 chest center chevron',[(-.028,-.134,1.374),(0,-.147,1.343),
              (.028,-.134,1.374),(0,-.143,1.320)],.006,'accent','Spine',.001)
    elif style=='swept':
        for sign in (-1,1):
            plate('Bot02 broad collar reinforcement',[(sign*.073,-.079,1.472),
                  (sign*.152,-.083,1.455),(sign*.147,-.124,1.399),
                  (sign*.118,-.128,1.409)],.014,'metal','Spine',.002)
            box('Bot02 chest side clasp',(sign*.156,-.116,1.366),(.026,.011,.027),'metal','Spine',.002)
    elif style=='locs':
        plate('Bot03 copper chest diagonal',[(-.145,-.119,1.422),(-.122,-.125,1.433),
              (.100,-.142,1.324),(.083,-.142,1.309)],.007,'metal','Spine',.001)
        plate('Bot03 long lateral hip tab',[(-.151,-.106,1.102),(-.083,-.120,1.093),
              (-.102,-.145,.891),(-.174,-.120,.912)],.017,'armor','RightUpLeg',.003)
        box('Bot03 tab clasp',(-.131,-.148,1.008),(.035,.009,.025),'metal','RightUpLeg',.002)


def atlas_for(name, colors):
    image=bpy.data.images.new(name+' palette',width=256,height=256,alpha=True)
    pixels=[]
    for y in range(256):
        for x in range(256):
            index=(y//64)*4+x//64
            pixels.extend((*colors[ORDER[index]],1))
    image.pixels.foreach_set(pixels)
    image.filepath_raw=str(assets/(name+'-Palette.png'))
    image.file_format='PNG'
    image.save()
    image.pack()
    mat=material(name+' palette surface',(1,1,1),.08,.65)
    node=mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image=image
    mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    return mat


def export(name, atlas):
    output=[]
    for level,ratio in enumerate((1,.58,.28)):
        copies=[]
        bpy.ops.object.select_all(action='DESELECT')
        for original in parts:
            obj=original.copy()
            obj.data=original.data.copy()
            bpy.context.collection.objects.link(obj)
            obj.select_set(True)
            copies.append(obj)
        bpy.context.view_layer.objects.active=copies[0]
        bpy.ops.object.join()
        obj=bpy.context.object
        obj.name=name+'_Body_LOD'+str(level)
        while obj.data.uv_layers: obj.data.uv_layers.remove(obj.data.uv_layers[0])
        uv=obj.data.uv_layers.new(name='PaletteUV')
        for polygon in obj.data.polygons:
            slot=obj.data.materials[polygon.material_index].name.split('_')[-1]
            index=ORDER.index(slot)
            for loop in polygon.loop_indices:
                uv.data[loop].uv=((index%4+.5)/4,(index//4+.5)/4)
            polygon.material_index=0
        obj.data.materials.clear()
        obj.data.materials.append(atlas)
        if level:
            modifier=obj.modifiers.new('Distance topology','DECIMATE')
            modifier.ratio=ratio
            # Simplification acts before skinning; animation modifiers remain unapplied.
            bpy.context.view_layer.objects.active=obj
            bpy.ops.object.modifier_move_up(modifier=modifier.name)
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        triangulate=obj.modifiers.new('Explicit game export triangles','TRIANGULATE')
        bpy.ops.object.modifier_move_up(modifier=triangulate.name)
        bpy.ops.object.modifier_apply(modifier=triangulate.name)
        topology=bmesh.new()
        topology.from_mesh(obj.data)
        collapsed=[face for face in topology.faces if face.calc_area()<1e-14]
        if collapsed: bmesh.ops.delete(topology,geom=collapsed,context='FACES')
        topology.to_mesh(obj.data)
        topology.free()
        obj.data.calc_loop_triangles()
        assert all(vertex.groups for vertex in obj.data.vertices),'All vertices must be skinned.'
        triangles=len(obj.data.loop_triangles)
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        rig.select_set(True)
        bpy.context.view_layer.objects.active=rig
        path=assets/(name+'_LOD'+str(level)+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path),
            use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,
            use_armature_deform_only=True,bake_anim=False,axis_forward='-Z',axis_up='Y')
        # FBX normalizes tiny polygon slivers. Report the delivered mesh, not pre-export tessellation.
        before=set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(path))
        imported=set(bpy.data.objects)-before
        delivered=next(item for item in imported if item.type=='MESH')
        delivered.data.calc_loop_triangles()
        output.append({'level':level,'triangles':len(delivered.data.loop_triangles),
                       'vertices':len(delivered.data.vertices),'sourceTriangles':triangles})
        for item in imported: bpy.data.objects.remove(item,do_unlink=True)
        bpy.data.objects.remove(obj,do_unlink=True)
    return output


def studio(name):
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=24
    scene.render.threads_mode='FIXED'
    scene.render.threads=4
    scene.render.resolution_x=900
    scene.render.resolution_y=1100
    scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Neutral review studio')
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.13,.16,.19,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.02))
    floor=bpy.context.object
    floor.name='Review floor (not exported)'
    floor.data.materials.append(material('Review floor',(.17,.19,.21),0,.85))
    target=Vector((0,0,1.02))
    for label,location,power,color in [('Key',(2,-3,4),320,(1,.89,.77)),
                                      ('Fill',(-2,-1,2.5),200,(.65,.78,1)),
                                      ('Rim',(1,2,3),400,(.8,.9,1))]:
        bpy.ops.object.light_add(type='AREA',location=location)
        light=bpy.context.object
        light.name=label
        light.data.energy=power
        light.data.color=color
        light.data.shape='DISK'
        light.data.size=3
        light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=(2.4,-5,2.15))
    camera=bpy.context.object
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'
    camera.data.ortho_scale=2.15
    scene.camera=camera
    scene.render.filepath=str(review/(name+'-Studio.png'))
    bpy.ops.render.render(write_still=True)
    if name=='Player':
        camera.location=(1.2,-3,1.85)
        target=Vector((0,-.01,1.715))
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=.53
        scene.render.resolution_x=1000
        scene.render.resolution_y=1000
        scene.render.filepath=str(review/(name+'-Face.png'))
        bpy.ops.render.render(write_still=True)


reports=[]
for name,design in DESIGNS.items():
    if args.only and args.only!=name: continue
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/ThirdParty/Grab3D/ArmoredContender.blend'),load_ui=False)
    if hasattr(bpy.context.preferences.filepaths,'save_preview_images'):
        bpy.context.preferences.filepaths.save_preview_images=False
    rig=next(obj for obj in bpy.data.objects if obj.type=='ARMATURE')
    for obj in list(bpy.data.objects):
        if obj!=rig: bpy.data.objects.remove(obj,do_unlink=True)
    rig.animation_data_clear()
    for bone in rig.pose.bones: bone.matrix_basis.identity()
    parts=[]
    colors={key:design[key] for key in ORDER if key in design}
    colors.update(rubber=(.012,.016,.020),eye=(.65,.68,.64),iris=(.09,.16,.12),
                  lip=tuple(c*.63 for c in design['skin']),
                  hairLight=tuple(min(1,c*1.65+.012) for c in design['hair']),
                  armorShade=tuple(c*.63 for c in design['armor']),
                  skinShade=tuple(c*.59 for c in design['skin']),stitch=(.41,.37,.29),white=(.9,.94,.95))
    materials={key:material(name+'_'+key,colors[key],.30 if key=='metal' else 0) for key in ORDER}
    torso(design)
    limbs(design)
    face(design)
    hairstyle(design)
    variant_details(design)
    atlas=atlas_for(name,colors)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.ops.wm.save_as_mainfile(filepath=str(source/(name+'.blend')))
    lods=export(name,atlas)
    points=[obj.matrix_world@Vector(corner) for obj in parts for corner in obj.bound_box]
    bounds=[max(point[i] for point in points)-min(point[i] for point in points) for i in range(3)]
    entry={'name':name,'parts':len(parts),'boundsMetres':bounds,'bones':len(rig.data.bones),'lods':lods,
           'geometry':'Original modeled geometry; CC0 Grab3D skeleton only; CC0 Quaternius animations in Unity',
           'approvalReference':'ArtSource/Original/StylizedContenders/Approved-Concept.png'}
    (source/(name+'-Preparation.json')).write_text(json.dumps(entry,indent=2)+'\n')
    reports.append(entry)
    if args.render: studio(name)
print('STYLIZED_CONTENDERS '+json.dumps(reports))
