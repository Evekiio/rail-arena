"""Author original bridge stone modules inside audited installed collision envelopes."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
folder = root / 'ArtSource/Original/ArtPass-v1/Environment/BridgeStone'
exports = root / 'RailArena/Assets/RailArena/Art/ArtPass/Environment/BridgeStone'
exports.mkdir(parents=True, exist_ok=True)
source = folder / 'bridge-source.json'
audit = json.loads(source.read_text(encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = [('BridgeIvory', (.87, .855, .76, 1)), ('BridgeStone', (.66, .70, .64, 1)),
           ('BridgeTeal', (.065, .255, .24, 1)), ('BridgeBronze', (.58, .37, .16, 1)),
           ('BridgeShade', (.19, .26, .25, 1))]
materials = []
paint = bpy.data.images.load(str(folder.parent / 'BasementDecks/Paving-Painted.png'))
paint.pack()
for name, color in palette:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = .75
    if name in ('BridgeIvory', 'BridgeStone'):
        texture = material.node_tree.nodes.new('ShaderNodeTexImage')
        texture.image = paint
        material.node_tree.links.new(texture.outputs['Color'], shader.inputs['Base Color'])
    materials.append(material)

records = []
for family, source_name in [('Deck', 'Bridge'), ('Screen', 'Bridge-Screen1'), ('Pier', 'Underpass-Pier1')]:
    measured = next(part for part in audit['parts'] if part['name'] == source_name)
    width, height, depth = (measured['size'][axis] for axis in ('x', 'y', 'z'))
    extent = Vector((width, depth, height)) / 2
    for lod in range(3):
        collection = bpy.data.collections.new(family + '_LOD' + str(lod))
        scene.collection.children.link(collection)
        parts = []

        def block(name, center, size, slot, bevel=0):
            bpy.ops.mesh.primitive_cube_add(size=1, location=center)
            obj = bpy.context.object
            obj.name = name
            obj.dimensions = size
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            for parent in list(obj.users_collection):
                parent.objects.unlink(obj)
            collection.objects.link(obj)
            obj.data.materials.append(materials[slot])
            if bevel and lod == 0:
                modifier = obj.modifiers.new('Single cut stone arris', 'BEVEL')
                modifier.width = bevel
                modifier.segments = 1
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            uv = obj.data.uv_layers.active
            for polygon in obj.data.polygons:
                for index in polygon.loop_indices:
                    point = obj.data.vertices[obj.data.loops[index].vertex_index].co + obj.location
                    uv.data[index].uv = (point.x / 4, point.y / 4) if abs(polygon.normal.z) > .5 else ((point.y if abs(polygon.normal.x) > .5 else point.x) / 4, point.z / 4)
            obj['construction_layer'] = name
            parts.append(obj)
            obj.select_set(False)
            return obj

        # All carving and trim is inset. Original silhouette and every collision face remain fixed.
        block('Recessed joint bed', (0, 0, 0), (width - .06, depth - .06, height - .06), 4)
        if family == 'Deck':
            nx, ny = (12, 6) if lod < 2 else (4, 2)
            for x in range(nx):
                for y in range(ny):
                    block('Paving_%02d_%02d' % (x, y), (-width/2+(x+.5)*width/nx, -depth/2+(y+.5)*depth/ny, height/2-.07), (width/nx-.012, depth/ny-.012, .14), (x+y)%2, .006)
            for sign in (-1, 1):
                block('Coping_' + str(sign), (0, sign*(depth/2-.09), height/2-.065), (width, .18, .13), 0, .008)
                block('Teal frieze_' + str(sign), (0, sign*(depth/2-.025), -.035), (width-.10, .05, .15), 2)
                block('Bronze string_' + str(sign), (0, sign*(depth/2-.012), .08), (width-.10, .024, .028), 3)
            block('Underside cornice', (0, 0, -height/2+.035), (width, depth, .07), 0, .008)
            if lod < 2:
                for x in range(9):
                    block('Underside coffer rib_' + str(x), (-width/2+.3+x*(width-.6)/8, 0, -height/2+.09), (.10, depth-.4, .11), 1, .005)
        else:
            courses = (5 if family == 'Pier' else 4) if lod < 2 else 2
            for course in range(courses):
                h = height/courses
                block('Ashlar course_' + str(course), (0, 0, -height/2+(course+.5)*h), (width-.018, depth-.018, h-.012), course%2, .012)
            for sign in (-1, 1):
                for side in (-1, 1):
                    block('End pilaster_%d_%d' % (sign, side), (side*width*.425, sign*(depth/2-.065), 0), (width*.15, .13, height), 0, .012)
                block('Teal end panel_' + str(sign), (0, sign*(depth/2-.009), .05), (width*.60, .018, height*.48), 2, .006)
                block('Bronze panel cap_' + str(sign), (0, sign*(depth/2-.006), height*.28), (width*.65, .012, .04), 3)
            for sign in (-1, 1):
                block('Capital base_' + str(sign), (0, 0, sign*(height/2-.065)), (width, depth, .13), 0, .012)
        bpy.context.view_layer.update()
        construction = []
        for obj in parts:
            points = [obj.matrix_world @ vertex.co for vertex in obj.data.vertices]
            assert all(all(abs(point[axis]) <= extent[axis]+.00001 for axis in range(3)) for point in points), obj.name
            construction.append({'name': obj.name, 'minimum': [min(point[i] for point in points) for i in range(3)], 'maximum': [max(point[i] for point in points) for i in range(3)]})
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
        visual.name = family + '_LOD' + str(lod)
        scene.cursor.location = (0, 0, 0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        visual.data.calc_loop_triangles()
        path = exports / (family + '-LOD' + str(lod) + '.fbx')
        bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, add_leaf_bones=False)
        records.append({'family': family, 'sourceName': source_name, 'lod': lod, 'sizeUnityMetres': [width, height, depth], 'pivot': 'source bounds center', 'triangles': len(visual.data.loop_triangles), 'export': str(path.relative_to(root)), 'exportSha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'construction': construction})
        bpy.data.objects.remove(visual, do_unlink=True)
        collection.hide_render = collection.hide_viewport = lod != 0
master = folder / 'Bridge-Stone-v1.blend'
scene['axes'] = 'Blender (x,y,z) -> Unity (x,z,-y); metres; per-module bounds-center origin'
scene['sourceSha256'] = hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.save_as_mainfile(filepath=str(master))
(folder / 'production.json').write_text(json.dumps({'id': 'SG-BRIDGE-STONE-v1', 'status': 'authored candidate; Unity integration pending', 'master': master.name, 'masterSha256': hashlib.sha256(master.read_bytes()).hexdigest(), 'sourceSha256': scene['sourceSha256'], 'axes': scene['axes'], 'provenance': 'Original Blender construction; reuses original basement painted stone image.', 'collision': 'No new collision. Every authored vertex checked inside measured source bounds. Installed collider geometry remains authoritative.', 'lods': records}, indent=2)+'\n', encoding='utf-8')
print('BRIDGE_STONE_EXPORTED', [(record['family'], record['lod'], record['triangles']) for record in records])
