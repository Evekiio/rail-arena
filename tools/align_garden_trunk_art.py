"""Align the editable branch scaffold to measured crown centres, preserving its original unit envelope."""
import bpy,bmesh,hashlib,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve();folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature';spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));source=json.loads((folder/'nature-source.json').read_text(encoding='utf-8'));master=folder/spec['master'];assert hashlib.sha256(master.read_bytes()).hexdigest()==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
trunk=next(p for p in source['parts'] if p['sourceName']=='TREE-1-BARK');crown=next(p for p in source['parts'] if p['sourceName']=='TREE-1-LEAVES')
def xyz(value):return Vector((value['x'],value['y'],value['z']))
def blender(value):return Vector((value.x,-value.z,value.y))
trunk_scale=Vector((trunk['frameSize']['x'],trunk['frameSize']['z'],trunk['frameSize']['y']));crown_scale=Vector((crown['frameSize']['x'],crown['frameSize']['z'],crown['frameSize']['y']));delta=blender(xyz(crown['frameCenter'])-xyz(trunk['frameCenter']))
targets=[]
for obj in bpy.data.collections['CrownA_LOD0'].objects:
 if not obj.name.startswith('Layered leaf crown '):continue
 points=[v.co for v in obj.data.vertices];center=Vector(tuple((min(p[i] for p in points)+max(p[i] for p in points))/2 for i in range(3)))
 world=Vector(tuple(center[i]*crown_scale[i] for i in range(3)))+delta;world.z-=.08
 target=Vector(tuple(world[i]/trunk_scale[i] for i in range(3)))
 # Choose a contact point inside the crown, with margin for the branch end ring.
 target=Vector(tuple(max(-.475,min(.475,value)) for value in target))
 crown_point=Vector(tuple((target[i]*trunk_scale[i]-delta[i])/crown_scale[i] for i in range(3)))
 if any(face.normal.dot(crown_point-obj.data.vertices[face.vertices[0]].co)>.00001 for face in obj.data.polygons):
  raise AssertionError(('Bark-envelope contact is not inside its convex crown',obj.name,tuple(target)))
 targets.append((math.atan2(target.y,target.x)%math.tau,obj.name,target,obj))
targets.sort(key=lambda item:item[0]);assert len(targets)==8
anchor_records=[]
for level in range(3):
 collection=bpy.data.collections['Trunk_LOD'+str(level)];branches=sorted([obj for obj in collection.objects if obj.name.startswith('Radial branch ')],key=lambda obj:int(obj.name.split(' ')[-1].split('.')[0]));assert len(branches)==8
 for index,obj in enumerate(branches):
  count=len(obj.data.vertices)//3;assert count in (7,5,3)
  old=[]
  for ring in range(3):old.append(sum((obj.data.vertices[ring*count+j].co.copy() for j in range(count)),Vector())/count)
  target=targets[index][2];middle=old[0].lerp(target,.55);middle.z+=.035;new=[old[0],middle,target]
  for ring in range(3):
   old_direction=(old[min(2,ring+1)]-old[max(0,ring-1)]).normalized();new_direction=(new[min(2,ring+1)]-new[max(0,ring-1)]).normalized();rotation=old_direction.rotation_difference(new_direction)
   for j in range(count):
    vertex=obj.data.vertices[ring*count+j];vertex.co=new[ring]+rotation@(vertex.co-old[ring])
  crown_object=targets[index][3]
  for j in range(count):
   tip=obj.data.vertices[2*count+j].co
   crown_point=Vector(tuple((tip[i]*trunk_scale[i]-delta[i])/crown_scale[i] for i in range(3)))
   if any(face.normal.dot(crown_point-crown_object.data.vertices[face.vertices[0]].co)>.00001 for face in crown_object.data.polygons):
    raise AssertionError(('Branch end ring is not enclosed by crown',obj.name,level))
  topology=bmesh.new();topology.from_mesh(obj.data);bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces));topology.to_mesh(obj.data);topology.free()
  anchor_records.append({'level':level,'branch':obj.name,'crownPart':targets[index][1],'targetCanonicalBlender':list(target),'tipInsetMetres':.08,'endRingInsideConvexCrown':True})
 points=[v.co.copy() for obj in collection.objects for v in obj.data.vertices]
 assert all(max(abs(c) for c in p)<=.50001 for p in points),'Branch rotation crossed the original reference envelope'
 bpy.ops.object.select_all(action='DESELECT');copies=[]
 for obj in collection.objects:
  clone=obj.copy();clone.data=obj.data.copy();bpy.context.scene.collection.objects.link(clone);clone.select_set(True);copies.append(clone)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object;joined.name='Trunk_LOD'+str(level);joined.data.calc_loop_triangles();assert len(joined.data.materials)==1
 export=root/next(x['export'] for x in spec['lods'] if x['family']=='Trunk' and x['level']==level)
 bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,bake_anim=False,use_mesh_modifiers=True,add_leaf_bones=False)
 lod=next(x for x in spec['lods'] if x['family']=='Trunk' and x['level']==level);vertices=[list(v.co) for v in joined.data.vertices]
 lo=[min(v[i] for v in vertices) for i in range(3)];hi=[max(v[i] for v in vertices) for i in range(3)]
 lod.update({'triangles':len(joined.data.loop_triangles),'editableParts':len(collection.objects),'boundsMinBlender':lo,'boundsMaxBlender':hi,'canonicalVerticesBlender':[{'x':v[0],'y':v[1],'z':v[2]} for v in vertices],'exportSha256':hashlib.sha256(export.read_bytes()).hexdigest()})
 if level==0:
  module=next(x for x in spec['modules'] if x['family']=='Trunk');module['geometryDimensionsMetresBlenderXYZ']=[hi[i]-lo[i] for i in range(3)];module['parts']=len(collection.objects)
 bpy.data.objects.remove(joined,do_unlink=True)
old_master_sha=spec['masterSha256'];bpy.ops.wm.save_as_mainfile(filepath=str(master));spec['masterSha256']=hashlib.sha256(master.read_bytes()).hexdigest();spec['branchScaffold']={'authority':'CrownA near cluster interiors mapped through TREE-1 audited frames; contacts sit 0.08m below centres and within the original bark unit envelope, with every branch end-ring vertex checked against every convex crown face. Clockwise order prevents crossed assignment. All LODs share targets. Other crown/site variants require native visual contact review.','previousMasterSha256':old_master_sha,'anchors':anchor_records}
(folder/'production.json').write_bytes((json.dumps(spec,indent=2)+'\n').encode('utf-8'))
print('GARDEN_TRUNK_SCAFFOLD_ALIGNED 8 branches / 3 LODs / original unit envelope retained')
