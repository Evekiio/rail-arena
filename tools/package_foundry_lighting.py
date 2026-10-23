"""Package matching headless Unity lighting views without external dependencies."""
import hashlib
import json
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Validation/FoundryLighting'
views=['Before-Overview','After-Overview','Before-LowerRoute','After-LowerRoute','Player-Ready']
records=[]
for name in views:
    data=(OUT/f'{name}.png').read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n'
    assert struct.unpack_from('>II',data,16)==(1920,1080)
    records.append({'file':name+'.png','sha256':hashlib.sha256(data).hexdigest()})
geometry=json.loads((OUT/'geometry.json').read_text())
assert geometry['unchanged'] and geometry['gameplayBefore']==geometry['gameplayAfter']
manifest={'unityVersion':geometry['unityVersion'],'geometryUnchanged':True,'renders':records,
          'scope':'Existing playable arena; cosmetic sky and lighting only. The v2 Foundry map and new environment models remain separate review candidates.',
          'limits':['Controlled Unity camera renders omit screen-space HUD','No 1080p/144fps certification','No human visual/input acceptance','Published client release unchanged']}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
def picture(name,label):
    return f'<figure><a href="{name}.png"><img src="{name}.png" alt="{label}" loading="lazy"></a><figcaption>{label}</figcaption></figure>'
body='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rail Arena / Foundry daylight review</title><style>
body{background:#101a24;color:#e8eeed;font:16px/1.6 system-ui;margin:0}main{max-width:1700px;margin:auto;padding:35px}
h1{font-size:40px}a{color:#62dfe9}.pair{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0 0 20px}img{width:100%;display:block;border:1px solid #38515b;border-radius:6px}figcaption{padding:10px 0}p{max-width:1000px;color:#bbccd0}h2{margin-top:40px}
@media(max-width:800px){.pair{grid-template-columns:1fr}main{padding:20px}}</style><main>
<p>RAIL ARENA / SANDERS RIDDLE</p><h1>Foundry / daylight and cloud-world review</h1>
<p>Cosmetic treatment of the current playable arena. These are actual headless Unity camera renders with matching before/after poses. The screen-space HUD is omitted. Gameplay geometry and traversal placement are unchanged. The new Foundry v2 layout and environment models are not installed by this update.</p>
<p><a href="manifest.json">Render manifest</a> · <a href="geometry.json">Gameplay geometry proof</a> · <a href="tests.xml">Unity tests</a></p>
<h2>Overview / identical camera</h2><div class="pair">'''
body+=picture('Before-Overview','Before / saved presentation before this authoring pass')+picture('After-Overview','After / neutral daylight and static faceted cloud sea')+'</div>'
body+='<h2>Lower route / identical player-height camera</h2><div class="pair">'+picture('Before-LowerRoute','Before / lower route')+picture('After-LowerRoute','After / lower route')+'</div>'
body+='<h2>World direction beside current implementation</h2><div class="pair"><figure><a href="../../ArtSource/Original/Foundry-Arena-v2/References-v1/Concepts/07-World-Lighting.png"><img loading="lazy" src="../../ArtSource/Original/Foundry-Arena-v2/References-v1/Concepts/07-World-Lighting.png" alt="World and lighting reference sheet"></a><figcaption>World and lighting reference / mood, palette and light direction</figcaption></figure>'+picture('Player-Ready','Live practice camera / current arena and installed carbine')+'</div>'
body+='<p>The reference remains a broader art target. Distant islands, landmarks, final cloud artwork, material refinement and production environment models remain outstanding. These renders establish presentation evidence, not human acceptance, competitive balance or measured frame-rate qualification.</p></main></html>'
(OUT/'index.html').write_text(body,encoding='utf-8')
print('Foundry lighting review: five 1080p renders and unchanged gameplay signature')
