"""Construct original summit paving, underside, cover and column modules from collision geometry."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder = root/'ArtSource/Original/ArtPass-v1/Environment/SummitStone'
exports = root/'RailArena/Assets/RailArena/Art/ArtPass/Environment/SummitStone'
exports.mkdir(parents=True, exist_ok=True)
source = folder/'summit-source.json'
audit = json.loads(source.read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = [('DeckIvory',(.87,.855,.76,1)), ('DeckStone',(.70,.715,.65,1)),
           ('DeckTeal',(.065,.255,.24,1)), ('DeckBronze',(.58,.37,.16,1)), ('DeckShade',(.22,.30,.28,1))]
paint = bpy.data.images.load(str(folder.parent/'BasementDecks/Paving-Painted.png'))
paint.pack()
materials = []
for name,color in palette:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = .75
    if name in ('DeckIvory','DeckStone'):
        image = material.node_tree.nodes.new('ShaderNodeTexImage')
        image.image = paint
        material.node_tree.links.new(image.outputs['Color'],shader.inputs['Base Color'])
    materials.append(material)
records = []
for profile in audit['profiles']:
    name = profile['name']
    height = profile['thicknessMetres']
    radius = profile['radiusMetres']
    vertices = profile['collision']['verticesUnityMetres']
    points = {(round(point['x'],6),round(-point['z'],6)) for point in vertices if abs(point['y'])<.00001}
    source_hull = sorted([Vector(point) for point in points], key=lambda point:math.atan2(point.y,point.x))
    assert len(source_hull) >= 4, (name,len(source_hull))
    # Anchor simplification at +X so every cylindrical LOD retains the four exact cardinal extrema.
    start = max(range(len(source_hull)),key=lambda index:(source_hull[index].x,-abs(source_hull[index].y)))
    source_hull = source_hull[start:]+source_hull[:start]
    for lod in range(3):
        stride = (1,2,6)[lod] if name=='Summit' else (1,2,3)[lod] if name=='Column' else 1
        hull = source_hull[::stride]
        collection = bpy.data.collections.new(name+'_LOD'+str(lod))
        scene.collection.children.link(collection)
        parts = []

        def prism(label, polygon, top, bottom, slot):
            count = len(polygon)
            mesh = bpy.data.meshes.new(label)
            positions = [(p.x,p.y,top) for p in polygon]+[(p.x,p.y,bottom) for p in polygon]
            faces = [tuple(range(count)),tuple(reversed(range(count,count*2)))]+[(i,i+count,(i+1)%count+count,(i+1)%count) for i in range(count)]
            mesh.from_pydata(positions,[],faces)
            mesh.update()
            obj = bpy.data.objects.new(label,mesh)
            collection.objects.link(obj)
            mesh.materials.append(materials[slot])
            uv = mesh.uv_layers.new(name='Metres / four metre paint period')
            for polygon in mesh.polygons:
                for index in polygon.loop_indices:
                    point = mesh.vertices[mesh.loops[index].vertex_index].co
                    uv.data[index].uv = (point.x/4,point.y/4) if abs(polygon.normal.z)>.5 else ((point.y if abs(polygon.normal.x)>.5 else point.x)/4,point.z/4)
            obj['construction_layer'] = label
            parts.append(obj)
            return obj

        def solid(label, scale, top, bottom, slot):
            return prism(label,[point*scale for point in hull],top,bottom,slot)

        def ring(label, inner, outer, top, bottom, slot, step=1, joints=False):
            for i in range(0,len(hull),step):
                a,b = hull[i],hull[(i+step)%len(hull)]
                gap = min(.1,.006/max((b-a).length*outer,.01)) if joints else 0
                a,b = a.lerp(b,gap),a.lerp(b,1-gap)
                polygon = [a*inner,a*outer,b*outer,b*inner]
                prism(label+'_'+str(i),polygon,top,bottom,slot)

        solid('Recessed structural core',.80 if name=='Column' else .975,-.03,-height+.035,4)
        if name == 'Summit':
            # Exact original 96-sided outer rim; inset joints cannot affect a jump takeoff.
            solid('Central ivory seal',.19,-.004,-.10,0)
            ring('Seal teal border',.19,.205,0,-.07,2)
            for index in range(8 if lod<2 else 4):
                angle = index*math.tau/(8 if lod<2 else 4)
                direction = Vector((math.cos(angle),math.sin(angle)))
                side = Vector((-direction.y,direction.x))
                reach = radius*.155
                prism('Compass petal_'+str(index),[direction*reach,direction*reach*.55+side*.13,Vector((0,0)),direction*reach*.55-side*.13],0,-.025,2 if index%2==0 else 3)
            step = max(1,len(hull)//24)
            ring('Radial paving inner',.21,.58,0,-.11,0,step,lod<2)
            ring('Radial paving outer',.585,.935,0,-.11,0,step,lod<2)
            ring('Bronze boundary',.938,.943,0,-.035,3,step)
            ring('Coping',.946,1,0,-.13,0,1,lod==0)
            solid('Stone drum',.985,-.13,-.44,1)
            ring('Teal frieze',.95,.99,-.20,-.36,2,4)
            ring('Bronze string',.95,.995,-.16,-.185,3,4)
            ring('Lower cornice',.85,1,-.43,-height,0,1)
            solid('Underside coffer bed',.85,-.46,-height+.035,4)
            ring('Underside central seal',.12,.20,-height+.03,-height,3,4)
            if lod<2:
                for index in range(0,len(hull),4 if lod==0 else 8):
                    a,b = hull[index],hull[(index+1)%len(hull)]
                    prism('Underside ray_'+str(index),[a*.21,a*.83,b*.83,b*.21],-height+.10,-height,0)
        elif name == 'Column':
            solid('Foot plinth',1,-height+.16,-height,0)
            solid('Teal foot band',.97,-height+.22,-height+.16,2)
            solid('Lower collar',.94,-height+.32,-height+.22,0)
            solid('Faceted shaft',.91,-.30,-height+.32,0)
            solid('Neck bronze ring',.95,-.24,-.30,3)
            solid('Teal capital',.98,-.12,-.24,2)
            solid('Capital coping',1,0,-.12,0)
            if lod<2:
                # Shallow inset strips on alternate facets; no extra width around the shaft.
                for i in range(0,len(hull),2 if lod==0 else 4):
                    a,b = hull[i],hull[(i+1)%len(hull)]
                    left,right = a.lerp(b,.40),a.lerp(b,.60)
                    prism('Shaft flute_'+str(i),[left*.91,left*.915,right*.915,right*.91],-.35,-height+.38,1)
        else:
            solid('Cover top coping',1,0,-.10,0)
            solid('Cover lower coping',1,-height+.10,-height,0)
            solid('Cover stone body',.99,-.10,-height+.10,0)
            ring('Cover teal frieze',.975,.995,-.18,-.32,2)
            if lod<2:
                ring('Cover bronze string',.975,.997,-.13,-.16,3)
            if lod==0:
                ring('Cover lower trim',.975,.995,-height+.22,-height+.18,1)
        bpy.context.view_layer.update()
        construction = []
        for obj in parts:
            for vertex in obj.data.vertices:
                point = Vector((vertex.co.x,vertex.co.y))
                assert -height-.00001 <= vertex.co.z <= .00001,(name,obj.name,vertex.co.z)
                for i in range(len(source_hull)):
                    edge,delta = source_hull[(i+1)%len(source_hull)]-source_hull[i],point-source_hull[i]
                    assert edge.x*delta.y-edge.y*delta.x >= -.00001,(name,obj.name,'outside collision hull')
            construction.append({'name':obj.name,'materialSlots':[material.name for material in obj.data.materials], 'minimum':[min(vertex.co[i] for vertex in obj.data.vertices) for i in range(3)],'maximum':[max(vertex.co[i] for vertex in obj.data.vertices) for i in range(3)]})
        bpy.ops.object.select_all(action='DESELECT')
        duplicates = []
        for obj in parts:
            duplicate = obj.copy()
            duplicate.data = obj.data.copy()
            collection.objects.link(duplicate)
            duplicate.select_set(True)
            duplicates.append(duplicate)
        bpy.context.view_layer.objects.active = duplicates[0]
        bpy.ops.object.join()
        visual = bpy.context.object
        visual.name = name+'_LOD'+str(lod)
        scene.cursor.location = (0,0,0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        # Material slots may have duplicates after joining; Unity maps by stable names.
        if lod == 2:
            modifier = visual.modifiers.new('Distant planar reduction','DECIMATE')
            modifier.decimate_type = 'DISSOLVE'
            modifier.angle_limit = .01
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        visual.data.calc_loop_triangles()
        path = exports/(name+'-LOD'+str(lod)+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,add_leaf_bones=False)
        records.append({'profile':name,'lod':lod,'export':str(path.relative_to(root)),'exportSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'triangles':len(visual.data.loop_triangles),'topPivotUnityMetres':[0,0,0],'radiusMetres':radius,'thicknessMetres':height,'collisionGeometrySha256':profile['geometrySha256'],'construction':construction})
        bpy.data.objects.remove(visual,do_unlink=True)
        collection.hide_render = collection.hide_viewport = collection.name!='Summit_LOD0'
master = folder/'Summit-Stone-v1.blend'
scene['axes'] = audit['axes']
scene['sourceSha256'] = hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.save_as_mainfile(filepath=str(master))
(folder/'production.json').write_text(json.dumps({'id':'SG-SUMMIT-STONE-v1','status':'Authored candidate; import and installed review pending','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'source':'summit-source.json','sourceSha256':scene['sourceSha256'],'axes':audit['axes'],'provenance':'Original Blender summit compass, radial stone paving, cornices, coffer rays, cover and column construction; existing original basement painted stone image','collision':'No added colliders; every vertex checked inside each audited convex source hull and original vertical envelope. Original summit deck, cover and columns remain authoritative.','materialSlots':[name for name,_ in palette],'lods':records},indent=2)+'\n',encoding='utf-8')
print('SUMMIT_STONE_EXPORTED',[(entry['profile'],entry['lod'],entry['triangles']) for entry in records])
