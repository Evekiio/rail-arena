"""Compose candidate crouch pose sheets; never label source references as installed gameplay."""
import hashlib
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parents[1]
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Crouch'
spec=json.loads((folder/'crouch-production.json').read_text(encoding='utf-8'))
evidence=json.loads((folder/'baked-contact-review.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']==evidence['masterSha256']
assert evidence['rendersUpdated']
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
cards=[];poster=Image.new('RGB',(1600,100+400*4),'#102029');poster_draw=ImageDraw.Draw(poster)
poster_draw.text((25,20),'RAIL ARENA / ORIGINAL CROUCH CANDIDATES / SOURCE POSES',font=font,fill='white')
for index,clip in enumerate(spec['clips']):
    record=next(item for item in evidence['clips'] if item['clip']==clip['name'])
    sheet=Image.new('RGB',(2304,2730),'#102029');draw=ImageDraw.Draw(sheet)
    draw.text((25,15),clip['name']+' / '+str(clip['fps'])+' Hz / '+str(clip['durationSeconds'])+' s / '+('loop' if clip['loop'] else 'transition reference'),font=font,fill='white')
    draw.text((25,52),'Canonical Blender geometry; unarmed bone reference. Unity weapon grips and stance integration remain pending.',font=small,fill='#b4d9d2')
    for row,view in enumerate(record['views']):
        for column,frame in enumerate(record['poseFrames']):
            x,y=column*384,95+row*425
            with Image.open(folder/'References'/clip['name']/(view+'-'+str(column)+'.png')) as source:
                sheet.paste(source.convert('RGB').resize((384,384)),(x,y+25))
            draw.text((x+8,y),view.upper()+' / '+str(frame)+'f / '+format(frame/60,'.3f')+'s',font=small,fill='#b4d9d2')
            draw.line((x+12,y+395,x+12+round(384/2.2),y+395),fill='#48cdd1',width=3)
            draw.text((x+12,y+402),'1 m / 2.2 m shared view span',font=small,fill='#b4d9d2')
    draw.text((25,2665),'ANIM-CROUCH-v1 / '+spec['masterSha256'][:32]+' / original shared skeleton / no actor root motion',font=small,fill='white')
    path=folder/'References'/clip['name']/'measured-poses.png';sheet.save(path)
    px,py=(index%2)*800,100+(index//2)*400
    with Image.open(folder/'References'/clip['name']/'front-0.png') as front:poster.paste(front.convert('RGB').resize((370,370)),(px,py+25))
    with Image.open(folder/'References'/clip['name']/'right-0.png') as side:poster.paste(side.convert('RGB').resize((370,370)),(px+390,py+25))
    poster_draw.text((px+12,py),clip['name'],font=small,fill='white')
    cards.append('<h2>'+clip['name']+'</h2><img src="References/'+clip['name']+'/measured-poses.png">')
poster.save(folder/'Crouch-Family-Poster.png')
page='<!doctype html><meta charset="utf-8"><title>Rail Arena crouch candidates</title><style>body{background:#102029;color:#edf0df;font:18px Arial;margin:3vw}a{color:#48cdd1}img{max-width:100%}p{max-width:950px;line-height:1.6}</style><h1>Original crouch motion / candidate reference package</h1><p>Seven original actions on the compatible shared skeleton. These are canonical Blender references and candidate humanoid imports, not installed gameplay. The existing live procedural crouch and Review 16 remain unchanged. Native retargeting, all four bodies, grips, hitboxes, stance transitions and moving-world foot contacts remain to be validated before installation.</p><p><a href="crouch-production.json">Source, cadence and ankle tracks</a> / <a href="baked-contact-review.json">All-bound-mesh floor and seam evidence</a></p><img src="Crouch-Family-Poster.png">'+''.join(cards)
(folder/'index.html').write_text(page,encoding='utf-8')
print('CROUCH_REFERENCE_PACKAGE_READY',len(cards),'sheets; candidate only')
