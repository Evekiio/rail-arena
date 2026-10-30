"""Verify the complete review package and record honest approval/integration state."""
import ast,hashlib,json,math,re
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1];out=root/'ArtSource/Original/PrecisionCarbine-References-v3'
manifest=json.loads((out/'manifest.json').read_text());geometry=json.loads((out/'Geometry-Validation.json').read_text());unity=json.loads((out/'Unity-Review.json').read_text())
assert geometry['passed'] and geometry['liveAssetsUnchanged']
assert unity['unityVersion']=='6000.6.7f1' and unity['captures']==16 and not unity['livePrefabChanged']
assert abs(unity['zoomVerticalFov']-math.degrees(2*math.atan(math.tan(math.radians(100)/2)/1.5)))<.001
assert unity['maximumWristErrorMetres']<.015
checks=manifest['checks']['perView'];assert max(v['maxBoundaryErrorPixels'] for v in checks)<=1
files=[]
for directory,count,size in [('Clean',6,(2048,2048)),('Details',10,(1600,1600)),('Unity',16,(1920,1080))]:
    paths=list((out/directory).glob('*.png'));assert len(paths)==count,(directory,len(paths))
    for path in paths:
        with Image.open(path) as image:assert image.size==size
        files.append(path)
assert len(list(out.glob('*.svg')))==6
for source in json.loads((out/'Provenance.json').read_text())['sources']:
    assert hashlib.sha256((out/source['file']).read_bytes()).hexdigest()==source['sha256']
for path in [out/'index.html',out/'README.md']:
    text=path.read_text(encoding='utf-8')
    links=re.findall(r'(?:href|src)="([^"]+)"',text) if path.suffix=='.html' else re.findall(r'\]\(([^)]+)\)',text)
    for link in links:
        if not link.startswith(('data:','http','#')):assert (path.parent/link.split('#')[0]).exists(),(path,link)
for relative,digest in json.loads((out/'Live-Asset-Baseline.json').read_text()).items():
    assert hashlib.sha256((root/relative).read_bytes()).hexdigest()==digest
for path in (root/'tools').glob('*carbine*v3.py'):ast.parse(path.read_text())
manifest.update(referenceStageComplete=True,creatorApproved=False,productionInstalled=False,geometryAudit=geometry,unityReview=unity,
    maximumSilhouetteBoundaryErrorPixels=max(v['maxBoundaryErrorPixels'] for v in checks),
    limitations=['Static review poses only; final finger and shoulder fitting pending','No production LOD/atlas or gameplay/performance certification'],
    deliverables=[dict(file=p.relative_to(out).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(out.rglob('*')) if p.is_file() and p.name!='manifest.json'])
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('PASS: 32 view/capture images, six SVG boards, geometry, calibrated bounds <=1px, sources, links, Unity FOV and unchanged live assets')
