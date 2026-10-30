"""Render a labelled vector plan directly from the shared greybox bounds."""
import html
import json
from pathlib import Path

root=Path(__file__).resolve().parents[1]
folder=root/'ArtSource/Original/Design-v1/Greybox'
layout=json.loads((folder/'Layout.json').read_text())
scale=15
def position(x,y):return 520+x*scale,420-y*scale
colors={'Lower':'#536970','Wing':'#96ada8','Upper':'#dfd6b6','Ramp':'#6a9da8',
        'Cover':'#172633','Pad':'#48dae8','Health':'#ffb027','Spawn':'#d88187','Portal':'#aa80e4'}
svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1120 890">',
     '<rect width="1120" height="890" fill="#121a22"/>',
     '<style>text{fill:#efe9d9;font-family:Arial,sans-serif;font-size:15px}.small{font-size:12px}</style>',
     '<text x="40" y="38" style="font-size:26px">RAIL ARENA / SHARED GREYBOX PLAN</text>',
     '<text x="40" y="65">56 × 40 metres · Blender Z-up · review-only · not playtested</text>']
objects=sorted(layout['objects'],key=lambda o:o['center_blender'][2])
for o in objects:
    if not o['collision']:continue
    lo=o['bounds_min'];hi=o['bounds_max'];x,y=position(lo[0],hi[1])
    svg.append(f'<rect x="{x}" y="{y}" width="{(hi[0]-lo[0])*scale}" height="{(hi[1]-lo[1])*scale}" fill="{colors[o["material"]]}" stroke="#25343f" stroke-width="2"/>')
    if o['role']=='ramp':
        cx,cy=position(*o['center_blender'][:2])
        svg.append(f'<text x="{cx}" y="{cy}" text-anchor="middle">0 → 2 → 4m</text>')
markers=[]
for i,pad in enumerate(layout['pads']):
    markers.append((f'P{i+1}',pad['start'],'#48dae8'))
    markers.append((f'L{i+1}',pad['destination'],'#48dae8'))
for i,p in enumerate(layout['spawns']):markers.append((f'S{i+1}',p,'#d88187'))
for i,p in enumerate(layout['health']):markers.append((f'H{i+1}',p,'#ffb027'))
for i,p in enumerate(layout['portals']):markers.append(('A' if i==0 else 'B',p['center'],'#aa80e4'))
for label,p,color in markers:
    x,y=position(p[0],p[1]);svg.append(f'<circle cx="{x}" cy="{y}" r="13" fill="{color}" stroke="#121a22"/>')
    svg.append(f'<text x="{x}" y="{y+5}" text-anchor="middle" style="fill:#121a22;font-size:12px;font-weight:bold">{html.escape(label)}</text>')
svg.extend(['<text x="40" y="790">P: pad start · L: landing · H: health · S: spawn candidate · A/B: paired portals</text>',
            '<text x="40" y="818">Lower: 0m · Wing: +2m · Bridge: +4m · Four ramp assemblies have level midpoint turnouts.</text>',
            '<text x="40" y="846">Overlaid levels: use separate deck renders and the section to inspect occlusion and vertical connections.</text>',
            '</svg>'])
(folder/'Labelled-Plan.svg').write_text('\n'.join(svg),encoding='utf-8')
print('Rendered labelled plan from Layout.json')
