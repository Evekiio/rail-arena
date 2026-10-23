"""Create an offline gallery and checksums for controlled HUD layout captures."""
import hashlib
import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
out = root / 'Validation/CombatStatistics'
cards = []
records = []
for name, title in [('Live', 'Live standings'), ('OfflineResults', 'Offline results'), ('DuelResults', 'Duel results')]:
    images = []
    for width, height in [(1920, 1080), (1280, 720)]:
        filename = f'{name}-{width}x{height}.png'
        data = (out / filename).read_bytes()
        assert data[:8] == b'\x89PNG\r\n\x1a\n'
        assert struct.unpack_from('>II', data, 16) == (width, height)
        records.append({'file': filename, 'width': width, 'height': height, 'sha256': hashlib.sha256(data).hexdigest()})
        images.append(f'<figure><a href="{filename}"><img src="{filename}" alt="{title} at {width} by {height}"></a><figcaption>{width} × {height}</figcaption></figure>')
    cards.append(f'<section><h2>{title}</h2><div class="pair">'+''.join(images)+'</div></section>')
(out / 'manifest.json').write_text(json.dumps({'scope': 'Controlled seeded uGUI layout review; neutral background; not human performance records', 'images': records}, indent=2)+'\n')
(out / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / standings review</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}section{margin:35px 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / standings and results</h1><p>Actual uGUI meshes and materials, controlled seeded statistics, neutral background. Layout review only; these are not human match records or standalone gameplay screenshots.</p><p><a href="../../docs/COMBAT-STATISTICS.md">Behavior and qualification limits</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Image checksums</a></p>'''+''.join(cards)+'</main></html>', encoding='utf-8')
print('Packaged six controlled HUD review captures')
