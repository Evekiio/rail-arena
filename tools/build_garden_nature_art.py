"""Original opaque garden families, editable Blender construction and reducing LODs."""
import bpy,bmesh,hashlib,json,math,random,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature'
exports=root/'RailArena/Assets/RailArena/Art/ArtPass/Nature/GardenNature'
exports.mkdir(parents=True,exist_ok=True)
source=folder/'nature-source.json';assert source.is_file()
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette=[('BarkShade',(.20,.25,.23)),('Bark',(.39,.41,.31)),('BarkLight',(.60,.60,.43)),('LeafShade',(.08,.29,.24)),('LeafJade',(.18,.48,.34)),('LeafLight',(.35,.65,.40)),('LeafSun',(.62,.76,.40)),('LeafVein',(.78,.83,.55)),('Petal',(.88,.91,.78))]
image=bpy.data.images.new('Garden painted palette / original',width=384,height=384,alpha=True)
image.colorspace_settings.name='sRGB';pixels=[]
for y in range(384):
 for x in range(384):
  index=(y//128)*3+x//128;u=(x%128)/127;v=(y%128)/127;color=palette[index][1]
  edge=(abs(v-.5)*2)**3;light=.91+.13*u-.09*edge
  if 3<=index<=7:light+=.08*math.exp(-((v-.5)/.025)**2)*math.sin(math.pi*u)
  elif index<3:light-=.05*math.sin(u*math.pi*12+math.sin(v*6))**10
  pixels.extend([max(0,min(1,c*light)) for c in color]+[1])
image.pixels.foreach_set(pixels);image.filepath_raw=str(folder/'Garden-Painted-Palette.png');image.file_format='PNG';image.save();image.pack()
material=bpy.data.materials.new('GardenPaint');material.use_nodes=True
node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;material.node_tree.nodes.active=node
material.node_tree.links.new(node.outputs['Color'],material.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.82
material.diffuse_color=(.23,.49,.33,1)
active_collection=None;parts=[]
def tile(index,uv):return ((index%3+(0.045+.91*uv[0]))/3,(index//3+(0.045+.91*uv[1]))/3)
def piece(name,points,faces,index,uv=None):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],faces);mesh.update();obj=bpy.data.objects.new(name,mesh);active_collection.objects.link(obj);mesh.materials.append(material)
 layer=mesh.uv_layers.new(name='PaintedPalette')
 for face in mesh.polygons:
  for j,loop in enumerate(face.loop_indices):
   vertex=mesh.loops[loop].vertex_index
   coord=uv[vertex] if uv else [(0.05,.05),(.95,.05),(.95,.95),(.05,.95)][j%4]
   layer.data[loop].uv=tile(index,coord)
 topology=bmesh.new();topology.from_mesh(mesh);bmesh.ops.recalc_face_normals(topology,faces=list(topology.faces));topology.to_mesh(mesh);topology.free()
 parts.append(obj);return obj
def tube(name,points,radii,sides,index,depth_scale=1):
 points=[Vector(p) for p in points];verts=[]
 for i,point in enumerate(points):
  direction=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized();side=direction.cross(Vector((0,1,0)))
  if side.length<.01:side=direction.cross(Vector((1,0,0)))
  side.normalize();other=direction.cross(side).normalized()
  for j in range(sides):
   offset=(side*math.cos(j*math.tau/sides)+other*math.sin(j*math.tau/sides))*radii[i];offset.y*=depth_scale;verts.append(point+offset)
 faces=[]
 for i in range(len(points)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;c=(i+1)*sides+(j+1)%sides;d=(i+1)*sides+j;faces += [(a,b,c),(a,c,d)]
 faces+=[tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))]
 return piece(name,verts,faces,index)
def crown(name,center,size,index,subdiv):
 bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=subdiv,radius=1)
 verts=[Vector((v.co.x*size[0],v.co.y*size[1],v.co.z*size[2]))+Vector(center) for v in bm.verts];bm.verts.ensure_lookup_table();lookup={v:i for i,v in enumerate(bm.verts)};faces=[tuple(lookup[v] for v in f.verts) for f in bm.faces];bm.free();return piece(name,verts,faces,index)
