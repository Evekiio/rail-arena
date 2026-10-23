"""Package actual health-beacon renders beside the existing visual reference."""
import hashlib
import json
import re
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'Validation/HealthBeacon'
geometry=json.loads((out/'Geometry.json').read_text())
assert geometry['unchanged'] and geometry['before']==geometry['after'] and geometry['sites']==3
meshes=[]
for path in sorted((root/'RailArena/Assets/RailArena/Art/HealthBeacon').glob('*.asset')):
    text=path.read_text()
    meshes.append({'file':path.name,'triangles':sum(map(int,re.findall(r'^    indexCount: (\d+)',text,re.M)))//3,
                   'sha256':hashlib.sha256(text.encode('utf-8')).hexdigest(),'hashEncoding':'UTF-8 with LF line endings'})
assert sum(item['triangles'] for item in meshes)==912
images=[]
for name in ['Available','Depleted','Arena-site']:
    data=(out/(name+'.png')).read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(1440,1080)
    images.append({'file':name+'.png','sha256':hashlib.sha256(data).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Actual headless Unity camera renders; current-arena cosmetic retrofit', 'images':images,'meshes':meshes,'triangles':912},indent=2)+'\n')
reference='../../ArtSource/Original/Foundry-Arena-v2/References-v1/Concepts/05-Health-Pickup.png'
assert (out/reference).is_file()
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / health beacon</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / health beacon</h1><p>Actual Unity camera renders. The concept supplies color, form and state direction; the runtime retrofit uses four outward-facing plus panels and a larger marker for readability. Full Foundry v2 art remains under review.</p><p><a href="../../docs/HEALTH-BEACON.md">Construction and qualification</a> · <a href="Geometry.json">Gameplay geometry receipt</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Asset checksums</a></p><h2>Reference / current model</h2><div class="pair"><figure><a href="'''+reference+'''"><img src="'''+reference+'''" alt="Existing health concept sheet"></a><figcaption>Visual direction reference</figcaption></figure><figure><a href="Available.png"><img src="Available.png" alt="Available amber health beacon"></a><figcaption>Current four-face marker and pedestal</figcaption></figure></div><h2>Available / collected</h2><div class="pair"><img src="Available.png" alt="Available"><img src="Depleted.png" alt="Collected, pedestal remains"></div><h2>Current arena site</h2><img src="Arena-site.png" alt="Health beacon in the current Foundry arena"></main></html>''',encoding='utf-8')
print('Packaged three health-beacon renders, reference comparison and 912-triangle inventory')
