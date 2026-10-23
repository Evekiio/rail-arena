"""Assemble the rifle modeling handoff from one calibrated reference study."""
import base64
import argparse
import csv
import hashlib
import html
import json
import math
from pathlib import Path
from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--folder',default='ArtSource/Original/RailRifle-Modeling-References-v2')
args=parser.parse_args()
OUT=ROOT/args.folder
data=json.loads((OUT/'Calibration.json').read_text())
carbine=data.get('designName')=='Precision Carbine'
asset=data['assets'][0]
titles={'RF-01':'Reinforced stock shell / trim / socket','RF-02':'Angled shoulder pad','RF-03':'Graphite cheek rest',
        'RF-04':'Receiver / side panels','RF-05':'Enclosed rail shroud / cosmetic bore','RF-06':'Muzzle collar / bore lip',
        'RF-07':'Optic / risers / clamps','RF-08':'Upper rail / teeth','RF-09':'Pistol grip / inserts',
        'RF-10':'Trigger / closed guard','RF-11':'Fixed capacitor cassette','RF-12':'Angled hand stop / contact strip' if carbine else 'Foregrip / lower accessory base'}
groups=[]
for identifier,title in titles.items():
    parts=[p for p in asset['parts'] if p['assembly']==identifier];assert parts,identifier
    lo=[min(p['minimum'][i] for p in parts) for i in range(3)]
    hi=[max(p['maximum'][i] for p in parts) for i in range(3)]
    groups.append(dict(id=identifier,title=title,parts=len(parts),minimum=lo,maximum=hi,dimensions=[b-a for a,b in zip(lo,hi)]))
(OUT/'Assembly.json').write_text(json.dumps(groups,indent=2)+'\n')
with (OUT/'Assembly-Dimensions.csv').open('w',newline='',encoding='utf-8') as f:
    writer=csv.writer(f);writer.writerow(['id','assembly','sizeX_mm','sizeY_mm','sizeZ_mm','parts'])
    for g in groups:writer.writerow([g['id'],g['title']]+[f'{v*1000:.3f}' for v in g['dimensions']]+[g['parts']])

def text(x,y,label,size=30,color='#20353f'):
    return f'<text x="{x}" y="{y}" font-family="Arial,sans-serif" font-size="{size}" fill="{color}">{html.escape(str(label))}</text>'

def image(file,x,y,w,h):
    content=base64.b64encode((OUT/file).read_bytes()).decode()
    return f'<image href="data:image/png;base64,{content}" x="{x}" y="{y}" width="{w}" height="{h}" preserveAspectRatio="xMidYMid meet"/>'

def begin(title,subtitle):
    return ['<svg xmlns="http://www.w3.org/2000/svg" width="3900" height="3200" viewBox="0 0 3900 3200">',
            '<rect width="100%" height="100%" fill="#edf2ef"/>',text(75,100,'RAIL ARENA / '+title,58),
            text(75,165,subtitle,30),text(75,215,'PROPOSED CONSTRUCTION / CREATOR APPROVAL PENDING / source units: metres',27)]

def save(name,body):
    body.extend([text(75,3150,'Reference study only. Source meshes and all views describe one version; no live Unity asset replaced.',25),'</svg>'])
    (OUT/(name+'.svg')).write_text('\n'.join(body),encoding='utf-8')

board=begin('STOCK CONSTRUCTION','Broad cheek rest, thick shoulder pad, one compact reinforced opening, small recessed sling socket.')
for i,(file,label) in enumerate([('Stock-Side','Stock side'),('Stock-Top','Stock top'),('Stock-Rear','Stock rear')]):
    x=75+i*1280;board.extend([text(x,300,label,36),image('Details/RailRifle-'+file+'.png',x,340,1180,1180)])
board.append(text(75,1640,'Assembly envelopes (X length / Y width / Z height), in millimetres:',36))
for i,g in enumerate(groups[:3]):
    dims=' × '.join(f'{v*1000:.1f}' for v in g['dimensions'])
    board.append(text(75,1720+i*65,f'{g["id"]}  {g["title"]}: {dims} mm',31))
