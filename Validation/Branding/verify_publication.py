"""Verify the scoped README/assets before staging the branding publication."""
from pathlib import Path
from html.parser import HTMLParser
import hashlib
import json
import re
import struct
import subprocess
import zlib

root=Path(__file__).resolve().parents[2]
refs=root/'ArtSource/Original/StylizedContenders/References-v1/Final'
brand=root/'docs/branding'
planned=[root/'docs/CHARACTER-REFERENCES.md',*brand.iterdir(),*refs.iterdir()]
planned={path.resolve() for path in planned if path.is_file()}

def tracked(path):
    rel=path.relative_to(root).as_posix()
    return subprocess.run(['git','cat-file','-e','HEAD:'+rel],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0

def target(link,base):
    if re.match(r'^(https?://|mailto:|#)',link):return
    path=(base/link.split('#')[0]).resolve()
    assert path.exists(),f'Missing target: {link}'
    assert path in planned or tracked(path),f'Target excluded from publication: {path}'

checked=0
for document,base in [(root/'Validation/Branding/README.publish.md',root),
                      (root/'docs/CHARACTER-REFERENCES.md',root/'docs'),
                      (brand/'README.md',brand)]:
    for link in re.findall(r'\]\(([^)]+)\)',document.read_text(encoding='utf-8')):
        target(link,base);checked+=1

class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for key,value in attrs:
            if key in ('src','href'):target(value,refs)
Links().feed((refs/'index.html').read_text(encoding='utf-8'))

pngs=[]
for path in sorted(planned):
    if path.suffix!='.png':continue
    data=path.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n'
    offset=8;idat=bytearray();ended=False;dimensions=None
    while offset<len(data):
        n=struct.unpack_from('>I',data,offset)[0]
        kind=data[offset+4:offset+8];payload=data[offset+8:offset+8+n]
        assert zlib.crc32(kind+payload)&0xffffffff==struct.unpack_from('>I',data,offset+8+n)[0]
        if kind==b'IHDR':dimensions=struct.unpack_from('>II',payload)
        if kind==b'IDAT':idat.extend(payload)
        if kind==b'IEND':ended=True;break
        offset+=n+12
    assert ended and zlib.decompress(idat)
    if path.parent==brand:assert dimensions==(2172,724)
    pngs.append(dict(path=path.relative_to(root).as_posix(),width=dimensions[0],height=dimensions[1],sha256=hashlib.sha256(data).hexdigest()))
assert len(pngs)==19
assert len(list(refs.glob('*-Turnaround.png')))==4
assert len(list(refs.glob('*-Head.png')))==4
assert len(list(refs.glob('*-Details.png')))==4
assert len(list(refs.glob('*-Axial.png')))==4
draft=(root/'Validation/Branding/README.publish.md').read_text(encoding='utf-8')
local=(root/'README.md').read_text(encoding='utf-8')
assert '77 Editor-suite tests passing' in draft and '87 Editor-suite tests passing' in local
assert 'docs/STYLIZED-CONTENDERS.md' not in draft
assert 'docs/STYLIZED-CONTENDERS.md' in local
report=dict(passed=True,pngsVerified=len(pngs),markdownLinksChecked=checked,selectedCharacterSheets=16,
            brandDimensions=[2172,724],scopedReadmePreservesPublishedImplementation=True,
            priorWorkingReadmeEditsPreserved=True,images=pngs)
(root/'Validation/Branding/Publication.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({key:value for key,value in report.items() if key!='images'},indent=2))
