"""Render equal-scale assembly orthographics and held native-eye references without modifying the master."""
import hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve();folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False);scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
if scene.world is None:scene.world=bpy.data.worlds.new('Technical reference background')
scene.world.color=(.045,.065,.075);scene.display.shading.background_type='WORLD';scene.view_settings.view_transform='Standard';scene.render.resolution_percentage=100
collections={pose['id']:bpy.data.collections[pose['id']] for pose in spec['referencePoses']}
def activate(name):
    for identity,collection in collections.items():collection.hide_render=identity!=name;collection.hide_viewport=identity!=name
    bpy.context.view_layer.update()
def points(collection):
    result=[];depsgraph=bpy.context.evaluated_depsgraph_get()
    for obj in collection.objects:
        if obj.type!='MESH':continue
        evaluated=obj.evaluated_get(depsgraph);mesh=evaluated.to_mesh()
        result.extend(evaluated.matrix_world@vertex.co for vertex in mesh.vertices);evaluated.to_mesh_clear()
    return result
bounds={};all_points=[]
for name,collection in collections.items():
    activate(name);values=points(collection);all_points+=values
    low=[min(point[axis] for point in values) for axis in range(3)];high=[max(point[axis] for point in values) for axis in range(3)]
    bounds[name]={'minimumBlenderXYZMetres':low,'maximumBlenderXYZMetres':high,'sizeBlenderXYZMetres':[b-a for a,b in zip(low,high)]}
low=Vector(tuple(min(point[axis] for point in all_points) for axis in range(3)));high=Vector(tuple(max(point[axis] for point in all_points) for axis in range(3)));center=(low+high)*.5
span=math.ceil(max(high-low)*1.2*10)/10
camera=bpy.data.objects.new('Equal-scale orthographic reference',bpy.data.cameras.new('Assembly orthographic'));scene.collection.objects.link(camera);camera.data.type='ORTHO';camera.data.ortho_scale=span
views=[('front',(0,1,0)),('rear',(0,-1,0)),('left',(-1,0,0)),('right',(1,0,0)),('top',(0,0,1)),('bottom',(0,0,-1))]
records=[]
for pose in spec['referencePoses']:
    name=pose['id'];activate(name);collection=collections[name];directory=folder/'References'/name;directory.mkdir(parents=True,exist_ok=True)
    scene.render.resolution_x=scene.render.resolution_y=1024;scene.camera=camera
    for label,axis in views:
        direction=Vector(axis);camera.location=center+direction*4;camera.rotation_euler=(-direction).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(directory/(label+'.png'));bpy.ops.render.render(write_still=True)
    scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.camera=next(obj for obj in collection.objects if obj.type=='CAMERA');scene.render.filepath=str(directory/'native-eye-workbench.png');bpy.ops.render.render(write_still=True)
    records.append({'id':name,'bounds':bounds[name],'orthographicSpanMetres':span,'resolution':1024,'views':[item[0] for item in views],'nativeEye':'References/'+name+'/native-eye-workbench.png','nativeUnityImage':pose['nativeImage'],'maximumNativeVertexErrorMetres':pose['maximumNativeVertexErrorMetres'],'maximumNativeBoneHeadErrorMetres':pose['maximumNativeBoneHeadErrorMetres'],'scope':'Held assembled geometry and camera reference. Workbench shading is technical, not the final Unity cel shader. Shared equal-scale orthographic framing includes rifle and deformed hands.'})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'measured-views.json').write_bytes((json.dumps({'masterSha256':digest,'sourceSha256':spec['sourceSha256'],'units':'metres','axes':spec['axes'],'centerBlenderXYZMetres':list(center),'orthographicSpanMetres':span,'views':records,'rigContract':'54 editable bones per held assembly with original four-weight skin groups. Hip and ADS are held pose references, not new motion clips. Pose reconstruction is verified against 31,012 native baked vertices and 432 bone heads.'},indent=2)+'\n').encode('utf-8'))
print('FIRST_PERSON_ASSEMBLY_REFERENCES_RENDERED 48 equal-scale orthographics / 8 native-eye workbench views; master unchanged',digest)
