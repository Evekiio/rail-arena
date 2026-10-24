from pathlib import Path
import hashlib,json,struct
root=Path(__file__).resolve().parents[1];folder=root/'Validation/ButtonStates';images=[];sections=[]
for state,title in [('Disabled','Unavailable bot match / zero bots'),('Hover','Scripted pointer hover / How to Play'),('Selected','EventSystem selection / How to Play'),('Inactive','VSync / inactive FPS limit'),('AudioMuted','Muted effects / unavailable sound preview')]:
 figures=[]
 for width,height in [(1280,720),(1920,1080)]:
  name=f'{state}-{width}x{height}.png';data=(folder/name).read_bytes()
  assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(width,height)
  images.append(dict(file=name,sha256=hashlib.sha256(data).hexdigest()))
  figures.append(f'<figure><a href="{name}"><img src="{name}" alt="{title}"></a><figcaption>{width} x {height}</figcaption></figure>')
 sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(figures)+'</div>')
(folder/'manifest.json').write_text(json.dumps(dict(scope='Actual uGUI meshes/materials plus renderer tint; scripted pointer/focus; main/FPS snapshots use zero-duration transitions, audio snapshots retain production timing; neutral background, not native-input acceptance',images=images),indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / button feedback</title><style>body{background:#121c28;color:#e6eeee;font:16px/1.6 system-ui;margin:30px}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / button feedback</h1><p>Actual uGUI geometry/materials with CanvasRenderer tint against a neutral background. Pointer/focus states are scripted. Main/FPS snapshots use zero-duration transitions; audio snapshots retain production timing. Production transitions are 80ms. No physical-input or human contrast acceptance implied.</p><p><a href="../../docs/BUTTON-FEEDBACK.md">Behavior and capture correction</a> | <a href="tests.xml">Scoped tests</a> | <a href="manifest.json">Image hashes</a></p>'''+''.join(sections)+'</main></html>',encoding='utf-8')
print('Packaged ten state-aware button captures')
