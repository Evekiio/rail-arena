"""Validate and assemble the measured/concept Foundry reference handoff."""
import hashlib
import html
import json
from pathlib import Path
from xml.sax.saxutils import escape

from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
ARENA=ROOT/'ArtSource/Original/Foundry-Arena-v2'
PACK=ARENA/'References-v1'
data=json.loads((ARENA/'Map.json').read_text())
metadata=json.loads((PACK/'Measured-Views.json').read_text())
map_hash=hashlib.sha256((ARENA/'Map.json').read_bytes()).hexdigest()
assert metadata['mapSha256']==map_hash,'Map changed after rendering; regenerate the master/views.'
records={r['id']:r for r in metadata['geometry']}
assert len(records)==len(metadata['geometry'])==75
for obj in data['surfaces']+data['cover']:
    actual=records[obj['id']]
    for i in range(3):
        assert abs(actual['min'][i]-(obj['center'][i]-obj['size'][i]/2))<.0001,obj['id']
        assert abs(actual['max'][i]-(obj['center'][i]+obj['size'][i]/2))<.0001,obj['id']
assert len(metadata['views'])==22
for view in metadata['views']:assert (PACK/'Measured'/view['file']).is_file()
images=[png_info(p) for p in sorted(PACK.rglob('*.png'))]
master=PACK/'Foundry-v2-Reference-Master.blend'
assert master.is_file() and master.stat().st_size>100000

# Exact six-direction envelope studies complement the qualitative art cards.
kit=json.loads((ARENA/'Environment-Kit.json').read_text())
cover=[item for item in kit if item['id'].startswith('COVER-')]
svg=['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 2300">',
     '<rect width="1440" height="2300" fill="#121a22"/>',
     '<style>text{font:16px Arial,sans-serif;fill:#efe9d9}</style>',
     '<text x="30" y="35" style="font-size:27px">FOUNDRY / EXACT COVER ENVELOPES</text>',
     '<text x="30" y="65">Six orthographic mass views per size variant. Filled solid envelopes, not decorative details. All units metres.</text>']
for row,item in enumerate(cover):
    w,d,h=item['dimensions'];y=100+row*240
    svg.append(f'<text x="30" y="{y}">{item["id"]} · W {w} × D {d} × H {h}m · {item["instances"]} placements</text>')
    scale=min(130/max(w,d,h),40)
    for col,(label,a,b) in enumerate([('FRONT',w,h),('BACK',w,h),('LEFT',d,h),('RIGHT',d,h),('TOP',w,d),('BOTTOM',w,d)]):
        x=35+col*230
        svg.append(f'<rect x="{x}" y="{y+20}" width="{a*scale}" height="{b*scale}" fill="#29424d" stroke="#c6d6cc" stroke-width="2"/>')
        svg.append(f'<text x="{x}" y="{y+175}">{label} · {a} × {b}</text>')
svg.append('<text x="30" y="2280">Map.json controls placement and size. Do not add sightline holes or protrusions outside the envelope.</text></svg>')
(PACK/'Measured/Cover-Envelopes.svg').write_text('\n'.join(svg),encoding='utf-8')

superseded={'02-Cover-Kit.png'} if (PACK/'Concepts/02-Cover-Kit-v2.png').exists() else set()
concepts=[p for p in sorted((PACK/'Concepts').glob('*.png')) if p.name not in superseded]
assert len(concepts)==8,'All eight selected concept sheets must be present.'
gallery=['<!doctype html><meta charset="utf-8"><title>Foundry full environment references</title>',
         '<style>body{background:#121a22;color:#efe9d9;font:17px system-ui;max-width:1500px;margin:30px auto;padding:20px}a{color:#48dae8}img{width:100%;height:auto}section{margin:45px 0}h1{font-size:42px}p{max-width:1000px}</style>',
         '<h1>Rail Arena / Foundry full references</h1>',
         '<p>Same-master measured references and separate visual concept proposals. Creator approval and live controller/balance validation remain open.</p>',
         '<p><a href="README.md">Handoff and review notes</a> · <a href="../../../../docs/ARENA-DESIGN.md">Arena specification</a> · <a href="../Map.json">Measured placements</a></p>',
         '<h2>Visual direction</h2>']
for p in concepts:
    ref=p.relative_to(PACK).as_posix()
    gallery.append(f'<section><h3>{html.escape(p.stem)}</h3><a href="{ref}"><img loading="lazy" src="{ref}" alt="{html.escape(p.stem)}"></a></section>')
gallery.append('<h2>Measured geometry / same Blender master</h2>')
for view in metadata['views']:
    ref='Measured/'+view['file']
    gallery.append(f'<section><h3>{html.escape(Path(ref).stem)}</h3><a href="{ref}"><img loading="lazy" src="{ref}" alt="{html.escape(view["file"])}"></a></section>')
gallery.append('<section><h2>Exact cover mass views</h2><img src="Measured/Cover-Envelopes.svg" alt="Cover mass envelopes"></section>')
(PACK/'index.html').write_text('\n'.join(gallery),encoding='utf-8')
report=dict(version=1,mapSha256=map_hash,masterSha256=hashlib.sha256(master.read_bytes()).hexdigest(),
            measuredViews=len(metadata['views']),selectedConceptSheets=[p.name for p in concepts],
            logicalGeometryRecords=len(records),sourceBoxChecks=46,images=images,
            checks='PNG signatures/chunk CRC/decompression/hashes; source bounds; Map hash; required views',
            status='review candidates; not approved final art',runtimeChanged=False,
            controllerTested=False,competitiveBalanceValidated=False,
            limitations=['Concept views may differ in small detail; Map.json and measured envelopes govern.',
                         'Cover sources are retained; exact union removes internal render seams.',
                         'Illustrated arena context and sky views are not extra playable geometry or a seamless skybox.'])
(PACK/'manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(measuredViews=22,selectedConcepts=len(concepts),verifiedPNGs=len(images),logicalObjects=len(records))))
