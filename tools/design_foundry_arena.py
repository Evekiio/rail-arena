"""Produce the measured Foundry arena design, diagrams and static checks.

No Blender/Unity execution and no runtime scene changes. Values are authoring
targets. Static visibility checks use the specified solids, not live physics.
"""
import json
import hashlib
import math
from collections import deque
from pathlib import Path
from xml.sax.saxutils import escape

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Original/Foundry-Arena-v2'
OUT.mkdir(parents=True,exist_ok=True)
surfaces=[];cover=[];spawns=[];pads=[]

def slab(name,x,n,w,d,h,kind='lower'):
    surfaces.append(dict(id=name,center=[x,n,h-.3],size=[w,d,.6],top=h,kind=kind))

def block(name,x,n,w,d,base,height,kind='sight_blocker'):
    cover.append(dict(id=name,center=[x,n,base+height/2],size=[w,d,height],kind=kind))

for s in (-1,1):
    slab(f'Outer-X{s}',s*25,0,6,40,0)
    slab(f'Outer-N{s}',0,s*17,44,6,0)
    slab(f'Upper-Spine{s}',s*8,0,4,12,4,'upper')
    for t in (-1,1):
        slab(f'Wing{s}_{t}',s*17,t*12,10,4,2,'wing')
        slab(f'Upper-Landing{s}_{t}',s*8,t*8,4,4,4,'upper')
        block(f'Wing-LowCover{s}_{t}',s*21,t*11,2,1,2,1.2,'low_cover')
        pads.append(dict(id=f'JP{len(pads)+1}',start=[s*25,t*12,0],
                         destination=[s*18,t*12,2],triggerRadius=1,
                         landingSize=[3,3],verticalVelocity=14,
                         lateralSteering='retain normal air control; no input lock',
                         retrigger='must leave trigger after contact; 30-tick guard',
                         trigger='grounded or descending contact with top surface'))
slab('Bridge',0,0,12,6,4,'upper')
slab('Underpass',0,0,8,28,0)
for s in (-1,1):
    block(f'Underpass-Pier{s}',s*3.8,0,1.6,4,0,3.4)
    block(f'Bridge-Screen{s}',s*3.5,s*1.8,1.5,2.4,4,2.6)
    for t in (-1,1):
        block(f'LongLane-Elbow{s}_{t}',s*(25+t),s*t*3,2,2,0,2.6)
        block(f'CrossLane-Elbow{s}_{t}',t*16,s*16.2,2.4,2.8,0,2.6)
        # Spawn screen stays inside the envelope and leaves >=3m bypass.
        block(f'Corner-Screen{s}_{t}',s*23,t*16,2,2,0,2.6)
        block(f'InnerSpawn-Lip{s}_{t}',t*9,s*14.8,3.2,1.6,0,2.6)
    block(f'InnerSpawn-Screen{s}',s*7,s*16.2,1.2,2.8,0,2.6)
    block(f'InnerSpawn-ScreenMirror{s}',-s*7,s*16.2,1.2,2.8,0,2.6)
    block(f'Portal-Baffle{s}',s*4.8,s*15.5,1,2,0,2.6)

spawnPoints=[(-25.5,-17.5),(25.5,17.5),(-25.5,17.5),(25.5,-17.5),
             (-9,-17),(9,17),(9,-17),(-9,17)]
for i,(x,n) in enumerate(spawnPoints):
    spawns.append(dict(id=f'S{i+1}',position=[x,n,0],eyeHeight=1.58,
                       initialFacingDegrees=math.degrees(math.atan2(-x,-n)),
                       requiredClearRadius=.65,selection='occluded first, then farthest enemy'))
health=[dict(id='H1',position=[-14,12,2]),dict(id='H2',position=[0,0,4]),
        dict(id='H3',position=[14,-12,2])]
ramps=[]
for s in (-1,1):
    for t in (-1,1):
        ramps.append(dict(id=f'R{len(ramps)+1}',width=4,
                          profile=[[s*26,t*8,0],[s*19.5,t*8,2],
                                   [s*16.5,t*8,2],[s*10,t*8,4]],
                          thickness=.4,wingJoin='3m flat turnout touches wing edge at north/south10m'))
