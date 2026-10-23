"""Create code-native engineering sheets around calibrated Blender renders."""
import base64
import argparse
import csv
import hashlib
import html
import json
from pathlib import Path
from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--folder',default='ArtSource/Original/ProductionBenchmarks-v1/Dimensional-References-v1')
parser.add_argument('--master',default='ArtSource/Original/ProductionBenchmarks-v1/Production-Benchmarks.blend')
args=parser.parse_args()
OUT=ROOT/args.folder
data=json.loads((OUT/'Calibration.json').read_text())
assert data['sourceMasterSha256']==hashlib.sha256((ROOT/args.master).read_bytes()).hexdigest()
W,H=3900,3200
header='''<defs><marker id="arrow" markerWidth="8" markerHeight="8" refX="4" refY="4" orient="auto-start-reverse"><path d="M8,0 L0,4 L8,8" fill="none" stroke="#244957"/></marker></defs>
<style>text{font-family:Arial,sans-serif;fill:#20353f}.title{font-size:58px;font-weight:bold}.sub{font-size:28px}.label{font-size:36px;font-weight:bold}.small{font-size:24px}.dim{stroke:#244957;stroke-width:2;fill:none}.grid{stroke:#cfdbdd;stroke-width:1}.axis{stroke:#98b3bb;stroke-width:2;stroke-dasharray:8 7}</style>'''
images=[];assets=[]
def text(x,y,value,kind='small',anchor='start'):
    return f'<text x="{x}" y="{y}" class="{kind}" text-anchor="{anchor}">{html.escape(str(value))}</text>'

