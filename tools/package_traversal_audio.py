from pathlib import Path
import hashlib,json,struct,wave
root=Path(__file__).resolve().parents[1];folder=root/'Validation/TraversalAudio';records=[];sounds=[]
for name in ['Pickup','Launch']:
 path=folder/f'{name}.wav'
 with wave.open(str(path),'rb') as audio:
  assert audio.getnchannels()==1 and audio.getsampwidth()==2 and audio.getframerate()==44100
  count=audio.getnframes();duration=count/44100
  assert .3<=duration<=.4
  samples=struct.unpack('<'+'h'*count,audio.readframes(count));assert max(abs(x) for x in samples)<=29491
 records.append(dict(file=path.name,frames=count,duration=duration,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 sounds.append(f'<section><h2>{name}</h2><audio controls preload="none" src="{path.name}"></audio><p><a href="{path.name}">Download source PCM preview</a></p></section>')
figures=[]
for width,height in [(1280,720),(1920,1080)]:
 name=f'Mix-{width}x{height}.png';data=(folder/name).read_bytes();assert struct.unpack_from('>II',data,16)==(width,height)
 records.append(dict(file=name,sha256=hashlib.sha256(data).hexdigest()));figures.append(f'<figure><img src="{name}" alt="Updated effects description"><figcaption>{width} x {height}</figcaption></figure>')
(folder/'manifest.json').write_text(json.dumps(dict(scope='Source cue PCM previews before gameplay gain; actual neutral-background uGUI review; no audible/human acceptance',files=records),indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / local traversal audio</title><style>body{background:#121c28;color:#e6eeee;font:16px/1.6 system-ui;margin:30px}main{max-width:1700px;margin:auto}a{color:#73e2ec}.pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}figure{margin:0}img{width:100%;height:auto}audio{width:min(100%,600px)}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style><main><h1>Rail Arena / local pickup and pad feedback</h1><p>Original tested source cues, mono 44.1kHz / 16-bit PCM. Previews precede master/effects and in-game gains; listening and final combat mix approval remain open.</p><p><a href="../../docs/TRAVERSAL-AUDIO.md">Behavior and delivery limits</a> | <a href="tests.xml">Scoped tests</a> | <a href="manifest.json">File hashes</a></p>'''+''.join(sounds)+'<h2>Audio settings / updated effects scope</h2><p>Actual uGUI meshes/materials/tint against a neutral background; not human gameplay.</p><div class="pair">'+''.join(figures)+'</div></main></html>',encoding='utf-8')
print('Verified two PCM previews and two audio-page captures')
