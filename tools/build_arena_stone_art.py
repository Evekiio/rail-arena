"""Author the complete measured deck/ramp family inside its installed collision hulls."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
import bmesh
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder = root/'ArtSource/Original/ArtPass-v1/Environment/ArenaStone'
exports = root/'RailArena/Assets/RailArena/Art/ArtPass/Environment/ArenaStone'
exports.mkdir(parents=True, exist_ok=True)
source_path = folder/'arena-source.json'
source = json.loads(source_path.read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
materials = []
paint = bpy.data.images.load(str(folder.parent/'BasementDecks/Paving-Painted.png'))
paint.pack()
for name, color in [('Ivory', (.87, .855, .76, 1)), ('Stone', (.66, .70, .64, 1)),
                    ('Teal', (.065, .255, .24, 1)), ('Bronze', (.58, .37, .16, 1)), ('Shade', (.19, .26, .25, 1))]:
    material = bpy.data.materials.new('Arena'+name)
    material.diffuse_color = color
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = .75
    if name in ('Ivory', 'Stone'):
        texture = material.node_tree.nodes.new('ShaderNodeTexImage')
        texture.image = paint
        material.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    materials.append(material)

families = list(dict.fromkeys(part['family'] for part in source['parts']))
records = []
collision_refs = bpy.data.collections.new('SOURCE_COLLISION_REFERENCES_DO_NOT_EXPORT')
scene.collection.children.link(collision_refs)
collision_refs.hide_render = True
collision_refs.hide_viewport = True
for family in families:
    measured = next(part for part in source['parts'] if part['family'] == family)
    width, height, depth = (measured['size'][axis] for axis in ('x', 'y', 'z'))
    hull = measured['canonicalCollision']
    vertices = [(p['x'], -p['z'], p['y']) for p in hull['vertices']]
    mesh = bpy.data.meshes.new(family+' exact source hull')
    mesh.from_pydata(vertices, [], [hull['triangles'][i:i+3] for i in range(0, len(hull['triangles']), 3)])
    reference = bpy.data.objects.new(family+' collision authority', mesh)
    collision_refs.objects.link(reference)
    reference['source_asset_guid'] = measured['collisionGuid']
    reference['source_asset'] = measured['collisionAsset']
    # The three ramp intervals are derived from the installed hull, including its level landing.
    xs = sorted(set(round(point[0], 5) for point in vertices))
    ramp_profile = [(x, min(point[2] for point in vertices if abs(point[0]-x)<.00002),
                       max(point[2] for point in vertices if abs(point[0]-x)<.00002)) for x in xs] if family == 'Ramp' else []

    def floor_at(x, upper=True):
        for left, right in zip(ramp_profile, ramp_profile[1:]):
            if x <= right[0]+.00001:
                t = max(0, min(1, (x-left[0])/(right[0]-left[0])))
                index = 2 if upper else 1
                return left[index]*(1-t)+right[index]*t
        return ramp_profile[-1][2 if upper else 1]

    for lod in range(3):
        collection = bpy.data.collections.new(family+'_LOD'+str(lod))
        scene.collection.children.link(collection)
        parts = []

        def finish(obj, name, slot):
            obj.name = name
            for parent in list(obj.users_collection):
                parent.objects.unlink(obj)
            collection.objects.link(obj)
            obj.data.materials.append(materials[slot])
            uv = obj.data.uv_layers.active or obj.data.uv_layers.new(name='Metre paving')
            for polygon in obj.data.polygons:
                for index in polygon.loop_indices:
                    point = obj.data.vertices[obj.data.loops[index].vertex_index].co+obj.location
                    uv.data[index].uv = (point.x/4, point.y/4) if abs(polygon.normal.z)>.5 else ((point.y if abs(polygon.normal.x)>.5 else point.x)/4, point.z/4)
            obj['construction_layer'] = name.split('_')[0]
            parts.append(obj)
            obj.select_set(False)
            return obj

        def block(name, center, size, slot, bevel=0):
            w,d,h = (value/2 for value in size)
            points = [(-w,-d,-h),(w,-d,-h),(w,d,-h),(-w,d,-h),(-w,-d,h),(w,-d,h),(w,d,h),(-w,d,h)]
            faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
            if lod>0 and name.startswith('Paving'):
                points=points[4:];faces=[(0,1,2,3)]
            elif lod==2 and name.startswith('Bronze'):
                indices = (1,2,6,5) if name.startswith('Bronze_X') and center[0]>0 else (3,0,4,7) if name.startswith('Bronze_X') else (2,3,7,6) if center[1]>0 else (0,1,5,4)
                points=[points[index] for index in indices];faces=[(0,1,2,3)]
            data = bpy.data.meshes.new(name)
            data.from_pydata(points,[],faces)
            if bevel and lod==0:
                edit=bmesh.new()
                try:
                    edit.from_mesh(data)
                    bmesh.ops.bevel(edit,geom=list(edit.edges),offset=bevel,segments=1,affect='EDGES',clamp_overlap=True)
                    edit.normal_update();edit.to_mesh(data)
                finally:edit.free()
            data.update()
            obj=bpy.data.objects.new(name,data);collection.objects.link(obj);obj.location=center
            return finish(obj,name,slot)

        def ramp_piece(name, a, b, low_y, high_y, lower_offset, upper_offset, slot):
            bottom_a, bottom_b = floor_at(a, False)+lower_offset, floor_at(b, False)+lower_offset
            top_a, top_b = floor_at(a)+upper_offset, floor_at(b)+upper_offset
            points = [(a,low_y,bottom_a),(b,low_y,bottom_b),(b,high_y,bottom_b),(a,high_y,bottom_a),
                      (a,low_y,top_a),(b,low_y,top_b),(b,high_y,top_b),(a,high_y,top_a)]
            data = bpy.data.meshes.new(name)
            faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
            if lod>0 and name.startswith('Paving'):
                points=points[4:];faces=[(0,1,2,3)]
            elif lod==2 and name.startswith('Bronze'):
                indices=(2,3,7,6) if high_y>0 else (0,1,5,4)
                points=[points[index] for index in indices];faces=[(0,1,2,3)]
            data.from_pydata(points, [], faces)
            data.update()
            obj = bpy.data.objects.new(name, data)
            collection.objects.link(obj)
            return finish(obj, name, slot)

        if family != 'Ramp':
            top, bottom = height/2, -height/2
            block('Core_joint_bed', (0,0,0), (width-.03,depth-.03,height-.10), 4)
            # Material regions stay fixed in metre space. Distant levels simplify depth, not layout.
            nx = max(1, math.ceil(width-.36))
            ny = max(1, math.ceil(depth-.36))
            for x in range(nx):
                for y in range(ny):
                    w, d = (width-.36)/nx, (depth-.36)/ny
                    block(f'Paving_{x:02d}_{y:02d}', (-width/2+.18+(x+.5)*w,-depth/2+.18+(y+.5)*d,top-.07),
                          (w-(.012 if lod<2 else 0),d-(.012 if lod<2 else 0),.14), 1 if (x*3+y*5)%11==7 else 0, .004)
            for sign in (-1,1):
                block('Coping_Y_'+str(sign), (0,sign*(depth/2-.09),top-.055), (width,.18,.11), 0, .006)
                block('Coping_X_'+str(sign), (sign*(width/2-.09),0,top-.055), (.18,depth-.36,.11), 0, .006)
                block('Frieze_Y_'+str(sign), (0,sign*(depth/2-.008),-.035), (width-.08,.016,.12), 2)
                block('Frieze_X_'+str(sign), (sign*(width/2-.008),0,-.035), (.016,depth-.08,.12), 2)
                block('Bronze_Y_'+str(sign), (0,sign*(depth/2-.006),.055), (width-.08,.012,.024), 3)
                block('Bronze_X_'+str(sign), (sign*(width/2-.006),0,.055), (.012,depth-.08,.024), 3)
                block('Underside_Y_'+str(sign), (0,sign*(depth/2-.07),bottom+.04), (width,.14,.08), 0)
                block('Underside_X_'+str(sign), (sign*(width/2-.07),0,bottom+.04), (.14,depth-.28,.08), 0)
            if lod<2:
                axis = 0 if width>depth else 1
                long_side = width if axis==0 else depth
                for index in range(max(2, math.ceil(long_side/2))):
                    count = max(2, math.ceil(long_side/2))
                    location = -long_side/2+.35+index*(long_side-.7)/(count-1)
                    block('Coffer_'+str(index), (location,0,bottom+.055) if axis==0 else (0,location,bottom+.055),
                          (.10,depth-.32,.11) if axis==0 else (width-.32,.10,.11), 1, .003)
        else:
            for segment,(left,right) in enumerate(zip(ramp_profile,ramp_profile[1:])):
                a,b = left[0],right[0]
                thickness = min(left[2]-left[1],right[2]-right[1])
                ramp_piece('Core_'+str(segment),a,b,-depth/2+.01,depth/2-.01,.04,-.04,4)
                for sign in (-1,1):
                    lo,hi = (depth/2-.18,depth/2) if sign>0 else (-depth/2,-depth/2+.18)
                    ramp_piece(f'Coping_{segment}_{sign}',a,b,lo,hi,thickness-.09,0,0)
                    lo,hi = (depth/2-.012,depth/2) if sign>0 else (-depth/2,-depth/2+.012)
                    ramp_piece(f'Frieze_{segment}_{sign}',a,b,lo,hi,thickness-.23,-.14,2)
                    ramp_piece(f'Bronze_{segment}_{sign}',a,b,lo,hi,thickness-.12,-.10,3)
                    lo,hi = (depth/2-.14,depth/2) if sign>0 else (-depth/2,-depth/2+.14)
                    ramp_piece(f'Underside_{segment}_{sign}',a,b,lo,hi,0,-thickness+.07,0)
                flat = abs(right[2]-left[2])<.00001
                nx = 3 if flat else math.ceil(b-a)
                ny = 3 if flat else 4
                for x in range(nx):
                    for y in range(ny):
                        gap=.006 if lod<2 else 0
                        x0,x1 = a+x*(b-a)/nx+gap,a+(x+1)*(b-a)/nx-gap
                        y0,y1 = -depth/2+.18+y*(depth-.36)/ny+gap,-depth/2+.18+(y+1)*(depth-.36)/ny-gap
                        arrow = flat and x0<-.45 and x1>.45 and y0<-.4 and y1>.4
                        ramp_piece(f'Paving_{segment}_{x}_{y}',x0,x1,y0,y1,thickness-.08,-.02 if arrow else 0,1 if (x+y)%7==5 else 0)
                if flat:
                    # The route arrow is inset into a lowered central stone, not a coplanar decal.
                    outline = [(-.45,-.26),(.04,-.26),(.04,-.40),(.45,0),(.04,.40),(.04,.26),(-.45,.26)]
                    z = floor_at(0)
                    data = bpy.data.meshes.new('Inset directional arrow')
                    data.from_pydata([(x,y,z) for x,y in outline], [], [tuple(range(len(outline)))])
                    data.update()
                    obj = bpy.data.objects.new('Wayfinding_inset_arrow',data)
                    collection.objects.link(obj)
                    finish(obj,'Wayfinding_inset_arrow',2)
        bpy.context.view_layer.update()
        construction = []
        for obj in parts:
            points = [obj.matrix_world@vertex.co for vertex in obj.data.vertices]
            for point in points:
                assert abs(point.x)<=width/2+.00002 and abs(point.y)<=depth/2+.00002, (family,obj.name,tuple(point))
                low,high = (floor_at(point.x,False),floor_at(point.x)) if family=='Ramp' else (-height/2,height/2)
                assert low-.00002<=point.z<=high+.00002, (family,obj.name,tuple(point),low,high)
            construction.append({'name':obj.name,'layer':obj['construction_layer'],'material':obj.data.materials[0].name,'minimum':[min(point[i] for point in points) for i in range(3)],'maximum':[max(point[i] for point in points) for i in range(3)]})
        duplicates=[]
        bpy.ops.object.select_all(action='DESELECT')
        for obj in parts:
            copy=obj.copy();copy.data=obj.data.copy();collection.objects.link(copy);copy.select_set(True);duplicates.append(copy)
        bpy.context.view_layer.objects.active=duplicates[0]
        bpy.ops.object.join()
        visual=bpy.context.object;visual.name=family+'_LOD'+str(lod)
        scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        visual.data.calc_loop_triangles()
        path=exports/(family+'-LOD'+str(lod)+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,add_leaf_bones=False)
        records.append({'family':family,'sourceName':measured['name'],'lod':lod,'sizeUnityMetres':[width,height,depth],'pivot':'original collider bounds centre',
            'triangles':len(visual.data.loop_triangles),'export':path.relative_to(root).as_posix(),'exportSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'construction':construction})
        bpy.data.objects.remove(visual,do_unlink=True)
        collection.hide_render=collection.hide_viewport=lod!=0
for family in families:
    triangles=[entry['triangles'] for entry in records if entry['family']==family]
    assert triangles[0]>triangles[1]>triangles[2], (family,triangles)
master=folder/'Arena-Stone-v1.blend'
scene['axes']='Blender metres (x,y,z) -> Unity (x,z,-y); bounds-centre pivot; canonical ramp climbs +X'
scene['sourceSha256']=hashlib.sha256(source_path.read_bytes()).hexdigest()
bpy.ops.wm.save_as_mainfile(filepath=str(master))
(folder/'production.json').write_text(json.dumps({'id':'SG-ARENA-STONE-v1','status':'authored candidate; installed review and Windows integration pending',
    'master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'sourceSha256':scene['sourceSha256'],'axes':scene['axes'],
    'provenance':'Original Blender construction using existing original painted stone and shared ivory/teal/bronze cel materials; no external assets.',
    'collision':'No collision exported. Every visual vertex checked inside the exact original box or piecewise ramp hull. Hidden canonical collision references retain original Unity source coordinates.',
    'unityImportCorrection':'ArenaStoneArtBuilder applies a 180-degree Y rotation to each imported model, normalizing the native FBX half-turn. The asymmetric ramp direction and all imported hull vertices are checked against source collision.',
    'lodAppearance':'Paving colour regions and arrow wells keep the same metre-space layout at every level. LOD1 removes hidden tread depth; LOD2 closes subpixel paving gaps and reduces narrow bronze bands to their visible faces.',
    'supersededCandidate':{'commit':'791e57d35768b06196ad4f3acdcd8c84380fea12','masterSha256':'e355d0250eef363c25c5e387949a9b940c75f4b856d9f48fa7d0b60942cb7a77','disposition':'archive-reference in Git; coarse grids moved paving colour regions'},
    'rampProfileBlenderXBottomTop':ramp_profile,'instances':source['parts'],'lods':records},indent=2)+'\n',encoding='utf-8')
print('ARENA_STONE_EXPORTED',[(entry['family'],entry['lod'],entry['triangles']) for entry in records])
