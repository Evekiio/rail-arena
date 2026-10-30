"""Package authored action provenance, same-timeline motion and reference frame strip."""
import csv
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Gate2/RifleActions'
action=json.loads((folder/'ReloadAction.json').read_text())
rows=list(csv.DictReader((folder/'PlaybackTiming.csv').open()))
assert len(rows)==180
assert all(int(b['simulationTick'])-int(a['simulationTick'])==1 for a,b in zip(rows,rows[1:]))
active=[r for r in rows if r['reloading']=='True']
first_active=next(i for i,r in enumerate(rows) if r['reloading']=='True')
trigger_tick=int(rows[first_active-1]['simulationTick'])
complete_tick=int(next(r['simulationTick'] for r in rows[first_active:] if r['reloading']=='False'))
assert complete_tick-trigger_tick==120
assert rows[-1]['rounds']=='30' and rows[-1]['reloading']=='False'
assert hashlib.sha256((folder/'Carbine-Reload-v1.blend').read_bytes()).hexdigest()==action['canonicalMasterSha256']
sheet=Image.new('RGB',(1920,1390),'#101c23')
draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',25)
draw.text((24,18),'RAIL ARENA / authored rifle reload / 120 simulation ticks / 2.00 seconds',font=font,fill='#dce7df')
labels=[(0,'Ready'),(21,'Magazine grip'),(36,'Withdraw'),(65,'Replace below view'),(91,'Seat'),(118,'Support grip'),(120,'Complete')]
for index,(frame,label) in enumerate(labels):
    x=index%3*640;y=70+index//3*420
    with Image.open(folder/'in-game'/f'reload-{frame:03}.png') as image:sheet.paste(image.resize((640,360)),(x,y+42))
    draw.text((x+14,y+7),f'{frame/60:.2f}s / {label}',font=font,fill='#6cddd5')
draw.text((24,1345),'Pose reference: fixed weapon camera. Playback MP4: live simulation, weapon tilt and HUD. Human playtest pending.',font=font,fill='#bdcbc8')
sheet.save(folder/'Reload-Frame-Strip.png')
record={'schema':'rail-arena-action-review/1','id':'FPS-RIFLE-RELOAD-v1','source':'Carbine-Reload-v1.blend','sourceSha256':action['canonicalMasterSha256'],'actionChannels':['Magazine carrier offset/rotation','Left wrist contact position/rotation'],'sampleCount':121,'sourceFps':60,'durationSeconds':2,'simulationTimingEvidence':'PlaybackTiming.csv','playbackFrames':180,'playbackDurationSeconds':3,'playbackFps':60,'performanceEvidence':False,'normalSpeedEvidence':'Reload-Playback.mp4','mp4Sha256':hashlib.sha256((folder/'Reload-Playback.mp4').read_bytes()).hexdigest(),'validation':['Seven wrist targets within 25 mm reach tolerance','Simulation-quarter-progress follows reload tick','Interruption restores seated magazine and support grip','Visual action does not write rounds or completion ticks','LOD part hierarchy retained'],'limits':['Editor hidden capture with offscreen uGUI composition; native player/input/audio acceptance pending','Wrist reach check does not certify palm/finger surface contact at every interpolated frame','Broader FPS and character action library remains in production'],'approval':'Final integrated user playtest/QA gate'}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n')
(folder/'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena rifle action</title><style>body{background:#101c23;color:#e8e9dc;font:18px/1.6 system-ui;margin:32px auto;max-width:1400px;padding:24px}a{color:#7cdad4}video,img{width:100%;height:auto}</style><h1>Authored rifle reload</h1><p>Original Blender animation follows the unchanged 120-tick reload. Intermediate review does not stop production; final integrated playtest remains Sanders' acceptance gate.</p><video controls preload="metadata" src="Reload-Playback.mp4"></video><p>Normal-speed simulation playback, 60 Hz. Fixed capture time is not measured rendering performance.</p><img src="Reload-Frame-Strip.png" alt="Seven measured action poses"><p><a href="Carbine-Reload-v1.blend">Editable Blender action master</a> · <a href="ReloadAction.json">Evaluated track and construction pivots</a> · <a href="review-evidence.json">Evidence and limitations</a> · <a href="../PRODUCTION-STATUS.md">Full remaining production scope</a></p></html>''',encoding='utf-8')
print('RIFLE_ACTION_REVIEW_VERIFIED',complete_tick-trigger_tick,'reload ticks; 180 playback frames; active samples',len(active))
