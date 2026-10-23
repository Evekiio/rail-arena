"""Export grouped reference-only assemblies so FP can hide the rear stock normally."""
import bpy
import math
import sys
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=ROOT/'ArtSource/Original/PrecisionCarbine-References-v3'
bpy.ops.wm.open_mainfile(filepath=str(folder/'Carbine-Reference-Study.blend'))
scene=bpy.context.scene
meshes=[o for o in scene.objects if o.type=='MESH']
groups={f'RF-{number:02}':[o for o in meshes if o['assemblyId']==f'RF-{number:02}'] for number in range(1,13)}
exports=[]
for number in range(1,13):
    objects=groups[f'RF-{number:02}']
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object
    obj.name=f'RF_{number:02}_Review';scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    obj.rotation_euler[2]=-math.pi/2;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);exports.append(obj)
bpy.ops.object.select_all(action='DESELECT')
for obj in exports:obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'RailArena/Assets/RailArena/Art/CarbineReferenceReview/Carbine-Reference.fbx'),
    use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
print('CARBINE_GROUPED_REVIEW_EXPORT_COMPLETE')
