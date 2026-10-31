"""Compare near-detail vertex/material clouds before and after the LOD-only revision."""
import hashlib
import json
from pathlib import Path
import runpy
import sys
import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Environment/ArenaStone'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'))
previous=folder/'Arena-Stone-v1.blend1'
current=folder/spec['master']
assert hashlib.sha256(previous.read_bytes()).hexdigest()==spec['supersededCandidate']['masterSha256']
assert hashlib.sha256(current.read_bytes()).hexdigest()==spec['masterSha256']

def measure(path):
    bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False)
    bpy.context.view_layer.update()
    clouds,triangles={},{}
    for collection in bpy.context.scene.collection.children:
        if not collection.name.endswith('_LOD0'):continue
        family=collection.name[:-5];clouds[family]={};triangles[family]=0
        for obj in collection.objects:
            if obj.type!='MESH':continue
            obj.data.calc_loop_triangles();triangles[family]+=len(obj.data.loop_triangles)
            material=obj.data.materials[0].name
            clouds[family].setdefault(material,[]).extend(tuple(obj.matrix_world@vertex.co) for vertex in obj.data.vertices)
    return clouds,triangles

old,old_triangles=measure(previous)
new,new_triangles=measure(current)
assert old_triangles==new_triangles
records=[]
for family in old:
    assert old[family].keys()==new[family].keys()
    maximum=0
    for material in old[family]:
        for reference,observed in [(old[family][material],new[family][material]),(new[family][material],old[family][material])]:
            tree=KDTree(len(reference))
            for index,point in enumerate(reference):tree.insert(Vector(point),index)
            tree.balance()
            for point in observed:maximum=max(maximum,tree.find(Vector(point))[2])
    assert maximum<.00001,(family,maximum)
    records.append({'family':family,'triangles':new_triangles[family],'maximumBidirectionalMaterialVertexDeviationMetres':maximum})
(root/'Validation/arena-stone-near-preservation.json').write_text(json.dumps({'previousMasterSha256':spec['supersededCandidate']['masterSha256'],
    'masterSha256':spec['masterSha256'],'scope':'All LOD0 family vertex clouds grouped by assigned material, compared in both directions within 10 micrometres; equal triangle counts. Does not certify triangle index order, shading or every UV. Native captures and the installed hull/material checks are separate.',
    'families':records},indent=2)+'\n',encoding='utf-8')
print('ARENA_NEAR_DETAIL_PRESERVED',max(record['maximumBidirectionalMaterialVertexDeviationMetres'] for record in records))
if '--render' in sys.argv:
    sys.argv.append('--arena')
    runpy.run_path(str(root/'tools/render_bridge_stone_refs.py'),run_name='__main__')
