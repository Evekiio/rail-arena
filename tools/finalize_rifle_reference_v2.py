"""Final reference-package integrity and coverage checks, after raster/geometry validation."""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from pathlib import Path
from PIL import Image
from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Original/RailRifle-Modeling-References-v2'
meta=json.loads((OUT/'Calibration.json').read_text())
manifest=json.loads((OUT/'manifest.json').read_text())
geometry=json.loads((OUT/'Geometry-Validation.json').read_text())
assert geometry['passed'] and geometry['stockShellThroughOpenings']==1
assert meta['builderSha256']==hashlib.sha256((ROOT/'tools/build_rifle_reference_v2.py').read_bytes()).hexdigest()
for detail in meta['details']:
    info=png_info(OUT/detail['file']);assert info['width']==2048 and info['height']==2048
    with Image.open(OUT/detail['file']) as image:assert image.getchannel('A').getbbox() is not None
manifest['boards']=[]
for name in manifest['requiredSheets']:
    ET.parse(OUT/(name+'.svg'))
    info=png_info(OUT/(name+'.png'));assert (info['width'],info['height'])==(3900,3200)
    manifest['boards'].append(info)
class LinkCheck(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('href','src') and not value.startswith(('https:','data:','#')):
                assert (OUT/value).exists(),value
LinkCheck().feed((OUT/'index.html').read_text())
for target in re.findall(r'\]\(([^)]+)\)',(OUT/'README.md').read_text()):
    assert (OUT/target).exists(),target
manifest['geometryValidation']=geometry
manifest['referenceCoverageComplete']=True
manifest['creatorApproved']=False
manifest['files']=[dict(path=p.relative_to(OUT).as_posix(),bytes=p.stat().st_size,
                        sha256=hashlib.sha256(p.read_bytes()).hexdigest())
                   for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='manifest.json' and p.suffix!='.blend1']
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(dict(deliveredViewAndBoardPNGs=22,assemblyGroups=12,creatorApproved=False,
                     maxOrthographicBoundaryErrorPixels=max(v['maxBoundaryErrorPixels'] for v in manifest['checks']['perView']),
                     nonStockMeshesUnchanged=geometry['nonStockMeshesUnchanged'],links='passed')))
