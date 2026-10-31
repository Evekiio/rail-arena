"""Package measured FPS assembly sheets, exact socket frames and honest native/Workbench comparisons."""
import gzip,hashlib,json,math
from pathlib import Path
from html.parser import HTMLParser
import xml.etree.ElementTree as ET
from PIL import Image,ImageDraw,ImageFont,ImageStat
root=Path(__file__).resolve().parents[1];folder=root/'ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1'
spec=json.loads((folder/'production.json').read_text(encoding='utf-8'));views=json.loads((folder/'measured-views.json').read_text(encoding='utf-8'));digest=hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest();assert digest==spec['masterSha256']==views['masterSha256']
source=folder/spec['source'];assert hashlib.sha256(source.read_bytes()).hexdigest()==spec['sourceSha256'];native=json.loads(gzip.decompress(source.read_bytes()))
capture=json.loads((folder/'RuntimeSource/capture.json').read_text(encoding='utf-8'));assert capture['sourceSha256']==spec['sourceSha256']
case=[item for item in ET.parse(root/'Validation/fps-assembly-export-scale-correct.xml').getroot().iter('test-case') if item.attrib.get('fullname','').endswith('FirstPersonAssemblyExport.ExportHeldAssemblyWithSkinProof')];assert len(case)==1 and case[0].attrib['result']=='Passed' and spec['sourceSha256'] in ''.join(case[0].itertext())
for item in spec['referencePoses']:assert item['maximumNativeVertexErrorMetres']<.00005 and item['maximumNativeBoneHeadErrorMetres']<.00005
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',28);small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
for item in views['views']:
    name=item['id'];directory=folder/'References'/name;sheet=Image.new('RGB',(3584,2350),'#14252c');draw=ImageDraw.Draw(sheet)
    draw.text((24,18),'RAIL ARENA / FPS ASSEMBLY v1 / '+name,font=font,fill='white');draw.text((24,59),'Metres / editable 54-bone rig / exact installed four-weight skin and rifle LOD0 / held pose reference',font=small,fill='#a7ddd2')
    scale=1024/item['orthographicSpanMetres'];ruler=1 if item['orthographicSpanMetres']>=1.2 else .5
    for index,label in enumerate(item['views']):
        x=(index%3)*1024;y=115+(index//3)*1024
        with Image.open(directory/(label+'.png')) as image:
            assert image.size==(1024,1024) and max(ImageStat.Stat(image).stddev)>8;sheet.paste(image.convert('RGB'),(x,y))
        draw.text((x+18,y+15),label.upper(),font=font,fill='white');draw.line((x+24,y+975,x+24+ruler*scale,y+975),fill='#83e0cd',width=4);draw.text((x+24,y+932),f'{ruler:g} metre',font=small,fill='#a7ddd2')
    x=3328;base=2070;height=1.8*scale;top=base-height;radius=.09*scale
    draw.ellipse((x-radius,top,x+radius,top+2*radius),outline='#a7ddd2',width=4);neck=top+.22*scale;hip=top+.95*scale
    draw.line((x,neck,x,hip),fill='#a7ddd2',width=5);draw.line((x-.20*scale,neck+.10*scale,x+.20*scale,neck+.10*scale),fill='#a7ddd2',width=5)
    for sign in (-1,1):draw.line((x,hip,x+sign*.13*scale,base),fill='#a7ddd2',width=5)
    draw.text((3090,2180),'1.8 m human',font=small,fill='white');draw.text((3090,2215),'scale envelope',font=small,fill='white');draw.text((3090,2250),'schematic only',font=small,fill='#a7ddd2')
    draw.text((24,2184),f"Same {item['orthographicSpanMetres']:.2f} m view span in every assembly/view. Dimensions and axes: measured-views.json / construction.json.",font=small,fill='white')
    draw.text((24,2224),f"Master SHA-256: {digest}",font=small,fill='white');draw.text((24,2264),'Workbench textures/lighting are technical reference. Native Unity captures remain the final cel-shading authority.',font=small,fill='#a7ddd2');sheet.save(directory/'measured-sheet.png')
    pair=Image.new('RGB',(2560,790),'#14252c');draw=ImageDraw.Draw(pair)
    for x,label,path in [(0,'UNITY / held gameplay camera',folder/item['nativeUnityImage']),(1280,'BLENDER / same eye and held geometry',folder/item['nativeEye'])]:
        draw.text((x+20,15),name+' / '+label,font=small,fill='white')
        with Image.open(path) as image:assert image.size==(1280,720);pair.paste(image.convert('RGB'),(x,65))
    pair.save(directory/'native-eye-comparison.png')
# Keep exact raw socket matrices in the source; provide an explicit readable metre-scale construction index.
construction={'masterSha256':digest,'sourceSha256':spec['sourceSha256'],'pivot':'Weapon root at Blender origin; X right, Y forward, Z up. No collision or gameplay ownership.','parts':[{'name':part['name'],'source':part['source'],'vertices':len(part['geometry']['vertices']),'triangles':sum(len(section['values'])//3 for section in part['geometry']['submeshes']),'materials':part['materials'],'nativeWeaponRootRowMajorFrame':part['toRoot']['values'],'sourcePivotBlenderXYZMetres':[part['toRoot']['values'][index] for index in (3,11,7)]} for part in native['rifle']], 'materials':native['materials'],'assemblies':[]}
for record,part in zip(construction['parts'],native['rifle']):
    frame=part['toRoot']['values'];points=[]
    for vertex in part['geometry']['vertices']:
        local=[vertex['x'],vertex['y'],vertex['z'],1]
        point=[sum(frame[row*4+column]*local[column] for column in range(4)) for row in range(3)]
        points.append([point[0],point[2],point[1]])
    low=[min(point[axis] for point in points) for axis in range(3)];high=[max(point[axis] for point in points) for axis in range(3)]
    record['weaponRootBoundsBlenderMetres']={'minimum':low,'maximum':high,'size':[b-a for a,b in zip(low,high)]}
    record['materialAssignments']=[native['materials'][index]['name'] for index in part['materials']]
for view,pose in zip(views['views'],native['poses']):
    assert view['id']==pose['identity']+'_'+pose['mode'];sockets=[]
    for marker in pose['markers']:
        values=marker['matrix']['values'];sockets.append({'name':marker['name'],'positionBlenderXYZMetres':[values[3],values[11],values[7]],'nativeWeaponRootRowMajorMatrix':values})
    layers=['References/'+view['id']+'/right-'+role+'-only.png' for role in ('arms','rifle')]
    assert all((folder/path).is_file() for path in layers)
    construction['assemblies'].append({'id':view['id'],'constructionLayers':layers,'clearanceEnvelope':view['bounds'],'attachmentSockets':sockets,'nativeCameraToWeaponRoot':pose['cameraToRoot'],'nativeVerticalFov':pose['verticalFov'],'maximumNativeLinearSkinErrorMetres':pose['maximumNativeLinearSkinError'],'referenceSheet':'References/'+view['id']+'/measured-sheet.png'})
(folder/'construction.json').write_bytes((json.dumps(construction,indent=2)+'\n').encode('utf-8'))
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / calibrated FPS assembly</title><style>body{max-width:1650px;margin:30px auto;padding:24px;background:#14252c;color:#eef1df;font:18px/1.6 system-ui}a{color:#83e0cd}img{width:100%;height:auto}</style><h1>Rail Arena / editable first-person assemblies</h1><p>Four appearances, held hip and ADS poses, 54 editable bones per assembly, original four-weight skin groups, 14 installed rifle parts and exact grip/sight/eye frames. The physical first-person optic opening is preserved. All 31,012 posed arm vertices and 432 bone heads are checked against native calibration, with maximum vertex error below 0.003 mm. This package is held geometry/reference production, not a new animation library or a completed full art pass.</p><p><a href="FirstPerson-Assembly-v1.blend">Editable Blender master with packed paint maps</a> · <a href="production.json">Source hashes and skin verification</a> · <a href="measured-views.json">Equal-scale views and dimensions</a> · <a href="construction.json">Parts, clearances, sockets and cameras</a> · <a href="RuntimeSource/capture.json">Native captured source identity</a></p><p>Blender Workbench images support measured geometry and camera inspection. Native Unity images are the cel-shading authority. The original character/rifle masters remain preserved and listed by hash. No gameplay, collision, movement, accuracy, weapon timing or multiplayer change is installed.</p>'''
for item in views['views']:
    name=item['id'];html+=f'<h2>{name} / isolated construction layers</h2><p>Same right-side camera and scale. These images expose the held baseline finger/grip defects; they are not a corrected hand-art pass.</p><img loading="lazy" src="References/{name}/right-arms-only.png"><img loading="lazy" src="References/{name}/right-rifle-only.png">'
    html+=f'<h2>{name}</h2><h3>Matched eye and held geometry</h3><img loading="lazy" src="References/{name}/native-eye-comparison.png"><h3>Six equal-scale orthographics</h3><img loading="lazy" src="References/{name}/measured-sheet.png">'
html+='<p>Remaining: visible finger/grip intersections and detached-looking fingertip shapes in the held baseline, full first-person motion, reload finger/magazine contacts and transitions, additional asset production and complete reference certification, sustained performance and Sanders’ final native input/audio/artistic acceptance. Verified playable build remains Review 25.</p></html>'
class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src'):assert (folder/value).resolve().is_file(),value
Links().feed(html);(folder/'index.html').write_text(html,encoding='utf-8')
print('FPS_ASSEMBLY_REFERENCE_PACKAGE_VERIFIED 8 sheets / 48 equal-scale orthographics / 8 matched eye pairs / editable rigs; no runtime replacement')
