"""Author release/grasp/return finger tracks on the measured FPS rig in Blender."""
import argparse,gzip,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector,Quaternion
from mathutils.bvhtree import BVHTree
parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.root.resolve()
folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1';folder.mkdir(parents=True,exist_ok=True)
master=folder/'Carbine-Reload-Hands-v1.blend';assert not master.exists(),'Preserve versioned authored masters'
assembly=root/'ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2';spec=json.loads((assembly/'production.json').read_text(encoding='utf-8'))
source=assembly/spec['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==spec['sourceSha256'];data=json.loads(gzip.decompress(source.read_bytes()));skin=data['skins'][0]
calibration_path=folder/'Source/reload-rig.json.gz';calibration_bytes=gzip.decompress(calibration_path.read_bytes()) if calibration_path.is_file() else (root/'Validation/ReloadContacts-01/NativeCalibrated-06/reload-rig.json').read_bytes();calibration_path.parent.mkdir(exist_ok=True);calibration_path.write_bytes(gzip.compress(calibration_bytes,mtime=0));calibration=json.loads(calibration_bytes);assert calibration['passed'] and len(calibration['frames'])==121 and len({f['enginePoseFrame'] for f in calibration['frames']})==121
assert calibration['bones']==skin['bones'];assert calibration['maximumWristError']<.0005
M=lambda x:Matrix([x['values'][i:i+4] for i in range(0,16,4)])
V=lambda p:Vector([p[a] for a in 'xyz'])
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)));CI=C.inverted()
base=[M(m) for m in calibration['frames'][21]['bonesToRoot']];names=skin['bones'];parents=skin['parents'];wi=names.index('LeftHand');wrist=base[wi]
verts=[];faces=[]
for part in data['rifle']:
 if not part['name'].startswith('RF_11_'):continue
 offset=len(verts);frame=M(part['toRoot']);points=[frame@V(v) for v in part['geometry']['vertices']];normals=[(frame.to_3x3().inverted().transposed()@V(n)).normalized() for n in part['geometry']['normals']];verts+=points
 for section in part['geometry']['submeshes']:
  values=section['values']
  for i in range(0,len(values),3):
   tri=values[i:i+3];a,b,c=[points[j] for j in tri]
   if (b-a).cross(c-a).dot(sum([normals[j] for j in tri],Vector()))<0:tri.reverse()
   faces.append(tuple(offset+j for j in tri))
bvh=BVHTree.FromPolygons(verts,faces,all_triangles=True)
mesh_to_root=M(skin['meshToRoot']);unposed=[mesh_to_root@V(v) for v in skin['geometry']['vertices']];binds=[M(m) for m in skin['inverseBindsRoot']]
joints=['LeftHand'+finger+str(i) for finger in ['Thumb','Index','Middle','Ring','Pinky'] for i in (1,2,3)]

def tip(index):
 selected=[i for i,w in enumerate(skin['weights']) if w['bones'][0]==index and w['weights'][0]>.9999]
 coords=[(i,(binds[index]@unposed[i]).y) for i in selected];end=max(v for _,v in coords);ids=[i for i,v in coords if abs(v-end)<1e-7]
 # The source ring centre, transformed by the exact native finger matrix.
 local=sum([binds[index]@unposed[i] for i in ids],Vector())/len(ids)
 return local

def solve_chain(points,target,outward):
 lengths=[(b-a).length for a,b in zip(points,points[1:])];anchor=points[0].copy();assert (target-anchor).length<sum(lengths)
 current=[anchor]
 for i,length in enumerate(lengths):
  direction=(outward if i==0 else Vector((-.8,-.6,0)) if i==1 else Vector((-1,.25,0))).normalized();current.append(current[-1]+direction*length)
 for _ in range(100):
  current[-1]=target.copy()
  for i in range(2,-1,-1):current[i]=current[i+1]+(current[i]-current[i+1]).normalized()*lengths[i]
  current[0]=anchor.copy()
  for i in range(3):current[i+1]=current[i]+(current[i+1]-current[i]).normalized()*lengths[i]
  if (current[-1]-target).length<1e-7:break
 assert (current[-1]-target).length<.00002
 return current

