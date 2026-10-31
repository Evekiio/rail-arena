"""Publish measured arena references and matching native camera comparisons."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root/'ArtSource/Original/ArtPass-v1/Environment/ArenaStone'
spec = json.loads((folder/'production.json').read_text(encoding='utf-8'))
review = json.loads((folder/'UnityReview/installed-review.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest() == spec['masterSha256']
assert review['placements']==19 and review['uniqueLodAssets']==21 and review['lodMeshInstances']==57
assert review['physicsPreserved'] and review['navigationPreserved'] and review['devicesPreserved'] and review['cleanupPassed']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 25)
views = {view['name']: view for view in review['views']}
cards, comparisons = [], []
for name in ['arena-overview','outer-gameplay','ramp-west','ramp-east','underpass-gameplay','outer-underside','corner-detail']:
    before, after = views[name+'-before'], views[name+'-after']
    assert before['position']==after['position'] and before['euler']==after['euler'], name
    image = Image.new('RGB',(2560,780),'#101d22')
    draw = ImageDraw.Draw(image)
    for x,label,suffix in [(0,'BEFORE / original deck and ramp rendering','before'),(1280,'AFTER / measured original stone construction','after')]:
        draw.text((x+22,17),label,font=font,fill='#efead7')
        with Image.open(folder/'UnityReview'/(name+'-'+suffix+'.png')) as native:
            assert native.size==(1920,1080)
            image.paste(native.convert('RGB').resize((1280,720)),(x,60))
    image.save(folder/(name+'-comparison.png'))
    cards.append('<h2>'+name+'</h2><img src="'+name+'-comparison.png">')
    comparisons.append({'view':name,'position':after['position'],'euler':after['euler'],'scope':'Identical staged camera, 65 degree vertical FOV. Existing art profile retained while only this family is toggled. Not native input or performance acceptance.'})
(folder/'comparison-evidence.json').write_text(json.dumps({'masterSha256':spec['masterSha256'],'views':comparisons},indent=2)+'\n',encoding='utf-8')
page = (folder/'index.html').read_text(encoding='utf-8')
marker = '<!-- NATIVE_ARENA_REVIEW -->'
page = page.split(marker)[0]
page += marker+'<h1>Installed arena comparison</h1><p>Nineteen source placements and fifty-seven LOD mesh instances pass the focused hull, unchanged physics/navigation/device and cleanup checks. These are controlled Editor views. Final Windows performance, all LOD transitions, native input/audio and artistic acceptance remain separate.</p><p><a href="UnityReview/installed-review.json">Native contract evidence</a> / <a href="comparison-evidence.json">Matching camera poses</a></p>'+''.join(cards)+'<h2>Forced simplified levels / same corner camera</h2><img src="UnityReview/corner-detail-lod1.png"><img src="UnityReview/corner-detail-lod2.png">'
(folder/'index.html').write_text(page,encoding='utf-8')
print('ARENA_NATIVE_COMPARISONS_PACKAGED',len(comparisons))
