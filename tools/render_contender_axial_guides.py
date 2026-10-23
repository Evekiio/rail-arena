"""Headless camera-only pole guides; not approved character appearance references."""
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/StylizedContenders/References-v1/Projection-Guides'
folder.mkdir(parents=True,exist_ok=True)
for name in ('Player','Bot01','Bot02','Bot03'):
    bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Original/StylizedContenders'/(name+'.blend')),load_ui=False)
    bpy.context.preferences.filepaths.file_preview_type='NONE'
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=4
    scene.render.threads_mode='FIXED'
    scene.render.threads=4
    scene.render.resolution_x=1500
    scene.render.resolution_y=800
    scene.render.resolution_percentage=100
    scene.world=bpy.data.worlds.new('Projection guide neutral')
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.58,.58,.58,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
    for view,z in (('Top',5),('Bottom',-5)):
        bpy.ops.object.camera_add(location=(0,0,z))
        camera=bpy.context.object
        camera.name='Exact orthographic '+view
        camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type='ORTHO'
        camera.data.ortho_scale=1.35
        scene.camera=camera
        scene.render.filepath=str(folder/(name+'-'+view+'.png'))
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(camera,do_unlink=True)
print('AXIAL_GUIDES_RENDERED '+str(folder))
