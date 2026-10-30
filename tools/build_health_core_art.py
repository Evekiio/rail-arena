"""Original faceted medical medallion, in metres, for the existing animated pickup root."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
import bmesh

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
source=root/'ArtSource/Original/ArtPass-v1/Devices/HealthCore'
export=root/'RailArena/Assets/RailArena/Art/ArtPass/Devices'
source.mkdir(parents=True,exist_ok=True);export.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palettes=[('MedicalIvory',(.88,.91,.84)),('MedicalBronze',(.66,.43,.2)),('MedicalEnergy',(.1,.85,.59)),('MedicalGraphite',(.08,.18,.18))]
materials=[]
for name,color in palettes:
    material=bpy.data.materials.new(name);material.diffuse_color=(*color,1);materials.append(material)
parts=[]


def box(name,size,location,material,bevel=.008):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location)
    obj=bpy.context.object;obj.name=name;obj.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(materials[material]);modifier=obj.modifiers.new('Intentional bevel','BEVEL');modifier.width=bevel;modifier.segments=1
    bpy.ops.object.modifier_apply(modifier=modifier.name);parts.append(obj)


def ring(name,outer,inner,depth,material):
    count=16;vertices=[]
    for y,radius in [(-depth/2,outer),(-depth/2,inner),(depth/2,outer),(depth/2,inner)]:
        vertices.extend([(math.cos(i*2*math.pi/count)*radius,y,math.sin(i*2*math.pi/count)*radius) for i in range(count)])
    faces=[]
    for i in range(count):
        j=(i+1)%count
        faces.extend([(i,j,count+j,count+i),(2*count+j,2*count+i,3*count+i,3*count+j),(i,2*count+i,2*count+j,j),(count+j,3*count+j,3*count+i,count+i)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj);obj.data.materials.append(materials[material]);parts.append(obj)


ring('Faceted ivory medical housing',.42,.32,.12,0)
ring('Bronze perimeter rim',.432,.417,.145,1)
ring('Inner graphite gasket',.321,.298,.09,3)
for side in (-1,1):
    box('Medical cross upright',(.12,.045,.48),(0,side*.045,0),2)
    box('Medical cross lateral',(.46,.045,.12),(0,side*.045,0),2)
    for x in (-.24,.24):box('Ivory cross bracket',(.055,.055,.1),(x,side*.045,0),0,.006)
for z in (-.45,.45):box('Bronze alignment fin',(.09,.10,.06),(0,0,z),1,.009)
bpy.ops.object.select_all(action='DESELECT')
for part in parts:part.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='MedicalCore_Visual'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
topology=bmesh.new();topology.from_mesh(obj.data);bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces));topology.to_mesh(obj.data);topology.free()
scene['spec']='Metres; center pivot; Blender(x,y,z)->Unity(x,z,-y). No collider, timer or pickup authority.'
bpy.ops.wm.save_as_mainfile(filepath=str(source/'Medical-Core-v1.blend'))
bpy.ops.export_scene.fbx(filepath=str(export/'Medical-Core-v1.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',apply_unit_scale=True,bake_space_transform=True,bake_anim=False)
record={'id':'DEVICE-MEDICAL-CORE-v1','masterSha256':hashlib.sha256((source/'Medical-Core-v1.blend').read_bytes()).hexdigest(),'exportSha256':hashlib.sha256((export/'Medical-Core-v1.fbx').read_bytes()).hexdigest(),'dimensionsBlenderXYZ':[.864,.145,.96],'dimensionsUnityXYZ':[.864,.96,.145],'units':'metres','pivot':'center','materials':dict(palettes),'runtime':'Existing FloatingRoot float/spin and availability remain authoritative; original pickup collision retained','provenance':'Original Blender construction','approval':'Final integrated user playtest pending'}
(source/'production.json').write_text(json.dumps(record,indent=2)+'\n')
camera=bpy.data.objects.new('Measured medical camera',bpy.data.cameras.new('Measured medical camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=1.25
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
for label,position in [('front',(0,-4,0)),('rear',(0,4,0)),('left',(-4,0,0)),('right',(4,0,0)),('top',(0,0,4)),('bottom',(0,0,-4))]:
    camera.location=position;camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(source/(label+'.png'));bpy.ops.render.render(write_still=True)
print('MEDICAL_CORE_AUTHORED')
