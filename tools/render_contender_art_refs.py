"""Read-only measured views of the refined canonical contender masters."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Characters'
for name in ('Player','Bot01','Bot02','Bot03'):
    source=folder/f'{name}-ArtPass-v1.blend'
    digest=hashlib.sha256(source.read_bytes()).hexdigest()
    bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False)
    scene=bpy.context.scene
    for obj in scene.objects:
        if obj.type=='ARMATURE':obj.data.pose_position='REST'
    bpy.context.view_layer.update()
    parts=[o for o in scene.objects if o.type=='MESH' and not o.hide_render]
    points=[o.matrix_world@Vector(c) for o in parts for c in o.bound_box]
    low=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(low+high)/2
    camera=bpy.data.objects.new('Measured character camera',bpy.data.cameras.new('Measured character camera'))
    scene.collection.objects.link(camera);scene.camera=camera
    camera.data.type='ORTHO';camera.data.ortho_scale=2.2
    scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
    scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
    scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
    output=folder/'References'/name;output.mkdir(parents=True,exist_ok=True)
    views=[('front',(0,-5,0)),('rear',(0,5,0)),('left',(-5,0,0)),('right',(5,0,0)),('top',(0,0,5)),('bottom',(0,0,-5))]
    for label,offset in views:
        camera.location=center+Vector(offset);camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(output/(label+'.png'));bpy.ops.render.render(write_still=True)
    (output/'measurements.json').write_text(json.dumps({'id':f'CHAR-{name.upper()}-ART-v1','master':str(source.relative_to(root)),'masterSha256':digest,'blenderDimensionsMetresXYZ':list(high-low),'axisConversion':'Unity(x,z,-y) from Blender(x,y,z)','orthographicSpanMetres':2.2,'views':[v[0] for v in views],'pose':'Canonical rig rest pose; technical material preview, not Unity cel shading','units':'metres','version':1,'approval':'Final integrated user playtest pending'},indent=2)+'\n')
    assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
    print('CONTENDER_MEASURED_VIEWS',name)
