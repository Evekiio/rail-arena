"""Original opaque low-poly garden canopy; measured unit master fits existing leaf envelopes."""
import hashlib
import json
import math
from pathlib import Path
import sys
import bpy
import bmesh
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
source=root/'ArtSource/Original/ArtPass-v1/Nature/Canopy'
export=root/'RailArena/Assets/RailArena/Art/ArtPass/Nature'
source.mkdir(parents=True,exist_ok=True);export.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
colors=[('LeafShade',(.32,.54,.38)),('LeafJade',(.4,.69,.5)),('LeafLight',(.56,.81,.56)),('LeafSun',(.75,.88,.55))]
materials=[]
for name,color in colors:
    material=bpy.data.materials.new(name)
    linear=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in color]
    material.diffuse_color=(*linear,1);materials.append(material)
parts=[]
clusters=[(-.28,-.12,.02,.27,.25,.12),(.05,-.23,.09,.29,.25,.13),(.3,-.03,.03,.23,.26,.12),(-.12,.15,.13,.29,.25,.14),(.18,.2,.18,.28,.23,.13),(-.36,.17,-.03,.18,.22,.1),(.35,.22,.0,.2,.2,.11),(-.02,-.02,.25,.25,.24,.12)]
for index,(x,y,z,sx,sy,sz) in enumerate(clusters):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(x,y,z))
    obj=bpy.context.object;obj.name=f'Layered leaf crown {index+1}'
    obj.scale=(sx,sy,sz);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for material in materials:obj.data.materials.append(material)
    for face in obj.data.polygons:
        face.material_index=0 if face.normal.z<-.25 else 3 if face.normal.z>.7 and index%3==0 else 2 if face.normal.z>.3 else 1
    parts.append(obj)
    for sprig in range(3):
        angle=index*1.3+sprig*2.1
        direction=Vector((math.cos(angle),math.sin(angle),.12))
        side=Vector((-direction.y,direction.x,0))
        base=Vector((x,y,z))+direction*.12
        points=[base,base+direction*.13+side*.05+Vector((0,0,.025)),base+direction*.27,base+direction*.13-side*.05+Vector((0,0,.025)),base+direction*.13+Vector((0,0,.055))]
        mesh=bpy.data.meshes.new('Folded leaf blade');mesh.from_pydata(points,[],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]);mesh.update()
        blade=bpy.data.objects.new('Folded outer leaf',mesh);scene.collection.objects.link(blade)
        blade.data.materials.append(materials[2]);blade.data.materials.append(materials[1])
        modifier=blade.modifiers.new('Solid leaf thickness','SOLIDIFY');modifier.thickness=.006
        bpy.context.view_layer.objects.active=blade;bpy.ops.object.modifier_apply(modifier=modifier.name)
        parts.append(blade)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name='SkyGarden_Canopy'
scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
points=[v.co.copy() for v in obj.data.vertices]
low=Vector(tuple(min(v[i] for v in points) for i in range(3)));high=Vector(tuple(max(v[i] for v in points) for i in range(3)))
center=(low+high)/2;extent=high-low
for vertex in obj.data.vertices:vertex.co=Vector(tuple((vertex.co[i]-center[i])/extent[i] for i in range(3)))
topology=bmesh.new();topology.from_mesh(obj.data);bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces));topology.to_mesh(obj.data);topology.free()
scene['spec']='1m centered visual envelope; scale to existing tree-leaf bounds; no added collision; opaque geometry'
bpy.ops.wm.save_as_mainfile(filepath=str(source/'SkyGarden-Canopy-v1.blend'))
bpy.ops.export_scene.fbx(filepath=str(export/'SkyGarden-Canopy-v1.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,bake_anim=False)
obj.data.calc_loop_triangles()
record={'id':'SG-CANOPY-v1','dimensionsMetresXYZ':[1,1,1],'pivot':'envelope center','construction':'Eight layered faceted crowns and 24 folded outer leaf blades; opaque geometry','triangles':len(obj.data.loop_triangles),'paletteSrgb':dict(colors),'collision':'none; retains existing tree collision and leaf envelope','provenance':'Original Blender primitive construction; no third-party assets','masterSha256':hashlib.sha256((source/'SkyGarden-Canopy-v1.blend').read_bytes()).hexdigest(),'exportSha256':hashlib.sha256((export/'SkyGarden-Canopy-v1.fbx').read_bytes()).hexdigest(),'approval':'Final integrated user playtest pending'}
(source/'production.json').write_text(json.dumps(record,indent=2)+'\n')
camera=bpy.data.objects.new('Measured canopy camera',bpy.data.cameras.new('Measured canopy camera'));scene.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=1.35
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
for label,position in [('front',(0,-4,0)),('rear',(0,4,0)),('left',(-4,0,0)),('right',(4,0,0)),('top',(0,0,4)),('bottom',(0,0,-4))]:
    camera.location=position;camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(source/(label+'.png'));bpy.ops.render.render(write_still=True)
print('SKY_CANOPY_AUTHORED',record['triangles'],'triangles')
