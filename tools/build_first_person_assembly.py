"""Reconstruct editable held FPS assemblies from hashed Unity geometry/skin calibration."""
import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
import bpy
from mathutils import Matrix, Vector

parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);parser.add_argument('--family',default='Assembly-v1')
parser.add_argument('--native-result',default='Validation/fps-assembly-export-scale-correct.xml');parser.add_argument('--saved-master-result',default='Validation/fps-assembly-saved-master-pivots.json')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.root.resolve()
assert args.family.startswith('Assembly-v') and args.family[len('Assembly-v'):].isdigit()
folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson'/args.family
source_dir=folder/'RuntimeSource';capture=json.loads((source_dir/'capture.json').read_text(encoding='utf-8'))
source=source_dir/capture['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==capture['sourceSha256']
payload=gzip.decompress(source.read_bytes());assert hashlib.sha256(payload).hexdigest()==capture['decompressedSha256'];data=json.loads(payload)
result=ET.parse(root/args.native_result).getroot();cases=[case for case in result.iter('test-case') if case.attrib.get('fullname','').endswith('FirstPersonAssemblyExport.ExportHeldAssemblyWithSkinProof')]
assert len(cases)==1 and cases[0].attrib['result']=='Passed' and capture['sourceSha256'] in ''.join(cases[0].itertext())
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
# Weapon-root Unity: +X right / +Y up / +Z forward. Blender: +X right / +Y forward / +Z up.
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)));CI=C.inverted()
def matrix(record):
    values=record['values'];return Matrix(tuple(tuple(values[row*4+col] for col in range(4)) for row in range(4)))
def vector(record):return Vector((record['x'],record['y'],record.get('z',0)))
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
materials=[]
for item in data['materials']:
    mat=bpy.data.materials.new(item['name']);color=item['color'];mat.diffuse_color=tuple(color[key] for key in ('r','g','b','a'));mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=mat.diffuse_color;shader.inputs['Roughness'].default_value=.65
    if item.get('baseMap'):
        image=bpy.data.images.load(str(root/item['baseMap']),check_existing=True);image.pack()
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color']);mat.node_tree.nodes.active=node
    mat['unity_source']=item['source'];mat['appearance_authority']='Unity native review; Blender workbench textures support geometry reference only';materials.append(mat)

def geometry(name,record,to_root,slots):
    vertices=[C@(to_root@vector(value)) for value in record['vertices']]
    normals=[(C.to_3x3()@to_root.to_3x3().inverted().transposed()@vector(value)).normalized() for value in record['normals']]
    faces=[];indices=[];flipped=0
    for section,part in enumerate(record['submeshes']):
        assert section<len(slots)
        values=part['values']
        for offset in range(0,len(values),3):
            tri=list(values[offset:offset+3]);a,b,c=(vertices[index] for index in tri)
            if (b-a).cross(c-a).dot(sum((normals[index] for index in tri),Vector()))<0:tri.reverse();flipped+=1
            faces.append(tri);indices.append(section)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    for slot in slots:mesh.materials.append(materials[slot])
    for polygon,slot in zip(mesh.polygons,indices):polygon.material_index=slot;polygon.use_smooth=True
    uv=mesh.uv_layers.new(name='Installed source UV')
    for loop in mesh.loops:
        value=record['uv'][loop.vertex_index];uv.data[loop.index].uv=(value['x'],value['y'])
    mesh.normals_split_custom_set_from_vertices(normals)
    mesh['source_triangles']=len(faces);mesh['winding_corrected_against_source_normals']=flipped
    return mesh

