"""Headless Blender construction and export. Consumes the same mesh generator as SVG sheets."""
import bpy,bmesh,json,sys,hashlib,os,math
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'tools'))
from build_sky_blueprints import M,OUT,mesh
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
collections={};records=[];bounds={}
palette={'structure':(.65,.61,.47,1),'cover':(.5,.49,.39,1),'ivory_trim':(.83,.78,.62,1),'bronze':(.32,.19,.065,1),'teal':(.04,.55,.5,1),'bark':(.18,.105,.045,1),'foliage':(.12,.3,.055,1),'grass':(.25,.4,.075,1),'water':(.08,.52,.65,.7),'foam':(.74,.92,.94,1),'summit':(.65,.61,.47,1),'ramps':(.67,.63,.5,1)}
materials={}
for role,color in palette.items():
 mat=bpy.data.materials.new(role);mat.diffuse_color=color;mat.use_nodes=True
 node=mat.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=color;node.inputs['Roughness'].default_value=.45 if role in ('water','bronze') else .8
 if role=='bronze':node.inputs['Metallic'].default_value=.65
 if role=='water':node.inputs['Alpha'].default_value=.7;mat.surface_render_method='DITHERED'
 materials[role]=mat
for o in M['objects']:
 group=o['group']
 if group not in collections:
  c=bpy.data.collections.new(group);bpy.context.scene.collection.children.link(c);collections[group]=c
 vertices,faces=mesh(o);center=[sum(p[k] for p in vertices)/len(vertices) for k in range(3)]
 local=[[p[k]-center[k] for k in range(3)] for p in vertices]
 data=bpy.data.meshes.new(o['id']);data.from_pydata(local,[],faces);data.update()
 bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(data);bm.free();data.update()
 obj=bpy.data.objects.new(o['id'],data);collections[group].objects.link(obj);obj.location=center
 obj.data.materials.append(materials.get(group,materials['structure']))
 obj['blueprint_id']=o['id'];obj['gameplay_collision']=o['collision'];obj['role']=group
 if group.endswith('envelopes') or group.endswith('reservations'):obj.display_type='WIRE';obj.hide_render=True
 data.calc_loop_triangles();triangles=[i for tri in data.loop_triangles for i in tri.vertices]
 records.append(dict(id=o['id'],group=group,collision=o['collision'],center=center,vertices=[q for v in local for q in v],triangles=triangles))
 bounds[o['id']]=[[min(p[k] for p in vertices) for k in range(3)],[max(p[k] for p in vertices) for k in range(3)]]
for point in M['spawns']:
 obj=bpy.data.objects.new(point['id'],None);obj.location=point['position'];obj.empty_display_type='ARROWS';obj.empty_display_size=.6;bpy.context.scene.collection.objects.link(obj)
(OUT/'MeshData.json').write_text(json.dumps(dict(objects=records)),encoding='utf-8')
# Bounded neutral studio review cameras are stored in the master.
if os.environ.get('RAIL_BLUEPRINT_PACKAGE','').endswith('v2'):
 from mathutils import Vector
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
 scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 scene.world=bpy.data.worlds.new('Review sky');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.65,.72,.8,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
 light=bpy.data.lights.new('Review sunlight','SUN');light.energy=3;sun=bpy.data.objects.new('Review sunlight',light);scene.collection.objects.link(sun);sun.rotation_euler=(.4,-.45,-.4)
 camera=bpy.data.cameras.new('Review camera');obj=bpy.data.objects.new('Review camera',camera);scene.collection.objects.link(obj);scene.camera=obj
 for name,position,target,ortho in [('Detailed-Overview',(65,-80,65),(0,0,6),88),('Detailed-Summit',(20,-25,36),(0,0,23.5),22),('Detailed-Garden',(38,-26,8),(29.5,-16,1.5),15)]:
  obj.location=position;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler();camera.type='ORTHO';camera.ortho_scale=ortho
  scene.render.filepath=str(OUT/(name+'.png'))
  if os.environ.get('RAIL_SKIP_REVIEW_RENDER')!='1':bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SkyGardens-Construction.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'SkyGardens-Construction.glb'),export_format='GLB',export_extras=True,export_yup=True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'SkyGardens-Construction.fbx'),object_types={'MESH','EMPTY'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,add_leaf_bones=False)
# Reimport the portable mesh export, then check every object against canonical bounds.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/'SkyGardens-Construction.glb'))
maximum=0;checked=0
for obj in bpy.context.scene.objects:
 if obj.type!='MESH':continue
 key=obj.get('blueprint_id',obj.name)
 if key not in bounds:raise RuntimeError('Unknown exported mesh '+key)
 vertices=[obj.matrix_world@v.co for v in obj.data.vertices]
 actual=[[min(v[k] for v in vertices) for k in range(3)],[max(v[k] for v in vertices) for k in range(3)]]
 error=max(abs(actual[i][k]-bounds[key][i][k]) for i in range(2) for k in range(3));maximum=max(maximum,error);checked+=1
 if error>.001:raise RuntimeError('Roundtrip bounds mismatch '+key+str(error))
assert checked==len(M['objects'])
report=dict(meshes=checked,maximumRoundtripBoundsErrorMetres=maximum,sourceSha256=hashlib.sha256((OUT/'Scene.json').read_bytes()).hexdigest(),blender=bpy.app.version_string,status='PASS; construction geometry only, not gameplay or detailed art acceptance')
(OUT/'Blender-Validation.json').write_text(json.dumps(report,indent=2));print('SKY_CONSTRUCTION_VERIFIED',checked,maximum)
