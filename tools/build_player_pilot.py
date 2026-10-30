"""Reference-led player pilot. Never writes the installed contender or bot assets.

Source: Blender Z up, -Y forward, metres. Uses the existing licensed skeleton
and project geometry helpers, with a separately authored pilot surface model.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

cli = argparse.ArgumentParser()
cli.add_argument('root', type=Path)
cli.add_argument('--stage', choices=('model', 'render', 'audit', 'fit', 'texture', 'rigfix', 'details'), default='model')
options = cli.parse_args(sys.argv[sys.argv.index('--') + 1:])
root = options.root.resolve()
out = root / 'ArtSource/Original/PlayerPilot-v1'
assets = root / 'RailArena/Assets/RailArena/Art/PlayerPilot'
review = root / 'Validation/PlayerPilot-v1'
refs = root / 'ArtSource/Original/StylizedContenders/References-v1/Final'
for folder in (out, assets, review):
    folder.mkdir(parents=True, exist_ok=True)

# Reuse pure construction helpers, not the old generation loop or its outputs.
helper_text = (root / 'tools/build_stylized_contenders.py').read_text()
helper_text = helper_text[helper_text.index('DESIGNS ='):helper_text.index('\nreports=[]')]
exec(compile(helper_text, 'contender_geometry_helpers', 'exec'), globals())


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n')


def remove_parts(prefixes):
    for obj in list(parts):
        if obj.name.startswith(prefixes):
            parts.remove(obj)
            bpy.data.objects.remove(obj, do_unlink=True)


def front_panel(name, outline, surface='armor', bone='Spine', depth=.016):
    return plate(name, outline, depth, surface, bone, 0)


def face_master():
    """Continuous neck, jaw, cheek, nose, brow and cranium volume.

    Front samples use x coordinates to sculpt the nose/philtrum in the same
    surface, instead of a disconnected nose glued onto an ellipsoid.
    """
    rings = [
        (1.505,.043,.040,.042,.022), (1.565,.044,.041,.043,.022),
        (1.595,.043,.044,.045,.020), (1.609,.038,.061,.047,.018),
        (1.620,.050,.071,.051,.015), (1.633,.065,.078,.058,.012),
        (1.647,.067,.083,.064,.010), (1.659,.073,.085,.071,.009),
        (1.670,.079,.085,.077,.009), (1.681,.085,.088,.081,.010),
        (1.694,.089,.089,.083,.010), (1.707,.090,.086,.084,.011),
        (1.718,.090,.086,.085,.012), (1.729,.091,.088,.086,.013),
        (1.740,.092,.090,.086,.014), (1.756,.092,.085,.087,.015),
        (1.775,.088,.078,.083,.015), (1.795,.076,.067,.073,.015),
        (1.812,.057,.049,.053,.015), (1.824,.032,.028,.030,.015),
        (1.828,.005,.005,.005,.015)]
    obj = loft('Head - connected neck jaw cheeks nose and brow', rings, 'skin', 'Head', 64)
    for vertex in obj.data.vertices:
        x,y,z = vertex.co
        angle = (vertex.index % 64) * 2 * math.pi / 64
        front = math.cos(angle)
        if front > 0 and z > 1.606:
            # Flatten the anterior face into cheek planes; preserve the skull sides.
            half = rings[vertex.index // 64][1]
            depth = rings[vertex.index // 64][2]
            y = rings[vertex.index // 64][4] - depth * max(0, 1-(x/half)**2)**.29
            nose_profile=float(np.interp(z,[1.670,1.681,1.694,1.707,1.718,1.729,1.740],
                [0,.006,.020,.018,.010,.004,0]))
            nose = nose_profile * max(0,1-abs(x)/.015)
            bridge = 0
            nostril = .007 * math.exp(-((abs(x)-.011)/.006)**2) * math.exp(-((z-1.683)/.008)**2)
            orbital = .010 * math.exp(-((abs(x)-.034)/.018)**4) * math.exp(-((z-1.719)/.009)**4)
            cheek = .004 * math.exp(-((abs(x)-.058)/.018)**2) * math.exp(-((z-1.693)/.015)**2)
            lip = .006 * math.exp(-(x/.021)**4) * math.exp(-((z-1.659)/.007)**2)
            vertex.co.y = y - nose - bridge - nostril + orbital - cheek - lip
    # Blend the connected lower neck into the neck bone, preserving a continuous skin.
    neck_group = obj.vertex_groups.new(name='neck')
    for v in obj.data.vertices:
        neck_weight = max(0, min(1, (1.610-v.co.z)/.035))
        if neck_weight:
            obj.vertex_groups['Head'].add([v.index],1-neck_weight,'REPLACE')
            neck_group.add([v.index],neck_weight,'REPLACE')
    for poly in obj.data.polygons:
        poly.use_smooth = True
    for sign in (-1,1):
        x = sign*.034
        # Eyes are seated under lid rings and orbital surfaces.
        eye_vertices=[(x,-.081,1.718)]
        for i in range(24):
            a=i*math.pi/12
            px=.018*math.cos(a)
            eye_vertices.append((x+px,-.070-.010*abs(math.sin(a)),
                1.718+.0058*math.sin(a)+sign*px*.10))
        eye=mesh('Eye - seated almond sclera',eye_vertices,
            [(0,1+i,1+(i+1)%24) for i in range(24)],'eye','Head')
        for p in eye.data.polygons:p.use_smooth=True
        ico('Warm brown iris', (x,-.082,1.718), (.0058,.0010,.0057),'iris','Head',2)
        ico('Pupil', (x,-.083,1.718), (.0030,.0005,.0037),'rubber','Head',2)
        ico('Catchlight', (x-.0016,-.0835,1.720),(.0009,.0003,.0009),'white','Head',1)
        for upper in (True,False):
            arc=[]
            for i in range(9):
                t=i/8
                px=x+(.0195- .039*t)
                pz=1.718+(.0055 if upper else -.0045)*math.sin(math.pi*t)+sign*(px-x)*.10
                py=-.071-.011*math.sin(math.pi*t)
                arc.append((px,py,pz))
            outer=[(px,py+.003,pz+(.005 if upper else -.004)) for px,py,pz in reversed(arc)]
            plate('Anatomical upper eyelid' if upper else 'Anatomical lower eyelid',arc+outer,.004,'skin','Head',0)
        plate('Angular expressive brow',[(sign*.014,-.088,1.729),(sign*.017,-.089,1.737),
              (sign*.061,-.073,1.746),(sign*.070,-.064,1.738),
              (sign*.043,-.080,1.735)],.005,'hair','Head',0)
        # Ear rim with an actual concha depression, facing forward and outward.
        ear=ico('Ear helix', (sign*.092,.007,1.697),(.014,.019,.026),'skin','Head',3)
        for v in ear.data.vertices:
            if v.co.y < -.004:
                v.co.y+=.006*math.exp(-((v.co.z)/.016)**2)*math.exp(-((v.co.x)/.008)**2)
        ico('Ear concha shadow',(sign*.096,-.009,1.695),(.006,.002,.013),'skinShade','Head',2)
        ico('Ear tragus',(sign*.087,-.011,1.693),(.004,.004,.006),'skin','Head',2)
        ico('Nostril aperture',(sign*.009,-.087,1.681),(.003,.0008,.0017),'skinShade','Head',2)
    plate('Defined upper lip',[(-.023,-.079,1.658),(-.009,-.085,1.662),(0,-.083,1.660),
          (.009,-.085,1.662),(.023,-.079,1.658),(0,-.087,1.657)],.003,'lip','Head',0)
    plate('Lower lip plane',[(-.021,-.080,1.657),(0,-.086,1.656),(.021,-.080,1.657),
          (.011,-.083,1.652),(-.011,-.083,1.652)],.003,'skin','Head',0)
    for obj in parts:
        if obj.name.startswith(('Eye -','Warm brown iris','Pupil','Catchlight','Anatomical upper eyelid','Anatomical lower eyelid')):
            obj.location.y+=.008
        if obj.name.startswith(('Defined upper lip','Lower lip plane')):
            for v in obj.data.vertices:v.co.x*=.88;v.co.y+=.004


def hair_lock(name,start,bend,end,width,surface='hair'):
    """Swept solid leaf, with its width tangent to the scalp at its root."""
    start,bend,end=Vector(start),Vector(bend),Vector(end)
    axis=(end-start).normalized()
    normal=(start-Vector((0,.015,1.735))).normalized()
    u=axis.cross(normal).normalized()
    if u.length<.01:u=Vector((1,0,0))
    v=u.cross(axis).normalized()
    if v.dot(normal)<0:v=-v
    verts=[];count=8
    for centre,radius in ((start,width*.55),(start.lerp(bend,.55),width),
        (bend,width*.8),(end,.0004)):
        for i in range(count):
            a=i*2*math.pi/count
            verts.append(centre+u*math.cos(a)*radius+v*math.sin(a)*radius*.53)
    faces=[tuple(reversed(range(count)))]
    for row in range(3):
        faces.extend((row*count+i,row*count+(i+1)%count,(row+1)*count+(i+1)%count,(row+1)*count+i) for i in range(count))
    faces.append(tuple(range(3*count,4*count)))
    return mesh(name,verts,faces,surface,'Head')


def hair_master():
    cap=loft('Hair - fitted scalp foundation',[(1.710,.094,.069,.084,.016),
        (1.750,.094,.072,.086,.016),(1.790,.087,.077,.082,.015),
        (1.824,.062,.054,.059,.014),(1.846,.015,.015,.015,.014)],'hair','Head',24)
    # Raise the anterior foundation above the forehead; nape remains low.
    for v in cap.data.vertices[:24]:
        c=math.cos(v.index*2*math.pi/24)
        v.co.z += .058*max(0,c)-.012*max(0,-c)
    # Directional overlapping blades lie along the skull rather than standing as cones.
    for i in range(43):
        # Golden-angle roots avoid repeated rows; each blade follows a swept tangent.
        radius=math.sqrt((i+.5)/43)
        angle=i*2.399963
        x=.091*radius*math.cos(angle)
        y=.015+.084*radius*math.sin(angle)
        z=1.772+.073*math.sqrt(max(.02,1-radius*radius))
        start=Vector((x,y,z))
        normal=Vector((x/.10,(y-.015)/.10,.65)).normalized()
        tangent=Vector((-.75+.22*math.sin(i*1.7),-.42+.37*math.cos(i*.8),.15))
        tangent=(tangent-normal*tangent.dot(normal)).normalized()
        length=.044+.018*(.5+.5*math.sin(i*2.7))
        hair_lock('Hair - individual swept crown %02d'%i,start-normal*.010,
            start+tangent*length*.5+normal*.014,
            start+tangent*length+normal*(.012+.012*(i%3)/2),
            .022+.005*(i%3)/2,'hairLight' if i%11==0 else 'hair')
    # Front fringe breaks the hairline, with a longer sweep on anatomical right.
    for i,(x,tx,drop) in enumerate([(-.077,-.100,.043),(-.051,-.079,.059),
        (-.020,-.048,.048),(.015,-.019,.037),(.047,.021,.057),(.075,.068,.036)]):
        hair_lock('Hair - individual fringe %d'%i,(x,-.040,1.810),
            ((x+tx)/2,-.079,1.808),(tx,-.094,1.811-drop),.025)
    for sign in (-1,1):
        for row in range(3):
            for col in range(4):
                y=.005+col*.030
                z=1.790-row*.027
                x=sign*(.080+(.008 if row==0 else -.003))
                hair_lock('Hair - temple and rear feather',(x,y,z),
                    (x+sign*.014,y+.014,z+.009),
                    (x+sign*.019,y+.035,z-.010),.020 if row else .024)
    for row in range(3):
        for col in range(5):
            x=(col-2)*.032
            y=.084+(.008 if col in (1,2,3) else -.007)
            z=1.776-row*.022
            hair_lock('Hair - nape cascade',(x,y-.012,z+.014),
                (x-.010,y+.015,z),(x-.016,y+.023,z-.032),.018)


def armor_master():
    remove_parts(('Beveled pectoral','Pectoral lower','Oblique layered','Chest cyan','Shoulder strap',
        'Chest strap rivet','Recessed chest','Back scapular','Back harness','Fitted abdominal',
        'Abdominal textile','Layered scarf','Scarf folded','Scarf diagonal','Neck',
        'Split hip cloth','Hip panel fitted','Thigh inset seam','Shin lateral','Toe reinforcement',
        'Boot instep','Boot lateral accent','Boot tread division','Shoulder accent','Forearm inset'))
    # Compact unified central breastplate with separated shoulder straps and lower apron.
    for sign in (-1,1):
        front_panel('Chest - rising clavicle frame',[(sign*.052,-.098,1.456),
            (sign*.141,-.085,1.473),(sign*.168,-.103,1.423),(sign*.132,-.124,1.343),
            (sign*.090,-.129,1.349),(sign*.106,-.116,1.410)],depth=.020)
        front_panel('Chest - clavicle recessed dark edge',[(sign*.075,-.118,1.444),
            (sign*.103,-.119,1.438),(sign*.111,-.130,1.370),(sign*.092,-.134,1.367)],'armorShade',depth=.005)
        front_panel('Chest - cyan frame inset',[(sign*.077,-.123,1.432),
            (sign*.087,-.124,1.430),(sign*.102,-.133,1.381),(sign*.094,-.135,1.379)],'accent',depth=.004)
        # Back is a compact shell, never a backpack.
        front_panel('Back - scapular frame',[(sign*.025,.108,1.449),
            (sign*.136,.093,1.456),(sign*.158,.098,1.390),
            (sign*.100,.119,1.321),(sign*.045,.121,1.330)],depth=-.020)
        for z,x in ((1.414,.151),(1.338,.112)):
            ico('Armor - flush titanium fastener',(sign*x,-.132,z),(.0035,.0017,.0035),'metal','Spine',2)
        # Broad ivory-bordered dark hip panel. Both borders belong to the same panel.
        outline=[(sign*.063,-.111,1.070),(sign*.154,-.103,1.064),
            (sign*.207,-.111,.842),(sign*.089,-.142,.858)]
        panel=front_panel('Hip - broad ivory panel frame',outline,'armor','Hips',.013)
        cx=sum(p[0] for p in outline)/4;cz=sum(p[2] for p in outline)/4
        inner=[(cx+(x-cx)*.68,y-.003,cz+(z-cz)*.91) for x,y,z in outline]
        front_panel('Hip - recessed charcoal fabric insert',inner,'cloth','Hips',.005)
        ico('Hip - cyan lower inset',(sign*.180,-.130,.859),(.005,.002,.006),'accent','Hips',1)
    formed_panel('Chest - central faceted breastplate',[(-.110,-.128,1.390),(.110,-.128,1.390),
        (.122,-.134,1.357),(.085,-.141,1.318),(-.085,-.141,1.318),(-.122,-.134,1.357)],'armor','Spine',.008)
    front_panel('Chest - lower dark gasket',[(-.086,-.133,1.324),(.086,-.133,1.324),
        (.083,-.129,1.302),(-.083,-.129,1.302)],'rubber',depth=.012)
    front_panel('Back - central fitted plate',[(-.045,.121,1.417),(.045,.121,1.417),
        (.063,.132,1.341),(0,.139,1.314),(-.063,.132,1.341)],'armorShade',depth=-.018)
    box('Back - cyan lower seam',(0,.141,1.330),(.086,.003,.004),'accent','Spine',0)
    # Hollow cloth collar: outer and inner rim are bridged, leaving an actual neck opening.
    count=24;verts=[]
    collar_rings=[(.125,.115,1.510,.055),(.129,.119,1.550,.070),
        (.119,.108,1.560,.058),(.124,.110,1.590,.066),
        (.116,.103,1.612,.042),(.084,.071,1.606,.042),(.059,.052,1.510,.055)]
    for layer,(w,d,z,droop) in enumerate(collar_rings):
        for i in range(count):
            a=i*2*math.pi/count
            front=math.cos(a)
            ripple=1+.035*math.sin(3*a+layer*.7)
            verts.append((w*math.sin(a)*ripple,.023-d*front*ripple,z-droop*max(0,front)+.006*math.sin(3*a)))
    faces=[]
    for layer in range(len(collar_rings)):
        nxt=(layer+1)%len(collar_rings)
        for i in range(count):
            j=(i+1)%count
            faces.append((layer*count+i,layer*count+j,nxt*count+j,nxt*count+i))
    mesh('Scarf - hollow wrapped collar',verts,faces,'scarf','Spine')


def detail_master():
    for side,sign in (('Left',1),('Right',-1)):
        a,b,c=head(side+'Arm'),head(side+'ForeArm'),head(side+'Hand')
        upper,lower,hand=side+'Arm',side+'ForeArm',side+'Hand'
        # Outer shoulder sockets are seated on the actual shell with an inset cyan bar.
        center=a.lerp(b,.21)+Vector((sign*.079,0,.016))
        box('Shoulder - dark outer socket',center,(.010,.065,.068),'rubber',upper,0)
        box('Shoulder - cyan seated signal',center+Vector((sign*.0055,-.002,0)),(.003,.040,.046),'accent',upper,0)
        for frac in (.35,.60):
            p=b.lerp(c,frac)+Vector((0,-.065,.002))
            ico('Bracer - flush fastener',p,(.003,.0018,.003),'metal',lower,2)
        p=b.lerp(c,.53)+Vector((0,-.064,0))
        box('Bracer - charcoal recessed insert',p,(.026,.004,.080),'rubber',lower,0,direction=c-b)
        box('Bracer - cyan insert edge',p+Vector((sign*.016,-.002,0)),(.004,.002,.056),'accent',lower,0,direction=c-b)
        # Glove dorsal plates follow the wrist's actual local basis, not world Z.
        hand_basis=rig.matrix_world @ rig.data.bones[hand].matrix_local
        for i,finger in enumerate(('Index','Middle','Ring','Pinky')):
            p=head(hand+finger+'1')
            box('Glove - ivory knuckle plate',p+hand_basis.to_3x3()@Vector((0,0,.012)),
                (.012,.023,.006),'armor',hand,0)
        palm=c.lerp(sum((head(hand+f+'1') for f in ('Index','Middle','Ring','Pinky')),Vector())/4,.58)
        pad=ico('Glove - raised palm contact pad',palm+hand_basis.to_3x3()@Vector((0,0,-.017)),(.030,.023,.008),'rubber',hand,2)
        x=head(side+'Foot').x
        foot=side+'Foot'
        # Instep armor follows the sloping upper; top is not a vertical plaque.
        mesh('Boot - continuous ivory instep',[(x-.041,-.145,.103),(x+.041,-.145,.103),
            (x+.032,-.052,.177),(x-.032,-.052,.177),
            (x-.041,-.136,.092),(x+.041,-.136,.092),(x+.032,-.045,.165),(x-.032,-.045,.165)],
            [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'armor',foot)
        front_panel('Boot - ivory toe cap',[(x-.055,-.169,.075),(x+.055,-.169,.075),
            (x+.046,-.191,.044),(x-.046,-.191,.044)],'armorShade',foot,.025)
        # Outer right ankle only, as specified by the reference detail card.
        if side=='Right':
            box('Boot - RIGHT outer cyan ankle',(x+sign*.071,-.022,.079),(.005,.067,.018),'accent',foot,0)
        # Shallow actual tread lugs, plus a separate cyan midfoot sole insert.
        for row in range(5):
            for s in (-1,1):
                lug=box('Sole - seated chevron tread',(x+s*.042,-.142+row*.052,.011),(.044,.034,.012),'rubber',foot,0)
                lug.rotation_euler.z=s*.32
        box('Sole - recessed cyan midfoot',(x,-.004,.007),(.029,.040,.004),'accent',foot,0)
    # Sculpt cloth folds into the existing connected limb surfaces, rather than floating strips.
    for obj in parts:
        if obj.name.startswith(('Tailored trouser thigh','Lower trouser leg','Upper arm tailored','Forearm undersuit')):
            for v in obj.data.vertices:
                world=obj.matrix_world@v.co
                v.co.y+=.004*math.sin(world.z*69+world.x*17)


def normalize_weights(obj):
    valid={bone.name for bone in rig.data.bones}
    for v in obj.data.vertices:
        values=sorted([(g.group,g.weight) for g in v.groups if g.weight>1e-8 and obj.vertex_groups[g.group].name in valid],key=lambda x:-x[1])[:4]
        if not values:
            raise ValueError('Unweighted vertex: '+obj.name)
        total=sum(w for _,w in values)
        for g in list(v.groups):obj.vertex_groups[g.group].remove([v.index])
        for group,weight in values:obj.vertex_groups[group].add([v.index],weight/total,'REPLACE')


def shoulder_rig():
    # Pauldrons follow the clavicles, allowing the upper arms to rotate under
    # their shells instead of rotating rigid armor into the rifle stock.
    for obj in parts:
        if not obj.name.startswith(('Sculpted shoulder shell','Shoulder sculpted front cap','Shoulder -')):continue
        for side in ('Left','Right'):
            old=obj.vertex_groups.get(side+'Arm')
            if old is None:continue
            obj.vertex_groups.remove(old)
            obj.vertex_groups.new(name=side+'Shoulder').add(list(range(len(obj.data.vertices))),1,'REPLACE')


def unwrap():
    # Preserve a real paintable unwrap on every individually editable part.
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.015)
    bpy.ops.object.mode_set(mode='OBJECT')
    for index,obj in enumerate(parts):
        uv=obj.data.uv_layers.active
        uv.name='SurfaceUV'
        # Disjoint part tiles preserve the editable unwrap after joining the runtime skin.
        grid=math.ceil(math.sqrt(len(parts)))
        low=Vector((min(c.uv.x for c in uv.data),min(c.uv.y for c in uv.data)))
        span=max(max(c.uv.x for c in uv.data)-low.x,max(c.uv.y for c in uv.data)-low.y,1e-8)
        for corner in uv.data:
            p=(corner.uv-low)/span
            corner.uv=((index%grid+.04+p.x*.92)/grid,(index//grid+.04+p.y*.92)/grid)
        normalize_weights(obj)


def runtime_atlas():
    """Rasterize authored surface constants into unique UV islands and PBR maps.

    This is a base-color/material bake, not a claim of a sculpted normal bake.
    Source meshes retain their independent, editable materials.
    """
    size=2048
    maps={key:np.zeros((size,size,4),dtype=np.float32) for key in ('BaseColor','MetallicSmoothness','Emission')}
    mask=np.zeros((size,size),dtype=bool)
    grid=math.ceil(math.sqrt(len(parts)))
    for obj_index,obj in enumerate(parts):
        obj.data.calc_loop_triangles()
        uv=obj.data.uv_layers.active
        # Fill the whole part tile first. Tiny UV islands can be subpixel at export;
        # their base material must never sample an unpainted black background.
        shader=obj.data.materials[0].node_tree.nodes['Principled BSDF']
        color=shader.inputs['Base Color'].default_value[:3]
        encoded=[12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055 for x in color]
        tile=np.s_[int(obj_index//grid*size/grid):math.ceil((obj_index//grid+1)*size/grid),
            int(obj_index%grid*size/grid):math.ceil((obj_index%grid+1)*size/grid)]
        maps['BaseColor'][tile]=(*encoded,1)
        maps['MetallicSmoothness'][tile]=(shader.inputs['Metallic'].default_value,0,0,1-shader.inputs['Roughness'].default_value)
        maps['Emission'][tile]=(*([min(1,x*.6) for x in encoded] if obj.data.materials[0].name.endswith('_accent') else (0,0,0)),1)
        mask[tile]=True
        for tri in obj.data.loop_triangles:
            p=np.array([uv.data[i].uv[:] for i in tri.loops])*size
            lo=np.maximum(0,np.floor(p.min(axis=0)-1).astype(int))
            hi=np.minimum(size-1,np.ceil(p.max(axis=0)+1).astype(int))
            if np.any(hi<lo):continue
            xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
            a,b,c=p
            den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
            if abs(den)<1e-10:continue
            u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den
            v=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den
            inside=(u>=-.01)&(v>=-.01)&(u+v<=1.01)
            shader=obj.data.materials[tri.material_index].node_tree.nodes['Principled BSDF']
            color=shader.inputs['Base Color'].default_value[:3]
            encoded=[12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055 for x in color]
            values=dict(BaseColor=(*encoded,1),MetallicSmoothness=(shader.inputs['Metallic'].default_value,0,0,1-shader.inputs['Roughness'].default_value),
                Emission=(*([min(1,x*.6) for x in encoded] if obj.data.materials[tri.material_index].name.endswith('_accent') else (0,0,0)),1))
            target=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1]
            for key,value in values.items():maps[key][target][inside]=value
            mask[target][inside]=True
    # Three-pixel chart gutters reduce edge bleed without filling neighboring charts.
    for _ in range(3):
        previous=mask.copy()
        for axis,shift in ((0,1),(0,-1),(1,1),(1,-1)):
            source_mask=np.roll(previous,shift,axis=axis)
            fill=source_mask&~mask
            for value in maps.values():value[fill]=np.roll(value,shift,axis=axis)[fill]
            mask[fill]=True
    atlas_mat=material('PlayerPilot runtime surface',(1,1,1),0,.7)
    for key,pixels in maps.items():
        image=bpy.data.images.new('Pilot '+key,width=size,height=size,alpha=True)
        if key=='MetallicSmoothness':image.colorspace_settings.name='Non-Color'
        image.pixels.foreach_set(pixels.ravel())
        image.filepath_raw=str(assets/(key+'.png'));image.file_format='PNG';image.save();image.pack()
        node=atlas_mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
        if key=='BaseColor':atlas_mat.node_tree.links.new(node.outputs['Color'],atlas_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    return atlas_mat


def export_pilot():
    results=[]
    atlas=runtime_atlas()
    for level,ratio in enumerate((1,.57,.27)):
        bpy.ops.object.select_all(action='DESELECT')
        clones=[]
        for original in parts:
            clone=original.copy();clone.data=original.data.copy()
            bpy.context.collection.objects.link(clone);clone.select_set(True);clones.append(clone)
        bpy.context.view_layer.objects.active=clones[0]
        bpy.ops.object.join();obj=bpy.context.object;obj.name='PlayerPilot_LOD'+str(level)
        for poly in obj.data.polygons:poly.material_index=0
        obj.data.materials.clear();obj.data.materials.append(atlas)
        if level:
            dec=obj.modifiers.new('Distance simplification','DECIMATE');dec.ratio=ratio
            bpy.ops.object.modifier_move_up(modifier=dec.name);bpy.ops.object.modifier_apply(modifier=dec.name)
        tri=obj.modifiers.new('Stable export triangles','TRIANGULATE')
        bpy.ops.object.modifier_move_up(modifier=tri.name);bpy.ops.object.modifier_apply(modifier=tri.name)
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_area()<1e-14],context='FACES')
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
        normalize_weights(obj)
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True)
        bpy.context.view_layer.objects.active=rig
        path=assets/('PlayerPilot_LOD%d.fbx'%level)
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},
            use_armature_deform_only=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
        obj.data.calc_loop_triangles()
        results.append(dict(level=level,triangles=len(obj.data.loop_triangles),vertices=len(obj.data.vertices),
            materials=[m.name for m in obj.data.materials]))
        bpy.data.objects.remove(obj,do_unlink=True)
    return results


def setup_studio():
    scene=bpy.context.scene
    scene.render.engine='CYCLES';scene.cycles.samples=20
    scene.cycles.use_denoising=True
    scene.render.threads_mode='FIXED';scene.render.threads=4
    scene.view_settings.view_transform='AgX'
    scene.world=bpy.data.worlds.new('Pilot neutral studio');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
    scene.render.resolution_percentage=100
    for name,loc,power,size in [('Key',(2,-3,4),280,3),('Fill',(-3,-1,2),180,3),('Rim',(1,3,3),350,2)]:
        bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object
        light.name='REVIEW '+name;light.data.energy=power;light.data.shape='DISK';light.data.size=size
        light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add();scene.camera=bpy.context.object
    scene.camera.name='REVIEW Camera';scene.camera.data.type='ORTHO'
    scene.render.image_settings.file_format='PNG'
    scene.render.film_transparent=False


def control_rig():
    """Optional authoring IK controls; deform-only exports keep the shared runtime rig."""
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    unit=max(rig.matrix_world.to_scale())
    inverse_basis=rig.matrix_world.inverted().to_3x3()
    for side,suffix,sign in [('Left','L',1),('Right','R',-1)]:
        for limb,end_name,mid_name in [('hand',side+'Hand',side+'ForeArm'),('foot',side+'Foot',side+'Leg')]:
            end=rig.data.edit_bones[end_name]
            ctrl=rig.data.edit_bones.new('CTRL-'+limb+'.'+suffix)
            ctrl.matrix=end.matrix.copy();ctrl.length=.10/unit;ctrl.use_deform=False
            pole=rig.data.edit_bones.new('CTRL-'+limb+'-pole.'+suffix)
            middle=rig.data.edit_bones[mid_name].head.copy()
            pole.head=middle+inverse_basis@Vector((sign*.12,-.40 if limb=='foot' else .40,0))
            pole.tail=pole.head+inverse_basis@Vector((0,0,.08));pole.use_deform=False
    bpy.ops.object.mode_set(mode='POSE')
    rig['Arm IK']=0.0;rig['Leg IK']=0.0
    for side,suffix in [('Left','L'),('Right','R')]:
        for limb,end,mid,prop in [('hand',side+'Hand',side+'ForeArm','Arm IK'),('foot',side+'Foot',side+'Leg','Leg IK')]:
            ik=rig.pose.bones[mid].constraints.new('IK');ik.name='Optional authoring '+limb+' IK'
            ik.target=rig;ik.subtarget='CTRL-'+limb+'.'+suffix
            ik.pole_target=rig;ik.pole_subtarget='CTRL-'+limb+'-pole.'+suffix
            ik.chain_count=2;ik.influence=0
            driver=ik.driver_add('influence').driver;driver.expression='blend'
            variable=driver.variables.new();variable.name='blend';variable.type='SINGLE_PROP'
            variable.targets[0].id=rig;variable.targets[0].data_path='["'+prop+'"]'
            rotation=rig.pose.bones[end].constraints.new('COPY_ROTATION');rotation.name='Optional control orientation'
            rotation.target=rig;rotation.subtarget='CTRL-'+limb+'.'+suffix;rotation.influence=0
            d=rotation.driver_add('influence').driver;d.expression='blend'
            v=d.variables.new();v.name='blend';v.type='SINGLE_PROP';v.targets[0].id=rig;v.targets[0].data_path='["'+prop+'"]'
    bpy.ops.object.mode_set(mode='OBJECT')


def render_view(name,location,target,scale,width=850,height=1100):
    scene=bpy.context.scene;cam=scene.camera
    cam.location=location;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale
    scene.render.resolution_x=width;scene.render.resolution_y=height
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)


def model():
    global rig,parts,materials
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/ThirdParty/Grab3D/ArmoredContender.blend'),load_ui=False)
    bpy.context.preferences.filepaths.file_preview_type='NONE'
    rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    for obj in list(bpy.data.objects):
        if obj!=rig:bpy.data.objects.remove(obj,do_unlink=True)
    rig.animation_data_clear()
    for bone in rig.pose.bones:bone.matrix_basis.identity()
    parts=[]
    colors=dict(armor=(.68,.61,.47),armorShade=(.40,.36,.29),cloth=(.018,.022,.026),
        scarf=(.019,.079,.095),skin=(.46,.245,.126),skinShade=(.24,.099,.047),
        hair=(.018,.012,.010),hairLight=(.032,.021,.017),accent=(.012,.55,.72),
        metal=(.27,.28,.26),rubber=(.008,.011,.013),eye=(.76,.72,.63),
        iris=(.075,.042,.015),lip=(.29,.125,.064),stitch=(.42,.37,.28),white=(.95,.95,.95))
    materials={key:material('Pilot_'+key,value,.65 if key=='metal' else 0,
        .30 if key in ('armor','armorShade') else .56 if key=='skin' else .42 if key in ('eye','iris') else .79)
        for key,value in colors.items()}
    design=DESIGNS['Player']
    torso(design);limbs(design)
    # Keep the belt lower, matching the approved overall proportion.
    for obj in parts:
        if obj.name.startswith(('Utility belt','Belt buckle','Buckle pin','Belt utility pouch','Pouch flap','Pouch clasp')):
            obj.location.z-=.045
    armor_master();detail_master();face_master();hair_master();shoulder_rig()
    bpy.context.scene['Pilot face revision']=2
    bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
    # Name semantic authoring collections and keep every source part editable.
    for obj in parts:
        label='Head and hair' if obj.vertex_groups.get('Head') else 'Suit armor and equipment'
        collection=bpy.data.collections.get(label) or bpy.data.collections.new(label)
        if collection.name not in bpy.context.scene.collection.children:
            bpy.context.scene.collection.children.link(collection)
        for old in list(obj.users_collection):old.objects.unlink(obj)
        collection.objects.link(obj)
    unwrap()
    for path in sorted(refs.glob('Player-*.png')):
        bpy.ops.object.empty_add(type='IMAGE');empty=bpy.context.object
        empty.name='REF '+path.stem;empty.data=bpy.data.images.load(str(path));empty.data.pack()
        empty.hide_render=True;empty.hide_viewport=True
    rig.name='Player pilot deformation rig';rig.show_in_front=True
    lods=export_pilot()
    control_rig()
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Player-Pilot.blend'))
    manifest=dict(sourceAuthority='Approved-Concept.png; Final player sheets expand construction for review',
        references=[dict(file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
            for p in [refs/'Approved-Concept.png',*sorted(refs.glob('Player-*.png'))]],
        methods=['reference-first proportions','single editable master','named contact anchors in Unity',
            'six-side and underside inspection','connected facial volume','separate material responses',
            'normalized four-influence skin','shared-rig deformation and contact sampling'],
        decisions=['Retain existing metre-scale skeleton and its gameplay-compatible proportions.',
            'Concept drawings are appearance guides, not pixel-calibrated projections.',
            'Boot outer cyan ankle inset is on anatomical right only; sole inserts are separate.',
            'Rig and reference extensions remain subject to creator review.'],
        editableParts=len(parts),bones=len(rig.data.bones),lods=lods,linearMaterials=colors,
        currentPlayableAssetsReplaced=False)
    write_json(out/'Model.json',manifest)
    setup_studio()
    render_view('Blockout-Front',(0,-5,1),(0,0,.95),2.10)
    render_view('Blockout-Side',(5,0,1),(0,0,.95),2.10)
    render_view('Player-Studio',(2,-5,1.8),(0,0,.96),2.12)
    render_view('Player-Face',(.40,-2,1.77),(0,-.01,1.706),.40,1000,1000)
    print('PLAYER_PILOT_MODEL_COMPLETE',json.dumps(lods))


def render_all():
    bpy.ops.wm.open_mainfile(filepath=str(out/'Player-Pilot.blend'),load_ui=False)
    setup_studio()
    render_view('Player-Studio',(2,-5,1.8),(0,0,.96),2.12)
    render_view('Player-Face',(.40,-2,1.77),(0,-.01,1.706),.40,1000,1000)
    for label,angle in [('Front',0),('FrontRight',-45),('Right',-90),('BackRight',-135),
        ('Back',180),('BackLeft',135),('Left',90),('FrontLeft',45)]:
        a=math.radians(angle)
        render_view('Turn-'+label,(5*math.sin(a),-5*math.cos(a),.96),(0,0,.96),2.08)
    render_view('Top',(0,0,5),(0,0,.96),1.20,1000,1000)
    render_view('Underside',(0,-.01,-4),(0,0,.8),1.20,1000,1000)
    render_view('Head-Front',(0,-3,1.72),(0,0,1.72),.40,1000,1000)
    render_view('Head-Right',(-3,0,1.72),(0,0,1.72),.40,1000,1000)
    render_view('Head-Back',(0,3,1.72),(0,0,1.72),.40,1000,1000)
    render_view('Head-Crown',(0,.01,4),(0,.01,1.72),.40,1000,1000)
    render_view('Head-UnderJaw',(0,-1.5,.7),(0,0,1.72),.42,1000,1000)
    underside_light()
    render_view('Underside',(0,-.01,-4),(0,0,.8),1.20,1000,1000)
    render_view('Boot-Sole',(-.12,-.02,-2),(-.12,-.02,.10),.36,1000,1000)


def underside_light():
    bpy.ops.object.light_add(type='AREA',location=(.4,-.5,-.8))
    lamp=bpy.context.object;lamp.name='REVIEW sole inspection fill'
    lamp.data.energy=180;lamp.data.size=1.2
    lamp.rotation_euler=(Vector((0,0,.10))-lamp.location).to_track_quat('-Z','Y').to_euler()


def audit():
    bpy.ops.wm.open_mainfile(filepath=str(out/'Player-Pilot.blend'),load_ui=False)
    arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    bpy.context.view_layer.objects.active=arm
    bpy.ops.object.mode_set(mode='EDIT')
    unit=max(arm.matrix_world.to_scale())
    inverse_basis=arm.matrix_world.inverted().to_3x3()
    for side,suffix,sign in [('Left','L',1),('Right','R',-1)]:
        for limb,end,mid in [('hand',side+'Hand',side+'ForeArm'),('foot',side+'Foot',side+'Leg')]:
            ctrl=arm.data.edit_bones['CTRL-'+limb+'.'+suffix]
            ctrl.length=.10/unit
            pole=arm.data.edit_bones['CTRL-'+limb+'-pole.'+suffix]
            pole.head=arm.data.edit_bones[mid].head+inverse_basis@Vector((sign*.12,-.4 if limb=='foot' else .4,0))
            pole.tail=pole.head+inverse_basis@Vector((0,0,.08))
    bpy.ops.object.mode_set(mode='OBJECT')
    arm['Arm IK']=1.0;arm['Leg IK']=1.0;arm.update_tag();bpy.context.view_layer.update()
    controls=[]
    for side,suffix in [('Left','L'),('Right','R')]:
        for limb,end in [('hand',side+'Hand'),('foot',side+'Foot')]:
            ctrl=arm.pose.bones['CTRL-'+limb+'.'+suffix]
            distance=(arm.matrix_world@(arm.pose.bones[end].head-ctrl.head)).length
            controls.append(dict(control=ctrl.name,targetErrorMetres=distance))
            assert distance<.004,'Authoring IK target outside four millimetres'
    arm['Arm IK']=0.0;arm['Leg IK']=0.0;arm.update_tag()
    for bone in arm.pose.bones:bone.matrix_basis.identity()
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Player-Pilot.blend'))
    records=[]
    for level in range(3):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(assets/('PlayerPilot_LOD%d.fbx'%level)))
        skin=next(o for o in bpy.data.objects if o.type=='MESH')
        arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
        skin.data.calc_loop_triangles()
        assert all(math.isfinite(c) for v in skin.data.vertices for c in v.co)
        assert all(t.area>1e-16 for t in skin.data.loop_triangles)
        assert skin.data.uv_layers.active is not None
        max_error=0
        for v in skin.data.vertices:
            weights=[g.weight for g in v.groups if g.weight>1e-8]
            assert 1<=len(weights)<=4
            max_error=max(max_error,abs(sum(weights)-1))
        assert max_error<.001
        fingers=[s+'Hand'+f+str(i) for s in ('Left','Right') for f in ('Thumb','Index','Middle','Ring','Pinky') for i in (1,2,3)]
        assert all(n in arm.data.bones for n in fingers)
        points=[skin.matrix_world@v.co for v in skin.data.vertices]
        bounds=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
        assert 1.7<bounds[2]<2.05
        records.append(dict(level=level,triangles=len(skin.data.loop_triangles),vertices=len(skin.data.vertices),
            bones=len(arm.data.bones),fingerBones=len(fingers),boundsMetres=bounds,maximumWeightError=max_error))
    assert records[0]['triangles']>records[1]['triangles']>records[2]['triangles']
    write_json(out/'Geometry-Validation.json',dict(passed=True,lods=records,authoringControls=controls,
        scope='Finite geometry, nondegenerate triangles, UV presence, skin normalization, scale, finger skeleton and LOD reduction. Artistic/deformation acceptance is separate.'))
    print('PLAYER_PILOT_AUDIT_PASSED',json.dumps(records))


def fit_source():
    """Bring the measured Unity pose back into a separate editable Blender assembly."""
    from mathutils import Matrix
    bpy.ops.wm.open_mainfile(filepath=str(out/'Player-Pilot.blend'),load_ui=False)
    arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    data=json.loads((out/'Fitted-Pose.json').read_text())
    def matrix(value):
        prefix='m' if 'm00' in value else 'e'
        return Matrix([[value[prefix+str(row)+str(col)] for col in range(4)] for row in range(4)])
    frames=[f for f in data['bones'] if f['name'] in arm.data.bones]
    # Finger rest rotations were recalibrated for the carbine in Unity; use only
    # the unchanged body skeleton to register the shared coordinate systems.
    registered=[f for f in frames if not any(x in f['name'] for x in ('Thumb','Index','Middle','Ring','Pinky'))]
    src=np.array([list(arm.matrix_world@arm.data.bones[f['name']].head_local)+[1] for f in registered])
    dst=np.array([list(matrix(f['rest']).translation) for f in registered])
    solution=np.linalg.lstsq(src,dst,rcond=None)[0]
    conversion=Matrix(np.vstack((solution.T,[0,0,0,1])))
    residual=float(np.max(np.linalg.norm(src@solution-dst,axis=1)))
    write_json(out/'Source-Registration.json',dict(maximumBodyErrorMetres=residual,
        bones=[dict(name=f['name'],errorMetres=float(error)) for f,error in zip(registered,np.linalg.norm(src@solution-dst,axis=1))]))
    assert residual<.001, 'Blender/Unity source registration exceeds one millimetre: '+str(residual)
    arm.animation_data_clear()
    for p in arm.pose.bones:p.matrix_basis.identity()
    lookup={f['name']:f for f in frames}
    def depth(b):
        n=0
        while b.parent:n+=1;b=b.parent
        return n
    for bone in sorted(arm.data.bones,key=depth):
        if bone.name not in lookup:continue
        frame=lookup[bone.name]
        desired=conversion.inverted()@matrix(frame['posed'])@matrix(frame['rest']).inverted()@conversion@arm.matrix_world@bone.matrix_local
        arm.pose.bones[bone.name].matrix=arm.matrix_world.inverted()@desired
        bpy.context.view_layer.update()
    for p in arm.pose.bones:
        p.keyframe_insert(data_path='location',frame=1)
        p.keyframe_insert(data_path='rotation_quaternion' if p.rotation_mode=='QUATERNION' else 'rotation_euler',frame=1)
        p.keyframe_insert(data_path='scale',frame=1)
    if arm.animation_data and arm.animation_data.action:arm.animation_data.action.name='Carbine fitted idle - measured Unity pose'
    carbine=root/'ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend'
    with bpy.data.libraries.load(str(carbine),link=False) as (available,loaded):
        loaded.objects=list(available.objects)
    collection=bpy.data.collections.new('Production carbine - editable fitted assembly')
    bpy.context.scene.collection.children.link(collection)
    # Carbine master X-forward to Unity Z-forward, then Unity to character source space.
    weapon_to_unity=Matrix(((0,-1,0,0),(0,0,1,0),(1,0,0,0),(0,0,0,1)))
    assembly=conversion.inverted()@matrix(data['weapon'])@weapon_to_unity
    linked=[]
    for obj in loaded.objects:
        if obj is None or not (obj.type=='MESH' or obj.name in ('Muzzle','RightGrip','LeftSupport','Aim','Shoulder')):continue
        collection.objects.link(obj)
        obj.hide_render=False;obj.hide_viewport=False
        linked.append(obj)
    # Library-loaded objects need a dependency update before their world matrices
    # can be used. Snapshot them together so transforming a parent cannot affect
    # a later child's source matrix.
    bpy.context.view_layer.update()
    original_matrices={obj:obj.matrix_world.copy() for obj in linked}
    for obj in linked:
        obj.parent=None
        obj.matrix_world=assembly@original_matrices[obj]
        override={'RightGrip':'rightGrip','LeftSupport':'leftGrip','Shoulder':'shoulder'}.get(obj.name)
        if override and override in data:obj.matrix_world=conversion.inverted()@matrix(data[override])
    bpy.context.view_layer.update()
    expected=json.loads((carbine.parent/'Production.json').read_text())['editableParts']
    assert sum(o.type=='MESH' for o in linked)==expected,'Editable carbine parts missing'
    weapon_points=[assembly.inverted()@obj.matrix_world@v.co for obj in linked if obj.type=='MESH' for v in obj.data.vertices]
    weapon_bounds=[max(p[i] for p in weapon_points)-min(p[i] for p in weapon_points) for i in range(3)]
    assert .90<weapon_bounds[0]<1.10,'Carbine source-space length was not preserved'
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Player-Pilot-Carbine-Fit.blend'))
    setup_studio()
    render_view('Blender-Carbine-Fit',(3.8,-4,1.8),(0,-.10,1.02),2.20,1100,1100)
    write_json(out/'Blender-Fit-Validation.json',dict(registrationMaximumErrorMetres=residual,
        measuredUnityBones=len(frames),carbineObjects=len(linked),editableCarbineMeshes=expected,carbineSourceBoundsMetres=weapon_bounds,
        source='Unity Fitted-Pose.json; no hand offsets invented in Blender',
        scope='Shared rest-frame registration and imported fitted pose. Fine contacts require visual review.'))


if options.stage=='model':model()
elif options.stage=='render':render_all()
elif options.stage=='audit':audit()
elif options.stage=='fit':fit_source()
elif options.stage=='details':
    bpy.ops.wm.open_mainfile(filepath=str(out/'Player-Pilot.blend'),load_ui=False)
    setup_studio();underside_light()
    render_view('Underside',(0,-.01,-4),(0,0,.8),1.20,1000,1000)
    render_view('Boot-Sole',(-.12,-.02,-2),(-.12,-.02,.10),.36,1000,1000)
else:
    bpy.ops.wm.open_mainfile(filepath=str(out/'Player-Pilot.blend'),load_ui=False)
    parts=[o for o in bpy.data.objects if o.type=='MESH']
    # Tile order is defined by the stored part UVs, independent of datablock order.
    grid=math.ceil(math.sqrt(len(parts)))
    parts.sort(key=lambda o: int(min(c.uv.y for c in o.data.uv_layers.active.data)*grid)*grid+
        int(min(c.uv.x for c in o.data.uv_layers.active.data)*grid))
    if options.stage=='rigfix':
        rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
        if bpy.context.scene.get('Pilot face revision',1)<2:
            for obj in parts:
                if obj.name.startswith('Head - connected'):
                    for vertex in obj.data.vertices:
                        x,y,z=vertex.co
                        if y<.015 and z>1.606:
                            old=float(np.interp(z,[1.670,1.681,1.694,1.707,1.718,1.729,1.740],[0,.009,.033,.023,.014,.006,0]))
                            new=float(np.interp(z,[1.670,1.681,1.694,1.707,1.718,1.729,1.740],[0,.006,.020,.018,.010,.004,0]))
                            vertex.co.y+=(old-new)*max(0,1-abs(x)/.015)
                if obj.name.startswith('Nostril aperture'):obj.location.y+=.024
                if obj.name.startswith(('Eye -','Warm brown iris','Pupil','Catchlight','Anatomical upper eyelid','Anatomical lower eyelid')):obj.location.y+=.008
                if obj.name.startswith(('Defined upper lip','Lower lip plane')):
                    for v in obj.data.vertices:v.co.x*=.88;v.co.y+=.004
            bpy.context.scene['Pilot face revision']=2
        shoulder_rig();updated_lods=export_pilot()
        record=json.loads((out/'Model.json').read_text())
        record['lods']=updated_lods;record['faceRevision']=2
        record['shoulderBinding']='Rigid pauldrons follow clavicles; upper arms articulate beneath them'
        write_json(out/'Model.json',record)
    else:runtime_atlas()
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'Player-Pilot.blend'))