def leaf(name,base,direction,length,width,index,solid=True,vertical=False):
 direction=Vector(direction).normalized();side=(Vector((1,0,0))-direction*direction.x) if vertical else direction.cross(Vector((0,0,1)))
 if side.length<.01:side=Vector((1,0,0))
 side.normalize();normal=side.cross(direction).normalized();base=Vector(base)
 if vertical:
  verts=[base,base+direction*length*.14+side*width*.32,base+direction*length*.38+side*width*.5,base+direction*length,base+direction*length*.38-side*width*.5,base+direction*length*.14-side*width*.32,base+direction*length*.38+normal*width*.13,base+direction*length*.38-normal*width*.045]
  uv=[(0,.5),(.14,.18),(.38,0),(1,.5),(.38,1),(.14,.82),(.38,.5),(.38,.5)]
  faces=[(i,(i+1)%6,6) for i in range(6)]+[((i+1)%6,i,7) for i in range(6)]
 else:
  verts=[base,base+direction*length*.48+side*width*.5,base+direction*length,base+direction*length*.48-side*width*.5,base+direction*length*.48+normal*width*.13]
  faces=[(0,1,4),(1,2,4),(2,3,4),(3,0,4)];uv=[(0,.5),(.48,0),(1,.5),(.48,1),(.48,.5)]
  if solid:
   verts.append(base+direction*length*.48-normal*width*.045);uv.append((.48,.5));faces += [(1,0,5),(2,1,5),(3,2,5),(0,3,5)]
 return piece(name,verts,faces,index,uv)
