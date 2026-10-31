"""Compose dimensioned bridge or summit sheets from canonical Blender renders."""
import hashlib
import html
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
summit = '--summit' in sys.argv
folder = root/('ArtSource/Original/ArtPass-v1/Environment/SummitStone' if summit else 'ArtSource/Original/ArtPass-v1/Environment/BridgeStone')
title = 'SUMMIT' if summit else 'BRIDGE'
spec = json.loads((folder/'production.json').read_text(encoding='utf-8'))
views = json.loads((folder/'measured-views.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest() == spec['masterSha256']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 28)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 21)
cards = []
for module in views['modules']:
    family = module['family']
    sheet = Image.new('RGB', (1800, 2120), '#101d22')
    draw = ImageDraw.Draw(sheet)
    draw.text((36,24), 'RAIL ARENA / SKY GARDENS / '+title+' '+family.upper(), font=font, fill='#efead7')
    dimensions = module['sizeUnityMetres']
    draw.text((36,67), ('Metres / Unity W %.3f × H %.3f × D %.3f / pivot: ' % tuple(dimensions))+module['pivot'], font=small, fill='#b5d3ce')
    for index, name in enumerate(module['views']):
        x, y = (index%3)*600, 125+(index//3)*650
        image = Image.open(folder/'References'/family/(name+'.png')).convert('RGB').resize((580,580))
        sheet.paste(image,(x+10,y+38))
        draw.text((x+22,y), name.upper(), font=small, fill='#efead7')
        ruler = round(580/module['orthographicSpanMetres'])
        draw.line((x+30,y+600,x+30+ruler,y+600), fill='#32bec5', width=4)
        draw.text((x+30,y+618),'1 m / shared scale within this sheet',font=small,fill='#b5d3ce')
    scale = 580/module['orthographicSpanMetres']
    x,y = 130,1980
    actor_height,actor_width = 1.8*scale,.72*scale
    draw.rounded_rectangle((x,y-actor_height,x+actor_width,y),radius=min(actor_width/2,actor_height/2),outline='#32bec5',width=3)
    draw.line((x-20,y,x+actor_width+20,y),fill='#b5d3ce',width=2)
    draw.text((400,1620),'Scale comparator: 1.80 m standing controller / 0.72 m width',font=small,fill='#efead7')
    draw.text((400,1660),'Same pixel/metre ratio as all six views above. Diagram only; not a character model.',font=small,fill='#b5d3ce')
    draw.text((36,2027),spec['id']+' / Blender source '+spec['masterSha256'][:24],font=small,fill='#efead7')
    draw.text((36,2060),'Geometry-derived views; concept images do not override dimensions. Collision remains in Unity.',font=small,fill='#b5d3ce')
    sheet.save(folder/(family+'-Measured-Sheet.png'))
    cards.append('<article><h2>'+family+'</h2><img src="'+family+'-Measured-Sheet.png"><img src="References/'+family+'/perspective.png"></article>')
source_name = 'summit-source.json' if summit else 'bridge-source.json'
page = '<!doctype html><meta charset="utf-8"><title>Rail Arena / '+title+' Stone</title><style>body{background:#101d22;color:#efead7;font:18px Arial;margin:3vw}img{max-width:100%}article{margin-top:48px}p{max-width:900px;line-height:1.6}</style><h1>Sky Gardens / '+title+' stone production</h1><p>Original measured Blender modules. Six orthographic views share a scale within each module sheet, with a standing-controller comparator. Three runtime LODs per module; shared painted ivory, teal and bronze materials. Final in-game visual and playtest acceptance remain pending.</p><p><a href="production.json">Production and construction manifest</a> / <a href="'+source_name+'">Installed source geometry</a> / <a href="measured-views.json">Camera scale</a></p>'+''.join(cards)
(folder/'index.html').write_text(page,encoding='utf-8')
print(title+'_MEASURED_SHEETS_READY',len(cards))
