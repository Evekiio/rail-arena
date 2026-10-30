"""Non-destructive v2 detail design. All positions are metres, canonical X east/N north/H up."""
import json,math,random
from pathlib import Path
R=Path(__file__).resolve().parents[1];O=R/'ArtSource/Original/SkyGardens-Blueprint-v2';O.mkdir(exist_ok=True)
m=json.loads((R/'ArtSource/Original/SkyGardens-Blueprint-v1/Scene.json').read_text());m['version']=2
m['objects']=[o for o in m['objects'] if o['group'] not in ('foliage_envelopes','waterfall_envelopes')]
objects=m['objects'];rng=random.Random(73029)
def add(id,vertices,faces,group,family):
 objects.append(dict(id=id,shape='mesh',vertices=vertices,faces=faces,group=group,family=family,collision=False))
def box(id,c,s,group,family):
 objects.append(dict(id=id,shape='box',center=c,size=s,group=group,family=family,collision=False))
def tube(points,radii,sides=10):
 v=[];f=[]
 # Ring axes stay horizontal; trunks and hanging vines never run horizontally.
 for p,r in zip(points,radii):
  for i in range(sides):
   a=i*2*math.pi/sides;v.append([p[0]+r*math.cos(a),p[1]+r*math.sin(a),p[2]])
 for j in range(len(points)-1):
  for i in range(sides):f.append([j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i])
 f.extend([list(range(sides-1,-1,-1)),list(range((len(points)-1)*sides,len(points)*sides))]);return v,f
def merge(dst,src):
 v,f=dst;sv,sf=src;offset=len(v);v.extend(sv);f.extend([[i+offset for i in face] for face in sf])
def leaf(c,length,width,angle,pitch):
 # Closed six-sided leaf blade, with explicit midrib camber and 4 mm thickness.
 outline=[(-.5,0),(-.18,.5),(.28,.36),(.5,0),(.28,-.36),(-.18,-.5)]
 v=[]
 for depth in [-.002,.002]:
  for a,b in outline:
   x=a*length;y=b*width;z=.04*(1-abs(a)*2)+depth
   xx=x*math.cos(pitch)-z*math.sin(pitch);zz=x*math.sin(pitch)+z*math.cos(pitch)
   v.append([c[0]+xx*math.cos(angle)-y*math.sin(angle),c[1]+xx*math.sin(angle)+y*math.cos(angle),c[2]+zz])
 f=[list(range(5,-1,-1)),list(range(6,12))]+[[i,(i+1)%6,(i+1)%6+6,i+6] for i in range(6)];return v,f
# Dressed collision envelopes keep original extents. Panel insets stay within those volumes.
for o in list(objects):
 if o['group']=='cover':
  c=o['center'];s=o['size']
  box(o['id']+'-CAP',[c[0],c[1],c[2]+s[2]/2+.005],[s[0],s[1],.01],'ivory_trim','StoneKit')
  for side in [-1,1]:box(o['id']+f'-INLAY-{side}',[c[0],c[1]+side*(s[1]/2-.008),c[2]+s[2]*.22],[s[0]*.72,.012,.065],'bronze','StoneKit')
# Summit broken annular canopy, supported at four existing cardinal columns.
for q in range(4):
 a=q*math.pi/2;x=6.4*math.cos(a);y=6.4*math.sin(a)
 box(f'CAPITAL-{q+1}',[x,y,26.88],[.92,.92,.24],'ivory_trim','SummitCanopy')
 for start,end in [(q*90,q*90+38),(q*90+52,q*90+90)]:
  v=[];f=[];steps=16
  for i in range(steps+1):
   a=math.radians(start+(end-start)*i/steps)
   v.extend([[r*math.cos(a),r*math.sin(a),z] for r,z in [(6.1,27),(6.7,27),(6.7,27.36),(6.1,27.36)]])
  for i in range(steps):
   for j in range(4):f.append([i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j])
  f.extend([[3,2,1,0],[steps*4+j for j in range(4)]])
  add(f'CANOPY-{q+1}-{start}',v,f,'ivory_trim','SummitCanopy')
# Two thin arch skins terminate on the original underpass piers; no extra floor obstacle.
for side in [-1,1]:
 v=[];f=[];segments=32
 for i in range(segments+1):
  x=-3+6*i/segments;h=2+1.2*math.sqrt(max(0,1-(x/3)**2))
  v.extend([[x,n,z] for n,z in [(side*1.85-.12,h),(side*1.85+.12,h),(side*1.85+.12,h+.18),(side*1.85-.12,h+.18)]])
 for i in range(segments):
  for j in range(4):f.append([i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j])
 f.extend([[3,2,1,0],[segments*4+j for j in range(4)]]);add(f'UNDERPASS-ARCH-{side}',v,f,'ivory_trim','Arch')
