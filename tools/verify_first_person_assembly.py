"""Verify the saved editable assembly artifact against native source, not transient authoring state."""
import argparse,gzip,hashlib,json,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector
parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);parser.add_argument('--output',default='Validation/fps-assembly-saved-master.json');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.root.resolve();folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));source=folder/spec['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==spec['sourceSha256'];native=json.loads(gzip.decompress(source.read_bytes()))
master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256'];bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)));CI=C.inverted()
def matrix(item):return Matrix(tuple(tuple(item['values'][row*4+col] for col in range(4)) for row in range(4)))
def vector(item):return Vector((item['x'],item['y'],item.get('z',0)))
skins={item['identity']:item for item in native['skins']};reports=[]
for pose in native['poses']:
    name=pose['identity']+'_'+pose['mode'];collection=bpy.data.collections[name]
    for item in spec['referencePoses']:
        other=bpy.data.collections[item['id']];other.hide_render=item['id']!=name;other.hide_viewport=other.hide_render
    bpy.context.view_layer.update();rig=next(obj for obj in collection.objects if obj.type=='ARMATURE');skin=skins[pose['identity']];mesh_obj=next(obj for obj in collection.objects if obj.name.startswith(name+' / skinned'))
    assert len(rig.data.bones)==len(skin['bones'])==54
    expected_rest=[C@(matrix(skin['meshToRoot'])@vector(point)) for point in skin['geometry']['vertices']]
    rest_error=max((vertex.co-expected).length for vertex,expected in zip(mesh_obj.data.vertices,expected_rest));assert rest_error<.000001
    for index,weight in enumerate(skin['weights']):
        expected={bone:value for bone,value in zip(weight['bones'],weight['weights']) if value>0};actual={entry.group:entry.weight for entry in mesh_obj.data.vertices[index].groups}
        assert expected.keys()==actual.keys() and all(abs(value-actual[bone])<.000001 for bone,value in expected.items())
    for bone_name,parent in zip(skin['bones'],skin['parents']):
        actual=rig.data.bones[bone_name].parent;assert (actual.name if actual else None)==(skin['bones'][parent] if parent>=0 else None)
    evaluated=mesh_obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();assert len(mesh.vertices)==len(pose['bakedRoot'])
    vertex_error=max((vertex.co-C@vector(expected)).length for vertex,expected in zip(mesh.vertices,pose['bakedRoot']));evaluated.to_mesh_clear();assert vertex_error<.00005,(name,vertex_error)
    bone_error=max((rig.pose.bones[bone].head-(C@matrix(pose['bonesToRoot'][index])).translation).length for index,bone in enumerate(skin['bones']));assert bone_error<.00005
    rigid_error=0;rigid_vertices=0;maximum_frame_relative=0;maximum_pivot_error=0
    for part in native['rifle']:
        obj=next(obj for obj in collection.objects if obj.type=='MESH' and obj.data.name==part['name']);expected=[C@(matrix(part['toRoot'])@vector(value)) for value in part['geometry']['vertices']]
        expected_frame=C@matrix(part['toRoot'])@CI
        # Imported rifle frames contain 100x scale; compare their linear basis relatively,
        # while preserving strict physical pivot/vertex tolerances in metres.
        frame_scale=max(abs(expected_frame[row][col]) for row in range(3) for col in range(3))
        frame_relative=max(abs(obj.matrix_world[row][col]-expected_frame[row][col]) for row in range(3) for col in range(3))/frame_scale
        pivot_error=(obj.matrix_world.translation-expected_frame.translation).length
        assert frame_relative<.000001 and pivot_error<.000001,(name,part['name'],frame_relative,pivot_error)
        assert max(abs(a-b) for a,b in zip(obj['native_source_frame_row_major'],part['toRoot']['values']))<1e-12
        maximum_frame_relative=max(maximum_frame_relative,frame_relative);maximum_pivot_error=max(maximum_pivot_error,pivot_error)
        assert len(obj.data.vertices)==len(expected);rigid_vertices+=len(expected);rigid_error=max(rigid_error,max((obj.matrix_world@vertex.co-target).length for vertex,target in zip(obj.data.vertices,expected)))
        expected_faces=[(tuple(sorted(section['values'][i:i+3])),section_index) for section_index,section in enumerate(part['geometry']['submeshes']) for i in range(0,len(section['values']),3)]
        assert [(tuple(sorted(face.vertices)),face.material_index) for face in obj.data.polygons]==expected_faces
        for loop in obj.data.loops:
            expected_uv=part['geometry']['uv'][loop.vertex_index];actual_uv=obj.data.uv_layers.active.data[loop.index].uv;assert abs(actual_uv.x-expected_uv['x'])<.000001 and abs(actual_uv.y-expected_uv['y'])<.000001
    assert rigid_error<.000001
    camera=next(obj for obj in collection.objects if obj.type=='CAMERA');expected=C@matrix(pose['cameraToRoot'])@Matrix.Diagonal((1,1,-1,1));camera_error=max(abs(camera.matrix_world[row][col]-expected[row][col]) for row in range(4) for col in range(4));assert camera_error<.000001
    for marker in pose['markers']:
        obj=next(obj for obj in collection.objects if obj.type=='EMPTY' and obj.name.split('.')[0]==marker['name']);expected=C@matrix(marker['matrix'])@CI
        assert max(abs(obj.matrix_world[row][col]-expected[row][col]) for row in range(4) for col in range(4))<.000001
    reports.append({'id':name,'skinVertices':len(pose['bakedRoot']),'rigidVertices':rigid_vertices,'bones':54,'maximumSkinErrorMetres':vertex_error,'maximumRestErrorMetres':rest_error,'maximumBoneHeadErrorMetres':bone_error,'maximumRigidErrorMetres':rigid_error,'maximumRifleFrameRelativeError':maximum_frame_relative,'maximumRiflePivotErrorMetres':maximum_pivot_error,'cameraMatrixError':camera_error,'weightsTopologyUvsAndSocketsVerified':True})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
output=(root/args.output).resolve();assert output.is_relative_to(root);output.parent.mkdir(parents=True,exist_ok=True);assert not output.exists(),'Preserve earlier verification';output.write_text(json.dumps({'masterSha256':digest,'sourceSha256':spec['sourceSha256'],'scope':'Read-only verification after reopening the actual saved Blender master. All eight held poses; calibrated weights and hierarchy; all rifle triangle/material assignments and UVs; camera/socket frames. Not runtime import, new animation, shading equivalence or final hand quality acceptance.','assemblies':reports,'passed':True},indent=2)+'\n',encoding='utf-8')
print('FPS_ASSEMBLY_SAVED_MASTER_VERIFIED',len(reports),'assemblies',sum(item['skinVertices'] for item in reports),'skin vertices',sum(item['rigidVertices'] for item in reports),'rigid vertices; unchanged master')
