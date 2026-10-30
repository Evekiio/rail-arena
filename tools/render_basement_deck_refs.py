"""Read-only geometry-derived orthographics, construction layers and exploded deck views."""
import hashlib
import json
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
folder=root/'ArtSource/Original/ArtPass-v1/Environment/BasementDecks'
spec=json.loads((folder/'production.json').read_text());master=folder/spec['master'];digest=hashlib.sha256(master.read_bytes()).hexdigest();assert digest==spec['masterSha256']
bpy.ops.wm.open_mainfile(filepath=str(master),load_ui=False)
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=scene.render.resolution_y=1024;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Measured deck camera',bpy.data.cameras.new('Measured deck camera'));scene.collection.objects.link(camera);scene.camera=camera;camera.data.type='ORTHO'
records=[]
for name in ('Staging','Basement'):
    profile=next(entry for entry in spec['lods'] if entry['profile']==name and entry['lod']==0)
    for collection in scene.collection.children:collection.hide_render=collection.name!=name+'_LOD0';collection.hide_viewport=collection.hide_render
    collection=bpy.data.collections[name+'_LOD0'];parts=list(collection.objects);span=profile['diameterMetres']*1.15;camera.data.ortho_scale=span
    directory=folder/'References'/name;directory.mkdir(parents=True,exist_ok=True);target=Vector((0,0,-profile['thicknessMetres']/2))
    construction=[]
    for obj in parts:
        corners=[obj.matrix_world@Vector(corner) for corner in obj.bound_box];minimum=[min(point[axis] for point in corners) for axis in range(3)];maximum=[max(point[axis] for point in corners) for axis in range(3)]
        construction.append({'id':obj.name,'layer':obj['construction_layer'],'minimumBlenderXYZMetres':minimum,'maximumBlenderXYZMetres':maximum,'materialSlots':[material.name for material in obj.data.materials],'pivotBlenderXYZMetres':list(obj.location)})
    for view,direction in [('front',(0,-1,0)),('rear',(0,1,0)),('left',(-1,0,0)),('right',(1,0,0)),('top',(0,0,1)),('bottom',(0,0,-1))]:
        camera.location=target+Vector(direction)*span*3;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(directory/(view+'.png'));bpy.ops.render.render(write_still=True)
    # Exploded render only: canonical geometry and authored master are never saved after translation.
    offsets={}
    for obj in parts:
        layer=obj['construction_layer']
        if layer.startswith('Underside'):offset=-1.1
        elif layer in ('LowerCornice',):offset=-.55
        elif layer in ('TealFrieze','FriezePier','BronzeString'):offset=-.1
        elif layer in ('StructuralDrum','UpperDrip'):offset=.25
        else:offset=.85
        offsets[obj]=offset;obj.location.z+=offset
    camera.location=Vector((span*.7,-span,span*.75));camera.rotation_euler=(Vector((0,0,-.3))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=span*1.1
    scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.filepath=str(directory/'exploded.png');bpy.ops.render.render(write_still=True)
    for obj,offset in offsets.items():obj.location.z-=offset
    scene.render.resolution_x=scene.render.resolution_y=1024
    records.append({'profile':name,'master':spec['master'],'masterSha256':digest,'diameterMetres':profile['diameterMetres'],'thicknessMetres':profile['thicknessMetres'],'orthographicSpanMetres':span,'resolution':1024,'views':['front','rear','left','right','top','bottom'],'explodedView':'exploded.png','explodedScope':'Temporary vertical layer offsets for construction readability; not installed dimensions','topPivotUnityMetres':[0,0,0],'parts':construction,'attachmentSurface':'Original top plane at Unity local Y=0. Return pads and portals retain source-scene placements.','collision':'Original audited mesh retained; this family contains no collision meshes or new blockers.'})
assert hashlib.sha256(master.read_bytes()).hexdigest()==digest
(folder/'measured-construction.json').write_text(json.dumps({'masterSha256':digest,'units':'metres','axes':'Blender(x,y,z) -> Unity(x,z,-y)','profiles':records},indent=2)+'\n')
print('BASEMENT_REFERENCES_RENDERED 12 equal-scale orthographics / 2 exploded views / per-part construction bounds')
