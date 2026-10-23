"""Create a blank, metre-calibrated Blender image rig after the renders exist."""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Matrix

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ProductionBenchmarks-v1/Dimensional-References-v1'
if '--folder' in sys.argv:folder=root/sys.argv[sys.argv.index('--folder')+1]
calibration=json.loads((folder/'Calibration.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
for asset in calibration['assets']:
    collection=bpy.data.collections.new(asset['asset']+' orthographic references')
    scene.collection.children.link(collection)
    for view in asset['views']:
        image=bpy.data.images.load(str(folder/view['file']),check_existing=True);image.pack()
        obj=bpy.data.objects.new(asset['asset']+' / '+view['name'],None)
        collection.objects.link(obj);obj.empty_display_type='IMAGE';obj.data=image
        obj.empty_display_size=asset['orthographicFrameMetres']
        obj.hide_select=True
        obj.location=asset['center']
        obj.rotation_euler=Matrix((view['right'],view['up'],view['normal'])).transposed().to_quaternion().to_euler()
        obj.empty_image_depth='BACK';obj.empty_image_side='FRONT';obj.color[3]=.65
        obj.show_empty_image_orthographic=True;obj.show_empty_image_perspective=False
        obj.show_empty_image_only_axis_aligned=True
        obj['frameMetres']=asset['orthographicFrameMetres']
        obj['approval']='PROPOSED; awaiting creator review'
        obj['viewDirection']=view['note']
    # Select the rifle initially; enable the cover collection for environment work.
    collection.hide_viewport=asset['asset']=='LowWingCover'
assert len([o for o in scene.objects if o.type=='EMPTY'])==6*len(calibration['assets'])
assert not any(o.type=='MESH' for o in scene.objects)
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'Blender-Reference-Rig.blend'))
print('DIMENSIONAL_REFERENCE_RIG_COMPLETE')
