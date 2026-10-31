"""Read-only, equal-scale technical rendering from the canonical nature geometry."""
import bpy,hashlib,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve();folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature';spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));master=folder/spec['master'];before=hashlib.sha256(master.read_bytes()).hexdigest();assert before==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.055,.08,.09)
scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG'
camera=bpy.data.objects.new('Measured nature camera',bpy.data.cameras.new('Measured nature camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.5;camera.data.clip_start=.01;camera.data.clip_end=100
views={'front':(0,-3,0),'rear':(0,3,0),'left':(-3,0,0),'right':(3,0,0),'top':(0,0,3),'bottom':(0,0,-3)};records=[]
for module in spec['modules']:
 family=module['family'];selected=bpy.data.collections[family+'_LOD0']
 for collection in scene.collection.children:
  collection.hide_render=collection!=selected;collection.hide_viewport=collection!=selected
 output=folder/'References'/family;output.mkdir(parents=True,exist_ok=True)
 for name,position in views.items():
  camera.location=position;camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(output/(name+'.png'));bpy.ops.render.render(write_still=True)
  records.append({'family':family,'view':name,'orthographicSpanMetres':1.5,'widthPixels':640,'path':scene.render.filepath,'masterSha256':before})
 camera.location=(1.9,-2.4,1.25);camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(output/'perspective.png');bpy.ops.render.render(write_still=True)
 camera.data.ortho_scale=.6;camera.location=(1.9,-2.4,.7);target=Vector((0,0,.08));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(output/'detail.png');bpy.ops.render.render(write_still=True);camera.data.ortho_scale=1.5
assert hashlib.sha256(master.read_bytes()).hexdigest()==before
(folder/'measured-views.json').write_bytes((json.dumps({'scope':'Read-only canonical Blender geometry; six equal-scale unit-frame views per module, separate labelled detail crops; source-site dimensions and frames remain authoritative for installed metre-scale instances. No native acceptance implied.','masterSha256':before,'views':records},indent=2)+'\n').encode('utf-8'))
print('GARDEN_NATURE_REFERENCES_RENDERED',len(records),'orthographics; master unchanged')
