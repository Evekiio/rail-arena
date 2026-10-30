"""Production carbine from the creator-approved v3 study. Run in background Blender."""
import bpy,bmesh,hashlib,json,math,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
out=root/'ArtSource/Original/PrecisionCarbine-Production-v1';out.mkdir(parents=True,exist_ok=True)
asset=root/'RailArena/Assets/RailArena/Art/PrecisionCarbine';asset.mkdir(parents=True,exist_ok=True)
source=root/'ArtSource/Original/PrecisionCarbine-References-v3/Carbine-Reference-Study.blend'
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
parts=sorted([o for o in scene.objects if o.type=='MESH'],key=lambda o:o.name)
# UV islands are unique within each part tile, with 8px atlas gutters. Every part remains editable.
grid=math.ceil(math.sqrt(len(parts)));tiles=[]
for i,obj in enumerate(parts):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.035,correct_aspect=True)
    bpy.ops.object.mode_set(mode='OBJECT')
    col=i%grid;row=i//grid;gutter=8/2048
    for loop in obj.data.uv_layers.active.data:
        loop.uv=((col/grid)+gutter+loop.uv.x*(1/grid-2*gutter),(row/grid)+gutter+loop.uv.y*(1/grid-2*gutter))
    obj.data.uv_layers.active.name='SurfaceAtlas'
    tiles.append(dict(name=obj.name,index=i,palette=int(obj['paletteIndex']),column=col,row=row))
# Keep the original material preview on the editable master. Runtime uses the surface atlas.
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Precision-Carbine.blend'))
# Export grouped editable assembly surfaces; trigger has an independent pivot.
counts=[];bounds=[]
for level,budget in enumerate((12000,6000,3000)):
    clones=[]
    for obj in parts:
        copy=obj.copy();copy.data=obj.data.copy();scene.collection.objects.link(copy);clones.append(copy)
    # Simplify tiny bevels/details while retaining every component and all major openings.
    original=sum(len(o.data.polygons) for o in clones)
    ratio=min(1,(budget-250)/original) if level else 1
    for obj in clones:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        if ratio<1:
            mod=obj.modifiers.new('LOD reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
        obj.data.calc_loop_triangles()
        assert all(t.area>1e-12 for t in obj.data.loop_triangles),obj.name
    groups={}
    for obj in clones:
        key=(obj['assemblyId'],int(obj['paletteIndex'])==6,'trigger' if obj.name.startswith('C3 trigger.') or obj.name=='C3 trigger' else 'shell')
        groups.setdefault(key,[]).append(obj)
    exports=[]
    for (assembly,energy,kind),objects in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object
        obj.name=f"{assembly.replace('-','_')}_{kind}_{'Energy' if energy else 'Surface'}"
        scene.cursor.location=( -.04,0,-.055) if kind=='trigger' else (0,0,0)
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        # Source X-forward/Z-up -> Unity Z-forward/Y-up using FBX axis conversion.
        # Rotate geometry and origin around the global origin, not the trigger pivot.
        from mathutils import Matrix
        obj.matrix_world=Matrix.Rotation(-math.pi/2,4,'Z')@obj.matrix_world
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        exports.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in exports:obj.select_set(True)
    triangles=sum(len(o.data.loop_triangles) for o in exports)
    for obj in exports:obj.data.calc_loop_triangles()
    triangles=sum(len(o.data.loop_triangles) for o in exports)
    assert triangles<=budget,(level,triangles,budget)
    points=[o.matrix_world@Vector(c) for o in exports for c in o.bound_box]
    envelope=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    bounds.append(envelope);counts.append(triangles)
    bpy.ops.export_scene.fbx(filepath=str(asset/f'Carbine_LOD{level}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
    for obj in exports:bpy.data.objects.remove(obj,do_unlink=True)
report=dict(approvedReferenceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),editableParts=len(parts),atlasGrid=grid,atlasSize=2048,tiles=tiles,lodTriangles=counts,lodSourceBounds=bounds,units='metres',unityAxes='+Z muzzle, +Y up',materialSlots=2,normalMap='Flat tangent normal: faceted bevel and hard-surface normals are authored in geometry',productionApproval='Design approved by creator; integrated asset acceptance remains pending')
(out/'Production.json').write_text(json.dumps(report,indent=2)+'\n')
print('CARBINE_PRODUCTION_EXPORTED '+json.dumps(counts))
