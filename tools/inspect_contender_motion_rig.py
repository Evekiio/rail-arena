"""Read-only pose-space inspection before authoring compatible motion."""
import json
from pathlib import Path
import sys
import bpy

root=Path(sys.argv[sys.argv.index('--')+1]).resolve()
source=root/'ArtSource/Original/ArtPass-v1/Characters/Player-ArtPass-v1.blend'
bpy.ops.wm.open_mainfile(filepath=str(source),load_ui=False)
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
data={'source':str(source.relative_to(root)),'rig':rig.name,'worldMatrix':[list(row) for row in rig.matrix_world],'bones':[]}
for name in ('Hips','Spine','Head','LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','LeftArm','LeftForeArm'):
    bone=rig.data.bones[name]
    data['bones'].append({'name':name,'head':list(bone.head_local),'tail':list(bone.tail_local),'restMatrix':[list(row) for row in bone.matrix_local],'parent':bone.parent.name if bone.parent else None})
folder=root/'ArtSource/Original/ArtPass-v1/Animation';folder.mkdir(exist_ok=True)
(folder/'rig-space-inspection.json').write_text(json.dumps(data,indent=2)+'\n')
print('MOTION_RIG_INSPECTED',rig.name,tuple(rig.scale))