def build(family,level):
 rng=random.Random(7001+sum(map(ord,family)));solid=True
 if family=='Trunk':
  segments=[(0,0,-.5),(.025,-.01,-.26),(-.04,.02,0),(.015,0,.23),(.08,.025,.46)]
  tube('Tapered bent trunk',segments,[.08,.06,.045,.028,.008], [10,7,5][level],1)
  for branch in range(8):
   angle=branch*math.tau/8;tip=Vector((math.cos(angle)*(.43 if branch%2 else .35),math.sin(angle)*.43,.20+(branch%3)*.09))
   tube('Radial branch '+str(branch),[(0,0,-.04),(math.cos(angle)*.19,math.sin(angle)*.2,.16),tip],[.025,.017,.004],[7,5,3][level],1 if branch%3 else 2)
  if level==0:
   for i in range(3):crown('Bark knot '+str(i),(.015,-.05,-.25+i*.2),(.03,.009,.04),0,1)
 elif family.startswith('Crown'):
  variant='ABC'.index(family[-1]);clusters=[(-.29,-.14,-.03),(.03,-.25,.03),(.31,-.02,-.04),(-.17,.15,.13),(.17,.23,.16),(-.36,.18,-.10),(.35,.19,-.03),(-.02,-.01,.28)]
  for i,c in enumerate(clusters):
   c=Vector(c);c.x+=.06*math.sin(i+variant*1.7);c.z+=variant*.035*math.cos(i)
   crown('Layered leaf crown '+str(i),c,(.23+.025*(i%2),.23,.115+.015*variant),[4,5,6][(i+variant)%3],[2,1,1][level])
   for j in range([6,4,1][level]):
    angle=j*math.tau/[6,4,1][level]+i*.71;direction=Vector((math.cos(angle),math.sin(angle),.08));leaf('Folded crown sprig '+str(i)+'-'+str(j),c+direction*.13,direction,.16,.075,5 if j%3 else 6,solid)
 elif family.startswith('Shrub'):
  variant=0 if family.endswith('A') else 1
  for plant,center in enumerate([(-.24,-.25,-.30),(.24,.25,-.30)]):
   crown('Rosette heart '+str(plant),Vector(center)+Vector((0,0,.06)),(.075,.075,.085),3,[2,1,1][level])
   for ring in range(2):
    count=([12,7] if level==0 else [8,5] if level==1 else [5,3])[ring]
    for j in range(count):
     angle=j*math.tau/count+plant*.7+variant*.35+ring*.27
     direction=(math.cos(angle)*(1 if ring==0 else .48),math.sin(angle)*(1 if ring==0 else .48),.50 if ring==0 else 1)
     base=Vector(center)+Vector((math.cos(angle)*.025,math.sin(angle)*.025,.025*ring))
     leaf('Cupped rosette blade '+str(plant)+'-'+str(ring)+'-'+str(j),base,direction,.36 if ring==0 else .30,.18 if ring==0 else .14,4+(j+ring+variant)%3,True)
 elif family.startswith('Grass'):
  variant=0 if family.endswith('A') else 1
  for i in range([48,32,16][level]):
   x=rng.uniform(-.4,.4);y=rng.uniform(-.43,.43);angle=rng.random()*math.tau
   leaf('Upright folded grass '+str(i),(x,y,-.3),(math.cos(angle)*.025,math.sin(angle)*.025,1),rng.uniform(.38,.7),rng.uniform(.008,.016),4+(i+variant)%3,solid)
 elif family=='VineStem':
  for strand in range(6):
   count=[18,10,7][level];points=[]
   for j in range(count):
    t=j/(count-1);length=[1,.84,.96,.77,.90,.88][strand];points.append((-.38+strand*.152+.027*math.sin(t*13.6+strand),-.12,.48-t*.96*length))
   tube('Hanging vine stem '+str(strand),points,[.004]*count,[6,4,3][level],0,8)
 elif family.startswith('VineLeaf'):
  variant=0 if family.endswith('A') else 1
  for strand in range(6):
   rows=list(range(15)) if level==0 else [0,1,3,4,6,7,9,10,12,14] if level==1 else [0,3,6,9,12,14]
   segments=[18,10,7][level];length=[1,.84,.96,.77,.90,.88][strand]
   def stem_point(t):return Vector((-.38+strand*.152+.027*math.sin(t*13.6+strand),-.12,.48-t*.96*length))
   for j in rows:
    t=.02+.90*j/14;segment=min(segments-2,int(t*(segments-1)));fraction=t*(segments-1)-segment
    base=stem_point(segment/(segments-1)).lerp(stem_point((segment+1)/(segments-1)),fraction)
    side=-1 if j%2 else 1;direction=(side*(.28+.16*math.sin(strand*1.7+j*.8+variant)),.10+.05*math.sin(j+strand),-.75)
    obj=leaf('Cupped heart vine leaf '+str(strand)+'-'+str(j),base,direction,.075,.095,4+(j+strand+variant)%3,True,True)
    for vertex in obj.data.vertices:vertex.co.y=-.12+(vertex.co.y+.12)*2.2
 else:raise AssertionError(family)
