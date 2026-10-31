"""Package installed-controller crouch playback and scoped native validation evidence."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont

root=Path(__file__).resolve().parents[1]
folder=root/'ArtSource/Original/ArtPass-v1/Animation/Crouch'
raw=root/'Validation/CrouchInstalledPlayback'
source=json.loads((folder/'crouch-production.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/source['master']).read_bytes()).hexdigest()==source['masterSha256']
results=ET.parse(root/'Validation/motion-crouch-final-focused.xml').getroot()
assert results.attrib['result']=='Passed' and int(results.attrib['total'])==3
rows=list(csv.DictReader((raw/'Playback.csv').open(newline='')))
poses=list(csv.DictReader((root/'Validation/CrouchIntegration/Poses.csv').open(newline='')))
lifecycle=list(csv.DictReader((root/'Validation/CrouchIntegration/Lifecycle.csv').open(newline='')))
frames=sorted(raw.glob('frame-*.png'))
assert len(frames)==600 and len(rows)==2400 and len(poses)==544 and len(lifecycle)==4
assert {int(row['characterId']) for row in rows}=={0,1,2,3}
assert all(float(row['controllerHeight'])==1.2 for row in rows)
assert not (raw/'Failures.txt').read_text().strip()
assert not (root/'Validation/CrouchIntegration/Failures.txt').read_text().strip()
for index,frame in enumerate(frames):assert frame.name==f'frame-{index:04d}.png'
flags=subprocess.CREATE_NO_WINDOW if hasattr(subprocess,'CREATE_NO_WINDOW') else 0
command=['ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(raw/'frame-%04d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'Crouch-Playback.mp4')]
process=subprocess.Popen(command,stdout=subprocess.PIPE,stderr=subprocess.PIPE,creationflags=flags)
(root/'Validation/motion-crouch-ffmpeg.pid').write_text(str(process.pid))
try:
    _,errors=process.communicate(timeout=120)
    assert process.returncode==0,errors.decode(errors='replace')
finally:
    if process.poll() is None:process.kill();process.wait()
probe=subprocess.run(['ffprobe','-v','error','-show_streams','-of','json',str(folder/'Crouch-Playback.mp4')],capture_output=True,check=True,timeout=20,creationflags=flags)
video=next(stream for stream in json.loads(probe.stdout)['streams'] if stream['codec_type']=='video')
assert int(video['nb_frames'])==600 and video['r_frame_rate']=='60/1' and abs(float(video['duration'])-10)<.01
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23)
poster=Image.new('RGB',(1600,1515),'#102029');draw=ImageDraw.Draw(poster)
draw.text((25,16),'RAIL ARENA / INSTALLED GROUNDED CROUCH / FOUR NATIVE CHARACTERS',font=font,fill='white')
names=['CrouchIdle','CrouchForward','CrouchBackward','CrouchLeft','CrouchRight']
for index,name in enumerate(names):
    x,y=(index%2)*800,70+(index//2)*470
    with Image.open(raw/f'frame-{index*120:04d}.png') as image:poster.paste(image.convert('RGB').resize((800,450)),(x,y+20))
    draw.text((x+12,y),name,font=font,fill='#b4d9d2')
poster.save(folder/'Crouch-Installed-Poster.png')
for original,name in [(raw/'Playback.csv','Installed-Playback.csv'),(root/'Validation/CrouchIntegration/Poses.csv','Integrated-Poses.csv'),(root/'Validation/CrouchIntegration/Lifecycle.csv','Lifecycle.csv')]:shutil.copy2(original,folder/name)
repeatability=json.loads((root/'Validation/motion-crouch-graph-repeatability.json').read_text())
record={'scope':'Five grounded motions sampled through the installed art controller at their native cadence, with native foot IK and live rifle grip solve. Manual 60 Hz pose staging; simulation held for geometry inspection. Not native input/audio, moving-world foot lock, complete interruption coverage or Windows player acceptance.',
    'sourceMasterSha256':source['masterSha256'],'controllerSha256':repeatability['controllerSha256'],'graphPreparationByteStable':repeatability['identical'],
    'focusedResults':'Validation/motion-crouch-final-focused.xml','focusedTestsPassed':3,'installedFrames':600,'installedCharacterSamples':2400,'directionalPoseSamples':544,
    'minimumInstalledSkinYMetres':min(float(row['minimumSkinY']) for row in rows),'maximumInstalledSkinYMetres':max(float(row['maximumSkinY']) for row in rows),
    'minimumDirectionalSkinYMetres':min(float(row['minimumSkinY']) for row in poses),'maximumDirectionalFloorHeightMetres':max(float(row['minimumSkinY']) for row in poses),
    'maximumWristErrorMetres':max(float(row[field]) for row in rows+poses for field in ('leftWristError','rightWristError')),
    'lifecycle':'Immediate stance pose/capsule synchronization, jump and slide fallbacks, unchanged slide ticks, death/respawn, restored legacy lowering and unchanged hitbox collider definitions/attachments for four characters.',
    'nativeFootIK':'Required for blended diagonal crouch contacts. Isolated source clips passed with IK off; the first live blend review failed floor bounds. Native state foot IK resolves the tested 16 directions without moving actor roots.',
    'installedClips':names,'referenceOnlyClips':['CrouchEnter','CrouchExit'],'transitionPolicy':'Preserve the existing immediate crouch/stand response. Full-body enter/exit references are not installed; a cosmetic transition solution that preserves hitbox timing remains open.',
    'video':{'path':'Crouch-Playback.mp4','width':int(video['width']),'height':int(video['height']),'fps':60,'frames':600,'seconds':10,'audio':False},
    'windowsBuild':'Review 16 remains the latest packaged build; next integration package pending','finalAcceptance':'Sanders native input/sound/artistic acceptance pending'}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print('CROUCH_INSTALLED_REVIEW_PACKAGED',record['minimumInstalledSkinYMetres'],record['maximumWristErrorMetres'])
