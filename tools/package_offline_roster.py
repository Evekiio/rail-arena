"""Package the offline setup review and its original character references."""
import hashlib
import json
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Validation/OfflineRoster'
labels=['Player / ivory','Bot 01 / ember','Bot 02 / teal','Bot 03 / plum']
references=['Player','Bot01','Bot02','Bot03']
records=[]
for name in [f'Character-{index}.png' for index in range(4)]+['Menu-1920x1080.png','Menu-1280x720.png']:
    path=OUT/name
    data=path.read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n'
    expected=(1280,720) if path.name=='Menu-1280x720.png' else (1920,1080)
    assert struct.unpack_from('>II',data,16)==expected
    records.append({'file':path.name,'dimensions':expected,'sha256':hashlib.sha256(data).hexdigest()})
assert len(records)==6
manifest={'scope':'Four selectable existing offline appearances; unique 0-3-bot roster; no new model geometry or network schema',
          'maximumContenders':4,'maximumBots':3,'references':references,'renders':records,
          'limits':['Menus are neutral-background renders of actual uGUI geometry/materials','First-person camera captures omit screen-space HUD',
                    'Shared first-person rig is palette-matched, not four newly authored arm meshes','No human input acceptance or FPS certification']}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
body='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rail Arena / offline roster review</title><style>
body{margin:0;background:#101b24;color:#e6edeb;font:16px/1.6 system-ui}main{max-width:1650px;margin:auto;padding:36px}h1{font-size:42px;line-height:1.15}a{color:#6ae0eb}p{max-width:1050px;color:#bdcdd1}.pair{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0 0 25px}img{display:block;width:100%;border-radius:7px;border:1px solid #37515b}figcaption{padding:10px 0}h2{margin-top:36px}@media(max-width:800px){.pair{grid-template-columns:1fr}main{padding:20px}}</style><main>
<p>RAIL ARENA / SANDERS RIDDLE</p><h1>Offline setup / four contenders</h1>
<p>The current source adds saved appearance selection, unique opponents and the approved four-contender cap. These use existing character prefabs. The menu separates launch actions from next-session configuration; combat matches always use combat AI.</p>
<p><a href="../../docs/OFFLINE-ROSTER.md">Behavior and limits</a> · <a href="tests.xml">Unity tests</a> · <a href="manifest.json">Render manifest</a></p>
<h2>Menu / 1080p and 720p</h2><p>Actual rebuilt uGUI geometry and materials on a neutral background. These are controlled layout reviews, not standalone screenshots.</p><div class="pair">'''
for size in ('1920x1080','1280x720'):
    body+=f'<figure><a href="Menu-{size}.png"><img src="Menu-{size}.png" alt="Offline setup at {size}"></a><figcaption>{size}</figcaption></figure>'
body+='</div><h2>Reference families / selected first-person palettes</h2><p>Each family retains its existing body prefab. The right-hand views show the actual running session with the corresponding hand/forearm palette. They do not establish perfect body-reference matching or new arm topology.</p>'
for index,(label,reference) in enumerate(zip(labels,references)):
    source=f'../../ArtSource/Original/StylizedContenders/References-v1/Final/{reference}-Turnaround.png'
    assert (OUT/source).is_file()
    body+=f'<h3>{label}</h3><div class="pair"><figure><a href="{source}"><img loading="lazy" src="{source}" alt="{label} reference family"></a><figcaption>Existing character reference family</figcaption></figure><figure><a href="Character-{index}.png"><img loading="lazy" src="Character-{index}.png" alt="{label} first-person palette"></a><figcaption>Live Unity camera / selected palette</figcaption></figure></div>'
body+='<p>Direct-IP duel keeps its fixed two-character mapping; ready-room selection is still outstanding. Damage, movement and traversal are unchanged. The public pre-alpha ZIP is unchanged. Human acceptance and measured combat rendering performance remain open.</p></main></html>'
(OUT/'index.html').write_text(body,encoding='utf-8')
print('Packaged six roster/menu renders with four reference families')
