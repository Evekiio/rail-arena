"""Render calibrated orthographic source images; no perspective or image generation."""
import bpy
import hashlib
import json
import sys
from pathlib import Path
from mathutils import Matrix, Vector

ROOT=Path(sys.argv[sys.argv.index('--')+1]).resolve()
SOURCE=ROOT/'ArtSource/Original/ProductionBenchmarks-v1'
OUT=SOURCE/'Dimensional-References-v1'
(OUT/'Clean').mkdir(parents=True,exist_ok=True)
master=SOURCE/'Production-Benchmarks.blend'
bpy.ops.wm.open_mainfile(filepath=str(master))
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=4
scene.render.resolution_x=2048;scene.render.resolution_y=2048;scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
camera=scene.camera;camera.data.type='ORTHO';camera.data.lens=50
definitions=[('Left',(1,0,0),(0,0,1),(0,-1,0),'Looking from -Y; muzzle/end +X on right'),
             ('Right',(-1,0,0),(0,0,1),(0,1,0),'Looking from +Y; muzzle/end +X on left'),
             ('Top',(1,0,0),(0,1,0),(0,0,1),'Looking down -Z; +X right, +Y up'),
             ('Bottom',(1,0,0),(0,-1,0),(0,0,-1),'Looking up +Z; +X right, -Y up'),
             ('Front',(0,1,0),(0,0,1),(1,0,0),'Looking from +X; muzzle / positive end'),
             ('Back',(0,-1,0),(0,0,1),(-1,0,0),'Looking from -X; butt / negative end')]
records=[]
for asset,collection_name,scale in [('RailRifle','Rifle editable parts',1.45),('LowWingCover','Low wing cover editable parts',2.5)]:
    objects=[o for o in bpy.data.collections[collection_name].objects if o.type=='MESH']
    for other in scene.objects:
        if other.type=='MESH':other.hide_render=other not in objects
    corners=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    low=Vector([min(c[i] for c in corners) for i in range(3)])
    high=Vector([max(c[i] for c in corners) for i in range(3)])
    center=(low+high)/2
    item=dict(asset=asset,center=list(center),minimum=list(low),maximum=list(high),
              dimensions=list(high-low),orthographicFrameMetres=scale,pixelsPerMetre=2048/scale,
              views=[],parts=[])
    for o in objects:
        pts=[o.matrix_world@Vector(c) for c in o.bound_box]
        item['parts'].append(dict(name=o.name,minimum=[min(c[i] for c in pts) for i in range(3)],
                                 maximum=[max(c[i] for c in pts) for i in range(3)]))
    for name,right,up,normal,note in definitions:
        camera.location=center+Vector(normal)*5
        camera.rotation_euler=Matrix((right,up,normal)).transposed().to_quaternion().to_euler()
        camera.data.ortho_scale=scale
        filename=f'{asset}-{name}.png'
        image_path=OUT/'Clean'/filename
        scene.render.filepath=str(image_path)
        reuse=False
        if '--resume' in sys.argv and image_path.exists() and image_path.stat().st_mtime>=master.stat().st_mtime:
            previous=image_path.read_bytes()
            reuse=previous[:8]==b'\x89PNG\r\n\x1a\n' and previous[-8:]==b'IEND\xaeB`\x82'
        if not reuse:bpy.ops.render.render(write_still=True)
        item['views'].append(dict(name=name,file='Clean/'+filename,right=right,up=up,normal=normal,
                                  note=note,cameraPosition=list(camera.location),resolution=[2048,2048]))
    records.append(item)
manifest=dict(status='PROPOSED dimensional approval references, not creator approved',
              sourceMasterSha256=hashlib.sha256(master.read_bytes()).hexdigest(),
              units='metres',sourceAxes='X forward/length, Y width, Z up',
              projection='orthographic, square pixels, common scale and origin per asset',assets=records)
(OUT/'Calibration.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('DIMENSIONAL_REFERENCES_COMPLETE')