safeDrops=[dict(id=f'DROP{i+1}',edge=[s*19.5,t*14,2],landing=[s*19.5,t*15.5,0],
                clearLandingSize=[3,3],type='walk-off; no launch impulse')
           for i,(s,t) in enumerate([(-1,-1),(-1,1),(1,-1),(1,1)])]
portals=[]
for s,name in [(-1,'PA'),(1,'PB')]:
    portals.append(dict(id=name,center=[0,s*18,1.3],frontNormal=[0,-s,0],
                        exit=[0,s*15.8,0],aperture=[1.4,2.2],
                        apertureBottom=0,apertureCenter=[0,s*18,1.1],
                        housing=[2.4,.8,2.8],frontEntryOnly=True,
                        appearance='opaque cyan field; no destination camera',
                        shots='absorbed on front field and solid housing; never continue through',
                        linkedTo='PB' if name=='PA' else 'PA'))

def bounds(obj):
    return ([a-b/2 for a,b in zip(obj['center'],obj['size'])],
            [a+b/2 for a,b in zip(obj['center'],obj['size'])])

def ray_box(a,b,lo,hi):
    low,high=0.,1.
    for i in range(3):
        delta=b[i]-a[i]
        if abs(delta)<1e-9:
            if a[i]<lo[i] or a[i]>hi[i]:return False
        else:
            u,v=(lo[i]-a[i])/delta,(hi[i]-a[i])/delta
            low=max(low,min(u,v));high=min(high,max(u,v))
            if low>high:return False
    return high>0 and low<1

def spawn_visibility():
    results=[]
    # Ramps are omitted: finding an occluding box is conservative for a blocked
    # center-eye ray. This does not test silhouettes, lean positions or a live controller.
    solids=surfaces+cover
    for i,first in enumerate(spawns):
        for second in spawns[i+1:]:
            a=first['position'].copy();b=second['position'].copy();a[2]+=1.58;b[2]+=1.58
            blockers=[o['id'] for o in solids if ray_box(a,b,*bounds(o))]
            results.append(dict(pair=[first['id'],second['id']],blocked=bool(blockers),blockers=blockers))
    return results

zones=[dict(id='CROWN',bounds=[-10,-10,10,10],height=4,purpose='contested high route; split exposure'),
       dict(id='UNDERPASS',bounds=[-4,-14,4,14],height=0,purpose='covered north/south circulation'),
       dict(id='WEST-WING',bounds=[-22,-14,-12,14],height=2,purpose='two linked-through-ramps combat balconies'),
       dict(id='EAST-WING',bounds=[12,-14,22,14],height=2,purpose='rotational counterpart'),
       dict(id='OUTER-LOOP',bounds=[-28,-20,28,20],height=0,purpose='continuous perimeter with offset exposure breaks'),
       dict(id='NORTH-GATE',bounds=[-4,14,4,20],height=0,purpose='portal junction with escape into loop/underpass'),
       dict(id='SOUTH-GATE',bounds=[-4,-20,4,-14],height=0,purpose='paired portal junction')]

g=25
for pad in pads:
    rise=pad['destination'][2]-pad['start'][2]
    t=(pad['verticalVelocity']+math.sqrt(pad['verticalVelocity']**2-2*g*rise))/g
    pad['nominalFlightSeconds']=round(t,4)
    pad['nominalHorizontalVelocity']=[round((b-a)/t,4) for a,b in zip(pad['start'][:2],pad['destination'][:2])]
    pad['nominalApexHeight']=round(pad['verticalVelocity']**2/(2*g),4)
    pad['healthClearance']=round(min(math.dist(pad['destination'],h['position']) for h in health),3)
