"""Deterministic orthographic sheets and Blender mesh inputs from Scene.json."""
import json,math,csv,html,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'ArtSource/Original'/os.environ.get('RAIL_BLUEPRINT_PACKAGE','SkyGardens-Blueprint-v1')
M=json.loads((OUT/'Scene.json').read_text())
def mesh(o):
 if o['shape']=='mesh':return o['vertices'],o['faces']
 if o['shape']=='box':
  c,s=o['center'],o['size'];v=[[c[j]+sg[j]*s[j]/2 for j in range(3)] for sg in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
  return v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
 if o['shape']=='cylinder':
  c=o['center'];r=o['radius'];d=o['depth'];n=o['segments'];v=[[c[0]+r*math.cos(i*2*math.pi/n),c[1]+r*math.sin(i*2*math.pi/n),c[2]+z*d/2] for z in [-1,1] for i in range(n)]
  return v,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 v=[];faces=[]
 for a,b in zip(o['profile'],o['profile'][1:]):
  start=len(v);w=o['width']/2;t=o['thickness']
  v.extend([[q[0],q[1]+side*w,q[2]-drop*t] for drop in [1,0] for q,side in [(a,-1),(b,-1),(b,1),(a,1)]])
  faces.extend(tuple(start+i for i in f) for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
 return v,faces
V={'Top':((1,0,0),(0,1,0),(0,0,1)),'Bottom':((1,0,0),(0,-1,0),(0,0,-1)),'Front-South':((1,0,0),(0,0,1),(0,-1,0)),'Back-North':((-1,0,0),(0,0,1),(0,1,0)),'Left-West':((0,-1,0),(0,0,1),(-1,0,0)),'Right-East':((0,1,0),(0,0,1),(1,0,0))}
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def sheet(name,objects,view,section=None):
 u,v,d=view;polys=[];labels=[]
 for o in objects:
  vertices,faces=mesh(o)
  if section:
   axis,offset=section;points=[]
   for face in faces:
    for i,j in zip(face,face[1:]+face[:1]):
     a,b=vertices[i],vertices[j];da,db=a[axis]-offset,b[axis]-offset
     if abs(da)<1e-7:points.append(a)
     if da*db<0:
      t=da/(da-db);points.append([a[k]+(b[k]-a[k])*t for k in range(3)])
   points=set((round(dot(p,u),6),round(dot(p,v),6)) for p in points)
   if len(points)<3:continue
   cx=sum(p[0] for p in points)/len(points);cy=sum(p[1] for p in points)/len(points)
   poly=sorted(points,key=lambda p:math.atan2(p[1]-cy,p[0]-cx));polys.append((0,poly,o));labels.append((cx,cy,o['id']))
  else:
   for face in faces:
    points=[vertices[i] for i in face];polys.append((sum(dot(p,d) for p in points)/len(points),[(dot(p,u),dot(p,v)) for p in points],o))
   c=[sum(p[k] for p in vertices)/len(vertices) for k in range(3)];labels.append((dot(c,u),dot(c,v),o['id']))
 pts=[p for _,poly,_ in polys for p in poly];xmin=min(p[0] for p in pts)-3;xmax=max(p[0] for p in pts)+3;ymin=min(p[1] for p in pts)-3;ymax=max(p[1] for p in pts)+3
 scale=min(1380/(xmax-xmin),830/(ymax-ymin));ox=90-xmin*scale;oy=960+ymin*scale
 def xy(p):return ox+p[0]*scale,oy-p[1]*scale
 s=['<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1120"><rect width="1600" height="1120" fill="#0c1c2c"/>',f'<text x="60" y="50" fill="white" font-size="26" font-family="sans-serif">SKY GARDENS / {html.escape(name.upper())}</text>','<text x="60" y="80" fill="#abc" font-size="16">METRES / ORTHOGRAPHIC / CONSTRUCTION PROPOSAL v1 / APPROVAL PENDING</text>']
 for x in range(math.ceil(xmin),math.floor(xmax)+1,2):
  a,b=xy((x,ymin)),xy((x,ymax));s.append(f'<path d="M{a[0]} {a[1]} L{b[0]} {b[1]}" stroke="#284057" stroke-width=".6"/><text x="{a[0]}" y="{a[1]+17}" fill="#abc" font-size="10">{x}</text>')
 for y in range(math.ceil(ymin),math.floor(ymax)+1,2):
  a,b=xy((xmin,y)),xy((xmax,y));s.append(f'<path d="M{a[0]} {a[1]} L{b[0]} {b[1]}" stroke="#284057" stroke-width=".6"/><text x="{a[0]-25}" y="{a[1]}" fill="#abc" font-size="10">{y}</text>')
 colors={'structure':'#d4d9d5','cover':'#71919c','ramps':'#d5bf81','ivory_trim':'#e7dcc3','bronze':'#aa7c47','teal':'#5cbbba','bark':'#876345','foliage':'#75a267','grass':'#a1b768','water':'#72cddd','foam':'#d8f4fa','summit':'#83c8cd','devices':'#eac776','landing_reservations':'#76d5b7','foliage_envelopes':'#6f9e75','waterfall_envelopes':'#8bd5ec'}
 for _,poly,o in sorted(polys,key=lambda p:p[0]):
  points=' '.join(f'{xy(p)[0]:.2f},{xy(p)[1]:.2f}' for p in poly);color=colors.get(o['group'],'#9cba9a')
  s.append(f'<polygon points="{points}" fill="{color}" stroke="#234" stroke-width=".65"><title>{html.escape(o["id"])}</title></polygon>')
 if name.startswith('Layer') or section:
  for x,y,id in labels:
   if id=='SUMMIT-DECK':continue
   a,b=xy((x,y));s.append(f'<text x="{a}" y="{b}" text-anchor="middle" fill="#123" font-size="9">{html.escape(id)}</text>')
 if name=='Top' and not section:
  for pad in M['jumpPads']:
   a=xy((dot(pad['start'],u),dot(pad['start'],v)));b=xy((dot(pad['destination'],u),dot(pad['destination'],v)));s.append(f'<path d="M{a[0]} {a[1]} L{b[0]} {b[1]}" stroke="#f5bc45" stroke-width="2" stroke-dasharray="7 5"/><text x="{a[0]}" y="{a[1]-10}" fill="#ffe" font-size="13">{pad["id"]}</text>')
 if name in V and view!=V['Top'] and view!=V['Bottom'] and not section:
  for pad in M['jumpPads']:
   points=[]
   for step in range(81):
    t=step/80*pad['flightSeconds'];p=[pad['start'][k]+pad['velocity'][k]*t-(.5*pad['gravity']*t*t if k==2 else 0) for k in range(3)];points.append(xy((dot(p,u),dot(p,v))))
   path=' '.join(('M' if i==0 else 'L')+f'{a:.2f} {b:.2f}' for i,(a,b) in enumerate(points))
   s.append(f'<path d="{path}" stroke="#ffe68a" stroke-width="2" fill="none"/>')
 if (name in V or section) and view not in (V['Top'],V['Bottom']):
  for level,label in [(0,'LOWER +0m'),(2,'WINGS +2m'),(4,'CROWN +4m'),(22,'SUMMIT +22m')]:
   a,b=xy((xmin,level)),xy((xmax,level))
   s.append(f'<path d="M{a[0]} {a[1]} L{b[0]} {b[1]}" stroke="#8abfa8" stroke-dasharray="5 7" stroke-width=".7"/><text x="{b[0]+12}" y="{b[1]+4}" fill="#b6ecd5" font-size="12">{label}</text>')
 bar=1 if xmax-xmin<8 else 5 if xmax-xmin<25 else 10
 s.append(f'<path d="M85 1030 h{bar*scale}" stroke="white" stroke-width="3"/><text x="85" y="1058" fill="white" font-size="14">{bar} m scale bar / coordinate ticks every 2 m / x east, n north, h up</text><text x="85" y="1086" fill="#abc" font-size="14">Base 56 x 40 m / summit diameter 14 m, top +22 m, underside +21.4 m / column top +27 m / cloud datum -24 m</text></svg>')
 (OUT/(name+'.svg')).write_text(''.join(s).replace('PROPOSAL v1','PROPOSAL v'+str(M['version'])),encoding='utf-8')
def main():
 for name,view in V.items():sheet(name,M['objects'],view)
 sheet('Layer-00-Lower',[o for o in M['objects'] if o['group'] in ['structure','cover','portal_envelopes','portal_fields','devices'] and o.get('center',[0,0,99])[2]<1.8],V['Top'])
 sheet('Layer-02-Wings',[o for o in M['objects'] if o['shape']=='ramp' or o['id'].startswith(('Wing','H1','H3'))],V['Top'])
 sheet('Layer-04-Crown',[o for o in M['objects'] if o['id'].startswith(('Upper','Bridge','H2'))],V['Top'])
 sheet('Layer-22-Summit',[o for o in M['objects'] if o['group'] in ['summit','landing_reservations'] or o.get('family')=='SummitCanopy'],V['Top'])
 sheet('Layer-Environment',[o for o in M['objects'] if o['group'] in ['garden_structure','planting_reservations','foliage_envelopes','waterfall_envelopes'] or o.get('family') in ['Garden','Tree','Ivy','Waterfall']],V['Top'])
 sheet('Section-EastWest-N0',M['objects'],V['Front-South'],(1,0));sheet('Section-NorthSouth-X0',M['objects'],V['Right-East'],(0,0));sheet('Section-Ramp-N8',M['objects'],V['Front-South'],(1,8))
 with (OUT/'Placement.csv').open('w',newline='') as f:
  w=csv.writer(f);w.writerow(['id','group','shape','center_x_n_h','size_x_n_h','radius','depth','collision'])
  for o in M['objects']:w.writerow([o['id'],o['group'],o['shape'],o.get('center','see profile'),o.get('size',''),o.get('radius',''),o.get('depth',''),o['collision']])
 links=''.join(f'<li><a href="{p.name}">{p.stem}</a></li>' for p in sorted(OUT.glob('*.svg')))
 (OUT/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Sky Gardens measured package</title><style>body{background:#102030;color:#ddd;font:18px sans-serif;margin:40px}a{color:#7de}img{width:100%}</style><h1>Sky Gardens / measured construction v1</h1><p>Design proposal, not production-ready art or validated gameplay. Scene.json is the geometric authority. Foliage and waterfalls are placement envelopes. Summit dimensions require approval.</p><ul>'+links+'</ul><p><a href="Scene.json">Full manifest</a> / <a href="Placement.csv">Placements</a> / <a href="SkyGardens-Construction.blend">Blender construction master</a></p><img src="Top.svg">',encoding='utf-8')
 print('Generated',len(list(OUT.glob('*.svg'))),'sheets')
if __name__=='__main__':main()
