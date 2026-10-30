import os,json,html
from pathlib import Path
os.environ['RAIL_BLUEPRINT_PACKAGE']='SkyGardens-Blueprint-v2'
from build_sky_blueprints import OUT,M,V,main,sheet,mesh
main()
objects=M['objects']
families={
 'StoneKit':[o for o in objects if o['id'].startswith('Wing-LowCover-1_-1')],
 'SummitCanopy':[o for o in objects if o['id'].startswith(('SUMMIT','CAPITAL','CANOPY'))],
 'Arch':[o for o in objects if o['id'].startswith(('UNDERPASS-ARCH','Underpass-Pier','Bridge'))],
 'Tree':[o for o in objects if o['id'].startswith('TREE-1-')],
 'Garden':[o for o in objects if o['id'].startswith(('GARDEN-1-','PLANTER-1-','GRASS-1','SHRUB-1'))],
 'Ivy':[o for o in objects if o['id'].startswith('IVY-1-')],
 'Waterfall':[o for o in objects if o['id'].startswith(('WATERFALL-1-','WATER-LIP-1'))],
 'PortalKit':[o for o in objects if o['id'].startswith('PA') and o['group']!='portal_envelopes']}
register=[]
for family,parts in families.items():
 assert parts,family
 vertices=[p for o in parts for p in mesh(o)[0]];lo=[min(p[k] for p in vertices) for k in range(3)];hi=[max(p[k] for p in vertices) for k in range(3)]
 row=dict(family=family,ids=[o['id'] for o in parts],minimum=lo,maximum=hi,size=[hi[k]-lo[k] for k in range(3)],note='Representative instance in exact canonical world coordinates')
 register.append(row)
 for view,basis in V.items():
  name='Detail-'+family+'-'+view;sheet(name,parts,basis)
  p=OUT/(name+'.svg');s=p.read_text();s=s.replace('Base 56 x 40 m / summit diameter 14 m, top +22 m, underside +21.4 m / column top +27 m / cloud datum -24 m',family+' / exact world-coordinate instance / size X,N,H: '+', '.join(f'{v:.3f}m' for v in row['size']))
  p.write_text(s,encoding='utf-8')
(OUT/'AssetRegister.json').write_text(json.dumps(register,indent=2))
links=''.join(f'<li><a href="{p.name}">{html.escape(p.stem)}</a></li>' for p in sorted(OUT.glob('*.svg')))
(OUT/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Sky Gardens detail blueprint v2</title><style>body{background:#102030;color:#ddd;font:18px sans-serif;margin:40px}a{color:#8dd}img{max-width:100%}</style><h1>Sky Gardens / detail construction v2</h1><p>Measured asset candidates. Same base layout as v1, with deterministic stone dressing, broken canopy, arches, vegetation and waterfall rest surfaces. Not a playable or production-approved map.</p><p><a href="Scene.json">Scene manifest</a> / <a href="AssetRegister.json">Family dimensions</a> / <a href="SkyGardens-Construction.blend">Editable Blender master</a> / <a href="BUILD-INSTRUCTIONS.txt">Build notes</a></p><img src="Detailed-Overview.png"><h2>Orthographic and detail sheets</h2><ul>'+links+'</ul>',encoding='utf-8')
print('Detailed measured sheets:',len(list(OUT.glob('*.svg'))))