def pose_chain(indices,desired):
 matrices={};parent=wrist
 for j,index in enumerate(indices):
  original_parent=base[wi] if j==0 else base[indices[j-1]];local=original_parent.inverted()@base[index];current=parent@local
  child=current@(base[index].inverted()@base[indices[j+1]].translation) if j<2 else current@tip(index)
  origin=current.translation;rotation=(child-origin).rotation_difference(desired[j+1]-origin).to_matrix().to_4x4()
  current=Matrix.Translation(origin)@rotation@Matrix.Translation(-origin)@current;matrices[index]=current;parent=current
 return matrices

open_deltas={};grasp_deltas={};contacts=[]
goals={'Thumb':(-.041,-.112,.018),'Index':(.01,-.16,.008),'Middle':(.01,-.16,.033),'Ring':(.01,-.16,.055),'Pinky':(.016,-.151,.067)}
for finger in ['Thumb','Index','Middle','Ring','Pinky']:
 indices=[names.index('LeftHand'+finger+str(i)) for i in (1,2,3)];points=[base[i].translation for i in indices]+[base[indices[-1]]@tip(indices[-1])]
 nearest,normal,face,distance=bvh.find_nearest(Vector(goals[finger]));target=nearest+normal*.009
 desired=solve_chain(points,target,Vector((-1,0,0)) if finger=='Thumb' else Vector((.25,-.97,0)))
 grasp=pose_chain(indices,desired)
 direction=(points[0]-wrist.translation).normalized();opened=[points[0]]
 for a,b in zip(points,points[1:]):opened.append(opened[-1]+direction*(b-a).length)
 release=pose_chain(indices,opened)
 for j,index in enumerate(indices):
  parent_index=wi if j==0 else indices[j-1];original=(base[parent_index].inverted()@base[index]).to_quaternion().normalized()
  grasp_parent=wrist if j==0 else grasp[parent_index];release_parent=wrist if j==0 else release[parent_index]
  grasp_deltas[names[index]]=(original.inverted()@(grasp_parent.inverted()@grasp[index]).to_quaternion()).normalized()
  open_deltas[names[index]]=(original.inverted()@(release_parent.inverted()@release[index]).to_quaternion()).normalized()
 before_tip=points[-1];after_tip=grasp[indices[-1]]@tip(indices[-1]);before_distance=bvh.find_nearest(before_tip)[3];after_distance=bvh.find_nearest(after_tip)[3]
 contacts.append({'finger':finger,'beforeTipCentreToSurfaceMetres':before_distance,'afterTipCentreToSurfaceMetres':after_distance,'targetCentreClearanceMetres':.009,'targetSurfacePointUnityMetres':list(nearest),'targetNormalUnity':list(normal),'solvedTipCentreUnityMetres':list(after_tip)})
 assert after_distance<.0091,(finger,after_distance)
 print('FINGER_CONTACT',finger,before_distance,after_distance)

bpy.ops.wm.open_mainfile(filepath=str(assembly/spec['master']));scene=bpy.context.scene
for collection in list(bpy.data.collections):
 if collection.name!='Player_Hip':
  for obj in list(collection.objects):bpy.data.objects.remove(obj,do_unlink=True)
  bpy.data.collections.remove(collection)
collection=bpy.data.collections['Player_Hip'];collection.hide_viewport=False;collection.hide_render=False
rig=next(o for o in collection.objects if o.type=='ARMATURE');rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
controls=[];identity=Quaternion((1,0,0,0))
for name in joints:
 obj=bpy.data.objects.new('Author / '+name,None);collection.objects.link(obj);obj.rotation_mode='QUATERNION';obj.empty_display_size=.01;controls.append(obj)
 previous=None
 for frame,rotation in [(0,identity),(9,identity),(15,open_deltas[name]),(21,grasp_deltas[name]),(91,grasp_deltas[name]),(98,open_deltas[name]),(105,grasp_deltas[name]),(118,identity),(120,identity)]:
  rotation=rotation.copy()
  if previous is not None and previous.dot(rotation)<0:rotation.negate()
  previous=rotation.copy()
  obj.rotation_quaternion=rotation;obj.keyframe_insert('rotation_quaternion',frame=frame,group=name)
 obj.animation_data.action.name='RA_Reload_FingerDelta_'+name
 for curve in obj.animation_data.action.layers[0].strips[0].channelbag(obj.animation_data.action_slot).fcurves:
  for key in curve.keyframe_points:key.interpolation='BEZIER';key.handle_left_type='AUTO_CLAMPED';key.handle_right_type='AUTO_CLAMPED'

