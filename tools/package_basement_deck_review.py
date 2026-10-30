"""Package measured deck construction sheets and matching installed-game comparisons."""
import hashlib
import json
from pathlib import Path
from html.parser import HTMLParser
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont,ImageStat

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Environment/BasementDecks'
result=ET.parse(root/'Validation/art-basement-installed-production.xml').getroot()
assert result.attrib['result']=='Passed' and result.attrib['total']=='1'
spec=json.loads((folder/'production.json').read_text());measurements=json.loads((folder/'measured-construction.json').read_text())
digest=hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest();assert digest==spec['masterSha256']==measurements['masterSha256']
assert hashlib.sha256((folder/'deck-source.json').read_bytes()).hexdigest()==spec['sourceSha256']
for lod in spec['lods']:assert hashlib.sha256((root/lod['export']).read_bytes()).hexdigest()==lod['exportSha256']
review=json.loads((folder/'UnityReview/index.json').read_text());assert review['decks']==8 and len(review['meshes'])==24 and len(review['views'])==9
for check in review['meshes']:assert check['minimumY']>=-.601 and check['maximumY']<=.001 and check['minimumHullCross']>=-.001
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',28)
for profile in measurements['profiles']:
    directory=folder/'References'/profile['profile'];span=profile['orthographicSpanMetres'];sheet=Image.new('RGB',(3072,2280),'#13232c');draw=ImageDraw.Draw(sheet)
    draw.text((25,15),f"RAIL ARENA / {profile['profile']} stone deck / SG-BASEMENT-DECKS-v1",font=font,fill='white')
    draw.text((25,57),f"{profile['diameterMetres']:.3f} m diameter / {profile['thicknessMetres']:.3f} m thick / top-center pivot / master {digest[:16]}",font=font,fill='#8ee5dc')
    for index,view in enumerate(profile['views']):
        x=(index%3)*1024;y=115+(index//3)*1050
        with Image.open(directory/(view+'.png')) as image:sheet.paste(image.convert('RGB'),(x,y))
        draw.text((x+20,y+15),view.upper()+f' / {span:.2f} m view span',font=font,fill='#8ee5dc')
        pixels=1024/span;draw.line((x+30,y+965,x+30+round(pixels),y+965),fill='#8ee5dc',width=4);draw.text((x+30,y+915),'1 metre',font=font,fill='#8ee5dc')
        height=round(1.8*pixels);width=round(.6*pixels);right=x+960;bottom=y+970
        draw.rounded_rectangle((right-width,bottom-height,right,bottom),radius=max(3,width//2),outline='#d9b97c',width=3)
        draw.text((right-240,bottom-height-43),'1.80 m controller',font=font,fill='#d9b97c')
    draw.text((25,2220),'Geometry-derived equal-scale views. Gold capsule is a size comparison, not an added game object. Unity shading shown in installed comparisons.',font=font,fill='white')
    sheet.save(directory/'measured-sheet.png')
comparisons=[]
for name in ('basement-top','staging-gameplay','basement-underside'):
    before=next(view for view in review['views'] if view['id']==name+'-before');after=next(view for view in review['views'] if view['id']==name+'-after')
    assert before['position']==after['position'] and before['euler']==after['euler'] and before['fieldOfView']==after['fieldOfView']
    sheet=Image.new('RGB',(3840,1160),'#13232c');draw=ImageDraw.Draw(sheet)
    for x,label,suffix in [(0,'BEFORE / original deck visual','before'),(1920,'AFTER / authored stone + inlaid marker','after')]:
        draw.text((x+24,18),name+' / '+label,font=font,fill='white')
        with Image.open(folder/'UnityReview'/(name+'-'+suffix+'.png')) as image:
            assert max(ImageStat.Stat(image).stddev)>8,'A real, non-uniform rendered scene is required'
            sheet.paste(image.convert('RGB'),(x,80))
    destination=folder/'UnityReview'/(name+'-comparison.png');sheet.save(destination);comparisons.append({'view':name,'position':after['position'],'euler':after['euler'],'fieldOfView':after['fieldOfView'],'image':str(destination.relative_to(folder)),'scope':'Only deck/landing marker visual visibility changes; production lighting and other art remain installed. Offscreen world-camera review, not native input/HUD/audio acceptance.'})
record={'masterSha256':digest,'installedDecks':8,'collisionAndTransformReview':'Focused Unity test preserves source mesh references, vertices, triangles, transforms, all scene collider identities and return-device transforms; removal restores original visuals/materials.','meshChecks':review['meshes'],'lodTriangles':{profile:[lod['triangles'] for lod in spec['lods'] if lod['profile']==profile] for profile in ('Staging','Basement')},'referenceViews':12,'explodedViews':2,'comparisons':comparisons,'scope':review['scope'],'acceptanceRemaining':['Native gameplay/input/audio/artistic acceptance by Sanders','Wide traversal and sustained frame-pacing acceptance','Remaining environment/device/animation production and complete inventory references']}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena measured basement stone kit</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;max-width:1800px;margin:30px auto;padding:24px}a{color:#8ee5dc}img{width:100%;height:auto}table{border-collapse:collapse}td,th{padding:8px 18px;border:1px solid #456}</style><h1>Sky Gardens / authored staging and basement stone</h1><p>Two measured families replace eight blockout deck visuals. Existing 20-sided collision meshes, top planes, route positions and return devices remain authoritative. The flat teal landing disc becomes an inlaid ring and crest. Every authored vertex stays inside the original footprint and 0.6 m thickness.</p><p><a href="Basement-Stone-Decks-v1.blend">Editable Blender master</a> · <a href="production.json">Exports and hashes</a> · <a href="deck-source.json">Exact collision source</a> · <a href="measured-construction.json">Per-part bounds and layers</a> · <a href="review-evidence.json">Installed review and limits</a> · <a href="paint-spec.json">Original paint maps</a></p><p>The master includes a hidden CollisionReference collection and packed colour/normal/cavity maps. Unity's shared cel shader remains the runtime lighting authority. Three LODs: 6,876 / 3,556 / 748 triangles per deck; five material slots.</p><h2>Matching in-game comparisons</h2>'''
for comparison in comparisons:html+=f'<h3>{comparison["view"]}</h3><img loading="lazy" src="{comparison["image"]}">'
for name in ('Staging','Basement'):html+=f'<h2>{name} / measured six-view sheet</h2><img loading="lazy" src="References/{name}/measured-sheet.png"><h3>Exploded construction layers</h3><img loading="lazy" src="References/{name}/exploded.png">'
html+='<h2>LOD review</h2><p>Same camera, explicitly selected LODs. These images review silhouette/detail changes; they are not performance measurements.</p>'
for level in range(3):html+=f'<h3>LOD {level}</h3><img loading="lazy" src="UnityReview/basement-lod{level}.png">'
html+='<p>Direction approved; native input, sound, sustained performance and artistic acceptance remain Sanders’ playtest gate. Public release held.</p></html>'
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('src','href'):assert (folder/value).resolve().is_file(),value
Links().feed(html);(folder/'index.html').write_text(html,encoding='utf-8')
print('BASEMENT_STONE_REVIEW_PACKAGED 8 installed decks / 24 checked LOD meshes / 12 orthographics / 2 exploded views / 3 matching comparisons')
