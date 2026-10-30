"""Package geometry-derived poses and the actual installed, skin-synchronized family reel."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Animation'
raw=root/'Validation/RetreatPlayback'
tests=ET.parse(root/'Validation/motion-retreat-grip-skinning.xml').getroot()
assert tests.attrib['result']=='Passed' and tests.attrib['total']=='1'
rows=list(csv.DictReader((raw/'Playback.csv').open()))
assert len(rows)==2160 and {int(row['frame']) for row in rows}==set(range(540))
for frame in range(540):assert {int(row['characterId']) for row in rows if int(row['frame'])==frame}=={0,1,2,3}
assert max(float(row['leftWristError']) for row in rows)<.025 and max(float(row['rightWristError']) for row in rows)<.025
low=min(float(row['minimumSoleY']) for row in rows);high=max(float(row['minimumSoleY']) for row in rows)
assert low>=-.005 and high<=.035
spec=json.loads((folder/'retreat-production.json').read_text())
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']
spec['integration']='Installed through the optional Sky Gardens art-profile ContenderRetreat override; legacy controller assets are retained.'
(folder/'retreat-production.json').write_text(json.dumps(spec,indent=2)+'\n')
baked=json.loads((folder/'baked-contact-review.json').read_text())
for entry in baked['clips']:
    clip=next(item for item in spec['clips'] if item['name']==entry['clip'])
    entry['poseFrames']=[round(clip['frames']*phase) for phase in (0,.125,.25,.375,.5,.75)]
    entry['posePhases']=[frame/clip['frames'] for frame in entry['poseFrames']]
(folder/'baked-contact-review.json').write_text(json.dumps(baked,indent=2)+'\n')
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
for entry in baked['clips']:
    directory=folder/'References'/entry['clip'];sheet=Image.new('RGB',(2400,940),'#13232c');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),entry['clip']+' / baked original Blender poses / 2.20 m identical view span / metres',font=font,fill='white')
    for row,view in enumerate(('front','side')):
        for index,frame in enumerate(entry['poseFrames']):
            x=12+index*398;y=65+row*430
            with Image.open(directory/(view+'-'+str(index)+'.png')) as image:sheet.paste(image.convert('RGB').resize((384,384)),(x,y))
            draw.text((x+8,y+6),f'{view} / frame {frame}',font=font,fill='#8ee5dc')
            draw.line((x+15,y+354,x+15+round(384/2.2),y+354),fill='#8ee5dc',width=3)
            draw.text((x+15,y+320),'1 metre',font=font,fill='#8ee5dc')
    draw.text((20,910),'Canonical master: Contender-Retreat-v1.blend / shared rest skeleton unchanged / no controller root motion',font=font,fill='white')
    sheet.save(directory/'measured-poses.png')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(raw/'frame-%04d.png'),'-an','-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p',str(folder/'Retreat-Playback.mp4')],check=True,timeout=120)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,height,r_frame_rate,nb_frames','-show_entries','format=duration','-of','json',str(folder/'Retreat-Playback.mp4')]))
assert probe['streams'][0]['nb_frames']=='540' and probe['streams'][0]['r_frame_rate']=='60/1' and abs(float(probe['format']['duration'])-9)<.02
shutil.copy2(raw/'Playback.csv',folder/'Playback.csv');shutil.copy2(raw/'frame-0090.png',folder/'Retreat-Family-Poster.png')
record={'sourceMasterSha256':spec['masterSha256'],'frames':540,'fps':60,'durationSeconds':9,'identities':[0,1,2,3],'samples':len(rows),'minimumSoleHeightMetres':low,'maximumStanceSoleHeightMetres':high,'maximumNumericalWristTargetErrorMetres':max(max(float(row[key]) for row in rows) for key in ('leftWristError','rightWristError')),'scope':'Installed clips at Gait=1, in-place, with live weapon/wrist/finger pose code. Captures wait for native skinning after the IK solve. Fixed capture time is not rendering performance or native-input acceptance. Wrist targets do not certify every palm/finger surface contact.','timeRanges':{'0–3 seconds':'RetreatWalk','3–6 seconds':'RetreatJog','6–9 seconds':'RetreatRun'},'remaining':['Moving-world speed/stance transitions and native playtest','Jump/landing replacement and broader full-action library']}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena authored retreat library</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;margin:32px auto;max-width:1500px;padding:24px}a{color:#8ee5dc}video,img{width:100%;height:auto}</style><h1>Original retreat locomotion</h1><p>Dedicated backward walk, jog and run replace the reversed forward clips in the Sky Gardens art profile. Gameplay movement and root authority remain unchanged.</p><video controls preload="metadata" poster="Retreat-Family-Poster.png" src="Retreat-Playback.mp4"></video><p>0–3 s walk; 3–6 s jog; 6–9 s run. Left to right: Plum, Teal, Ember, Ivory. Native clip rate at Gait=1; in-place reference, not a world-motion or performance benchmark.</p><p><a href="Contender-Retreat-v1.blend">Editable Blender action master</a> · <a href="retreat-production.json">Construction and timing</a> · <a href="review-evidence.json">Installed contact evidence and limits</a></p>'''
for name in ('RetreatWalk','RetreatJog','RetreatRun'):html+=f'<h2>{name}</h2><img loading="lazy" src="References/{name}/measured-poses.png">'
(folder/'index.html').write_text(html+'</html>',encoding='utf-8')
print('RETREAT_REVIEW_PACKAGED 540 frames / 4 identities / sole range',low,high)
