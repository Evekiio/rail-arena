"""Render six equal-scale geometry-derived views for canonical bridge or summit modules."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root = Path(sys.argv[sys.argv.index('--')+1]).resolve()
summit = '--summit' in sys.argv
arena = '--arena' in sys.argv
assert not (summit and arena), 'Choose one canonical environment family'
folder = root/('ArtSource/Original/ArtPass-v1/Environment/ArenaStone' if arena else 'ArtSource/Original/ArtPass-v1/Environment/SummitStone' if summit else 'ArtSource/Original/ArtPass-v1/Environment/BridgeStone')
spec = json.loads((folder/'production.json').read_text(encoding='utf-8'))
master = folder/spec['master']
assert hashlib.sha256(master.read_bytes()).hexdigest() == spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master), load_ui=False)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.render.resolution_x = scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
camera = bpy.data.objects.new('Measured camera', bpy.data.cameras.new('Measured camera'))
scene.collection.objects.link(camera)
scene.camera = camera
camera.data.type = 'ORTHO'
records = []
families = tuple(entry['family'] for entry in spec['lods'] if entry['lod']==0) if arena else ('Summit', 'Cover', 'Column') if summit else ('Deck', 'Screen', 'Pier')
for family in families:
    for collection in scene.collection.children:
        collection.hide_render = collection.hide_viewport = collection.name != family+'_LOD0'
    entry = next(item for item in spec['lods'] if item.get('family', item.get('profile')) == family and item['lod'] == 0)
    dimensions = [entry['radiusMetres']*2,entry['thicknessMetres'],entry['radiusMetres']*2] if summit else entry['sizeUnityMetres']
    target = Vector((0,0,-dimensions[1]/2)) if summit else Vector((0,0,0))
    span = max(dimensions)*1.15
    camera.data.ortho_scale = span
    output = folder/'References'/family
    output.mkdir(parents=True, exist_ok=True)
    views = [('front', (0,-1,0)), ('rear', (0,1,0)), ('left', (-1,0,0)), ('right', (1,0,0)), ('top', (0,0,1)), ('bottom', (0,0,-1))]
    for name, direction in views:
        camera.location = target+Vector(direction)*span*3
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath = str(output/(name+'.png'))
        bpy.ops.render.render(write_still=True)
    camera.location = target+Vector((span*.7, -span, span*.7))
    camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath = str(output/'perspective.png')
    bpy.ops.render.render(write_still=True)
    if arena:
        detail_target = Vector((0,0,.2)) if family=='Ramp' else Vector((dimensions[0]/2-1.3,-dimensions[2]/2+1.3,dimensions[1]/2-.15))
        camera.data.ortho_scale = 5.5
        camera.location = detail_target+Vector((4,-5,3))
        camera.rotation_euler = (detail_target-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath = str(output/'detail.png')
        bpy.ops.render.render(write_still=True)
        offsets = {'Paving':.9,'Wayfinding':.9,'Coping':.45,'Underside':-.65,'Coffer':-.65}
        positions = {obj:obj.location.copy() for obj in scene.collection.children[family+'_LOD0'].objects}
        try:
            for obj in positions:
                obj.location.z += offsets.get(obj.get('construction_layer',''),0)
            bpy.context.view_layer.update()
            scene.render.filepath = str(output/'exploded-detail.png')
            bpy.ops.render.render(write_still=True)
        finally:
            for obj,position in positions.items():obj.location=position
            bpy.context.view_layer.update()
    records.append({'family': family, 'orthographicSpanMetres': span, 'resolution': 768, 'views': [name for name,_ in views], 'sizeUnityMetres': dimensions, 'masterSha256': spec['masterSha256'], 'pivot': 'original top centre' if summit else entry['pivot']})
    if arena:records[-1].update(detailOrthographicSpanMetres=5.5,explodedLayerOffsetsBlenderZMetres=offsets)
assert hashlib.sha256(master.read_bytes()).hexdigest() == spec['masterSha256']
(folder/'measured-views.json').write_text(json.dumps({'units':'metres','axes':spec['axes'],'modules':records},indent=2)+'\n',encoding='utf-8')
print(('ARENA' if arena else 'SUMMIT' if summit else 'BRIDGE')+'_REFERENCES_RENDERED',len(records)*6,'equal-scale orthographics /',len(records),'perspective views; master unchanged')
