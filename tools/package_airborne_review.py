"""Package native-rate airborne clips and controller-driven motion evidence after focused review."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parent.parent
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Airborne'
for filename,count in [('motion-airborne-installed.xml',1),('motion-gaits-transitions.xml',2)]:
    result=ET.parse(root/'Validation'/filename).getroot()
    assert result.attrib['result']=='Passed' and int(result.attrib['total'])==count,filename
spec=json.loads((folder/'airborne-production.json').read_text())
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']
measurements=json.loads((folder/'measured-pose-review.json').read_text());assert measurements['masterSha256']==spec['masterSha256']
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
for clip in measurements['clips']:
    directory=folder/'References'/clip['clip'];sheet=Image.new('RGB',(2400,940),'#13232c');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),clip['clip']+' / canonical baked poses / 2.20 metre shared view span / 60 Hz',font=font,fill='white')
    for row,view in enumerate(('front','side')):
        for index,frame in enumerate(clip['poseFrames']):
            x=12+index*398;y=65+row*430
            with Image.open(directory/(view+'-'+str(index)+'.png')) as image:sheet.paste(image.convert('RGB').resize((384,384)),(x,y))
            draw.text((x+8,y+6),f'{view} / frame {frame} / {frame/60:.2f} s',font=font,fill='#8ee5dc')
            draw.line((x+15,y+354,x+15+round(384/2.2),y+354),fill='#8ee5dc',width=3);draw.text((x+15,y+320),'1 metre',font=font,fill='#8ee5dc')
    draw.text((20,910),'Contender-Airborne-v1.blend / shared rest skeleton / controller owns all root translation and gravity',font=font,fill='white')
    sheet.save(directory/'measured-poses.png')

rows=list(csv.DictReader((root/'Validation/AirbornePlayback/Playback.csv').open()))
world=list(csv.DictReader((root/'Validation/AirborneTransitions/Playback.csv').open()))
assert len(rows)==2160 and len(world)==360
summary={}
for name in ('Takeoff','AirborneHold','LandingRecover'):
    selected=[row for row in rows if row['clip']==name];assert len(selected)==720
    assert {int(row['characterId']) for row in selected}=={0,1,2,3}
    low=min(float(row['minimumSoleY']) for row in selected);high=max(float(row['minimumSoleY']) for row in selected)
    wrist=max(float(row[key]) for row in selected for key in ('leftWristError','rightWristError'))
    assert low>-.005 and wrist<.025
    if name=='LandingRecover':assert high<.035
    if name=='AirborneHold':assert low>.04
    summary[name]={'samples':len(selected),'minimumSoleY':low,'maximumSoleY':high,'maximumNumericalWristTargetErrorMetres':wrist}
transitions={}
for name in ('standing','forward','retreat'):
    selected=[row for row in world if row['scenario']==name];assert len(selected)==120
    states={row['state'] for row in selected};assert {'Jump','Air','Land','Locomotion'}.issubset(states)
    transitions[name]={'frames':len(selected),'states':sorted(states),'rootYRangeMetres':[min(float(row['rootY']) for row in selected),max(float(row['rootY']) for row in selected)],'minimumSoleY':min(float(row['minimumSoleY']) for row in selected),'airborneStateMinimumSoleY':min(float(row['minimumSoleY']) for row in selected if row['state']!='Locomotion'),'authoredGaitMinimumSoleY':min(float(row['minimumSoleY']) for row in selected if row['state']=='Locomotion')}
for raw,destination,frames,width in [('AirbornePlayback','Airborne-Playback.mp4',540,1920),('AirborneTransitions','Controller-Jumps.mp4',360,1280)]:
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(root/'Validation'/raw/'frame-%04d.png'),'-an','-c:v','libx264','-threads','4','-preset','fast','-crf','20','-pix_fmt','yuv420p',str(folder/destination)],check=True,timeout=120)
    probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_entries','stream=width,r_frame_rate,nb_frames','-of','json',str(folder/destination)]))['streams'][0]
    assert int(probe['nb_frames'])==frames and probe['r_frame_rate']=='60/1' and int(probe['width'])==width
shutil.copy2(root/'Validation/AirbornePlayback/Playback.csv',folder/'Pose-Playback.csv')
shutil.copy2(root/'Validation/AirborneTransitions/Playback.csv',folder/'Controller-Playback.csv')
shutil.copy2(root/'Validation/AirbornePlayback/frame-0375.png',folder/'Airborne-Family-Poster.png')
shutil.copy2(root/'Validation/AirborneTransitions/frame-0035.png',folder/'Controller-Jump-Poster.png')
record={'masterSha256':spec['masterSha256'],'fps':60,'familyFrames':540,'controllerFrames':360,'poseSamples':len(rows),'clips':summary,'controllerScenarios':transitions,'scope':'Original native-rate clips on four installed identities with live rifle/hand solve; controller-driven standing, forward and retreat jumps use ArenaActor.Move and the live presentation parameter update. Collision-deck reference staging, not a full competitive match or native-input/sound acceptance. Wrist targets do not certify all palm/finger surfaces.','remaining':['Crouch/slide and jump-pad/portal combinations','Native input/audio artistic playtest','Broader full-action library and inventory references']}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n')
spec['integration']='Installed through independent Art/ContenderMotion state graph and the Sky Gardens override; native clip speed 1, no root motion, legacy map controller retained.'
(folder/'airborne-production.json').write_text(json.dumps(spec,indent=2)+'\n')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena authored airborne motion</title><style>body{background:#13232c;color:#edf0df;font:18px/1.6 system-ui;margin:32px auto;max-width:1500px;padding:24px}a{color:#8ee5dc}video,img{width:100%;height:auto}</style><h1>Original takeoff, suspension and landing</h1><p>Three original actions on the shared contender rig. Native speed 1; the existing controller simulation owns jump height, gravity and contacts.</p><h2>Controller-driven motion</h2><video controls preload="metadata" poster="Controller-Jump-Poster.png" src="Controller-Jumps.mp4"></video><p>Standing, forward and retreat jumps, two seconds each. Actual CharacterController collision and live animation-state transitions on an isolated collision deck. Capture is fixed at 60 Hz; not a performance benchmark or native input acceptance.</p><h2>All four identities / contact and suspension</h2><video controls preload="metadata" poster="Airborne-Family-Poster.png" src="Airborne-Playback.mp4"></video><p>0–3 s takeoff; 3–6 s airborne hold; 6–9 s landing recovery. Non-looping actions hold their last pose between demonstrations. Left to right: Plum, Teal, Ember, Ivory. Root stays fixed in this pose review.</p><p><a href="Contender-Airborne-v1.blend">Editable master</a> · <a href="airborne-production.json">Timing and provenance</a> · <a href="review-evidence.json">Evidence and limits</a> · <a href="../../Characters/References/Player/measured-sheet.png">Shared mesh orthographics</a></p>'''
for name in ('Takeoff','AirborneHold','LandingRecover'):html+=f'<h2>{name}</h2><img loading="lazy" src="References/{name}/measured-poses.png">'
html+='</html>'
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src','poster'):assert (folder/value).resolve().is_file(),value
Links().feed(html);(folder/'index.html').write_text(html,encoding='utf-8')
print('AIRBORNE_REVIEW_PACKAGED 900 frames / 2160 family samples / 3 simulated jump scenarios')
