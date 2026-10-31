"""Join matched native nature views and the bounded fixed-step wind reel."""
from pathlib import Path
import csv,hashlib,json,subprocess
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1];folder=root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));native=json.loads((folder/'UnityReview/installed-review.json').read_text());imported=json.loads((root/'Validation/garden-nature-import.json').read_text())
for report in (native,imported):assert report['masterSha256']==spec['masterSha256'] and report['sourceSha256']==spec['sourceSha256']
assert native['physicsPreserved'] and native['colliderDefinitionsPreserved'] and native['navigationPreserved'] and native['devicesPreserved'] and native['cleanupPassed']
assert native['placements']==24 and native['canonicalModels']==33 and native['motionFrames']==240
views=native['views'];pairs=[];cards=[];font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',25)
for before in [view for view in views if view['legacy']]:
 after=next(view for view in views if view['name']==before['name'] and not view['legacy'])
 for key in ('position','euler','fov','lod'):assert before[key]==after[key],(before['name'],key)
 output=Image.new('RGB',(2560,820),'#102029');draw=ImageDraw.Draw(output)
 draw.text((20,12),'BEFORE / prior nature recreation from unchanged Review 20 assets',font=font,fill='#eef0df');draw.text((1300,12),'AFTER / authored nature installed in the same Editor scene',font=font,fill='#eef0df')
 paths=[]
 for index,phase in enumerate(('before','after')):
  path=folder/'UnityReview'/(before['name']+'-'+phase+'.png');paths.append(path)
  with Image.open(path) as image:output.paste(image.convert('RGB').resize((1280,720)),(index*1280,55))
 draw.text((20,783),'Same camera pose/FOV; resting wind; overlay HUD excluded. Native input/audio and final artistic acceptance remain open.',font=font,fill='#b1d5ce')
 destination=folder/(before['name']+'-comparison.png');output.save(destination)
 pairs.append({'view':before['name'],'position':before['position'],'euler':before['euler'],'fieldOfView':before['fov'],'comparison':destination.relative_to(root).as_posix(),'sourceImages':[{'path':path.relative_to(root).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths]})
 cards.append('<h3>'+before['name']+'</h3><img src="'+destination.name+'">')
movie=folder/'Garden-Wind.mp4';probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,nb_frames,r_frame_rate,duration','-of','json',str(movie)],text=True))['streams'][0]
assert int(probe['nb_frames'])==240 and float(probe['duration'])==8 and probe['width']==1280 and probe['height']==720
motion=root/'Validation/GardenNature-Wind-02/motion.csv';rows=list(csv.DictReader(motion.open()));assert len(rows)==240 and len({row['matchTick'] for row in rows})==1
assert max(float(row['maxPositionDriftMetres']) for row in rows)<.000001 and max(float(row['maxScaleDrift']) for row in rows)<.000001
assert max(float(row['maxWindAngleDegrees']) for row in rows)>.1
(folder/'Garden-Wind.csv').write_bytes(motion.read_bytes())
record={'scope':'Matched native Editor views and an eight-second 30 Hz visual-only wind reel; authority tick frozen; not a performance benchmark, native input/audio or artistic approval. Legacy nature is recreated from unchanged Review20 assets in the same current art scene.','masterSha256':spec['masterSha256'],'sourceSha256':spec['sourceSha256'],'pairs':pairs,'motion':{'path':movie.relative_to(root).as_posix(),'sha256':hashlib.sha256(movie.read_bytes()).hexdigest(),'csvSha256':hashlib.sha256(motion.read_bytes()).hexdigest(),'frames':240,'seconds':8,'fps':30,'resolution':[1280,720],'maximumWindAngle':native['maximumWindAngle'],'maximumRootPositionDrift':native['maximumRootPositionDrift'],'maximumRootScaleDrift':native['maximumRootScaleDrift'],'silent':True},'windowsIntegrationPending':True}
(folder/'native-review-evidence.json').write_bytes((json.dumps(record,indent=2)+'\n').encode('utf-8'))
page=folder/'index.html';text=page.read_text(encoding='utf-8');marker='<!-- NATIVE_NATURE_REVIEW -->';text=text.split(marker)[0]
text+=marker+'<h1>Native installation and matching views</h1><p>24 placements, 72 LOD mesh instances and 33 canonical model buffers pass unit-frame, material, normal/winding, collider-definition, map/controller-placement, navigation/device and cleanup checks. All 117 collider transforms remain fixed while only nature is toggled. Existing actor animation poses may change when the whole character-art controller is removed; collider definitions remain unchanged.</p><p><a href="UnityReview/installed-review.json">Native contract evidence</a> / <a href="native-review-evidence.json">Matched cameras and reel identity</a></p>'+''.join(cards)
text+='<h2>Forced intermediate/far detail levels</h2><img src="UnityReview/tree-detail-lod1.png"><img src="UnityReview/tree-detail-lod2.png"><h2>Native wind / 8 seconds, 30 FPS, silent</h2><video controls preload="metadata" style="max-width:100%" src="Garden-Wind.mp4"></video><p><a href="Garden-Wind.csv">Fixed-step motion samples</a>. Authority tick stays frozen; roots and scale do not drift. This is visual playback, not a frame-rate benchmark or native-input test.</p>'
page.write_bytes(text.encode('utf-8'))
print('GARDEN_NATIVE_REVIEW_PACKAGED',len(pairs),'camera pairs;240 native motion frames')
