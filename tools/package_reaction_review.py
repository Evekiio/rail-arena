"""Package authored reaction references and measured live lifecycle evidence."""
import csv
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Reactions'
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--raw',type=Path,default=root/'Validation/ReactionPlayback-Acceptance')
parser.add_argument('--legacy-raw',type=Path,default=root/'Validation/ReactionLegacyReference')
parser.add_argument('--lifecycle-results',type=Path,default=root/'Validation/motion-reactions-lifecycle.xml')
parser.add_argument('--reference-results',type=Path,default=root/'Validation/motion-reactions-legacy-reference.xml')
args=parser.parse_args();raw=args.raw.resolve();legacy=args.legacy_raw.resolve()
for path in (args.lifecycle_results,args.reference_results):
    report=ET.parse(path).getroot();assert report.attrib['result']=='Passed' and report.attrib['total']=='1',path
spec=json.loads((folder/'reactions-production.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']
measured=json.loads((folder/'measured-pose-review.json').read_text(encoding='utf-8'));assert measured['masterSha256']==spec['masterSha256']
rows=list(csv.DictReader((raw/'Playback.csv').open()));before=list(csv.DictReader((legacy/'Playback.csv').open()))
assert len(rows)==1200 and len(before)==480
for frame in range(300):assert {int(row['characterId']) for row in rows if int(row['frame'])==frame}=={0,1,2,3}
dead=[row for row in rows if 120<=int(row['frame'])<240];settled=[row for row in dead if int(row['frame'])>=210]
assert min(float(row['minimumSkinY']) for row in rows)>=-.01
assert max(float(row['minimumSkinY']) for row in settled)<=.06
assert all(row['reactionWeight']=='0.000000' and row['handsEnabled']=='False' and row['weaponVisible']=='False' for row in dead)
profiles=list(csv.DictReader((raw/'ContactProfile.csv').open()));assert len(profiles)==4
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
for clip in measured['clips']:
    directory=folder/'References'/clip['clip'];sheet=Image.new('RGB',(2400,2660),'#13232c');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),clip['clip']+' / ANIM-REACTIONS-v1 / identical 2.50 m orthographic span / 60 Hz',font=font,fill='white')
    for row,view in enumerate(clip['views']):
        for index,frame in enumerate(clip['poseFrames']):
            x=12+index*398;y=65+row*430
            with Image.open(directory/(view+'-'+str(index)+'.png')) as image:sheet.paste(image.convert('RGB').resize((384,384)),(x,y))
            draw.text((x+8,y+6),f'{view} / frame {frame}',font=font,fill='#8ee5dc')
            draw.line((x+15,y+354,x+15+round(384/2.5),y+354),fill='#8ee5dc',width=3);draw.text((x+15,y+320),'1 metre',font=font,fill='#8ee5dc')
    draw.text((20,2625),spec['master']+' / SHA-256 '+spec['masterSha256'][:16]+' / shared character rest references apply',font=font,fill='white');sheet.save(directory/'measured-poses.png')
for source,name,frames in ((raw,'Reaction-Playback.mp4',300),(legacy,'Legacy-Death-Reference.mp4',120)):
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(source/'frame-%04d.png'),'-an','-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p',str(folder/name)],check=True,timeout=120)
    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=nb_frames,r_frame_rate','-of','json',str(folder/name)]))
    assert probe['streams'][0]['nb_frames']==str(frames) and probe['streams'][0]['r_frame_rate']=='60/1'
for name in ('Playback.csv','ContactProfile.csv'):shutil.copy2(raw/name,folder/name)
shutil.copy2(legacy/'Playback.csv',folder/'Legacy-Playback.csv');shutil.copy2(raw/'frame-0210.png',folder/'Reaction-Family-Poster.png')
comparison=Image.new('RGB',(2560,790),'#13232c');draw=ImageDraw.Draw(comparison)
for x,label,source in ((0,'SHARED LEGACY DEATH / prior bone support and grip',legacy/'frame-0090.png'),(1280,'AUTHORED DEATH / sampled skin support and relaxed hands',raw/'frame-0210.png')):
    draw.text((x+20,18),label,font=font,fill='white')
    with Image.open(source) as image:comparison.paste(image.convert('RGB'),(x,70))
