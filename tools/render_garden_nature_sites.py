"""Preview authored geometry at the audited installed metre-scale, without saving the master."""
import bpy,hashlib,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve();folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature';spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False);scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=scene.render.resolution_y=960;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard'
scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.display.shading.background_type='VIEWPORT';scene.display.shading.background_color=(.055,.08,.09)
camera=bpy.data.objects.new('Installed scale reference camera',bpy.data.cameras.new('Installed scale reference camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO';camera.data.clip_start=.01;camera.data.clip_end=200
context=bpy.data.collections.new('DIAGRAM_CONTEXT_NOT_PRODUCTION_GEOMETRY');scene.collection.children.link(context)
def context_material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);return m
ivory=context_material('Diagram planter / original dimensions',(.58,.59,.49));marker=context_material('1.8 m comparator / diagram',(.14,.32,.35))
def block(name,position,size,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=position);o=bpy.context.object;o.name=name;o.scale=size;o.data.materials.append(material)
 for collection in list(o.users_collection):collection.objects.unlink(o)
 context.objects.link(o);return o
block('Diagram ground slab',(0,0,.05),(2.6,3.6,.14),ivory)
for sign in (-1,1):block('Source-sized planter curb',(sign*1.21,0,.28),(.18,3.6,.32),ivory);block('Source-sized planter curb',(0,sign*1.71,.28),(2.24,.18,.32),ivory)
bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.36,depth=1.8,location=(2.25,0,1.02));o=bpy.context.object;o.name='Standing controller comparator 1.8 x 0.72 m';o.data.materials.append(marker)
for collection in list(o.users_collection):collection.objects.unlink(o)
context.objects.link(o)
output=folder/'InstalledScale';output.mkdir(exist_ok=True);records=[]
for site in range(1,5):
 bindings=[x for x in spec['bindings'] if int(x['sourceName'].split('-')[1])==site];trunk=next(x for x in bindings if x['module']=='Trunk');origin=Vector((trunk['center']['x'],0,trunk['center']['z']))
 for collection in scene.collection.children:
  if collection==context:continue
  collection.hide_render=True;collection.hide_viewport=True
 for binding in bindings:
  collection=bpy.data.collections[binding['module']+'_LOD0'];collection.hide_render=False;collection.hide_viewport=False
  c=Vector((binding['center']['x'],binding['center']['y'],binding['center']['z']))-origin;s=binding['size']
  for obj in collection.objects:obj.location=(c.x,-c.z,c.y);obj.scale=(s['x'],s['z'],s['y'])
 camera.data.ortho_scale=10.2;camera.location=(6,7,3.8);target=Vector((0,.2,.65));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(output/('site-'+str(site)+'.png'));bpy.ops.render.render(write_still=True)
 records.append({'site':site,'path':scene.render.filepath,'bindings':[x['sourceName'] for x in bindings],'orthographicSpanMetres':10.2,'cameraBlenderXYZ':list(camera.location),'diagramContext':'Original-sized curb/slab diagram and 1.8 m / 0.72 m controller; these are not new production meshes'})
 if site==1:
  camera.data.ortho_scale=6.0;camera.location=(5,-6,4.0);target=Vector((0,0,2.6));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(output/'tree-close.png');bpy.ops.render.render(write_still=True)
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'site-views.json').write_bytes((json.dumps({'scope':'Canonical near geometry transformed by audited installed metre-scale frames; original-sized diagram context and controller comparator. Not Unity captures, physics or artistic acceptance.','masterSha256':digest,'sourceSha256':spec['sourceSha256'],'views':records},indent=2)+'\n').encode('utf-8'))
print('GARDEN_NATURE_SITE_PREVIEWS_RENDERED four audited planting scales; master unchanged')
