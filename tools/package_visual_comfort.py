"""Package actual isolated shot renders and controlled uGUI comfort reviews."""
import hashlib
import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / 'Validation/VisualComfort'
records = []

def image(name, label, expected):
    filename = name+'.png'
    data = (out/filename).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n'
    assert struct.unpack_from('>II',data,16) == expected
    records.append({'file':filename,'dimensions':list(expected),'sha256':hashlib.sha256(data).hexdigest()})
    return f'<figure><a href="{filename}"><img src="{filename}" alt="{label}"></a><figcaption>{label}</figcaption></figure>'

sections = ['<h2>Shot at initial emission / same camera</h2><div class="pair">'+
            image('Normal-Effect','Normal shot',(1440,810))+image('Reduced-Effect','Reduced Flash on',(1440,810))+'</div>']
for name, title in [('Settings','Comfort controls / reduced flash on, weapon motion off'),('Live','Controlled live HUD / reduced damage-edge tint')]:
    sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(image(f'{name}-{w}x{h}',f'{w} × {h}',(w,h)) for w,h in [(1920,1080),(1280,720)])+'</div>')
(out/'manifest.json').write_text(json.dumps({'scope':'Isolated actual shot renders; controlled uGUI layout captures; neutral backgrounds', 'images':records},indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / visual comfort</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / visual comfort</h1><p>Actual pooled shot effects rendered from one isolated camera without bloom. HUD captures use actual uGUI meshes/materials and controlled feedback on a neutral background. These are review fixtures, not human gameplay or performance measurements.</p><p><a href="../../docs/VISUAL-COMFORT.md">Behavior and limits</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Checksums</a></p>'''+''.join(sections)+'</main></html>',encoding='utf-8')
print('Packaged six visual comfort review captures')
