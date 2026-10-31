"""Reopen and verify the actual reload hand master, then render measured pose views."""
import argparse,gzip,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector,Quaternion
parser=argparse.ArgumentParser();parser.add_argument('root',type=Path);parser.add_argument('--verify-only',action='store_true');args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);root=args.root.resolve();folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1'
spec=json.loads((folder/'ReloadHandAction.json').read_text(encoding='utf-8'));master=folder/spec['master'];assert hashlib.sha256(master.read_bytes()).hexdigest()==spec['masterSha256']
assembly=root/'ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v2';a=json.loads((assembly/'production.json').read_text(encoding='utf-8'));native=json.loads(gzip.decompress((assembly/a['source']).read_bytes()));skin=native['skins'][0]
calibration_bytes=(root/spec['nativeCalibration']).read_bytes();calibration_bytes=gzip.decompress(calibration_bytes) if spec['nativeCalibration'].endswith('.gz') else calibration_bytes;calibration=json.loads(calibration_bytes);assert hashlib.sha256(calibration_bytes).hexdigest()==spec['nativeCalibrationSha256']
M=lambda x:Matrix([x['values'][i:i+4] for i in range(0,16,4)])
V=lambda p:Vector([p[a] for a in 'xyz'])
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)));CI=C.inverted();binds=[M(m) for m in skin['inverseBindsRoot']];unposed=[M(skin['meshToRoot'])@V(v) for v in skin['geometry']['vertices']]
names=skin['bones'];parents=skin['parents'];joint_map={names.index(name):i for i,name in enumerate(spec['joints'])}
def depth(i):return 0 if parents[i]<0 else depth(parents[i])+1
order=sorted(range(len(names)),key=depth)
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False);scene=bpy.context.scene;collection=bpy.data.collections['Player_Hip'];rig=next(o for o in collection.objects if o.type=='ARMATURE');surface=next(o for o in collection.objects if o.type=='MESH' and 'skinned forearms' in o.name);eye=next(o for o in collection.objects if o.type=='CAMERA')
maximum=0;maximum_bone=0;all_points=[];bounds=[];keyframes=[(0,'Support'),(15,'Release'),(21,'Grasp'),(65,'Withdrawn'),(98,'Tap'),(120,'Return')]
for frame in range(121):
 scene.frame_set(frame);bpy.context.view_layer.update();matrices=[M(m) for m in calibration['frames'][frame]['bonesToRoot']];new={}
 for i in order:
  if i in joint_map:
   q=spec['samples'][frame]['leftDeltas'][joint_map[i]];delta=Quaternion((q['w'],q['x'],q['y'],q['z']));parent=parents[i];new[i]=new[parent]@(matrices[parent].inverted()@matrices[i])@delta.to_matrix().to_4x4()
  else:new[i]=matrices[i]
  maximum_bone=max(maximum_bone,(rig.pose.bones[names[i]].head-(C@new[i]).translation).length)
 evaluated=surface.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();points=[]
 for index,vertex in enumerate(mesh.vertices):
  weights=skin['weights'][index];expected=sum([(new[bone]@binds[bone]@unposed[index])*weight for bone,weight in zip(weights['bones'],weights['weights']) if weight>0],Vector());actual=evaluated.matrix_world@vertex.co;maximum=max(maximum,(actual-C@expected).length);points.append(actual)
 evaluated.to_mesh_clear()
 if any(f==frame for f,_ in keyframes):
  for obj in collection.objects:
   if obj.type=='MESH' and obj!=surface:points += [obj.matrix_world@v.co for v in obj.data.vertices]
  low=[min(p[i] for p in points) for i in range(3)];high=[max(p[i] for p in points) for i in range(3)];bounds.append({'frame':frame,'minimumBlenderXYZMetres':low,'maximumBlenderXYZMetres':high,'sizeBlenderXYZMetres':[b-a for a,b in zip(low,high)]});all_points+=points
assert maximum<.00005,('Saved master skin mismatch',maximum);assert maximum_bone<.00005,('Saved master bone mismatch',maximum_bone)
for frame in [0,120]:
 for q in spec['samples'][frame]['leftDeltas']:assert abs(abs(q['w'])-1)<1e-6 and max(abs(q[axis]) for axis in 'xyz')<1e-6
report={'masterSha256':spec['masterSha256'],'frames':121,'skinVerticesPerFrame':len(unposed),'skinSamples':len(unposed)*121,'maximumSkinErrorMetres':maximum,'maximumBoneHeadErrorMetres':maximum_bone,'scope':'Reopened saved master versus independent native-frame FK and original four-weight skin. Finger deltas are Blender-authored; wrist/magazine retain original action. Not runtime import or complete surface collision certification.','passed':True}
(root/'Validation/reload-hands-saved-master.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print('RELOAD_HAND_SAVED_MASTER_VERIFIED',maximum,maximum_bone)
if args.verify_only:sys.exit(0)
scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.view_settings.view_transform='Standard';scene.render.resolution_percentage=100
if scene.world is None:scene.world=bpy.data.worlds.new('Reload reference background')
scene.world.color=(.045,.065,.075);scene.display.shading.background_type='WORLD'
low=Vector([min(p[i] for p in all_points) for i in range(3)]);high=Vector([max(p[i] for p in all_points) for i in range(3)]);center=(low+high)*.5;span=math.ceil(max(high-low)*12)/10
camera=bpy.data.objects.new('Reload equal-scale camera',bpy.data.cameras.new('Reload orthographic'));scene.collection.objects.link(camera);camera.data.type='ORTHO';camera.data.ortho_scale=span
views=[('front',(0,1,0)),('rear',(0,-1,0)),('left',(-1,0,0)),('right',(1,0,0)),('top',(0,0,1)),('bottom',(0,0,-1))]
for frame,label in keyframes:
 directory=folder/'References'/label;directory.mkdir(parents=True,exist_ok=True);scene.frame_set(frame);bpy.context.view_layer.update();scene.camera=camera;scene.render.resolution_x=scene.render.resolution_y=1024
 for name,axis in views:
  direction=Vector(axis);camera.location=center+direction*4;camera.rotation_euler=(-direction).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(directory/(name+'.png'));bpy.ops.render.render(write_still=True)
 scene.camera=eye;scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.filepath=str(directory/'native-eye-workbench.png');bpy.ops.render.render(write_still=True)
assert hashlib.sha256(master.read_bytes()).hexdigest()==spec['masterSha256']
(folder/'measured-views.json').write_text(json.dumps({'masterSha256':spec['masterSha256'],'axes':'Blender X right Y forward Z up; Unity (x,y,z) converted as (x,z,y). Metres.','orthographicSpanMetres':span,'centerBlenderXYZMetres':list(center),'poses':[{'frame':frame,'label':label,'bounds':bound,'views':[v[0] for v in views]} for (frame,label),bound in zip(keyframes,bounds)],'scope':report['scope']},indent=2)+'\n',encoding='utf-8')
print('RELOAD_HAND_REFERENCES_RENDERED 36 orthographics / 6 matched eye views / master unchanged')