visibility=spawn_visibility()
def lower_route_check():
    # Conservative horizontal capsule projection, not a CharacterController test.
    radius=.45
    ground=[o for o in surfaces if o['top']==0]
    obstacles=[o for o in cover+surfaces if bounds(o)[0][2]<1.8 and bounds(o)[1][2]>.05]
    def allowed(ix,iy):
        x,n=ix*.5,iy*.5
        for dx,dn in ((radius,0),(-radius,0),(0,radius),(0,-radius)):
            if not any((lo:=bounds(o)[0])[0]<=x+dx<=bounds(o)[1][0] and
                       lo[1]<=n+dn<=bounds(o)[1][1] for o in ground):return False
        for o in obstacles:
            lo,hi=bounds(o)
            dx=max(lo[0]-x,0,x-hi[0]);dn=max(lo[1]-n,0,n-hi[1])
            if dx*dx+dn*dn<radius*radius:return False
        return True
    cells={(x,n) for x in range(-56,57) for n in range(-40,41) if allowed(x,n)}
    first=tuple(round(v*2) for v in spawns[0]['position'][:2])
    seen={first};queue=deque([first])
    while queue:
        x,n=queue.popleft()
        for candidate in ((x+1,n),(x-1,n),(x,n+1),(x,n-1)):
            if candidate in cells and candidate not in seen:seen.add(candidate);queue.append(candidate)
    points=[(s['id'],s['position']) for s in spawns]+[(p['id'],p['start']) for p in pads]+[(p['id'],p['exit']) for p in portals]
    # The 15.8m exit coordinate uses its nearest grid sample only.
    unreachable=[name for name,p in points if tuple(round(v*2) for v in p[:2]) not in seen]
    assert not unreachable,unreachable
    return dict(gridMetres=.5,projectedRadius=radius,connectedTargets=len(points),
                reachableCells=len(seen),limitations='Horizontal projection only; ramps, portal housing and dynamic actors require live tests')
lowerConnectivity=lower_route_check()
data=dict(version=2,status='complete authored proposal; creator layout approval and live playtest pending',
          authority='docs/ARENA-DESIGN.md',units='metres; x east, n north, h up; Unity(x,h,-n)',
          footprint=[56,40],priorities=['duel first','flowing routes with deliberate exposure',
                                      'opaque shot-blocking portal pair'],
          surfaces=surfaces,ramps=ramps,cover=cover,spawns=spawns,health=health,
          jumpPads=pads,portals=portals,safeDrops=safeDrops,zones=zones,
          liveControllerValidation=False,competitiveBalanceValidated=False)
