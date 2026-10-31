"""Compose metre-calibrated nature sheets and audited installation bindings."""
from pathlib import Path
import hashlib,json,html
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));source=json.loads((folder/'nature-source.json').read_text(encoding='utf-8'));views=json.loads((folder/'measured-views.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']==views['masterSha256']
assert hashlib.sha256((folder/'nature-source.json').read_bytes()).hexdigest()==spec['sourceSha256']
bindings=[]
for part in source['parts']:
    family=part['family'];site=int(part['sourceName'].split('-')[1])
    module='Crown'+'ABC'[(site-1)%3] if family=='Crown' else family+('A' if site%2 else 'B') if family in ('Grass','Shrub','VineLeaf') else family
    bindings.append({'sourceName':part['sourceName'],'module':module,'center':part['frameCenter'],'size':part['frameSize'],'wind':part['wind'],'originalBoundsCenter':part['center'],'originalBoundsSize':part['size']})
spec['bindings']=bindings;spec['bindingAuthority']='Read-only installed nature-source.json; paired ivy stems/leaves share their original union frame. Other modules map into their original individual bounds. No new collision or navigation.'
(folder/'production.json').write_bytes((json.dumps(spec,indent=2)+'\n').encode('utf-8'))
font_path=Path('C:/Windows/Fonts/segoeui.ttf');font=lambda size:ImageFont.truetype(str(font_path),size)
views_order=['front','rear','left','right','top','bottom'];cards=[]
for module in spec['modules']:
    family=module['family'];directory=folder/'References'/family
    sheet=Image.new('RGB',(1920,2240),'#102029');draw=ImageDraw.Draw(sheet)
    draw.text((40,24),'RAIL ARENA / SKY GARDENS / '+family.upper(),font=font(36),fill='#f0eedc')
    draw.text((40,78),'Canonical unit frame: 1 x 1 x 1 m / Blender Z up / pivot at frame centre',font=font(25),fill='#b1d5ce')
    for index,name in enumerate(views_order):
        x=40+(index%3)*628;y=145+(index//3)*660
        draw.text((x,y-32),name.upper(),font=font(24),fill='#f0eedc')
        with Image.open(directory/(name+'.png')) as image:sheet.paste(image.convert('RGB').resize((576,576)),(x,y))
        draw.line((x+20,y+595,x+404,y+595),fill='#18b7c0',width=4)
        draw.text((x+20,y+603),'1 m / identical scale in all six views',font=font(19),fill='#b1d5ce')
    y=1485
    dims=module['geometryDimensionsMetresBlenderXYZ'];whd=(dims[0],dims[2],dims[1])
    notes=[f'Near geometry W x H x D: {whd[0]:.5f} x {whd[1]:.5f} x {whd[2]:.5f} m within the unit frame.',
        'Installed dimensions come from the audited frames below; a unit module is scaled at each site.',
        'All LODs share one frame. Paired vine stems/leaves also share one installation frame.',
        'One original painted palette material / opaque folded geometry / no added collision.',
        'No rig, clips, grip sockets or root motion. Existing decorative wind is retained by role.',
        'Triangle counts LOD0 / LOD1 / LOD2: '+' / '.join(str(x['triangles']) for x in spec['lods'] if x['family']==family),
        'Installation frames / original source name: W x H x D metres']
    for line in notes:draw.text((40,y),line,font=font(23),fill='#e7e8d8');y+=39
    for binding in [x for x in bindings if x['module']==family]:
        size=binding['size'];draw.text((55,y),f"{binding['sourceName']} : {size['x']:.5f} x {size['y']:.5f} x {size['z']:.5f}",font=font(23),fill='#b1d5ce');y+=36
    # Same 384 pixels/metre as every view; this is a scale diagram, not a character model.
    cx=1745;bottom=2190;height=round(1.8*384);width=round(.72*384)
    draw.rounded_rectangle((cx-width//2,bottom-height,cx+width//2,bottom),radius=width//2,outline='#18b7c0',width=4)
    draw.text((1490,2200),'1.8 m / 0.72 m controller',font=font(21),fill='#b1d5ce')
    draw.text((40,2090),'SG-GARDEN-NATURE-v2 / '+spec['masterSha256'][:32],font=font(23),fill='#e7e8d8')
    draw.text((40,2130),'Geometry-derived references; installed/native artistic acceptance remains pending.',font=font(22),fill='#b1d5ce')
    path=folder/(family+'-Measured-Sheet.png');sheet.save(path)
    cards.append(f'<h2>{family}</h2><a href="{path.name}"><img src="{path.name}"></a><p><a href="References/{family}/perspective.png">Unit-frame perspective</a> / <a href="References/{family}/detail.png">0.6 m detail crop</a></p>')
page='<!doctype html><meta charset="utf-8"><title>Rail Arena measured garden nature</title><style>body{background:#102029;color:#eef0df;font:18px system-ui;max-width:1500px;margin:32px auto}a{color:#6ce0d0}img{max-width:100%;height:auto}</style><h1>Garden nature / source production</h1><p>Eleven original metre-scaled unit-frame modules and 33 reducing LOD exports. These are geometry-derived Blender references, not installed-game acceptance. Source frames bind all 24 existing noncolliding planting renderings; paired ivy uses its original combined frame. Canonical unit proportions are scaled to the measured installed dimensions listed on each sheet.</p><p><a href="production.json">Production manifest and installation bindings</a> / <a href="nature-source.json">Read-only installed authority</a> / <a href="measured-views.json">Measured view evidence</a> / <a href="Garden-Nature-v2.blend">Editable Blender master</a> / <a href="Garden-Painted-Palette.png">Original painted palette</a></p>'+''.join(cards)
if (folder/'site-views.json').exists():
    site_views=json.loads((folder/'site-views.json').read_text(encoding='utf-8'));assert site_views['masterSha256']==spec['masterSha256']
    page+='<h2>Audited installed-scale diagrams / not Unity captures</h2><p>Source geometry uses actual installation frame dimensions. Planter context and the 1.8 m comparator are diagrams.</p>'
    page+='<img src="InstalledScale/tree-close.png">'
    page+=''.join('<img src="InstalledScale/site-'+str(site)+'.png">' for site in range(1,5))
(folder/'index.html').write_bytes(page.encode('utf-8'))
print('GARDEN_NATURE_SHEETS_PACKAGED',len(cards),'sheets;',len(bindings),'audited installation bindings')