board.extend([text(75,1990,'REFERENCE SHAPE INPUT',34),image('Sources/User-Stock-Reference.png',75,2030,1050,950),
              text(1250,2140,'Orientation is reversed from the supplied photo:',32),
              text(1250,2200,'shoulder pad at the left rear; receiver attachment at right.',30),
              text(1250,2310,'Both faces share the same construction. Graphite contact',30),
              text(1250,2370,'surfaces contrast with ivory structural armor. The lower',30),
              text(1250,2430,'brace surrounds a single opening; the small socket is a',30),
              text(1250,2490,'shallow cosmetic recess, not another stock cut-through.',30),
              text(1250,2610,'New 300mm rear region within the 1.0m carbine target.' if carbine else 'Rear extent and receiver interface retain the benchmark',30),
              text(1250,2670,'Contact fit and pad angle require creator review.' if carbine else 'envelope. Contact fit and pad angle require creator review.',30)])
save('02-Stock-Construction',board)

board=begin('AXIAL / CONTACT DETAILS','Enlarged end views and grip/muzzle details. Each clean detail has its own recorded frame size.')
for i,(name,label) in enumerate([('Front-Detail','Front / muzzle end'),('Back-Detail','Back / shoulder end'),
                               ('Grip-Contacts','Grip / trigger clearance'),('Muzzle-Construction','Cosmetic muzzle assembly')]):
    x=75+(i%2)*1930;y=310+(i//2)*1250
    board.extend([text(x,y,label,35),image('Details/RailRifle-'+name+'.png',x,y+35,1800,1160)])
save('03-Axial-Contacts',board)

board=begin('EXPLODED ART ASSEMBLY','Visual separation only. Offsets are illustration offsets, not manufacturing or mating clearances.')
board.append(image('Details/RailRifle-Exploded.png',75,270,3750,1800))
# Project group callouts with the same orthographic camera as the exploded render.
normal=[1.3,-2.8,1.4]
length=math.sqrt(sum(v*v for v in normal));normal=[v/length for v in normal]
right=[-normal[1],normal[0],0];length=math.sqrt(sum(v*v for v in right));right=[v/length for v in right]
up=[normal[1]*right[2]-normal[2]*right[1],normal[2]*right[0]-normal[0]*right[2],normal[0]*right[1]-normal[1]*right[0]]
label_offsets=[(-30,110),(-100,-20),(-20,-75),(-30,-95),(35,-70),(115,0),(50,-90),
               (-50,-85),(-55,95),(-135,80),(20,115),(115,80)]
exploded_frame=next(d['orthographicFrameMetres'] for d in data['details'] if d['name']=='Exploded')
for g,(dx,dy) in zip(groups,label_offsets):
    position=[(a+b)/2+c-d for a,b,c,d in zip(g['minimum'],g['maximum'],data['explodedOffsets'][g['id']],asset['center'])]
    x=1950+sum(a*b for a,b in zip(position,right))*1800/exploded_frame
    y=1170-sum(a*b for a,b in zip(position,up))*1800/exploded_frame
    lx,ly=x+dx,y+dy
    board.extend([f'<path d="M{x},{y} L{lx},{ly}" fill="none" stroke="#507583" stroke-width="2"/>',
                  f'<rect x="{lx-49}" y="{ly-22}" width="98" height="38" rx="6" fill="#f5f7f2" stroke="#507583"/>',
                  text(lx-38,ly+4,g['id'],25)])
for i,g in enumerate(groups):
    x=75+(i%2)*1930;y=2160+(i//2)*115
    dims=' / '.join(f'{v*1000:.1f}' for v in g['dimensions'])
    board.append(text(x,y,f'{g["id"]}  {g["title"]}',30))
    board.append(text(x,y+42,f'X/Y/Z: {dims} mm · {g["parts"]} editable source parts',24))
save('04-Exploded-Assembly',board)

board=begin('SECTION / ANCHORS','Centre section at Y=0; negative Y half removed. The section is uncapped to expose existing modeled surfaces.')
board.append(image('Details/RailRifle-Centre-Section.png',75,250,3750,1700))
board.append(text(75,2030,'MODELING ANCHORS / SOURCE X-Y-Z IN MM',38))
for i,(name,value) in enumerate(data['anchors'].items()):
    board.append(text(75,2110+i*60,name+'  '+', '.join(f'{v*1000:.2f}' for v in value),30))
for i,line in enumerate(['RightGrip / LeftSupport are pose targets, not calibrated fingers.',
    'AimLine is the optical centre; Muzzle is the existing game-art emission origin.',
    'StockContact is revised to the angled pad; fit to character shoulder is pending.',
    'The cutaway illustrates the cosmetic mesh, not a real firing mechanism.',
    'Dimensions and six orthographic views govern; do not trace the exploded offsets.']):
    board.append(text(75,2580+i*65,line,30))
save('05-Section-Anchors',board)

zones=[('M01','Ivory ceramic',(.83,.81,.71),'0.00 / 0.42–0.55','Large armor shells; single-facet bevels'),
       ('M02','Graphite structure',(.025,.04,.05),'0.25 / 0.40–0.55','Core, cheek rest, reinforcement'),
       ('M03','Deep teal inset',(.065,.17,.19),'0.15 / 0.40–0.55','Recessed panels; lower stock accents'),
       ('M04','Cool metal',(.28,.32,.32),'0.65 / 0.32–0.45','Socket rim, clamps and small fittings'),
       ('M05','Graphite elastomer',(.018,.024,.026),'0.00 / 0.65–0.80','Shoulder pad and grip contact inserts'),
       ('M06','Restrained cyan',(.04,.65,.70),'0.10 / 0.30','Inlays, optic lens and rail liner')]
board=begin('MATERIAL / MOTION GUIDE','Art material zones; proposed production surface responses. Current reference study uses palette + cyan materials.')
for i,(code,label,rgb,response,purpose) in enumerate(zones):
    y=310+i*310;hexcolor='#'+''.join(f'{round(c**(1/2.2)*255):02x}' for c in rgb)
    board.append(f'<rect x="75" y="{y}" width="300" height="210" rx="15" fill="{hexcolor}" stroke="#70878b" stroke-width="3"/>')
    board.extend([text(430,y+52,f'{code} / {label}',38),text(430,y+112,'Proposed metallic / roughness: '+response,30),text(430,y+172,purpose,30)])
for i,line in enumerate(['MODELING / PRESENTATION STATES',
    'Idle: assembled, no charging geometry or anticipatory opening.',
    'Fire: whole-rifle recoil and brief muzzle effect; separate trigger mesh retained.',
    'Recovery: return to rest within the 0.6s firing cycle. No reload animation.',
    'Fixed capacitor cassette is an art assembly, not a magazine/reload mechanic.',
    'Zoom: stable geometry; no scope extension. Character hands/contacts require fitting.',
    'Glow and flash must remain readable with reduced-flash settings.']):
    board.append(text(75,2290+i*90,line,36 if i==0 else 29))
save('06-Materials-Motion',board)

details={d['name']:d for d in data['details']}
images=[png_info(OUT/v['file']) for v in asset['views']+data['details']]
assert len(images)==16 and len(groups)==12
page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / rifle modeling references v2</title>',
      '<style>body{font:18px system-ui;background:#18232b;color:#f2eee3;max-width:1800px;margin:30px auto;padding:20px}a{color:#6bdce5}img{width:100%;height:auto;background:#dce5e2}p{line-height:1.6;max-width:1200px}section{margin:60px 0}.compare{display:grid;grid-template-columns:1fr 1fr;gap:20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:20px}h1{font-size:42px}figure{margin:0}figcaption{padding:12px}select{font:inherit;background:#22323e;color:white;padding:10px;margin:15px 0}</style>',
      '<h1>Rail Arena / rifle modeling references v2</h1><p>Revised stock direction, one shared reference study and six coordinated modeling views. This is a proposed construction package for approval. The production weapon and Unity prefab remain unchanged.</p>',
      '<p><a href="README.md">Modeling handoff</a> · <a href="Assembly-Dimensions.csv">Assembly dimensions</a> · <a href="Blender-Reference-Rig.blend">Calibrated image rig</a> · <a href="Rifle-Reference-Study.blend">Reference geometry</a></p>',
      '<h2>Source direction / revised study</h2><div class="compare"><figure><img src="Sources/User-Stock-Reference.png" alt="User supplied stock shape"><figcaption>Supplied shape reference; attachment orientation reversed in the rifle.</figcaption></figure><figure><img src="Details/RailRifle-Stock-Side.png" alt="Revised stock side"><figcaption>Revised stock side. Its top and rear views come from the same geometry.</figcaption></figure><figure><img src="Sources/User-Rifle-Reference.png" alt="Original supplied rifle look"><figcaption>Original overall rifle look. No physical scale is given in this illustration.</figcaption></figure><figure><img src="Clean/RailRifle-Left.png" alt="Full rifle revised side"><figcaption>Full revised study, including the new stock direction.</figcaption></figure></div>']
sheetnames=['RailRifle-Six-View','02-Stock-Construction','03-Axial-Contacts','04-Exploded-Assembly','05-Section-Anchors','06-Materials-Motion']
if carbine:
    page[0]=page[0].replace('rifle modeling references v2','precision carbine references v3')
    page[2]='<h1>Rail Arena / precision carbine references v3</h1><p>New 1.0m precision carbine proposal. References and static Unity previews are for design approval; final production and replacement are gated on that approval.</p>'
    page[3]='<p><a href="README.md">Design and approval gate</a> · <a href="Assembly-Dimensions.csv">Assembly dimensions</a> · <a href="Blender-Reference-Rig.blend">Calibrated image rig</a> · <a href="Carbine-Reference-Study.blend">Reference study</a></p>'
    page.append('<h2>Five additional references / originals preserved</h2><div class="grid">')
    for i,role in enumerate(['Compact optic and carbine balance','Receiver and stock vocabulary','Precision silhouette study, no bipod retained','Low-profile angled support cue','Long vented fore-end and precision-carbine silhouette'],1):
        page.append(f'<figure><a href="Sources/Additional-Reference-{i}.png"><img src="Sources/Additional-Reference-{i}.png" alt="Additional reference {i}"></a><figcaption>{i}. {role}. Shape inspiration only; watermark preserved.</figcaption></figure>')
    page.append('</div><h2>Actual Unity framing / preliminary character poses</h2><p>Normal: 100° vertical FOV. Zoom: perspective-correct 1.5× magnification. These are static review captures, not gameplay acceptance or final finger fitting.</p>')
    for i,label in enumerate(['Player','Bot 01','Bot 02','Bot 03']):
        page.append(f'<h3>{label}</h3><div class="grid">')
        for name,description in [(f'FP-{i}-Normal','First person / 100°'),(f'FP-{i}-Zoom','First person / 1.5× zoom'),(f'TP-{i}-ThreeQuarter','Third person / three-quarter'),(f'TP-{i}-Side','Third person / side')]:
            page.append(f'<figure><a href="Unity/{name}.png"><img loading="lazy" src="Unity/{name}.png" alt="{label} {description}"></a><figcaption>{description}</figcaption></figure>')
        page.append('</div>')
for name in sheetnames:
    page.append(f'<section><h2>{html.escape(name.replace("-"," "))}</h2><p><a href="{name}.png">Full PNG</a> · <a href="{name}.svg">Editable SVG</a></p><a href="{name}.png"><img loading="lazy" src="{name}.png" alt="{html.escape(name)}"></a></section>')
page.append('<h2>Clean calibrated views</h2><p>Use these six unannotated images with the provided image rig. The larger detail views have different frame sizes; consult Calibration.json.</p><div class="grid">')
for view in asset['views']+data['details']:
    page.append(f'<figure><a href="{view["file"]}"><img loading="lazy" src="{view["file"]}" alt="{view["name"]}"></a><figcaption>{view["name"]} / frame {view["orthographicFrameMetres"]:.3f}m</figcaption></figure>')
page.append('</div></html>');(OUT/'index.html').write_text('\n'.join(page),encoding='utf-8')
manifest=dict(status=data['status'],images=images,sourceMasterSha256=data['sourceMasterSha256'],
    sourceBenchmarkSha256=data.get('sourceBenchmarkSha256'),runtimeChanged=False,assemblyGroups=12,
    cleanViews=6,detailViews=10,requiredSheets=sheetnames,
    approvalOpen=['new stock silhouette and hidden surfaces','all six construction views','grip/shoulder fitting','material targets'],
    precision='Raster has finite resolution; dimensions come from reference study, not recovered photo measurements')
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('16 renders, 12 assembly groups, 6 board sources and review page packaged')
