"""Package measured reload hand references and native silent motion evidence."""
import gzip,hashlib,json,shutil,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
from html.parser import HTMLParser
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1';source=root/'Validation/ReloadHandsPlayback-01';spec=json.loads((folder/'ReloadHandAction.json').read_text(encoding='utf-8'));views=json.loads((folder/'measured-views.json').read_text(encoding='utf-8'));proof=json.loads((root/'Validation/reload-hands-saved-master.json').read_text(encoding='utf-8'));review=json.loads((source/'review.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest()==spec['masterSha256']==proof['masterSha256']==review['masterSha256']==views['masterSha256'];assert proof['passed'] and review['passed']
cases=[c for c in ET.parse(root/'Validation/reload-hands-native.xml').getroot().iter('test-case') if c.attrib.get('fullname','').endswith('AuthoredFingersFollowMagazineAndRestore')];assert len(cases)==1 and cases[0].attrib['result']=='Passed' and spec['masterSha256'] in ''.join(cases[0].itertext())
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',28);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
for pose in views['poses']:
 directory=folder/'References'/pose['label'];sheet=Image.new('RGB',(3584,2350),'#14252c');draw=ImageDraw.Draw(sheet);draw.text((24,18),'RAIL ARENA / RELOAD HANDS v1 / '+pose['label']+f" / {pose['frame']}/120",font=font,fill='white');draw.text((24,60),'Metres / 54-bone compatible rig / unchanged two-second wrist and magazine action',font=small,fill='#a7ddd2')
 for index,label in enumerate(pose['views']):
  x=index%3*1024;y=115+index//3*1024
  with Image.open(directory/(label+'.png')) as image:assert image.size==(1024,1024);sheet.paste(image.convert('RGB'),(x,y))
  draw.text((x+18,y+16),label.upper(),font=font,fill='white');draw.line((x+24,y+975,x+24+.5*1024/views['orthographicSpanMetres'],y+975),fill='#83e0cd',width=4);draw.text((x+24,y+932),'.5 metre',font=small,fill='white')
 scale=1024/views['orthographicSpanMetres'];x=3328;base=2070;top=base-1.8*scale;radius=.09*scale;draw.ellipse((x-radius,top,x+radius,top+2*radius),outline='#a7ddd2',width=4);neck=top+.22*scale;hip=top+.95*scale;draw.line((x,neck,x,hip),fill='#a7ddd2',width=5);draw.line((x-.2*scale,neck+.1*scale,x+.2*scale,neck+.1*scale),fill='#a7ddd2',width=5)
 for sign in [-1,1]:draw.line((x,hip,x+sign*.13*scale,base),fill='#a7ddd2',width=5)
 draw.text((3090,2180),'1.8 m human',font=small,fill='white');draw.text((3090,2220),'scale envelope',font=small,fill='white');draw.text((3090,2260),'schematic only',font=small,fill='#a7ddd2');draw.text((24,2184),f"All views use {views['orthographicSpanMetres']:.2f} m span. Technical geometry reference; native Unity images govern cel shading.",font=small,fill='white');draw.text((24,2230),'Source SHA-256: '+spec['masterSha256'],font=small,fill='white');sheet.save(directory/'measured-sheet.png')
