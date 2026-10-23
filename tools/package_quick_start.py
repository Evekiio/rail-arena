"""Package the actual uGUI quick-start layout reviews."""
import hashlib
import json
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'Validation/QuickStart'
records=[]
sections=[]
for name,title in [('Menu','Optional menu entry / configured zero-bot state'),('Help','Main-menu help / solo launch'),('PausedHelp','Paused-session help / resume')]:
    figures=[]
    for w,h in [(1920,1080),(1280,720)]:
        filename=f'{name}-{w}x{h}.png';data=(out/filename).read_bytes()
        assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(w,h)
        records.append({'file':filename,'dimensions':[w,h],'sha256':hashlib.sha256(data).hexdigest()})
        figures.append(f'<figure><a href="{filename}"><img src="{filename}" alt="{title} at {w} by {h}"></a><figcaption>{w} × {h}</figcaption></figure>')
    sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(figures)+'</div>')
(out/'manifest.json').write_text(json.dumps({'scope':'Actual controlled uGUI mesh/material layout captures on a neutral background','images':records},indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / quick start</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / quick start</h1><p>Actual uGUI meshes and materials on a neutral review background, with controlled menu selections. These are layout reviews, not human gameplay or performance measurements.</p><p><a href="../../docs/QUICK-START.md">Behavior and limits</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Checksums</a></p>'''+''.join(sections)+'</main></html>',encoding='utf-8')
print('Packaged six quick-start layout captures')
