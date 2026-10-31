"""Compose dimensioned bridge sheets from canonical Blender renders, without altering source geometry."""
import hashlib
import html
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root/'ArtSource/Original/ArtPass-v1/Environment/BridgeStone'
spec = json.loads((folder/'production.json').read_text(encoding='utf-8'))
views = json.loads((folder/'measured-views.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest() == spec['masterSha256']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 28)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 21)
cards = []
for module in views['modules']:
    family = module['family']
    sheet = Image.new('RGB', (1800, 1540), '#101d22')
    draw = ImageDraw.Draw(sheet)
    draw.text((36,24), 'RAIL ARENA / SKY GARDENS / BRIDGE '+family.upper(), font=font, fill='#efead7')
    dimensions = module['sizeUnityMetres']
    draw.text((36,67), 'Metres / Unity W %.3f × H %.3f × D %.3f / pivot: bounds centre' % tuple(dimensions), font=small, fill='#b5d3ce')
    for index, name in enumerate(module['views']):
        x, y = (index%3)*600, 125+(index//3)*650
        image = Image.open(folder/'References'/family/(name+'.png')).convert('RGB').resize((580,580))
        sheet.paste(image,(x+10,y+38))
        draw.text((x+22,y), name.upper(), font=small, fill='#efead7')
        ruler = round(580/module['orthographicSpanMetres'])
        draw.line((x+30,y+600,x+30+ruler,y+600), fill='#32bec5', width=4)
        draw.text((x+30,y+618),'1 m / shared scale within this sheet',font=small,fill='#b5d3ce')
    draw.text((36,1447),'SG-BRIDGE-STONE-v1 / Blender source '+spec['masterSha256'][:24],font=small,fill='#efead7')
    draw.text((36,1480),'Geometry-derived views; concept images do not override dimensions. Collision remains in Unity.',font=small,fill='#b5d3ce')
    sheet.save(folder/(family+'-Measured-Sheet.png'))
    cards.append('<article><h2>'+family+'</h2><img src="'+family+'-Measured-Sheet.png"><img src="References/'+family+'/perspective.png"></article>')
page = '<!doctype html><meta charset="utf-8"><title>Rail Arena / Bridge Stone</title><style>body{background:#101d22;color:#efead7;font:18px Arial;margin:3vw}img{max-width:100%}article{margin-top:48px}p{max-width:900px;line-height:1.6}</style><h1>Sky Gardens / Bridge stone production</h1><p>Original measured Blender modules: deck, cover screen and pier. Six orthographic views share a scale within each module sheet. Three runtime LODs per module; shared painted ivory, teal and bronze materials. Final in-game visual and playtest acceptance remain pending.</p><p><a href="production.json">Production and construction manifest</a> / <a href="bridge-source.json">Installed source geometry</a> / <a href="measured-views.json">Camera scale</a></p>'+''.join(cards)
(folder/'index.html').write_text(page,encoding='utf-8')
print('BRIDGE_MEASURED_SHEETS_READY',len(cards))
