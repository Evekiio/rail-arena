"""Isolate held arm and rifle construction layers at the measured right-side camera."""
import argparse
import hashlib,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);parser.add_argument('--family',default='Assembly-v1');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.root.resolve();assert args.family.startswith('Assembly-v') and args.family[len('Assembly-v'):].isdigit();folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson'/args.family;spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));measure=json.loads((folder/'measured-views.json').read_text(encoding='utf-8'));master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False);scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.view_settings.view_transform='Standard';scene.display.shading.background_type='WORLD'
if scene.world is None:scene.world=bpy.data.worlds.new('Technical background')
scene.world.color=(.045,.065,.075);scene.render.resolution_x=scene.render.resolution_y=1024;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Measured right-side construction camera',bpy.data.cameras.new('Construction camera'));scene.collection.objects.link(camera);camera.data.type='ORTHO';camera.data.ortho_scale=measure['orthographicSpanMetres'];camera.location=Vector(measure['centerBlenderXYZMetres'])+Vector((4,0,0));camera.rotation_euler=Vector((-1,0,0)).to_track_quat('-Z','Y').to_euler();scene.camera=camera
for pose in spec['referencePoses']:
    name=pose['id'];collection=bpy.data.collections[name]
    for item in spec['referencePoses']:
        other=bpy.data.collections[item['id']];other.hide_render=item['id']!=name;other.hide_viewport=other.hide_render
    for layer in ('arms','rifle'):
        for obj in collection.objects:
            if obj.type=='MESH':obj.hide_render=(obj.name.startswith(name+' / skinned')) != (layer=='arms')
        scene.render.filepath=str(folder/'References'/name/('right-'+layer+'-only.png'));bpy.ops.render.render(write_still=True)
    for obj in collection.objects:obj.hide_render=False
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
print('FPS_ASSEMBLY_CONSTRUCTION_LAYERS_RENDERED 16 role-isolation images / same orthographic scale / master unchanged')
