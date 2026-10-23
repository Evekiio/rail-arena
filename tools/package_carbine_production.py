"""Build side-by-side review and verify the installed production handoff."""
import argparse,hashlib,html,json,math,re,struct,xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
parser=argparse.ArgumentParser();parser.add_argument('--standalone-folder',default='Validation/Carbine');args=parser.parse_args()
root=Path(__file__).resolve().parents[1];out=root/'ArtSource/Original/PrecisionCarbine-Production-v1';reference=root/'ArtSource/Original/PrecisionCarbine-References-v3'
geometry=json.loads((out/'Geometry-Validation.json').read_text());exports=json.loads((out/'Export-Validation.json').read_text());unity=json.loads((out/'Unity-Validation.json').read_text());cal=json.loads((reference/'Calibration.json').read_text())
assert geometry['passed'] and exports['passed'] and unity['unityVersion']=='6000.6.7f1'
assert unity['maximumWristError']<.015 and unity['maximumShoulderError']<.002 and unity['captures']==32
assert geometry['masterSha256']==hashlib.sha256((out/'Precision-Carbine.blend').read_bytes()).hexdigest()
tests=ET.parse(root/'Validation/carbine-regressions.xml').getroot();assert tests.attrib['failed']=='0' and int(tests.attrib['passed'])>=80
bounds=[]
for view in cal['assets'][0]['views']:
    file=out/'Clean'/Path(view['file']).name
    with Image.open(file) as image:
        assert image.size==(2048,2048);box=image.getchannel('A').point(lambda p:255 if p>127 else 0).getbbox()
    dims=cal['assets'][0]['dimensions'];ppm=2048/1.25
    w=dims[next(i for i in range(3) if view['right'][i])]*ppm;h=dims[next(i for i in range(3) if view['up'][i])]*ppm
    expected=[1024-w/2,1024-h/2,1024+w/2,1024+h/2];error=max(abs(a-b) for a,b in zip(box,expected))
    assert error<=1,(view['name'],box,expected,error);bounds.append(dict(view=view['name'],maximumErrorPixels=error))
for file in (out/'Unity').glob('*.png'):
    with Image.open(file) as image:assert image.size==(1920,1080)
for name in ('BaseColor','Normal','MetallicSmoothness','Emission'):
    with Image.open(root/f'RailArena/Assets/RailArena/Art/PrecisionCarbine/{name}.png') as image:assert image.size==(2048,2048)
approval=dict(date='2026-10-23',status='creator-approved-design',evidence='User explicitly approved new carbine design and authorized final production replacement with fitting.',referenceMasterSha256=cal['sourceMasterSha256'],productionPackage='../PrecisionCarbine-Production-v1/README.md')
(reference/'Approval.json').write_text(json.dumps(approval,indent=2)+'\n')
page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / production carbine</title>',
'<style>body{margin:36px auto;max-width:1600px;padding:20px;background:#14202a;color:#e7ecec;font:17px system-ui}a{color:#69dce7}h1{font-size:42px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}img{width:100%;background:#22313a}figure{margin:0}figcaption{padding:12px}section{margin:40px 0}.tag{color:#93ece3}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style>',
'<h1>RAIL ARENA / PRECISION CARBINE</h1><p class="tag">Design approved. Production asset integrated. Human acceptance and performance qualification remain open.</p>',
'<p><a href="README.md">Handoff notes</a> · <a href="Precision-Carbine.blend">Editable Blender master</a> · <a href="../PrecisionCarbine-References-v3/index.html">Approved reference package</a> · <a href="manifest.json">Validation manifest</a></p>',
'<p>1,000 × 100 × 330mm · 6,174 / 5,666 / 2,682 triangles · two materials · 2K atlas maps. All views below are actual Blender renders or installed Unity captures.</p>',
'<section><h2>Approved study beside final production</h2><div class="pair"><figure><img src="../PrecisionCarbine-References-v3/Details/RailRifle-Hero.png"><figcaption>Approved study</figcaption></figure><figure><img src="Carbine-Studio.png"><figcaption>Final textured master</figcaption></figure></div></section>']
for view in cal['assets'][0]['views']:
    file=Path(view['file']).name
    page.append(f'<section><h2>{html.escape(view["name"])} / same geometry and calibrated scale</h2><div class="pair"><figure><img loading="lazy" src="../PrecisionCarbine-References-v3/Clean/{file}"><figcaption>Approved construction</figcaption></figure><figure><img loading="lazy" src="Clean/{file}"><figcaption>Production material / geometry</figcaption></figure></div></section>')
for i in range(4):
    page.append(f'<section><h2>{["Player","Bot 01","Bot 02","Bot 03"][i]} / installed Unity presentation</h2>')
    for a,b in [('Normal','Zoom'),('Fire','Reset')]:page.append(f'<div class="pair"><figure><img loading="lazy" src="Unity/FP-{i}-{a}.png"><figcaption>{a}</figcaption></figure><figure><img loading="lazy" src="Unity/FP-{i}-{b}.png"><figcaption>{b}</figcaption></figure></div>')
    page.append(f'<div class="pair"><figure><img loading="lazy" src="Unity/TP-{i}-Side.png"><figcaption>Shoulder and hand contacts</figcaption></figure><figure><img loading="lazy" src="Unity/TP-{i}-LOD0.png"><figcaption>Third person LOD0</figcaption></figure></div>')
    page.append(f'<div class="pair"><figure><img loading="lazy" src="Unity/TP-{i}-LOD1.png"><figcaption>LOD1</figcaption></figure><figure><img loading="lazy" src="Unity/TP-{i}-LOD2.png"><figcaption>LOD2</figcaption></figure></div></section>')
page.append('</html>');(out/'index.html').write_text('\n'.join(page),encoding='utf-8')
for link in re.findall(r'(?:href|src)="([^"]+)"','\n'.join(page)):
    if link=='manifest.json':continue
    assert (out/link).is_file(),link
build=json.loads((out/'Windows-Build.json').read_text()) if (out/'Windows-Build.json').exists() else None
if build:
    exe=root/'Builds/Carbine-v1/RailArena.exe';assert hashlib.sha256(exe.read_bytes()).hexdigest()==build['executableSha256']
    with exe.open('rb') as f:f.seek(0x3c);pe=struct.unpack('<I',f.read(4))[0];f.seek(pe+4);assert struct.unpack('<H',f.read(2))[0]==0x8664
standalone={}
for name in ('mechanics','offline','host','client'):
    path=root/args.standalone_folder/(name+'.json')
    if path.exists():
        report=json.loads(path.read_text());assert report['passed']
        if build:assert report['fingerprint']==build['compatibilityIdentity']
        standalone[name]=report
manifest=dict(standalone=standalone,status='integrated; human acceptance and performance qualification pending',creatorDesignApproved=True,geometry=geometry,exports=exports,unity=unity,tests=dict(passed=int(tests.attrib['passed']),failed=0),orthographicBounds=bounds,windowsBuild=build,
    maps=dict(size=2048,materials=2,normal='Flat tangent normal, geometry-authored facets/bevels'),
    files=[dict(file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json' and p.suffix not in ('.blend1','.blend2')],
    liveAssets={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [root/'RailArena/Assets/Resources/Weapons/Railgun.prefab',root/'RailArena/Assets/RailArena/Characters/Hand grip.json']})
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'PASS: {manifest["tests"]["passed"]} tests, 32 Unity captures, six calibrated views <=1px, production maps/geometry/links'+(', Windows x64 build' if build else ''))
