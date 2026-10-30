"""Preserve established identities/skeletons; refine facial readability and UV-painted construction."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
import bmesh
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--')+1]).resolve()
output = root / 'ArtSource/Original/ArtPass-v1/Characters'
exports = root / 'RailArena/Assets/RailArena/Art/ArtPass/Characters'
order = ['armor','cloth','scarf','skin','hair','accent','metal','rubber','eye','iris','lip','hairLight','armorShade','skinShade','stitch','white']
for name in ('Player','Bot01','Bot02','Bot03'):
    original = root / f'ArtSource/Original/StylizedContenders/{name}.blend'
    original_hash = hashlib.sha256(original.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(original), load_ui=False)
    bpy.context.preferences.filepaths.file_preview_type = 'NONE'
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    rig.data.pose_position = 'REST'
    parts = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
    for obj in parts:
        if obj.name.startswith(('Inset faceted eye globe','Iris','Pupil')):
            center = sum((v.co for v in obj.data.vertices), Vector()) / len(obj.data.vertices)
            for v in obj.data.vertices:
                delta = v.co-center; delta.x *= 1.12; delta.z *= 1.08; v.co = center+delta
        if obj.name.startswith('Sculpted expressive eyebrow'):
            center = sum((v.co for v in obj.data.vertices), Vector()) / len(obj.data.vertices)
            for v in obj.data.vertices:
                v.co.z = center.z + (v.co.z-center.z)*1.16
    # An intentional fitted crest adds readable chest construction without expanding silhouette or hitboxes.
    material = next(m for m in bpy.data.materials if m.name == name+'_metal')
    points = [(-.04,-.145,1.39),(.04,-.145,1.39),(.032,-.149,1.35),(0,-.152,1.328),(-.032,-.149,1.35)]
    vertices = points + [(x,y+.006,z) for x,y,z in points]
    faces = [(0,1,2,3,4),(9,8,7,6,5)] + [(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
    mesh = bpy.data.meshes.new(name+' fitted crest')
    mesh.from_pydata(vertices, [], faces); mesh.update()
    crest = bpy.data.objects.new('Fitted chest crest', mesh); bpy.context.scene.collection.objects.link(crest)
    crest.data.materials.append(material); crest.parent = rig
    crest.vertex_groups.new(name='Spine').add(list(range(len(vertices))),1,'REPLACE')
    crest.modifiers.new('Shared skeleton deformation','ARMATURE').object = rig
    parts.append(crest)
    image = bpy.data.images.load(str(exports/f'{name}-Painted.png'), check_existing=False)
    image.pack()
    atlas = bpy.data.materials.new(name+' painted cel surface'); atlas.use_nodes = True
    node = atlas.node_tree.nodes.new('ShaderNodeTexImage'); node.image = image
    atlas.node_tree.nodes.active = node
    atlas.node_tree.links.new(node.outputs['Color'], atlas.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    for obj in parts:
        while obj.data.uv_layers: obj.data.uv_layers.remove(obj.data.uv_layers[0])
        uv = obj.data.uv_layers.new(name='PaintedConstructionUV')
        low = Vector(tuple(min(v.co[i] for v in obj.data.vertices) for i in range(3)))
        high = Vector(tuple(max(v.co[i] for v in obj.data.vertices) for i in range(3)))
        extent = high-low
        for polygon in obj.data.polygons:
            family = obj.data.materials[polygon.material_index].name.split('_')[-1]
            index = order.index(family)
            normal = polygon.normal
            dominant = max(range(3), key=lambda i: abs(normal[i]))
            axes = (1,2) if dominant==0 else (0,2) if dominant==1 else (0,1)
            for loop in polygon.loop_indices:
                vertex = obj.data.vertices[obj.data.loops[loop].vertex_index].co
                a,b = [(.5 if extent[i]<1e-6 else (vertex[i]-low[i])/extent[i]) for i in axes]
                uv.data[loop].uv = ((index%4+.04+a*.92)/4,(index//4+.04+b*.92)/4)
            polygon.material_index = 0
        obj.data.materials.clear(); obj.data.materials.append(atlas)
    scene = bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    scene['art_pass'] = 'Original projected painted cel atlas, fitted crest and expressive eye/brow refinement; existing rig and identities preserved'
    scene['source_sha256'] = original_hash
    scene['axes'] = 'Blender metres (x,y,z) -> Unity (x,z,-y)'
    bpy.ops.wm.save_as_mainfile(filepath=str(output/f'{name}-ArtPass-v1.blend'))
    lods=[]
    for level,ratio in enumerate((1,.58,.28)):
        bpy.ops.object.select_all(action='DESELECT')
        copies=[]
        for original_part in parts:
            obj=original_part.copy();obj.data=original_part.data.copy();scene.collection.objects.link(obj);obj.select_set(True);copies.append(obj)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();obj=bpy.context.object
        obj.name=name+'_Body_LOD'+str(level)
        if level:
            modifier=obj.modifiers.new('Distance topology','DECIMATE');modifier.ratio=ratio
            bpy.ops.object.modifier_move_up(modifier=modifier.name);bpy.ops.object.modifier_apply(modifier=modifier.name)
        modifier=obj.modifiers.new('Export triangles','TRIANGULATE');bpy.ops.object.modifier_move_up(modifier=modifier.name);bpy.ops.object.modifier_apply(modifier=modifier.name)
        topology=bmesh.new();topology.from_mesh(obj.data)
        collapsed=[face for face in topology.faces if face.calc_area()<1e-14]
        if collapsed:bmesh.ops.delete(topology,geom=collapsed,context='FACES')
        bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces))
        topology.to_mesh(obj.data);topology.free()
        assert all(v.groups for v in obj.data.vertices), 'Unweighted vertices'
        obj.data.calc_loop_triangles()
        lods.append({'level':level,'triangles':len(obj.data.loop_triangles),'vertices':len(obj.data.vertices)})
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.export_scene.fbx(filepath=str(exports/f'{name}_LOD{level}.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_armature_deform_only=True,bake_anim=False,axis_forward='-Z',axis_up='Y')
        bpy.data.objects.remove(obj,do_unlink=True)
    assert hashlib.sha256(original.read_bytes()).hexdigest()==original_hash
    points=[o.matrix_world@Vector(c) for o in parts for c in o.bound_box]
    bounds=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    (output/f'{name}-Production.json').write_text(json.dumps({'id':f'CHAR-{name.upper()}-ART-v1','originalSource':str(original.relative_to(root)),'originalSha256':original_hash,'master':f'{name}-ArtPass-v1.blend','boundsMetresBlenderXYZ':bounds,'axes':'Unity(x,z,-y) from Blender(x,y,z)','bones':[b.name for b in rig.data.bones],'lods':lods,'texture':f'{name}-Painted.png','textureSize':[2048,2048],'rig':'Existing shared skeleton, original rest transforms; no root-motion authority','approval':'Final integrated playtest pending'},indent=2)+'\n')
    print('CONTENDER_ART_REFINED',name,lods)