def depth(i):return 0 if parents[i]<0 else depth(parents[i])+1
order=sorted(range(len(names)),key=depth);joint_map={names.index(name):i for i,name in enumerate(joints)}
reload=json.loads((root/'ArtSource/Original/ArtPass-v1/Gate2/RifleActions/ReloadAction.json').read_text(encoding='utf-8'));pivot=V(reload['magazinePivot'])
rifle={part['name']:part for part in data['rifle']};parts=[o for o in collection.objects if o.type=='MESH' and o.name in rifle]
camera=next(o for o in collection.objects if o.type=='CAMERA');scene.camera=camera;samples=[]
for frame in range(121):
 scene.frame_set(frame);deltas=[obj.rotation_quaternion.normalized() for obj in controls];samples.append({'leftDeltas':[dict(zip('xyzw',[q.x,q.y,q.z,q.w])) for q in deltas]})
 native=[M(m) for m in calibration['frames'][frame]['bonesToRoot']];changed={}
 for i in order:
  if i in joint_map:
   parent=parents[i];local=native[parent].inverted()@native[i];changed[i]=changed.get(parent,native[parent])@local@deltas[joint_map[i]].to_matrix().to_4x4()
  else:changed[i]=native[i]
  deform=C@changed[i]@binds[i]@CI;bone=rig.pose.bones[names[i]];bone.rotation_mode='QUATERNION';bone.matrix=deform@rest[names[i]];bpy.context.view_layer.update()
  for channel in ['location','rotation_quaternion','scale']:bone.keyframe_insert(channel,frame=frame,group=names[i])
 carrier=M(calibration['frames'][frame]['magazine']);movement=carrier@Matrix.Translation(-pivot)
 for obj in parts:
  part=rifle[obj.name];transform=M(part['toRoot']);obj.matrix_world=C@(movement@transform if obj.name.startswith('RF_11_') else transform)@CI;obj.rotation_mode='QUATERNION'
  for channel in ['location','rotation_quaternion','scale']:obj.keyframe_insert(channel,frame=frame,group=obj.name)
 camera.matrix_world=C@M(calibration['frames'][frame]['cameraToRoot'])@Matrix.Diagonal((1,1,-1,1))
 for channel in ['location','rotation_quaternion','scale']:camera.keyframe_insert(channel,frame=frame)
rig.animation_data.action.name='RA_FPS_Reload_Hands_2s_Calibrated'
scene.frame_start=0;scene.frame_end=120;scene.render.fps=60;scene['scope']='Original release/grasp/return finger action on the measured compatible rig; existing Blender wrist/magazine action and native two-bone calibration retained. Cosmetic presentation only, not a replacement Avatar.'
scene['source_sha256']=spec['sourceSha256'];scene.frame_set(21);bpy.context.view_layer.update();bpy.context.preferences.filepaths.file_preview_type='NONE';bpy.ops.wm.save_as_mainfile(filepath=str(master))
record={'schema':'rail-arena-reload-hands/1','fps':60,'durationSeconds':2,'joints':joints,'samples':samples,'master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'assemblyMasterSha256':spec['masterSha256'],'assemblySourceSha256':spec['sourceSha256'],'nativeCalibration':calibration_path.relative_to(root).as_posix(),'nativeCalibrationSha256':hashlib.sha256(calibration_bytes).hexdigest(),'nativeCalibrationResult':'Validation/reload-hand-source-calibrated-06.xml','originalWristMagazineAction':'ArtSource/Original/ArtPass-v1/Gate2/RifleActions/ReloadAction.json','originalActionSha256':hashlib.sha256((root/'ArtSource/Original/ArtPass-v1/Gate2/RifleActions/ReloadAction.json').read_bytes()).hexdigest(),'contacts':contacts,'scope':scene['scope'],'limits':'Contact target measurement is fingertip sleeve-ring centre to RF-11 surface, not full skin collision certification. Fine glove/palm fit, tap contact, transition/interruption and native artistic acceptance require review. Existing two-second gameplay reload remains authoritative.','provenance':'Original Blender-authored finger poses using original Rail Arena rifle and measured character geometry; existing CC0 foundation license retained.'}
(folder/'ReloadHandAction.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print('RELOAD_HANDS_AUTHORED',len(samples),'frames',len(joints),'joints',record['masterSha256'])