for i,g in enumerate(m['plantings']):
 x,y,z=g['trunkCenter'];family='Garden'
 # Four stone curb pieces remain inside the defined outboard planting rectangle.
 for s in [-1,1]:
  box(f'PLANTER-{i+1}-X{s}',[x+s*1.21,y,.28],[.18,3.6,.32],'ivory_trim',family)
  box(f'PLANTER-{i+1}-N{s}',[x,y+s*1.71,.28],[2.24,.18,.32],'ivory_trim',family)
 points=[[x,y,z],[x+.08,y-.04,1.3],[x-.12,y+.08,2.5],[x+.18,y,3.5],[x+.35,y+.1,4.5]]
 bark=tube(points,[.18,.15,.115,.07,.018],12)
 leaves=([],[])
 for b in range(10):
  a=b*2*math.pi/10;ex=x+math.cos(a)*(1.0 if b%2 else .75);ey=y+math.sin(a)*1.15;eh=3.5+(b%3)*.45
  merge(bark,tube([[x,y,2.3],[x+math.cos(a)*.5,y+math.sin(a)*.5,3.1],[ex,ey,eh]],[.065,.04,.012],8))
  for j in range(24):
   r=math.sqrt(rng.random())*.48;theta=rng.random()*math.tau
   c=[ex+r*math.cos(theta),ey+r*math.sin(theta),eh+rng.uniform(-.2,.4)]
   merge(leaves,leaf(c,rng.uniform(.22,.4),.16,theta,rng.uniform(-.4,.4)))
 add(f'TREE-{i+1}-BARK',*bark,'bark','Tree');add(f'TREE-{i+1}-LEAVES',*leaves,'foliage','Tree')
 grass=([],[]);shrubs=([],[]);vines=([],[]);vineleaves=([],[])
 for j in range(110):
  cx=x+rng.uniform(-1.05,1.05);cy=y+rng.uniform(-1.5,1.5)
  merge(grass,leaf([cx,cy,.22],rng.uniform(.18,.32),.035,rng.random()*math.tau,1.2))
 for s in [-1,1]:
  for j in range(70):
   a=rng.random()*math.tau;r=math.sqrt(rng.random())*.38;c=[x+s*.72+r*math.cos(a),y+s*.95+r*math.sin(a),.35+rng.random()*.4]
   merge(shrubs,leaf(c,.21,.12,a,rng.uniform(-.6,.6)))
 for strand in range(6):
  xx=x-1.05+strand*.42;yy=y+1.98;pts=[[xx+.08*math.sin(t*.8+strand),yy,-t*.22] for t in range(18)]
  merge(vines,tube(pts,[.012]*len(pts),6))
  for j in range(16):
   c=[xx+.08*math.sin(j*.8+strand)+(.08 if j%2 else -.08),yy+.02,-j*.22]
   merge(vineleaves,leaf(c,.19,.13,math.pi/2,.5))
 add(f'GRASS-{i+1}',*grass,'grass','Garden');add(f'SHRUB-{i+1}',*shrubs,'foliage','Garden')
 add(f'IVY-{i+1}-STEMS',*vines,'bark','Ivy');add(f'IVY-{i+1}-LEAVES',*vineleaves,'foliage','Ivy')
 # Waterfall surface is a deterministic tessellated static rest shape, not animated yet.
 side=1 if x>0 else -1;lipx=x+side*1.55
 box(f'WATER-LIP-{i+1}',[lipx,y,.04],[.3,2.2,.16],'bronze','Waterfall')
 v=[];f=[];rows=64;cols=8
 for row in range(rows+1):
  t=row/rows
  for col in range(cols+1):
   n=(col/cols-.5)*2;xx=lipx+side*(.06+math.sin(t*math.pi)*.18+.035*math.sin(t*47+n*6));zz=-20*t
   v.append([xx,y+n*(1-.16*t),zz])
 for row in range(rows):
  for col in range(cols):a=row*(cols+1)+col;f.append([a,a+1,a+cols+2,a+cols+1])
 add(f'WATERFALL-{i+1}-SURFACE',v,f,'water','Waterfall')
 for stripe in [-.65,0,.65]:
  vv=[];ff=[]
  for row in range(rows+1):
   t=row/rows
   for delta in [-.025,.025]:vv.append([lipx+side*(.085+math.sin(t*math.pi)*.18+.035*math.sin(t*47+stripe*6)),y+stripe*(1-.16*t)+delta,-20*t])
  for row in range(rows):ff.append([row*2,row*2+1,row*2+3,row*2+2])
  add(f'WATERFALL-{i+1}-FOAM-{stripe}',vv,ff,'foam','Waterfall')
# Portal dress is distinct from the retained non-colliding aperture reservation.
for p in m['portals']:
 x,n,h=p['center'];name=p['id']
 for s in [-1,1]:box(name+f'-STONE-JAMB-{s}',[x+s*.95,n,1.2],[.5,.8,2.4],'ivory_trim','PortalKit')
 box(name+'-SOLID-BACK',[x,n-p['frontNormal'][1]*.35,1.3],[2.4,.1,2.8],'ivory_trim','PortalKit')
 box(name+'-STONE-HEADER',[x,n,2.45],[2.4,.8,.5],'ivory_trim','PortalKit')
 for s in [-1,1]:box(name+f'-TEAL-INLAY-{s}',[x+s*.71,n+p['frontNormal'][1]*.411,1.1],[.035,.02,2.2],'teal','PortalKit')
m['detailDesign']=dict(seed=73029,scope='Measured detailed asset candidates; pending creator review',canopy=dict(innerRadius=6.1,outerRadius=6.7,bottom=27,top=27.36,breakDegrees=14),arches=dict(clearSpan=6,springHeight=2,innerCrown=3.2,outerCrown=3.38,depth=.24),foliage='closed 4mm-thick leaf blades, deterministic tree/grass/shrub/vine placement',waterfall='20m rest surface; 64 longitudinal x 8 lateral segments, 3 foam ribbons; animation not implemented',materials='semantic groups; authored review palette, no final texture/LOD qualification')
m['limits']=['Detailed meshes are dimensioned candidates, not a claim of exact correspondence to every pixel of the AI concept','collision proxies and base layout preserved; new detail meshes are cosmetic','foliage wind, flowing water, textures and LODs remain production tasks','summit pad and drop behavior needs actual-controller validation before gameplay integration']
(O/'Scene.json').write_text(json.dumps(m,indent=2),encoding='utf-8');print('DETAIL_OBJECTS',len(objects))
