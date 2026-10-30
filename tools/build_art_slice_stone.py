"""Original measured 2 x 1 x 1.2 m stone-cover family; Blender 5.2 background job."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'ArtSource/Original/ArtPass-v1/Gate2/StoneCover'
export = root / 'RailArena/Assets/RailArena/Art/ArtSlice'
source.mkdir(parents=True, exist_ok=True)
export.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
palette = [('Ivory', (.79, .78, .68, 1)), ('Teal', (.065, .25, .25, 1)), ('Bronze', (.42, .23, .085, 1)), ('Stone', (.46, .51, .49, 1))]
materials = []
for name, color in palette:
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    materials.append(material)
parts = []


def block(name, size, position, slot, bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[slot])
    modifier = obj.modifiers.new('Authored stone arris', 'BEVEL')
    modifier.width = bevel
    modifier.segments = 1
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj['material_family'] = palette[slot][0]
    parts.append(obj)


# All pieces stay inside the existing collision envelope; no protruding handles or footholds.
block('Stone_Core', (1.94, .94, .98), (0, 0, -.04), 3, .035)
block('Ivory_Coping', (2, 1, .14), (0, 0, .53), 0, .025)
block('Ivory_Plinth', (2, 1, .14), (0, 0, -.53), 0, .02)
for x in (-.85, .85):
    block('Ivory_Pier', (.18, 1, .98), (x, 0, 0), 0, .015)
for side in (-1, 1):
    block('Teal_Recess', (1.5, .018, .54), (0, side * .48, .03), 1, .007)
    for z in (-.28, .34):
        block('Bronze_Rail', (1.5, .025, .025), (0, side * .485, z), 2, .005)
    for x in (-.55, 0, .55):
        block('Ivory_Panel_Rib', (.028, .025, .47), (x, side * .485, .03), 0, .004)
    block('Bronze_Center_Seal', (.16, .028, .16), (0, side * .485, .03), 2, .012)
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
scene['canonical_axes'] = 'Blender (x,y,z) metres -> Unity (x,z,-y); centered pivot; no collision or root motion'
scene['version'] = 'StoneCover-v1'
scene['collision_envelope_xyz_blender'] = (2, 1, 1.2)
bpy.ops.wm.save_as_mainfile(filepath=str(source / 'StoneCover-v1.blend'))
# Export one visual mesh with four material slots; retain separate construction parts in the master.
bpy.ops.object.join()
bpy.context.object.name = 'StoneCover_Visual'
scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.export_scene.fbx(filepath=str(export / 'StoneCover-v1.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, use_mesh_modifiers=True, add_leaf_bones=False)
views = [('front', (0, -5, 0)), ('rear', (0, 5, 0)), ('left', (-5, 0, 0)), ('right', (5, 0, 0)), ('top', (0, 0, 5)), ('bottom', (0, 0, -5))]
camera_data = bpy.data.cameras.new('Measured camera')
camera = bpy.data.objects.new('Measured camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 2.6
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.render.resolution_x = scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
for name, position in views:
    camera.location = position
    camera.rotation_euler = (-camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(source / (name + '.png'))
    bpy.ops.render.render(write_still=True)
record = {'id': 'SG-COVER-2M-v1', 'version': 1, 'provenance': 'Original procedural Blender construction authored for Rail Arena', 'master': str((source / 'StoneCover-v1.blend').relative_to(root)), 'export': str((export / 'StoneCover-v1.fbx').relative_to(root)), 'blenderDimensionsMetresXYZ': [2, 1, 1.2], 'unityDimensionsMetresXYZ': [2, 1.2, 1], 'pivot': 'envelope center', 'axes': 'Unity(x,z,-y) from Blender(x,y,z)', 'orthographicSpanMetres': 2.6, 'parts': len(parts), 'views': [v[0] + '.png' for v in views], 'collision': 'Existing 2 x 1.2 x 1 m envelope retained by runtime; visual art has no colliders', 'approval': 'Gate 2 candidate, pending in-game slice review'}
record['masterSha256'] = hashlib.sha256((source / 'StoneCover-v1.blend').read_bytes()).hexdigest()
record['exportSha256'] = hashlib.sha256((export / 'StoneCover-v1.fbx').read_bytes()).hexdigest()
(source / 'reference.json').write_text(json.dumps(record, indent=2) + '\n')
print('ART_SLICE_STONE_EXPORTED', len(parts))
