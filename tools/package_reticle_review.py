"""Package controlled uGUI review captures and their checksums."""
import hashlib
import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / 'Validation/Reticle'
records, sections = [], []
for name, title in [('Settings', 'Size 64 / magenta previews'), ('Live', 'Size 48 / white during recovery')]:
    images = []
    for width, height in [(1920, 1080), (1280, 720)]:
        filename = f'{name}-{width}x{height}.png'
        data = (out / filename).read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        assert struct.unpack_from('>II', data, 16) == (width, height)
        records.append({'file': filename, 'dimensions': [width, height], 'sha256': hashlib.sha256(data).hexdigest()})
        images.append(f'<figure><a href="{filename}"><img src="{filename}" alt="{title} at {width} by {height}"></a><figcaption>{width} × {height}</figcaption></figure>')
    sections.append(f'<h2>{title}</h2><div class="pair">'+''.join(images)+'</div>')
(out / 'manifest.json').write_text(json.dumps({'scope': 'Controlled uGUI mesh/material captures on a neutral background', 'images': records}, indent=2)+'\n')
(out / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / reticle settings</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / reticle review</h1><p>Controlled uGUI mesh/material captures against a neutral background. These show layout and appearance, not human input acceptance or standalone gameplay.</p><p><a href="../../docs/RETICLE-SETTINGS.md">Behavior and validation limits</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Image checksums</a></p>'''+''.join(sections)+'</main></html>', encoding='utf-8')
print('Packaged four reticle review captures')