for asset in data['assets']:
    name=asset['asset'];scale=asset['orthographicFrameMetres'];center=asset['center']
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',header,
         '<rect width="100%" height="100%" fill="#f2f5f3"/>',text(75,90,'RAIL ARENA / '+name+' / SIX-VIEW APPROVAL','title'),
         text(75,140,'PROPOSED CONSTRUCTION • Dimensions in mm • Orthographic • One shared master • Not creator approved','sub'),
         text(75,185,'Source axes: X length / muzzle-forward, Y width, Z up. Front is viewed from +X; back from -X.','sub')]
    dims=asset['dimensions']
    svg.append(text(75,230,f'Overall X {dims[0]*1000:.1f} × Y {dims[1]*1000:.1f} × Z {dims[2]*1000:.1f} mm. All six panels use the same scale.','sub'))
    for index,view in enumerate(asset['views']):
        p=OUT/view['file'];images.append(png_info(p))
        assert png_info(p)['width']==2048 and png_info(p)['height']==2048
        right=view['right'];up=view['up'];normal=view['normal']
        assert sum(x*x for x in normal)==1 and sum(a*b for a,b in zip(right,up))==0
        x=(index%3)*1300+100;y=(index//3)*1400+340;s=1040
        ppm=s/scale
        xr=next(i for i in range(3) if right[i]);yu=next(i for i in range(3) if up[i])
        width=dims[xr]*ppm;height=dims[yu]*ppm
        cx=x+s/2;cy=y+s/2
        left=cx-width/2;rightEdge=cx+width/2;top=cy-height/2;bottom=cy+height/2
        svg.extend([text(x,y-42,view['name'].upper(),'label'),f'<rect x="{x}" y="{y}" width="{s}" height="{s}" fill="#e4ebea"/>'])
        # Calibration grid is anchored to physical source coordinates, not to image corners.
        interval=.1 if name=='RailRifle' else .2
        for axis,vec in [('horizontal',right),('vertical',up)]:
            projected=sum(a*b for a,b in zip(center,vec))
            for k in range(-30,31):
                q=k*interval-projected
                v=(cx+q*ppm) if axis=='horizontal' else (cy-q*ppm)
                if axis=='horizontal' and x<=v<=x+s:svg.append(f'<path d="M{v},{y} v{s}" class="grid"/>')
                if axis=='vertical' and y<=v<=y+s:svg.append(f'<path d="M{x},{v} h{s}" class="grid"/>')
        encoded=base64.b64encode(p.read_bytes()).decode()
        svg.append(f'<image href="data:image/png;base64,{encoded}" x="{x}" y="{y}" width="{s}" height="{s}"/>')
        ox=cx-sum(a*b for a,b in zip(center,right))*ppm
        oy=cy+sum(a*b for a,b in zip(center,up))*ppm
        if x<=ox<=x+s:svg.append(f'<path d="M{ox},{y} v{s}" class="axis"/>')
        if y<=oy<=y+s:svg.append(f'<path d="M{x},{oy} h{s}" class="axis"/>')
        dy=y+s+47;dx=x+s+47
        svg.append(f'<path d="M{left},{bottom+8} V{dy+12} M{rightEdge},{bottom+8} V{dy+12} M{left},{dy} H{rightEdge}" class="dim"/>')
        for a in (left,rightEdge):svg.append(f'<path d="M{a-7},{dy-7} l14,14" class="dim"/>')
        svg.append(text(cx,dy+38,f'{dims[xr]*1000:.1f} mm / '+('XYZ'[xr]),'small','middle'))
        svg.append(f'<path d="M{rightEdge+8},{top} H{dx+12} M{rightEdge+8},{bottom} H{dx+12} M{dx},{top} V{bottom}" class="dim"/>')
        for a in (top,bottom):svg.append(f'<path d="M{dx-7},{a-7} l14,14" class="dim"/>')
        svg.append(f'<text transform="translate({dx+32},{cy}) rotate(-90)" text-anchor="middle" class="small">{dims[yu]*1000:.1f} mm / {"XYZ"[yu]}</text>')
        svg.append(text(x,y+s+132,view['note'],'small'))
        view['sheetImageRegion']=[x,y,s,s]
    svg.append(text(75,3150,f'Grid: {100 if name=="RailRifle" else 200} mm. Clean PNG full-frame size: {scale:.3f}m square; 2048px. Raster pitch {scale/2048*1000:.3f}mm/px. Do not trace the dimensioned sheet as a scale image.','small'))
    svg.append('</svg>')
    (OUT/(name+'-Six-View.svg')).write_text('\n'.join(svg),encoding='utf-8')
    with (OUT/(name+'-Parts.csv')).open('w',newline='',encoding='utf-8') as f:
        writer=csv.writer(f);writer.writerow(['part','minX_mm','minY_mm','minZ_mm','maxX_mm','maxY_mm','maxZ_mm','sizeX_mm','sizeY_mm','sizeZ_mm'])
        for part in asset['parts']:
            lo=part['minimum'];hi=part['maximum']
            writer.writerow([part['name']]+[f'{v*1000:.4f}' for v in lo+hi+[b-a for a,b in zip(lo,hi)]])
    assets.append(name)
body=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Rail Arena / dimensional approval</title>',
      '<style>body{background:#18232b;color:#f4eedf;font:18px system-ui;margin:30px auto;max-width:1800px;padding:20px}a{color:#65dfe2}img{width:100%;height:auto}p{max-width:1100px;line-height:1.6}section{margin:50px 0}.views{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:16px}.views img{background:#cfd8d9}</style>',
      '<h1>Rail Arena / dimensional approval references</h1><p>Six consistent views per asset, rendered from one geometry source. These are proposals for review, not approval of the existing benchmark. Rifle scale is proposed; cover dimensions follow the fixed environment envelope. <a href="README.md">Calibration and Blender setup</a></p>']
for asset in data['assets']:
    name=asset['asset'];body.append(f'<section><h2>{name}</h2><p><a href="{name}-Six-View.png">Full-resolution sheet PNG</a> · <a href="{name}-Six-View.svg">Editable SVG sheet</a> · <a href="{name}-Parts.csv">Part dimensions CSV</a></p><a href="{name}-Six-View.png"><img src="{name}-Six-View.png" alt="{name} six-view dimensioned approval sheet"></a><h3>Calibrated clean views for Blender</h3><div class="views">')
    for view in asset['views']:
        body.append(f'<figure><figcaption>{view["name"]}</figcaption><a href="{view["file"]}"><img loading="lazy" src="{view["file"]}" alt="{name} {view["name"]} orthographic reference"></a></figure>')
    body.append('</div></section>')
body.append('</html>');(OUT/'index.html').write_text('\n'.join(body),encoding='utf-8')
(OUT/'manifest.json').write_text(json.dumps(dict(status=data['status'],sourceMasterSha256=data['sourceMasterSha256'],cleanImages=images,assets=assets,
    precision='Dimensions from source mesh bounds; raster views have finite pixel pitch; no claim of perfect match to unscaled source concept'),indent=2)+'\n')
print(f'Packaged {len(images)} calibrated images and {len(assets)} six-view SVG engineering sheets')
