"""Create the material-pass environment master without altering the approved geometry source."""
import hashlib
import json
from pathlib import Path
import sys
import bpy

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
original=root/'ArtSource/Original/SkyGardens-Blueprint-v2/SkyGardens-Construction.blend'
source=root/'ArtSource/Original/ArtPass-v1/Environment'
before=hashlib.sha256(original.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(original),load_ui=False)
roles={'structure','cover','garden_structure','ivory_trim','ramps','summit'}
image=bpy.data.images.load(str(source/'Stone-Painted.png'));image.pack()
normal=bpy.data.images.load(str(source/'Stone-Normal.png'));normal.colorspace_settings.name='Non-Color';normal.pack()
changed=[]
for material in bpy.data.materials:
    role=material.name.replace('Material-','')
    if role not in roles:continue
    material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF')
    if shader is None:continue
    color_node=material.node_tree.nodes.new('ShaderNodeTexImage');color_node.image=image
    material.node_tree.links.new(color_node.outputs['Color'],shader.inputs['Base Color'])
    normal_node=material.node_tree.nodes.new('ShaderNodeTexImage');normal_node.image=normal
    normal_map=material.node_tree.nodes.new('ShaderNodeNormalMap');normal_map.inputs['Strength'].default_value=.55
    material.node_tree.links.new(normal_node.outputs['Color'],normal_map.inputs['Color']);material.node_tree.links.new(normal_map.outputs['Normal'],shader.inputs['Normal'])
    shader.inputs['Roughness'].default_value=.72;changed.append(material.name)
if not changed:raise RuntimeError('Canonical stone material families not found')
bpy.context.scene['art_pass']='Painted cel stone variant; approved construction geometry retained; URP cel response is defined by shared Unity profile'
bpy.context.scene['original_sha256']=before
target=source/'SkyGardens-Stone-Art-v1.blend';bpy.ops.wm.save_as_mainfile(filepath=str(target))
assert hashlib.sha256(original.read_bytes()).hexdigest()==before
(source/'Environment-Master.json').write_text(json.dumps({'id':'SG-ENV-STONE-MASTER-v1','source':str(original.relative_to(root)),'originalSha256':before,'master':target.name,'masterSha256':hashlib.sha256(target.read_bytes()).hexdigest(),'materialFamilies':changed,'geometry':'Unchanged approved construction geometry; separate canopy, medical and cover family masters carry their refinements','axes':'Blender metres (x,y,z) -> Unity(x,z,-y)','reference':'Approved SkyGardens-Blueprint-v2 construction views and Gate 1 measured summit sheets remain geometry authority','approval':'Final integrated user playtest pending'},indent=2)+'\n')
print('SKY_STONE_MASTER_PREPARED',len(changed),'material families')
