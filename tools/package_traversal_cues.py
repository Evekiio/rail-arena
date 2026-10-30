"""Package actual traversal-cue views and geometry evidence."""
import hashlib
import json
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[1]
out=root/'Validation/TraversalCues'
geometry=json.loads((out/'Geometry.json').read_text())
assert geometry['unchanged'] and geometry['before']==geometry['after']
assert geometry['pads']==4 and geometry['sharedLandingZones']==2
records=[]
for name in ['After-Pad','Before-Landing','After-Landing']:
    data=(out/(name+'.png')).read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack_from('>II',data,16)==(1440,1080)
    records.append({'file':name+'.png','sha256':hashlib.sha256(data).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Actual headless Unity camera renders; cosmetic cues on current arena','beforeLandingSource':'8150907d247925700b42318d255d45bdd45d3894','images':records},indent=2)+'\n')
(out/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / traversal cues</title><style>body{margin:30px;background:#121c28;color:#e6eeee;font:16px/1.6 system-ui}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / traversal cues</h1><p>Actual Unity camera renders. Existing pad physics and map collision are unchanged. The current four pads feed two shared landing zones.</p><p><a href="../../docs/TRAVERSAL-CUES.md">Behavior and limits</a> · <a href="Geometry.json">Gameplay geometry receipt</a> · <a href="tests.xml">Tests</a> · <a href="manifest.json">Checksums</a></p><h2>Corrected pad / destination is toward the top of the image</h2><img src="After-Pad.png" alt="Pad chevrons converge toward the configured destination"><h2>Landing centre / before and after</h2><div class="pair"><figure><img src="Before-Landing.png" alt="Original landing without brackets"><figcaption>Before / source 8150907</figcaption></figure><figure><img src="After-Landing.png" alt="Landing with muted corner brackets"><figcaption>After / cosmetic brackets</figcaption></figure></div></main></html>''',encoding='utf-8')
print('Packaged three traversal-cue renders and geometry receipt')
