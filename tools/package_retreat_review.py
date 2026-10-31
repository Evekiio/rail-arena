"""Package geometry-derived poses and the actual installed, skin-synchronized family reel."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Animation'
forward='--forward' in sys.argv
strafe='--strafe' in sys.argv
prefix='Strafe' if strafe else 'Advance' if forward else 'Retreat'
if forward:folder=folder/'Forward'
if strafe:folder=folder/'Strafe'
raw=root/('Validation/StrafePlayback' if strafe else 'Validation/AdvancePlayback' if forward else 'Validation/RetreatPlayback')
frames=360 if strafe else 540
tests=ET.parse(root/('Validation/motion-strafe-installed.xml' if strafe else 'Validation/motion-gaits-transitions.xml' if forward else 'Validation/motion-retreat-grip-skinning.xml')).getroot()
assert tests.attrib['result']=='Passed' and tests.attrib['total']==('2' if forward or strafe else '1')
rows=list(csv.DictReader((raw/'Playback.csv').open()))
assert len(rows)==frames*4 and {int(row['frame']) for row in rows}==set(range(frames))
for frame in range(frames):assert {int(row['characterId']) for row in rows if int(row['frame'])==frame}=={0,1,2,3}
assert max(float(row['leftWristError']) for row in rows)<.025 and max(float(row['rightWristError']) for row in rows)<.025
low=min(float(row['minimumSoleY']) for row in rows);high=max(float(row['minimumSoleY']) for row in rows)
assert low>=-.005 and high<=.035
spec_path=folder/('strafe-production.json' if strafe else 'forward-production.json' if forward else 'retreat-production.json')
spec=json.loads(spec_path.read_text())
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']
spec['integration']='Installed through the optional Sky Gardens art-profile ContenderRetreat override; legacy controller assets are retained.'
spec_path.write_text(json.dumps(spec,indent=2)+'\n')
baked=json.loads((folder/'baked-contact-review.json').read_text())
for entry in baked['clips']:
    clip=next(item for item in spec['clips'] if item['name']==entry['clip'])
    entry['poseFrames']=[round(clip['frames']*phase) for phase in (0,.125,.25,.375,.5,.75)]
    entry['posePhases']=[frame/clip['frames'] for frame in entry['poseFrames']]
(folder/'baked-contact-review.json').write_text(json.dumps(baked,indent=2)+'\n')
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
for entry in baked['clips']:
    directory=folder/'References'/entry['clip'];height=1370 if strafe else 940;sheet=Image.new('RGB',(2400,height),'#13232c');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),entry['clip']+' / baked original Blender poses / 2.20 m identical view span / metres',font=font,fill='white')
    for row,view in enumerate(('front','side','top') if strafe else ('front','side')):
        for index,frame in enumerate(entry['poseFrames']):
            x=12+index*398;y=65+row*430
            with Image.open(directory/(view+'-'+str(index)+'.png')) as image:sheet.paste(image.convert('RGB').resize((384,384)),(x,y))
            draw.text((x+8,y+6),f'{view} / frame {frame}',font=font,fill='#8ee5dc')
            draw.line((x+15,y+354,x+15+round(384/2.2),y+354),fill='#8ee5dc',width=3)
            draw.text((x+15,y+320),'1 metre',font=font,fill='#8ee5dc')
    draw.text((20,height-30),'Canonical master: '+spec['master']+' / shared rest skeleton unchanged / no controller root motion',font=font,fill='white')
    sheet.save(directory/'measured-poses.png')
subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(raw/'frame-%04d.png'),'-an','-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p',str(folder/(prefix+'-Playback.mp4'))],check=True,timeout=120)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,height,r_frame_rate,nb_frames','-show_entries','format=duration','-of','json',str(folder/(prefix+'-Playback.mp4'))]))
assert probe['streams'][0]['nb_frames']==str(frames) and probe['streams'][0]['r_frame_rate']=='60/1' and abs(float(probe['format']['duration'])-frames/60)<.02
shutil.copy2(raw/'Playback.csv',folder/'Playback.csv');shutil.copy2(raw/'frame-0090.png',folder/(prefix+'-Family-Poster.png'))
record={'sourceMasterSha256':spec['masterSha256'],'frames':540,'fps':60,'durationSeconds':9,'identities':[0,1,2,3],'samples':len(rows),'minimumSoleHeightMetres':low,'maximumStanceSoleHeightMetres':high,'maximumNumericalWristTargetErrorMetres':max(max(float(row[key]) for row in rows) for key in ('leftWristError','rightWristError')),'scope':'Installed clips at Gait=1, in-place, with live weapon/wrist/finger pose code. Captures wait for native skinning after the IK solve. Fixed capture time is not rendering performance or native-input acceptance. Wrist targets do not certify every palm/finger surface contact.','timeRanges':{'0–3 seconds':'RetreatWalk','3–6 seconds':'RetreatJog','6–9 seconds':'RetreatRun'},'remaining':['Moving-world speed/stance transitions and native playtest','Jump/landing replacement and broader full-action library']}
record['timeRanges']={'0–3 seconds':prefix+'Walk','3–6 seconds':prefix+'Jog','6–9 seconds':prefix+'Run'}
record['remaining']=['Full movement/stance combinations and native playtest','Broader full-action library; current forward/airborne evidence is linked by the production matrix']
if strafe:
    blend_rows=list(csv.DictReader((raw/'BlendChecks.csv').open()))
    assert len(blend_rows)==256 and min(float(row['soleY']) for row in blend_rows)>=-.005
    record.update({'frames':frames,'durationSeconds':frames/60,'samples':len(rows),'blendSamples':len(blend_rows),'handednessValidated':True,'legacyRestorationValidated':True,
        'scope':'Installed authored cardinal clips at native state/child speed 1, in-place with live weapon/wrist/finger code. Additional sampled diagonal/crouched/yaw poses check skin contact and unchanged actor root/velocity/controller height. Native skinning completes before capture. This is not moving-world foot-lock, native input/audio or performance acceptance.',
        'timeRanges':{f'{index}-{index+1} seconds':clip['name'] for index,clip in enumerate(spec['clips'])}})
    shutil.copy2(raw/'BlendChecks.csv',folder/'BlendChecks.csv')
    transitions=ET.parse(root/'Validation/motion-strafe-airborne.xml').getroot()
    assert transitions.attrib['result']=='Passed' and transitions.attrib['total']=='1'
    transition_raw=root/'Validation/StrafeAirborneTransitions'
    transition_rows=list(csv.DictReader((transition_raw/'Playback.csv').open()))
    assert len(transition_rows)==360
    states={name:sorted({row['state'] for row in transition_rows if row['scenario']==name}) for name in ('standing','forward','retreat')}
    assert all(set(names)=={'Locomotion','Jump','Air','Land'} for names in states.values())
    record['controllerDrivenTransitions']={'frames':360,'scenarios':states,'minimumSoleHeightMetres':min(float(row['minimumSoleY']) for row in transition_rows),'scope':'Existing discrete jump physics and actual controller state transitions; not lateral jump or full foot-lock certification.'}
    shutil.copy2(transition_raw/'Playback.csv',folder/'Controller-Transitions.csv')
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(transition_raw/'frame-%04d.png'),'-an','-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p',str(folder/'Controller-Jumps.mp4')],check=True,timeout=120)
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena authored retreat library</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;margin:32px auto;max-width:1500px;padding:24px}a{color:#8ee5dc}video,img{width:100%;height:auto}</style><h1>Original retreat locomotion</h1><p>Dedicated backward walk, jog and run replace the reversed forward clips in the Sky Gardens art profile. Gameplay movement and root authority remain unchanged.</p><video controls preload="metadata" poster="Retreat-Family-Poster.png" src="Retreat-Playback.mp4"></video><p>0–3 s walk; 3–6 s jog; 6–9 s run. Left to right: Plum, Teal, Ember, Ivory. Native clip rate at Gait=1; in-place reference, not a world-motion or performance benchmark.</p><p><a href="Contender-Retreat-v1.blend">Editable Blender action master</a> · <a href="retreat-production.json">Construction and timing</a> · <a href="review-evidence.json">Installed contact evidence and limits</a></p>'''
if forward:html=html.replace('Retreat','Advance').replace('retreat','forward').replace('backward','forward').replace('reversed forward clips','legacy forward clips')
if strafe:
    html='''<!doctype html><html lang="en"><meta charset="utf-8"><title>Rail Arena lateral locomotion</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;max-width:1500px;margin:32px auto;padding:24px}a{color:#8ee5dc}video,img{width:100%;height:auto}</style><h1>Original lateral locomotion</h1><p>Six native-rate left/right walk, jog and run clips feed the Sky Gardens directional velocity blend. The controller and legacy maps remain authoritative. The existing crouch solve is retained; a full authored crouch/transition library remains production work.</p><video controls preload="metadata" poster="Strafe-Family-Poster.png" src="Strafe-Playback.mp4"></video><p>0-1 s left walk, 1-2 s left jog, 2-3 s left run, 3-4 s right walk, 4-5 s right jog, 5-6 s right run. Left to right: Plum, Teal, Ember, Ivory. In-place 60 Hz playback with native skinning, not world foot-lock or native input/audio acceptance.</p><p><a href="Contender-Strafe-v1.blend">Editable Blender master</a> | <a href="strafe-production.json">Construction and timing</a> | <a href="review-evidence.json">Installed evidence and limits</a> | <a href="BlendChecks.csv">Sampled diagonal/crouched/yaw contacts</a></p>'''
for name in ([clip['name'] for clip in spec['clips']] if strafe else (prefix+'Walk',prefix+'Jog',prefix+'Run')):html+=f'<h2>{name}</h2><img loading="lazy" src="References/{name}/measured-poses.png">'
if strafe:html+='<h2>Actual controller jump and recovery</h2><video controls preload="metadata" src="Controller-Jumps.mp4"></video><p>0-2 s standing; 2-4 s forward; 4-6 s retreat. The existing discrete simulation drives the root; the installed graph must visit Locomotion, Jump, Air and Land in each case.</p>'
(folder/'index.html').write_text(html+'</html>',encoding='utf-8')
print('MOTION_REVIEW_PACKAGED',frames,'frames / 4 identities / sole range',low,high)