flags={'creationflags':subprocess.CREATE_NO_WINDOW} if hasattr(subprocess,'CREATE_NO_WINDOW') else {}
clips=[]
for stage,label in [('before','Before'),('after-Player','After')]:
 path=folder/('Reload-'+label+'.mp4')
 if not path.exists():subprocess.run(['ffmpeg','-v','error','-threads','4','-framerate','60','-i',str(source/stage/'frame-%03d.png'),'-frames:v','121','-c:v','libx264','-threads','4','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(path)],check=True,capture_output=True,timeout=120,**flags)
 probe=json.loads(subprocess.run(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,avg_frame_rate,duration,nb_frames','-of','json',str(path)],check=True,capture_output=True,timeout=30,**flags).stdout)['streams'][0];assert (probe['width'],probe['height'],probe['avg_frame_rate'],probe['nb_frames'])==(1280,720,'60/1','121');clips.append({'path':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),**probe})
path=folder/'Reload-Before-After.mp4'
if not path.exists():subprocess.run(['ffmpeg','-v','error','-i',str(folder/'Reload-Before.mp4'),'-i',str(folder/'Reload-After.mp4'),'-filter_complex','hstack=inputs=2','-c:v','libx264','-threads','4','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(path)],check=True,capture_output=True,timeout=120,**flags)
comparison=Image.new('RGB',(2560,3080),'#14252c');draw=ImageDraw.Draw(comparison)
for row,frame in enumerate([15,21,65,98]):
 for column,(stage,label) in enumerate([('before','BEFORE / fixed support fingers'),('after-Player','AFTER / authored release and grasp')]):
  x=column*1280;y=row*770;draw.text((x+18,y+12),label+f' / frame {frame}',font=small,fill='white')
  with Image.open(source/stage/f'contact-{frame:03}.png') as image:comparison.paste(image.convert('RGB'),(x,y+50))
comparison.save(folder/'Reload-Contact-Comparison.png');native=folder/'NativeReview';native.mkdir(exist_ok=True)
for directory in sorted(source.iterdir()):
 if directory.is_dir():
  for image in directory.glob('*.png'):
   if image.name.startswith('contact-') or image.name in [f'frame-{f:03}.png' for f in [0,15,21,65,98,120]]:
    target=native/directory.name/image.name;target.parent.mkdir(exist_ok=True);shutil.copy2(image,target)
context=folder/'Source';context.mkdir(exist_ok=True);calibration=(root/spec['nativeCalibration']).read_bytes();calibration=gzip.decompress(calibration) if spec['nativeCalibration'].endswith('.gz') else calibration;assert hashlib.sha256(calibration).hexdigest()==spec['nativeCalibrationSha256'];(context/'reload-rig.json.gz').write_bytes(gzip.compress(calibration,mtime=0))
records={**review,'clips':clips,'comparisonClip':path.name,'comparisonClipSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'nativeResult':'Validation/reload-hands-native.xml','savedMasterResult':'Validation/reload-hands-saved-master.json','samples':'Validation/ReloadHandsPlayback-01/samples.csv','timing':'121 inclusive boundary samples at 60 Hz cover the authoritative two-second action. Encoded clips include the final boundary frame (2.0167 s). Silent review, not frame-pacing measurement.','remaining':spec['limits']}
(folder/'review-evidence.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena reload hands</title><style>body{max-width:1650px;margin:30px auto;padding:24px;background:#14252c;color:#eef1df;font:18px/1.6 system-ui}a{color:#83e0cd}img,video{width:100%;height:auto}</style><h1>Rail Arena / authored reload hand contact</h1><p>Release, magazine grasp, extraction, seating/tap and return poses on the compatible measured rig. Original two-second wrist/magazine track, recoil, simulation and right-hand grip are preserved. This is one action family within the unfinished full art pass.</p><p><a href="Carbine-Reload-Hands-v1.blend">Editable Blender master</a> · <a href="ReloadHandAction.json">Sampled finger action and provenance</a> · <a href="measured-views.json">Dimensions and axes</a> · <a href="review-evidence.json">Native/saved-master checks and limits</a></p><h2>Normal first-person camera / native before and after</h2><video controls preload="metadata" src="Reload-Before-After.mp4"></video><p>Both stages retain camera, geometry and wrist/magazine track. Clouds and ambient effects continue in staged time. 121 inclusive boundary samples at 60 Hz; the two-second action reel includes its final frame. Silent review, not native input/audio or performance acceptance.</p><h2>Native contact inspection / same camera per beat</h2><img src="Reload-Contact-Comparison.png"><p>Four appearance skins pass 484 corrected first-person poses; 28 third-person contact poses and interrupted/reset/death-hand checks pass. All 12,100 sleeve-joint samples include the 121-pose before stage. Finger tip-ring centres target 9 mm surface clearance; this is not a full palm/finger surface collision certificate. Fine fit, native sound/input and final artistic acceptance remain open.</p>'''
for pose in views['poses']:html+=f'<h2>{pose["label"]} / {pose["frame"]}/120</h2><img loading="lazy" src="References/{pose["label"]}/measured-sheet.png"><img loading="lazy" src="References/{pose["label"]}/native-eye-workbench.png">'
html+='</html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ['href','src']:assert (folder/value).is_file(),value
Links().feed(html);(folder/'index.html').write_text(html,encoding='utf-8');print('RELOAD_HAND_PACKAGE_VERIFIED 6 sheets / 36 orthographics / 6 eye views / native motion and contact comparison')