(OUT/'Map.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
inventory=[]
for prefix,purpose,size,count,collision in [
    ('FLOOR-4','Main floor bay',[4,4,.6],77,'walkable'),
    ('FLOOR-4x2','Half-width closure',[4,2,.6],49,'walkable'),
    ('EDGE','Contrasting deck lip',[4,.15,.15],None,'cosmetic; no snagging collider'),
    ('BEAM','Understructure beam',[4,.4,.8],None,'cosmetic'),
    ('PAD','Recessed launch device',[2,2,.35],4,'separate trigger; flush walking surface'),
    ('PORTAL','Shared A/B housing',[2.4,.8,2.8],2,'housing/opaque field block shots; entry plane separate'),
    ('HEALTH','Pedestal and plus marker',[.8,.8,.25],3,'nonblocking art; pickup trigger separate')]:
    inventory.append(dict(id=prefix,purpose=purpose,dimensions=size,instances=count,
                          countPolicy='derive tiling during greybox import' if count is None else 'fixed',
                          collision=collision))
groups={}
for obj in cover:
    key=tuple(obj['size'])
    groups.setdefault(key,[]).append(obj['id'])
for i,(size,ids) in enumerate(groups.items()):
    inventory.append(dict(id=f'COVER-{i+1:02d}',purpose='Reusable cover variant',dimensions=list(size),
                          instances=len(ids),placements=ids,collision='exact footprint/height from Map.json'))
inventory.append(dict(id='RAMP',purpose='Shared two-incline ramp assembly',dimensions=[16,4,4],
                      instances=4,profileRuns=[6.5,3,6.5],profileHeights=[0,2,2,4],
                      collision='walkable profile; four assemblies, not extra gameplay ramps'))
dressing={
    'VENT':[[s*27.8,n,-.8] for s in (-1,1) for n in (-12,0,12)]+
           [[x,s*19.8,-.8] for s in (-1,1) for x in (-12,12)],
    'CONDUIT':[[s*27.8,n,-.4] for s in (-1,1) for n in (-6,6)]+
              [[x,s*19.8,-.4] for s in (-1,1) for x in (-18,18)],
    'FLOODLIGHT':[[s*27.7,n,.03] for s in (-1,1) for n in (-14,0,14)]+
                 [[x,s*19.7,.03] for s in (-1,1) for x in (-18,0,18)],
    'WEST-TURBINE-LANDMARK':[[-90,0,-10]],'EAST-SPLIT-FIN-LANDMARK':[[90,0,-10]],
    'NORTH-THREE-BAR-SIGN':[[0,18,2.7]],'SOUTH-TWO-CHEVRON-SIGN':[[0,-18,2.7]],
    'SKY':[[0,0,0]],
    'DISTANT-ISLAND':[[math.cos(i*math.pi/4)*200,math.sin(i*math.pi/4)*200,-35-(i%3)*15] for i in range(8)],
    'DISTANT-CITADEL':[[200,0,-30],[-141.42,141.42,-45],[0,-200,-60]]}
for name,placements in dressing.items():
    inventory.append(dict(id=name,purpose='Nonblocking environment dressing/world',instances=len(placements),
                          placements=placements,countPolicy='fixed initial art target',collision='none',
                          mounting='flush/inset or distant background; never protrude into movement lanes'))
assert sum(o['size'][0]*o['size'][1] for o in surfaces)==77*16+49*8
for item in inventory:
    item['requiredReferences']=['front','back','left/right','top','underside','three-quarter','material separation']
    item['approval']='awaiting layout/art review'
    item['editableMasterRequired']=True
(OUT/'Environment-Kit.json').write_text(json.dumps(inventory,indent=2)+'\n',encoding='utf-8')

colors={'lower':'#516a71','wing':'#9db5b0','upper':'#e5dac0'}
def diagram(name,level=None,markers=True):
    def xy(x,n):return 520+x*15,410-n*15
    out=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1120 860">',
         '<rect width="1120" height="860" fill="#121a22"/>',
         '<style>text{fill:#efe9d9;font:14px Arial,sans-serif}.tiny{font-size:11px}</style>',
         f'<text x="40" y="35" style="font-size:26px">RAIL ARENA / {escape(name)}</text>',
         '<text x="40" y="62">56 × 40m · duel-first proposal · all dimensions in Map.json · live testing pending</text>']
    for o in sorted(surfaces,key=lambda item:item['top']):
        if level is not None and o['top']!=level:continue
        lo,hi=bounds(o);x,y=xy(lo[0],hi[1]);out.append(f'<rect x="{x}" y="{y}" width="{o["size"][0]*15}" height="{o["size"][1]*15}" fill="{colors[o["kind"]]}" stroke="#20313d"/>')
    if level is None:
        for ramp in ramps:
            points=[xy(p[0],p[1]) for p in ramp['profile']]
            out.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in points)+'" fill="none" stroke="#739da7" stroke-width="52"/>')
            x,y=xy(*ramp['profile'][1][:2]);out.append(f'<text x="{x}" y="{y}" text-anchor="middle">{ramp["id"]}</text>')
    for o in cover:
        base=o['center'][2]-o['size'][2]/2
        if level is not None and abs(base-level)>.01:continue
        lo,hi=bounds(o);x,y=xy(lo[0],hi[1]);out.append(f'<rect x="{x}" y="{y}" width="{o["size"][0]*15}" height="{o["size"][1]*15}" fill="#14212a" stroke="#cfba96"/>')
    if markers:
        items=[(s['id'],s['position'],'#f2a7aa') for s in spawns]
        items += [(h['id'],h['position'],'#ffb331') for h in health]
        items += [(p['id'],p['start'],'#48dae8') for p in pads]
        items += [(p['id']+'L',p['destination'],'#48dae8') for p in pads]
        items += [(p['id'],p['edge'],'#b1d1ab') for p in safeDrops]
        items += [(p['id'],[p['center'][0],p['center'][1],0],'#bf9eee') for p in portals]
        for label,p,color in items:
            if level is not None and p[2]!=level:continue
            x,y=xy(p[0],p[1]);out.append(f'<circle cx="{x}" cy="{y}" r="12" fill="{color}"/><text x="{x}" y="{y+4}" text-anchor="middle" style="fill:#121a22;font-size:10px;font-weight:bold">{label}</text>')
    out.extend(['<text x="40" y="780">Ivory: +4m · pale teal: +2m · dark teal: 0m · outlined dark blocks: cover</text>',
                '<text x="40" y="807">S: spawn · H: health · JP: pad · JPL: landing · PA/PB: linked portal gates</text>',
                '<text x="40" y="834">Diagram is measured authoring data, not a rendered runtime scene or a balance certification.</text>','</svg>'])
    (OUT/(name+'.svg')).write_text('\n'.join(out),encoding='utf-8')

