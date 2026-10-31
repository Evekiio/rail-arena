"""Construct exact-envelope, layered stone deck families from the audited Unity collision profiles."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Environment/BasementDecks'
export=root/'RailArena/Assets/RailArena/Art/ArtPass/Environment/BasementDecks'
audit_path=folder/'deck-source.json';audit=json.loads(audit_path.read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette=[('DeckIvory',(.87,.855,.76,1)),('DeckStone',(.70,.715,.65,1)),('DeckTeal',(.065,.255,.24,1)),('DeckBronze',(.58,.37,.16,1)),('DeckShade',(.22,.30,.28,1))]
materials=[]
paint=bpy.data.images.load(str(folder/'Paving-Painted.png'));paint.pack()
normal=bpy.data.images.load(str(folder/'Paving-Normal.png'));normal.colorspace_settings.name='Non-Color';normal.pack()
cavity=bpy.data.images.load(str(folder/'Paving-Cavity.png'));cavity.colorspace_settings.name='Non-Color';cavity.pack()
for name,color in palette:
    material=bpy.data.materials.new(name);material.diffuse_color=color;material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=color;shader.inputs['Roughness'].default_value=.75
    if name in ('DeckIvory','DeckStone'):
        image=material.node_tree.nodes.new('ShaderNodeTexImage');image.image=paint;material.node_tree.links.new(image.outputs['Color'],shader.inputs['Base Color'])
        if name=='DeckStone':
            tint=material.node_tree.nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(.86,.88,.84,1);material.node_tree.links.new(image.outputs['Color'],tint.inputs[1]);material.node_tree.links.new(tint.outputs[0],shader.inputs['Base Color'])
        image=material.node_tree.nodes.new('ShaderNodeTexImage');image.image=normal;mapping=material.node_tree.nodes.new('ShaderNodeNormalMap');mapping.inputs['Strength'].default_value=.5;material.node_tree.links.new(image.outputs['Color'],mapping.inputs['Color']);material.node_tree.links.new(mapping.outputs['Normal'],shader.inputs['Normal'])
        image=material.node_tree.nodes.new('ShaderNodeTexImage');image.image=cavity;image.label='Unity indirect occlusion / green / strength 0.25';material['unity_occlusion_strength']=.25
    materials.append(material)
records=[];collections=[]

def mesh_part(name,points,top,bottom,slot,parts,collection,bevel=0):
    count=len(points);vertices=[(p.x,p.y,top) for p in points]+[(p.x,p.y,bottom) for p in points]
    faces=[tuple(range(count)),tuple(reversed(range(count,2*count)))]+[(i,i+count,(i+1)%count+count,(i+1)%count) for i in range(count)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);mesh.materials.append(materials[slot])
    if bevel:
        bpy.context.view_layer.objects.active=obj;obj.select_set(True);modifier=obj.modifiers.new('Stone edge arris','BEVEL');modifier.width=bevel;modifier.segments=1
        bpy.ops.object.modifier_apply(modifier=modifier.name);obj.select_set(False)
    mesh=obj.data
    uv=mesh.uv_layers.new(name='Metres / 4 m paint period')
    for polygon in mesh.polygons:
        for index in polygon.loop_indices:
            vertex=mesh.vertices[mesh.loops[index].vertex_index].co
            uv.data[index].uv=(vertex.x/4,vertex.y/4) if abs(polygon.normal.z)>.5 else ((vertex.y if abs(polygon.normal.x)>abs(polygon.normal.y) else vertex.x)/4,vertex.z/4)
    obj['construction_layer']=name.split('_')[0];parts.append(obj);return obj

for profile in audit['profiles']:
    name=profile['name'];radius=profile['radiusMetres'];depth=profile['thicknessMetres']
    candidates={(round(p['x'],6),round(-p['z'],6)) for p in profile['collision']['verticesUnityMetres'] if abs(p['y'])<.00001 and p['x']**2+p['z']**2>1}
    hull=sorted((Vector(p) for p in candidates),key=lambda point:math.atan2(point.y,point.x));count=len(hull);assert count==20
    for level in range(3):
        collection=bpy.data.collections.new(name+'_LOD'+str(level));scene.collection.children.link(collection);collections.append(collection);parts=[]
        def polygon(scale):return [point*scale for point in hull]
        outer_marker=2/radius if name=='Basement' else .292
        inner_marker=outer_marker-.1/radius
        def ring(label,inner,outer,top,bottom,slot,segmented=False,bevel=0):
            for i in range(count):
                a,b=hull[i],hull[(i+1)%count]
                gap=(.01/max((b-a).length*outer,.01)) if segmented else 0
                left=a.lerp(b,gap);right=a.lerp(b,1-gap)
                points=[left*inner,left*outer,right*outer,right*inner] if inner>0 else [Vector((0,0)),left*outer,right*outer]
                mesh_part(label+'_'+str(i),points,top,bottom,slot,parts,collection,bevel)
        # The original hull remains authoritative. Every visual vertex stays at or below its top plane.
        # Recess the grout backing behind the coping wall, avoiding coplanar material faces.
        mesh_part('JointBed',polygon(.997),-.008,-.115,4,parts,collection)
        mesh_part('CenterStone',polygon(inner_marker),-.004,-.10,0,parts,collection,.005 if level<2 else 0)
        ring('CenterTeal',inner_marker,outer_marker,0,-.07,2)
        ring('InnerPaving',outer_marker+.02/radius,.604,0,-.10,0,level<2,.004 if level==0 else 0)
        if level<2:
            for index in range(8):
                angle=index*math.pi/4;direction=Vector((math.cos(angle),math.sin(angle)));side=Vector((-direction.y,direction.x));reach=min(radius*.18,1.15)
                points=[direction*reach,direction*reach*.6+side*.10,direction*reach*.28,direction*reach*.6-side*.10]
                mesh_part('CrestPetal_'+str(index),points,0,-.025,2 if index%2==0 else 3,parts,collection)
        ring('BronzeInlay',.607,.614,0,-.025,3)
        ring('OuterPaving',.617,.895,0,-.10,1,level<2,.004 if level==0 else 0)
        ring('Coping',.898,1,0,-.13,0,level<2,.006 if level==0 else 0)
        mesh_part('StructuralDrum',polygon(.965),-.10,-.48,1,parts,collection)
        ring('UpperDrip',.91,.99,-.13,-.185,0)
        ring('BronzeString',.91,.977,-.197,-.216,3)
        ring('TealFrieze',.905,.974,-.235,-.405,2,level==0)
        if level<2:
            for i in range(count):
                a,b=hull[i],hull[(i+1)%count];mid=a.lerp(b,.5);half=.035 if radius>4 else .05
                left=a.lerp(b,.5-half);right=a.lerp(b,.5+half)
                mesh_part('FriezePier_'+str(i),[left*.91,left*.981,right*.981,right*.91],-.22,-.44,0,parts,collection,.004 if level==0 else 0)
        ring('LowerCornice',.83,.97,-.43,-.51,0)
        mesh_part('UndersideCoffer',polygon(.85),-.47,-.515,4,parts,collection)
        ring('UndersideCrown',.82,.94,-.51,-depth,1)
        ring('UndersideTeal',.23,.28,-.512,-.555,2)
        mesh_part('UndersideSeal',polygon(.20),-.51,-depth,3,parts,collection)
        if level<2:
            for i in range(0,count,2 if level==1 else 1):
                a,b=hull[i],hull[(i+1)%count]
                left=a.lerp(b,.46);right=a.lerp(b,.54)
                mesh_part('UndersideRib_'+str(i),[left*.27,left*.83,right*.83,right*.27],-.515,-.586,0,parts,collection,.004 if level==0 else 0)
        if level==2:
            keep={'StructuralDrum','TealFrieze','UndersideCrown','UndersideSeal'}
            for obj in list(parts):
                if obj['construction_layer'] not in keep:parts.remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
            mesh_part('DistantPaving',polygon(1),-.004,-.13,0,parts,collection)
            vertices=[(point.x*scale,point.y*scale,0) for scale in (inner_marker,outer_marker) for point in hull]
            mesh=bpy.data.meshes.new('Distant marker ring');mesh.from_pydata(vertices,[],[(i,i+count,(i+1)%count+count,(i+1)%count) for i in range(count)]);mesh.update();mesh.materials.append(materials[2]);obj=bpy.data.objects.new('DistantMarkerRing',mesh);collection.objects.link(obj);obj['construction_layer']='DistantMarkerRing';parts.append(obj)
            assert all(face.normal.z>.99 for face in mesh.polygons),'Distant marker must face upward'
        bpy.context.view_layer.update()
        # Convex source footprint and exact vertical envelope, including bevelled pieces.
        for obj in parts:
            for vertex in obj.data.vertices:
                point=Vector((vertex.co.x,vertex.co.y));assert -depth-.00001<=vertex.co.z<=.00001,(name,obj.name,vertex.co.z)
                for i in range(count):
                    edge=hull[(i+1)%count]-hull[i];delta=point-hull[i]
                    assert edge.x*delta.y-edge.y*delta.x>=-.0001,(name,obj.name,'outside collision footprint')
        for obj in bpy.context.selected_objects:obj.select_set(False)
        for obj in parts:obj.select_set(True)
        bpy.context.view_layer.objects.active=parts[0]
        # Preserve construction layers in the canonical master; join a duplicate for each runtime LOD.
        duplicates=[]
        for obj in parts:
            duplicate=obj.copy();duplicate.data=obj.data.copy();collection.objects.link(duplicate);duplicates.append(duplicate);obj.select_set(False);duplicate.select_set(True)
        bpy.context.view_layer.objects.active=duplicates[0];bpy.ops.object.join();visual=bpy.context.object;visual.name=name+'_Visual_LOD'+str(level)
        scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');visual.data.calc_loop_triangles()
        path=export/(name+'-Deck-LOD'+str(level)+'.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,use_mesh_modifiers=True,add_leaf_bones=False)
        records.append({'profile':name,'lod':level,'export':str(path.relative_to(root)),'exportSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'triangles':len(visual.data.loop_triangles),'parts':len(parts),'materials':[material.name for material in visual.data.materials],'topPivotUnityMetres':[0,0,0],'collisionGeometrySha256':profile['geometrySha256'],'diameterMetres':radius*2,'thicknessMetres':depth})
        bpy.data.objects.remove(visual,do_unlink=True)
for collection in collections:collection.hide_render=not collection.name.endswith('LOD0');collection.hide_viewport=collection.hide_render
collision_collection=bpy.data.collections.new('CollisionReference');scene.collection.children.link(collision_collection);collision_collection.hide_render=True;collision_collection.hide_viewport=True
for profile in audit['profiles']:
    data=profile['collision'];mesh=bpy.data.meshes.new(profile['name']+' exact source collision');vertices=[(point['x'],-point['z'],point['y']) for point in data['verticesUnityMetres']];triangles=data['triangles'];mesh.from_pydata(vertices,[],[tuple(triangles[index:index+3]) for index in range(0,len(triangles),3)]);mesh.update();obj=bpy.data.objects.new(profile['name']+'_CollisionReference',mesh);collision_collection.objects.link(obj);obj.display_type='WIRE';obj['sourceGeometrySha256']=profile['geometrySha256']
scene['axes']='Blender metres (x,y,z) -> Unity (x,z,-y); top-center pivot; source collision remains unchanged'
scene['source_collision_authority']=hashlib.sha256(audit_path.read_bytes()).hexdigest()
scene['version']='SG-BASEMENT-DECKS-v1'
master=folder/'Basement-Stone-Decks-v1.blend';bpy.ops.wm.save_as_mainfile(filepath=str(master))
(folder/'production.json').write_text(json.dumps({'id':'SG-BASEMENT-DECKS-v1','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'source':'deck-source.json','sourceSha256':hashlib.sha256(audit_path.read_bytes()).hexdigest(),'axes':scene['axes'],'provenance':'Original layered limestone, frieze, coping and underside construction authored in Blender from the audited collision hull; original paint maps','collision':'No new colliders. Visual vertices validated inside each exact 20-sided source hull and 0.6 metre vertical envelope. Original top planes, route transforms and device clearances remain authoritative. Hidden CollisionReference collection retains both exact source meshes.','materialSlots':[name for name,_ in palette],'lods':records,'approval':'Direction approved; final integrated Sanders playtest pending'},indent=2)+'\n')
print('BASEMENT_STONE_DECKS_EXPORTED',[(record['profile'],record['lod'],record['triangles']) for record in records])
