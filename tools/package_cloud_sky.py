from pathlib import Path
import hashlib,json,struct
root=Path(__file__).resolve().parents[1];folder=root/'Validation/CloudSky';images=[];sections=[]
for view,title in [('Overview','Arena overview'),('Edge','Outward edge'),('Horizon','Upper horizon'),('UpperSky','Upper sky')]:
 figures=[]
 for state in ['Before','After']:
  name=f'{state}-{view}.png';data=(folder/name).read_bytes()
  assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(1920,1080)
  images.append(dict(file=name,sha256=hashlib.sha256(data).hexdigest()))
  figures.append(f'<figure><a href="{name}"><img src="{name}" alt="{state}: {title}"></a><figcaption>{state} / matching camera</figcaption></figure>')
 sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(figures)+'</div>')
player=folder/'Player-Ready.png';data=player.read_bytes()
assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(1920,1080)
images.append(dict(file=player.name,sha256=hashlib.sha256(data).hexdigest()))
sections.append('<h2>Automated solo-practice frame</h2><p>Captured by the daylight lifecycle fixture; no human-input or combat-quality acceptance implied.</p><img src="Player-Ready.png" alt="Current first-person view with refined clouds">')
(folder/'manifest.json').write_text(json.dumps(dict(scope='Headless Unity scene renders; same cameras and sky material values; not human gameplay or performance qualification',images=images),indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / cloud sky</title><style>body{background:#121c28;color:#e6eeee;font:16px/1.6 system-ui;margin:30px}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Foundry / cloud sky</h1><p>Actual matching-camera Unity renders. Static sky shading only; no lighting, gameplay or performance qualification.</p><p><a href="../../docs/CLOUD-SKY.md">Construction and limits</a> | <a href="tests.xml">Scoped tests</a> | <a href="manifest.json">Image hashes</a></p>'''+''.join(sections)+'</main></html>',encoding='utf-8')
print('Packaged eight matching views and one automated practice frame')
