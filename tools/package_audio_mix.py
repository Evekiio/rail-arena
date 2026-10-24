from pathlib import Path
import hashlib,json,struct
root=Path(__file__).resolve().parents[1];folder=root/'Validation/AudioMix';images=[];sections=[]
for state,title in [('Levels','Master and effects / rifle preview'),('Muted','Effects muted / preview disabled')]:
 figures=[]
 for width,height in [(1280,720),(1920,1080)]:
  name=f'{state}-{width}x{height}.png';data=(folder/name).read_bytes()
  assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(width,height)
  images.append(dict(file=name,width=width,height=height,sha256=hashlib.sha256(data).hexdigest()))
  figures.append(f'<figure><a href="{name}"><img src="{name}" alt="{title}"></a><figcaption>{width} x {height}</figcaption></figure>')
 sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(figures)+'</div>')
(folder/'manifest.json').write_text(json.dumps(dict(scope='Headless actual uGUI meshes/materials; neutral background, not audible acceptance or human gameplay',images=images),indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / audio mix</title><style>body{background:#121c28;color:#e6eeee;font:16px/1.6 system-ui;margin:30px}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / audio mix</h1><p>Actual uGUI mesh/material review against a neutral background. This demonstrates layout, not sound-on quality.</p><p><a href="../../docs/AUDIO-MIX.md">Behavior and limits</a> | <a href="tests.xml">Scoped tests</a> | <a href="manifest.json">Image hashes</a></p>'''+''.join(sections)+'</main></html>',encoding='utf-8')
print('Packaged four audio-mix captures')
