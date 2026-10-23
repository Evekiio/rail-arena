import bpy
import json
import math
import os
import sys

source, asset_dir, source_dir = sys.argv[sys.argv.index('--') + 1:]
os.makedirs(asset_dir, exist_ok=True)
os.makedirs(source_dir, exist_ok=True)
texture_dir = os.path.join(asset_dir, 'Textures')
os.makedirs(texture_dir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=source)
armature = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
armature.animation_data_clear()
for bone in armature.pose.bones:
    bone.matrix_basis.identity()
# Add original finger joints and bind the authored combat grip before LOD reduction.
sys.path.insert(0, os.path.dirname(__file__))
from hand_rig import articulate
hand_report = articulate(armature, meshes)
images = []
for image in bpy.data.images:
    if image.type != 'IMAGE':
        continue
    name = os.path.splitext(os.path.basename(image.name))[0]
    image.filepath_raw = os.path.join(texture_dir, name + '.png')
    image.file_format = 'PNG'
    image.save()
    image.pack()
    images.append({'name': image.name, 'path': image.filepath_raw, 'size': list(image.size)})
material_report = []
for material in bpy.data.materials:
    if material.use_nodes:
        material_report.append({'name': material.name, 'textures': [
            {'node': n.name, 'label': n.label, 'image': n.image.name,
             'links': [l.to_socket.name for s in n.outputs for l in s.links]}
            for n in material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image]})
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_dir, 'ArmoredContender.blend'))
report = {'images': images, 'materials': material_report, 'handRig': hand_report, 'lods': []}
for level, ratio in enumerate((.25, .10, .035)):
    bpy.ops.object.select_all(action='DESELECT')
    armature.select_set(True)
    copies = []
    for mesh in meshes:
        obj = mesh.copy()
        obj.data = mesh.data.copy()
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        modifier = obj.modifiers.new('Topology reduction', 'DECIMATE')
        modifier.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(True)
        copies.append(obj)
    bpy.context.view_layer.objects.active = armature
    path = os.path.join(asset_dir, f'Contender_LOD{level}.fbx')
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'},
        add_leaf_bones=False, use_armature_deform_only=True, bake_anim=False,
        axis_forward='-Z', axis_up='Y', path_mode='AUTO')
    report['lods'].append({'path': path, 'triangles': sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in copies)})
    for obj in copies:
        bpy.data.objects.remove(obj, do_unlink=True)
with open(os.path.join(source_dir, 'preparation.json'), 'w') as handle:
    json.dump(report, handle, indent=2)