modules=['Trunk','CrownA','CrownB','CrownC','ShrubA','ShrubB','GrassA','GrassB','VineStem','VineLeafA','VineLeafB']
records=[];module_records=[]
for family in modules:
 family_levels=[]
 for level in range(3):
  active_collection=bpy.data.collections.new(family+'_LOD'+str(level));scene.collection.children.link(active_collection);parts=[];build(family,level)
  family_levels.append((level,active_collection,parts.copy()))
 all_points=[v.co.copy() for _,_,pieces in family_levels for obj in pieces for v in obj.data.vertices]
 raw_low=Vector(tuple(min(v[i] for v in all_points) for i in range(3)));raw_high=Vector(tuple(max(v[i] for v in all_points) for i in range(3)))
 center=(raw_low+raw_high)/2;size=raw_high-raw_low
 for level,active_collection,parts in family_levels:
  if not family.startswith('Vine'):
   # All LODs use one common frame; simplified parts never stretch to refill their bounds.
   for obj in parts:
    for vertex in obj.data.vertices:vertex.co=Vector(tuple((vertex.co[i]-center[i])/size[i] for i in range(3)))
  else:
   for obj in parts:
    for vertex in obj.data.vertices:
     if max(abs(c) for c in vertex.co)>.50001:raise AssertionError((family,tuple(vertex.co)))
  bpy.ops.object.select_all(action='DESELECT')
  copies=[]
  for obj in parts:
   copy=obj.copy();copy.data=obj.data.copy();scene.collection.objects.link(copy);copy.select_set(True);copies.append(copy)
  bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=family+'_LOD'+str(level)
  assert len(joined.data.materials)==1
  joined.data.calc_loop_triangles();triangles=len(joined.data.loop_triangles)
  export=exports/(family+'-LOD'+str(level)+'.fbx')
  bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,bake_anim=False,use_mesh_modifiers=True,add_leaf_bones=False)
  vertices=[list(v.co) for v in joined.data.vertices];lo=[min(v[i] for v in vertices) for i in range(3)];hi=[max(v[i] for v in vertices) for i in range(3)]
  records.append({'family':family,'level':level,'collection':active_collection.name,'triangles':triangles,'editableParts':len(parts),'boundsMinBlender':lo,'boundsMaxBlender':hi,'canonicalVerticesBlender':[{'x':v[0],'y':v[1],'z':v[2]} for v in vertices],'export':export.relative_to(root).as_posix(),'exportSha256':hashlib.sha256(export.read_bytes()).hexdigest()})
  bpy.data.objects.remove(joined,do_unlink=True)
  active_collection.hide_render=not(family=='CrownA' and level==0);active_collection.hide_viewport=active_collection.hide_render
  if level==0:module_records.append({'family':family,'referenceFrameMetresBlenderXYZ':[1,1,1],'geometryDimensionsMetresBlenderXYZ':[hi[i]-lo[i] for i in range(3)],'pivot':'bounds-centred unit reference frame; paired vines share one common frame','parts':len(parts),'lodFramePolicy':'Common frame across all LODs; no per-LOD normalization/stretching'})
 for earlier,later in zip([r for r in records if r['family']==family],[r for r in records if r['family']==family][1:]):assert earlier['triangles']>later['triangles'],family
scene['source_identity']='SG-GARDEN-NATURE-v2';scene['axes']='Blender (x,y,z) -> Unity (x,z,-y); native importer checks canonical vertices after axis normalization'
scene['collision']='None; original installed collider set/navigation/device transforms remain authoritative'
scene['paint']='One original opaque painted palette material per mesh; no alpha cards or new paid assets'
master=folder/'Garden-Nature-v2.blend';bpy.ops.wm.save_as_mainfile(filepath=str(master))
record={'id':'SG-GARDEN-NATURE-v2','status':'authored source candidate; native integration/visual/reference/Windows review pending','master':master.name,'masterSha256':hashlib.sha256(master.read_bytes()).hexdigest(),'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'axes':scene['axes'],'collision':scene['collision'],'paletteSrgb':dict(palette),'paint':{'texture':'Garden-Painted-Palette.png','dimensionsPixelsWH':[384,384],'layout':'3x3 swatches, indexed bottom-left to top-right; inset UVs; original gradients, central leaf veins and bark strokes','sha256':hashlib.sha256((folder/'Garden-Painted-Palette.png').read_bytes()).hexdigest()},'modules':module_records,'lods':records,'provenance':'Original deterministic Blender mesh construction and painted atlas; no external asset dependency','acceptance':'Final native and Sanders artistic acceptance remain open'}
(folder/'production.json').write_bytes((json.dumps(record,indent=2)+'\n').encode('utf-8'))
print('GARDEN_NATURE_AUTHORED',len(modules),'modules',len(records),'LODs',sum(r['triangles'] for r in records if r['level']==0),'near triangles / one painted material per mesh')
