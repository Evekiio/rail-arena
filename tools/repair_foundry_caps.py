"""Repair coplanar cover caps in the initial review run, then refresh affected exports/views.

The main builder contains the corrected construction for subsequent full rebuilds.
This bounded repair avoids regenerating unrelated floor and device views.
"""
import bpy
import bmesh
import hashlib
import json
import sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
OUT=ROOT/'ArtSource/Original/Foundry-Models-v1'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Foundry-Environment.blend'))
scene=bpy.context.scene
report=json.loads((OUT/'Model-Validation.json').read_text())
library=bpy.data.collections['Editable asset library']
arena=bpy.data.collections['Foundry v2 art assembly']
camera=scene.camera
changed=[]
for item in report['assets']:
    col=bpy.data.collections[item['id']]
    portal=item['id']=='PORTAL'
    if item['id'].startswith('COVER-') or portal:
        caps=[o for o in col.objects if o.name.startswith('Armored lintel' if portal else 'Top ceramic cap')]
        assert len(caps)==1
        if not caps[0].get('coplanar_cap_repaired',False):
            caps[0].location.z+=-.008 if portal else .008
            caps[0]['coplanar_cap_repaired']=True
        changed.append(item['id'])
    issues=[]
    for obj in col.objects:
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        if any(f.calc_area()<1e-10 for f in bm.faces):issues.append(obj.name+':degenerate')
        if any(not e.is_manifold for e in bm.edges):issues.append(obj.name+':nonmanifold')
        bm.to_mesh(obj.data);bm.free()
    item['geometryIssues']=issues
    assert not issues,(item['id'],issues)
    item['triangles']=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in col.objects)
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(p) for o in col.objects for p in o.bound_box]
    lo=[min(p[i] for p in pts) for i in range(3)]
    hi=[max(p[i] for p in pts) for i in range(3)]
    item['boundsMin']=lo;item['boundsMax']=hi;item['dimensions']=[b-a for a,b in zip(lo,hi)]
    library.hide_viewport=False;library.hide_render=False;arena.hide_render=True;col.hide_render=False
    bpy.ops.object.select_all(action='DESELECT')
    for o in col.objects:o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/'Exports'/f'{item["id"]}.glb'),use_selection=True,export_format='GLB',export_yup=True)
    col.hide_render=True
library.hide_viewport=True;library.hide_render=True;arena.hide_render=False
for record in report['renders']:
    asset=next((name for name in changed if record['name'].startswith(name+'-')),None)
    if '--arena-only' in sys.argv and asset:
        continue
    if not asset and record['name'] not in ('Arena-SW','Arena-NE','Arena-Top','Underpass','Crown','West-Wing','North-Gate'):
        continue
    arena.hide_render=bool(asset)
    library.hide_render=not bool(asset)
    for item in report['assets']:
        bpy.data.collections[item['id']].hide_render=bool(asset) and item['id']!=asset
    camera.location=record['camera']
    camera.rotation_euler=(Vector(record['target'])-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='PERSP' if record['perspective'] else 'ORTHO'
    camera.data.ortho_scale=record['scale'];camera.data.lens=22
    scene.render.filepath=str(OUT/'Renders'/f'{record["name"]}.png')
    bpy.ops.render.render(write_still=True)
    if asset:bpy.data.collections[asset].hide_render=True
arena.hide_render=False;library.hide_render=True
for item in report['assets']:bpy.data.collections[item['id']].hide_render=False
camera.location=(70,-75,65);camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=75
report['constructionRepair']='Offset eight cover caps and the portal lintel by 8mm; welded collapsed bevel vertices on thin bolts/hubs; audited and re-exported all meshes; affected visible surfaces rerendered.'
report['builderSHA256']=hashlib.sha256((ROOT/'tools/build_foundry_models.py').read_bytes()).hexdigest()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Foundry-Environment.blend'))
(OUT/'Model-Validation.json').write_text(json.dumps(report,indent=2)+'\n')
print('Cover cap repair complete')
