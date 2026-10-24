from pathlib import Path
import hashlib,json,struct
root=Path(__file__).resolve().parents[1];folder=root/'Validation/CharacterPicker';images=[];sections=[]
for state,title in [('Picker','Offline selection'),('NextSession','Paused / next-session choice')]:
 figures=[]
 for width,height in [(1280,720),(1920,1080)]:
  name=f'{state}-{width}x{height}.png';path=folder/name;data=path.read_bytes()
  assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(width,height)
  images.append(dict(file=name,sha256=hashlib.sha256(data).hexdigest()))
  figures.append(f'<figure><a href="{name}"><img src="{name}" alt="{title}"></a><figcaption>{width} x {height}</figcaption></figure>')
 sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(figures)+'</div>')
portraits=[]
for i in range(4):
 path=root/f'RailArena/Assets/Resources/UI/ContenderPortraits/Character-{i}.png';data=path.read_bytes();assert struct.unpack_from('>II',data,16)==(512,640)
 relative='../../'+path.relative_to(root).as_posix();images.append(dict(file=relative,sha256=hashlib.sha256(data).hexdigest()))
 portraits.append(f'<img src="{relative}" alt="Installed contender appearance {i}">')
(folder/'manifest.json').write_text(json.dumps(dict(scope='Actual model portraits and headless uGUI mesh/material review; no human gameplay or art acceptance',images=images),indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / contender picker</title><style>body{background:#121c28;color:#e6eeee;font:16px/1.6 system-ui;margin:30px}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}.portraits{display:grid;grid-template-columns:repeat(4,1fr);gap:15px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}.portraits{grid-template-columns:1fr 1fr}}</style><main><h1>Rail Arena / choose your contender</h1><p>Installed game-model portraits and actual uGUI mesh/material review against a neutral background. No human-input or final-art acceptance implied.</p><p><a href="../../docs/CHARACTER-PICKER.md">Behavior and limits</a> | <a href="Portraits.json">Portrait sources</a> | <a href="tests.xml">Initial scoped tests</a> | <a href="layout-tests.xml">Final picker refresh</a> | <a href="manifest.json">Image hashes</a></p>'''+''.join(sections)+'<h2>Portrait sources / actual model renders</h2><div class="portraits">'+''.join(portraits)+'</div></main></html>',encoding='utf-8')
print('Packaged four UI captures and four model portraits')
