"""Check rendered silhouette scale against source bounds, not just PNG metadata."""
import csv
import argparse
import hashlib
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image
from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--folder',default='ArtSource/Original/ProductionBenchmarks-v1/Dimensional-References-v1')
parser.add_argument('--master',default='ArtSource/Original/ProductionBenchmarks-v1/Production-Benchmarks.blend')
args=parser.parse_args()
OUT=ROOT/args.folder
data=json.loads((OUT/'Calibration.json').read_text())
manifest=json.loads((OUT/'manifest.json').read_text())
manifest['dimensionedSheets']=[]
assert hashlib.sha256((ROOT/args.master).read_bytes()).hexdigest()==data['sourceMasterSha256']
checks=[]
for asset in data['assets']:
    assert len(asset['views'])==6
    for view in asset['views']:
        r,u,n=view['right'],view['up'],view['normal']
        for vector in (r,u,n):assert sum(v*v for v in vector)==1
        assert [r[1]*u[2]-r[2]*u[1],r[2]*u[0]-r[0]*u[2],r[0]*u[1]-r[1]*u[0]]==n
        path=OUT/view['file'];png_info(path)
        with Image.open(path) as image:
            assert image.size==(2048,2048) and image.mode=='RGBA'
            box=image.getchannel('A').point(lambda p:255 if p>127 else 0).getbbox()
        width=asset['dimensions'][next(i for i in range(3) if r[i])]*asset['pixelsPerMetre']
        height=asset['dimensions'][next(i for i in range(3) if u[i])]*asset['pixelsPerMetre']
        target=[1024-width/2,1024-height/2,1024+width/2,1024+height/2]
        error=max(abs(a-b) for a,b in zip(box,target))
        assert error<3,(asset['asset'],view['name'],box,target,error)
        checks.append(dict(asset=asset['asset'],view=view['name'],silhouetteBoundsPixels=box,
                           expectedBoundsPixels=target,maxBoundaryErrorPixels=error))
    sheet=OUT/(asset['asset']+'-Six-View.png');manifest['dimensionedSheets'].append(png_info(sheet))
    assert ET.parse(OUT/(asset['asset']+'-Six-View.svg')).getroot().tag.endswith('svg')
    with (OUT/(asset['asset']+'-Parts.csv')).open(newline='',encoding='utf-8') as f:
        assert len(list(csv.DictReader(f)))==len(asset['parts'])
rig=OUT/'Blender-Reference-Rig.blend';assert rig.stat().st_size>100000
manifest['referenceRigSha256']=hashlib.sha256(rig.read_bytes()).hexdigest()
manifest['checks']={'orthographicImages':len(checks),'silhouetteBoundaryTolerancePixels':3,'perView':checks,
                    'basisOrthogonality':True,'sourceMasterHash':True,'csvPartCounts':True}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'PASS: {len(checks)} projected silhouettes, camera bases, sheets, source hash, part tables and reference rig')