diagram('Master-Plan')
diagram('Lower-Deck',0)
diagram('Wing-Deck',2)
diagram('Upper-Deck',4)
def section(name,axis,cut):
    # True numeric plane intersection of authored boxes and ramp profiles.
    # This is a design vector, not a screenshot of the historical v1 master.
    def xy(horizontal,height):return 520+horizontal*15,350-height*30
    svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1120 500">',
         '<rect width="1120" height="500" fill="#121a22"/>',
         '<style>text{fill:#efe9d9;font:15px Arial,sans-serif}</style>',
         f'<text x="40" y="35" style="font-size:25px">FOUNDRY / {escape(name)}</text>',
         '<text x="40" y="65">Measured authoring section · horizontal 15px/m, vertical 30px/m (2× exaggeration)</text>']
    other=1-axis
    for o in surfaces+cover:
        lo,hi=bounds(o)
        if not lo[other]<=cut<=hi[other]:continue
        x,y=xy(lo[axis],hi[2])
        svg.append(f'<rect x="{x}" y="{y}" width="{(hi[axis]-lo[axis])*15}" height="{o["size"][2]*30}" fill="{colors.get(o.get("kind"),"#253d4a")}" stroke="#b8c5c1"/>')
    if axis==0:
        for ramp in ramps:
            if abs(ramp['profile'][0][1]-cut)>ramp['width']/2:continue
            points=[xy(p[0],p[2]) for p in ramp['profile']]
            svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in points)+'" fill="none" stroke="#75bac7" stroke-width="8"/>')
    for h in (0,2,4):
        _,y=xy(0,h);svg.append(f'<text x="35" y="{y}">+{h}m</text>')
    svg.extend(['<text x="40" y="430">Use Map.json for exact dimensions; profiles and cut solids only, no inferred scenery.</text>','</svg>'])
    (OUT/(name+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
section('Section-EW-N8',0,8)
section('Section-NS-X0',1,0)
report=dict(staticChecksOnly=True,mapSha256=hashlib.sha256((OUT/'Map.json').read_bytes()).hexdigest(),spawnPairCount=len(visibility),
            lowerRouteProjection=lowerConnectivity,
            exposedSpawnPairs=[v for v in visibility if not v['blocked']],
            spawnVisibility=visibility,
            pickupLandingClearances={p['id']:p['healthClearance'] for p in pads},
            movementApproximation={'normalJumpApex':8.4375**2/50,
                                   'normalJumpSameHeightRangeAt10mps':10*2*8.4375/25},
            livePhysicsTested=False,performanceTested=False)
assert len(pads)==4 and len(portals)==2 and len(spawns)==8 and len(health)==3
assert all(p['healthClearance']>=4 for p in pads)
for pad in pads:
    x,n,h=pad['destination']
    for obj in cover:
        lo,hi=bounds(obj)
        if hi[2]<=h or lo[2]>=h+2:continue
        assert not (min(x+1.5,hi[0])>max(x-1.5,lo[0]) and
                    min(n+1.5,hi[1])>max(n-1.5,lo[1])),(pad['id'],obj['id'],'landing obstruction')
for drop in safeDrops:
    x,n,h=drop['landing']
    for obj in cover:
        lo,hi=bounds(obj)
        if hi[2]<=h or lo[2]>=h+2:continue
        assert not (min(x+1.5,hi[0])>max(x-1.5,lo[0]) and
                    min(n+1.5,hi[1])>max(n-1.5,lo[1])),(drop['id'],obj['id'],'drop landing obstruction')
assert not report['exposedSpawnPairs'],report['exposedSpawnPairs']
for obj in surfaces+cover:
    lo,hi=bounds(obj)
    assert -28<=lo[0]<=hi[0]<=28 and -20<=lo[1]<=hi[1]<=20,obj['id']
    assert any(other['size']==obj['size'] and all(abs(a-b)<.001 for a,b in
               zip(other['center'],[-obj['center'][0],-obj['center'][1],obj['center'][2]]))
               for other in surfaces+cover),(obj['id'],'unpaired critical geometry')
(OUT/'Static-Review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(f'Authored arena: {len(surfaces)} slabs, {len(cover)} cover pieces, 4 ramp assemblies; {len(visibility)} spawn-pair rays occluded.')