comparison.save(folder/'Death-Before-After.png')
record={'sourceMasterSha256':spec['masterSha256'],'frames':300,'fps':60,'durationSeconds':5,'characterSamples':len(rows),'characterIds':[0,1,2,3],
    'minimumSkinY':min(float(row['minimumSkinY']) for row in rows),'maximumSettledContactGap':max(float(row['minimumSkinY']) for row in settled),
    'contactProfiles':profiles,'scope':'Actual imported clips on four refined skinned contenders. Live cosmetic pose/floor methods; whole-skin validation, root preservation, masked hit, hit active before death, repeated death, relaxed hand posing, weapon/hitbox lifecycle and reset at the existing 120-tick respawn interval. Camera render waits for native skinning. No ragdoll or authoritative movement/damage changes.',
    'legacyReference':'Controlled capture of the unchanged shared Death01 state at its existing 1.3 speed, prior six-bone floor support and static grip. Those death assets were also used by Review-13. This is a matching scene-camera source reference, not a native-input recording of the older executable. No active hit was applied in this quiet-death reference.',
    'comparisonCamera':{'position':[3.2,2.3,17.5],'target':[0,.65,12],'orthographicSize':1.65,'resolution':[1280,720],'elapsedDeathSeconds':1.5},
    'performanceLimit':'Contact profiles are 1000 warm terminal-pose Editor calls per character, not player frame/GPU measurements or sustained 60 FPS acceptance.',
    'remaining':['Crouched/airborne/traversal and all interrupted combat-action combinations','Moving-world collision-edge and full LOD contact acceptance','Native input, sound, final artistic acceptance and the wider production plan']}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
spec['integration']='Installed in independent Sky Gardens graph at native state speed 1; legacy clip assets remain unchanged. Cached skin samples support the cosmetic corpse model only.'
(folder/'reactions-production.json').write_text(json.dumps(spec,indent=2)+'\n',encoding='utf-8')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Rail Arena combat reactions</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;max-width:1500px;margin:32px auto;padding:24px}a{color:#8ee5dc}video,img{width:100%;height:auto}</style><h1>Original hit, fall and settle</h1><p>18-frame hit response and 72-frame fall at 60 Hz. The existing damage and two-second respawn rules stay authoritative. Reaction override clears on death; relaxed authored hands replace the live grip until respawn. Cached skin contacts support only the cosmetic model, without ragdolls.</p><video controls preload="metadata" poster="Reaction-Family-Poster.png" src="Reaction-Playback.mp4"></video><p>0-2 s lateral movement with hit responses; 2-4 s death and settle; 4-5 s respawn. Four installed contenders, character IDs 0-3 left to right. Silent, skin-synchronized staged review; native input/audio and final artistic acceptance remain open.</p><h2>Matching controlled death reference</h2><img src="Death-Before-After.png"><video controls preload="metadata" src="Legacy-Death-Reference.mp4"></video><p>The legacy death state and prior floor/grip presentation were shared by Review-13. This is a source reference at the same camera, character scale, lighting and 1.5 s elapsed death time, not an older executable recording. New and legacy states retain their own intended clip speeds.</p><p><a href="Contender-Reactions-v1.blend">Editable Blender master</a> | <a href="reactions-production.json">Construction, rig and timing</a> | <a href="review-evidence.json">Measured lifecycle evidence and limits</a> | <a href="ContactProfile.csv">Editor contact-cost sample</a> | <a href="../../Characters/References/Player/measured-sheet.png">Shared character rest orthographics</a></p>'''
for clip in spec['clips']:html+=f'<h2>{clip["name"]}</h2><img loading="lazy" src="References/{clip["name"]}/measured-poses.png">'
(folder/'index.html').write_text(html+'</html>',encoding='utf-8')
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src'):assert (folder/value).resolve().is_file(),value
Links().feed(html)
print('REACTION_REVIEW_PACKAGED',len(rows),'character samples; minimum skin',record['minimumSkinY'],'settled contact gap',record['maximumSettledContactGap'])
