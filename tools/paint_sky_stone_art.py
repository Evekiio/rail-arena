"""Original temple-stone painting; preserves the established two-by-four UV course layout."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parent.parent
assets=root/'RailArena/Assets/RailArena/Art/ArtPass/Environment'
source=root/'ArtSource/Original/ArtPass-v1/Environment'
assets.mkdir(parents=True,exist_ok=True);source.mkdir(parents=True,exist_ok=True)
size=1024
y,x=np.mgrid[0:size,0:size].astype(np.float32)
row=(y//256).astype(int);column=((x+(row%2)*256)//512).astype(int)
xx=np.mod(x+(row%2)*256,512);yy=np.mod(y,256)
edge=np.minimum.reduce([xx,511-xx,yy,255-yy])
cloud=np.sin(x*.019)*np.sin(y*.027)*1.8+np.sin(x*.007+y*.011)*1.3
variation=((row*3+column*7)%5-2)*1.5
base=np.stack([224+cloud+variation,229+cloud+variation,217+cloud+variation],axis=2)
mortar=edge<3
base[mortar]=[142,162,151]
border=(edge>=3)&(edge<8)
base[border]*=.92
highlight=(yy>=8)&(yy<13)
base[highlight]=np.minimum(base[highlight]+9,255)
# Deliberate broad painted stone planes, without noisy photorealistic detail.
wash=np.clip((np.sin(xx*.011+row)*np.cos(yy*.012)+.35)*2,0,3)
base[:,:,1]+=wash
albedo=np.clip(base,0,255).astype(np.uint8)
height=np.clip((edge-2)/8,0,1)*.012
dy,dx=np.gradient(height)
normal=np.stack([-dx*18,dy*18,np.ones_like(dx)],axis=2)
normal/=np.linalg.norm(normal,axis=2,keepdims=True)
normal=np.clip((normal*.5+.5)*255,0,255).astype(np.uint8)
occlusion=np.where(edge<3,185,np.where(edge<8,222,255)).astype(np.uint8)
maps={'Stone-Painted':Image.fromarray(albedo),'Stone-Normal':Image.fromarray(normal),'Stone-Cavity':Image.fromarray(np.repeat(occlusion[:,:,None],3,axis=2))}
for name,image in maps.items():image.save(assets/(name+'.png'));image.save(source/(name+'.png'))
(source/'Stone-Paint-Spec.json').write_text(json.dumps({'id':'SG-STONE-PAINT-v1','provenance':'Original code-authored cel stone texture painting','sizePixels':[size,size],'layout':'Two horizontal tiles, four staggered courses; same normalized course frequency as baseline','units':'UV-space painting; world metres per tile remain determined by unchanged source mesh UVs','maps':{'baseColor':'sRGB','normal':'tangent-space linear','cavity':'linear green channel'},'geometry':'No displacement or collider changes','runtimeRoles':['structure','cover','garden_structure','ivory_trim','ramps','summit'],'sha256':{name:hashlib.sha256((assets/(name+'.png')).read_bytes()).hexdigest() for name in maps},'approval':'Final integrated user playtest pending'},indent=2)+'\n')
print('SKY_STONE_PAINTED',size)
