"""Original seamless limestone paint for the measured deck kit; tile joints are authored geometry."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Environment/BasementDecks';folder.mkdir(exist_ok=True)
export=root/'RailArena/Assets/RailArena/Art/ArtPass/Environment/BasementDecks';export.mkdir(parents=True,exist_ok=True)
size=1024;y,x=np.mgrid[0:size,0:size]/size
height=np.zeros((size,size));rng=np.random.default_rng(2147)
for frequency,amplitude in [(1,.30),(2,.14),(4,.065),(8,.025),(16,.008),(32,.003)]:
    for _ in range(3):
        a,b=rng.integers(1,frequency+1,size=2);phase=rng.uniform(0,2*np.pi)
        height+=amplitude*np.sin(2*np.pi*(a*x+b*y)+phase)/3
brush=np.sin(2*np.pi*(7*x+2*y)+.8*np.sin(2*np.pi*y))*.01
color=np.array([.83,.825,.735])[None,None,:]+(height*.055+brush)[:,:,None]
dx=(np.roll(height,-1,1)-np.roll(height,1,1))*.75;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*.75
normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=2);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
maps={'Paving-Painted':np.clip(color*255,0,255).astype('uint8'),'Paving-Normal':np.clip((normal*.5+.5)*255,0,255).astype('uint8'),'Paving-Cavity':np.repeat(np.clip(1-np.maximum(-height,0)*.12,0,1)[:,:,None],3,axis=2).__mul__(255).astype('uint8')}
hashes={}
for name,pixels in maps.items():
    path=folder/(name+'.png');Image.fromarray(pixels).save(path);(export/path.name).write_bytes(path.read_bytes());hashes[name]=hashlib.sha256(path.read_bytes()).hexdigest()
(folder/'paint-spec.json').write_text(json.dumps({'id':'SG-DECK-PAINT-v1','provenance':'Original analytic periodic limestone paint authored for Rail Arena; deterministic seed 2147','sizePixels':[size,size],'worldPeriodMetres':4,'jointAuthority':'Paving and coping joints are geometry; this paint adds restrained tonal brush variation without a competing brick grid','maps':{'baseColor':'sRGB','normal':'tangent-space linear','cavity':'green-channel linear'},'sha256':hashes},indent=2)+'\n')
print('BASEMENT_PAVING_PAINTED 3 original seamless maps / 1024 square / 4 metre UV period')