rifle_meshes=[geometry(item['name'],item['geometry'],Matrix.Identity(4),item['materials']) for item in data['rifle']]
skins={item['identity']:item for item in data['skins']};skin_meshes={name:geometry(name+' / calibrated arms',skin['geometry'],matrix(skin['meshToRoot']),[skin['material']]) for name,skin in skins.items()}
reports=[];collections=[]
for pose in data['poses']:
    identity=pose['identity'];skin=skins[identity];name=identity+'_'+pose['mode'];collection=bpy.data.collections.new(name);scene.collection.children.link(collection);collections.append(collection)
    for mesh,part in zip(rifle_meshes,data['rifle']):
        obj=bpy.data.objects.new(mesh.name,mesh);collection.objects.link(obj);obj.matrix_world=C@matrix(part['toRoot'])@CI;obj['role']='Installed first-person LOD0 rifle part; original source frame and pivot retained';obj['native_source_frame_row_major']=part['toRoot']['values']
    armature=bpy.data.armatures.new(name+' / 54-bone calibrated skeleton');rig=bpy.data.objects.new(name+' / editable rig',armature);collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for bone_name,inv in zip(skin['bones'],skin['inverseBindsRoot']):
        bind=C@matrix(inv).inverted()@CI;bone=armature.edit_bones.new(bone_name);bone.head=bind.translation
        direction=bind.to_3x3().col[1].normalized();bone.tail=bone.head+direction*.05;bone.align_roll(bind.to_3x3().col[2].normalized())
    for index,parent in enumerate(skin['parents']):
        if parent>=0:armature.edit_bones[skin['bones'][index]].parent=armature.edit_bones[skin['bones'][parent]]
    bpy.ops.object.mode_set(mode='POSE');rest={bone.name:bone.matrix_local.copy() for bone in rig.data.bones}
    def depth(index):return 0 if skin['parents'][index]<0 else depth(skin['parents'][index])+1
    order=sorted(range(len(skin['bones'])),key=depth)
    for index in order:
        deform=C@matrix(pose['bonesToRoot'][index])@matrix(skin['inverseBindsRoot'][index])@CI
        rig.pose.bones[skin['bones'][index]].matrix=deform@rest[skin['bones'][index]];bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
    mesh_obj=bpy.data.objects.new(name+' / skinned forearms and hands',skin_meshes[identity]);collection.objects.link(mesh_obj)
    for bone_name in skin['bones']:mesh_obj.vertex_groups.new(name=bone_name)
    for index,weight in enumerate(skin['weights']):
        for bone,value in zip(weight['bones'],weight['weights']):
            if value>0:mesh_obj.vertex_groups[bone].add([index],value,'REPLACE')
    modifier=mesh_obj.modifiers.new('Calibrated linear four-weight skin','ARMATURE');modifier.object=rig;modifier.use_deform_preserve_volume=False
    bpy.context.view_layer.update();evaluated=mesh_obj.evaluated_get(bpy.context.evaluated_depsgraph_get());baked=evaluated.to_mesh()
    errors=[(vertex.co-C@vector(expected)).length for vertex,expected in zip(baked.vertices,pose['bakedRoot'])]
    assert len(baked.vertices)==len(pose['bakedRoot']);evaluated.to_mesh_clear()
    bone_errors=[(rig.pose.bones[bone_name].head-(C@matrix(pose['bonesToRoot'][index])).translation).length for index,bone_name in enumerate(skin['bones'])]
    maximum=max(errors);assert maximum<.00005,(name,'Blender/native skin mismatch',maximum)
    assert max(bone_errors)<.00005,(name,'Blender/native bone mismatch',max(bone_errors))
    for marker in pose['markers']:
        obj=bpy.data.objects.new(marker['name'],None);collection.objects.link(obj);obj.matrix_world=C@matrix(marker['matrix'])@CI;obj.empty_display_type='ARROWS';obj.empty_display_size=.035;obj['role']='Exact native attachment frame / metres'
    camera=bpy.data.objects.new(name+' / native eye',bpy.data.cameras.new(name+' eye'));collection.objects.link(camera)
    camera.matrix_world=C@matrix(pose['cameraToRoot'])@Matrix.Diagonal((1,1,-1,1));camera.data.sensor_fit='VERTICAL';camera.data.sensor_height=24;camera.data.lens=24/(2*math.tan(math.radians(pose['verticalFov'])/2));camera.data.clip_start=pose['nearClip'];camera.data.clip_end=pose['farClip']
    rig['source_pose']=pose['mode'];rig['source_identity']=identity;rig['source_sha256']=capture['sourceSha256'];rig['skin_contract']='Linear four-weight skin; no root motion. Pose is editable; this held reference is not a motion clip.'
    report={'id':name,'identity':identity,'pose':pose['mode'],'vertices':len(errors),'bones':len(bone_errors),'maximumNativeVertexErrorMetres':maximum,'maximumNativeBoneHeadErrorMetres':max(bone_errors),'leftWristErrorMetres':pose['leftWristError'],'rightWristErrorMetres':pose['rightWristError'],'nativeVerticalFov':pose['verticalFov'],'nativeImage':'RuntimeSource/'+pose['nativeImage']}
    reports.append(report)
for collection in collections:collection.hide_render=collection.name!='Player_Hip';collection.hide_viewport=collection.hide_render
scene['version']='RA-FPS-'+args.family;scene['source_sha256']=capture['sourceSha256'];scene['axes']='Unity weapon-root (x,y,z) metres -> Blender (x,z,y) metres; attachment frames are conjugated by that reflection'
scene['scope']='Four appearance skins / held hip and ADS assemblies / editable linear-skin skeletons / exact rifle LOD0 and first-person sight opening. Not a new animation library or replacement gameplay asset.'
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
bpy.context.preferences.filepaths.file_preview_type='NONE';master=folder/('FirstPerson-'+args.family+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(master))
sources=[root/'ArtSource/Original/ArtPass-v1/Characters'/f'{name}-ArtPass-v1.blend' for name in skins]
sources.append(root/'ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend')
record={'schema':'rail-arena-first-person-assembly-production/1','family':args.family,'nativeResult':args.native_result,'savedMasterResult':args.saved_master_result,'master':master.name,'masterSha256':digest(master),'source':'RuntimeSource/'+capture['source'],'sourceSha256':capture['sourceSha256'],'axes':scene['axes'],'scope':scene['scope'],'referencePoses':reports,'sourceMasters':[{'path':path.relative_to(root).as_posix(),'sha256':digest(path)} for path in sources],'provenance':'Original Rail Arena rifle and character refinements with existing CC0 foundation rig, original paint and calibrated runtime bindings. Exact held assemblies reconstructed as editable Blender rigs; preserve existing licenses and source masters. Unity native images remain shading authority.','approval':'Direction approved; full animation/reference coverage and final Sanders playtest acceptance pending'}
(folder/'production.json').write_bytes((json.dumps(record,indent=2)+'\n').encode('utf-8'))
print('FIRST_PERSON_ASSEMBLY_AUTHORED',len(reports),'poses',sum(item['vertices'] for item in reports),'verified vertices',max(item['maximumNativeVertexErrorMetres'] for item in reports),'metres max error')
