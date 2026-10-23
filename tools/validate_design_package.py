"""Check review-package integrity, not gameplay quality or creator approval."""
import hashlib
import json
import math
import re
import struct
import zlib
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / 'ArtSource/Original/Design-v1'


def png_info(path):
    data=path.read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n',path
    pos=8; streams=[]; ended=False; size=None
    while pos < len(data):
        length=struct.unpack('>I',data[pos:pos+4])[0]
        kind=data[pos+4:pos+8];body=data[pos+8:pos+8+length]
        crc=struct.unpack('>I',data[pos+8+length:pos+12+length])[0]
        assert zlib.crc32(kind+body)&0xffffffff==crc,path
        if kind==b'IHDR':size=struct.unpack('>II',body[:8])
        if kind==b'IDAT':streams.append(body)
        pos+=length+12
        if kind==b'IEND':ended=True;break
    assert ended and pos==len(data) and zlib.decompress(b''.join(streams)),path
    return dict(path=path.relative_to(ROOT).as_posix(),width=size[0],height=size[1],
                bytes=len(data),sha256=hashlib.sha256(data).hexdigest())


def main():
    layout=json.loads((PACK/'Greybox/Layout.json').read_text())
    roles=Counter(o['role'] for o in layout['objects'])
    assert layout['envelope']==[56,40]
    assert roles['ramp']==4 and roles['pad_trigger']==4 and roles['pad_destination']==4
    assert roles['health']==3 and roles['spawn_candidate']==8 and roles['portal_aperture']==2
    assert len(layout['pads'])==4 and len(layout['health'])==3
    assert len(layout['portals'])==2 and len(layout['spawns'])==8
    all_bounds=[o for o in layout['objects'] if o['collision']]
    assert math.isclose(min(o['bounds_min'][0] for o in all_bounds),-28,abs_tol=.001)
    assert math.isclose(max(o['bounds_max'][0] for o in all_bounds),28,abs_tol=.001)
    assert math.isclose(min(o['bounds_min'][1] for o in all_bounds),-20,abs_tol=.001)
    assert math.isclose(max(o['bounds_max'][1] for o in all_bounds),20,abs_tol=.001)
    for obj in layout['objects']:
        assert all(math.isfinite(v) for k in ('center_blender','dimensions','bounds_min','bounds_max') for v in obj[k])
        assert all(v>0 for v in obj['dimensions']),obj['id']
        if obj['role'] in ('ramp','health','spawn_candidate','pad_trigger','portal_aperture'):
            c=obj['center_blender']
            assert any(other['role']==obj['role'] and all(abs(a-b)<.001 for a,b in
                       zip(other['center_blender'],[-c[0],-c[1],c[2]])) for other in layout['objects']),obj['id']
    # Coplanar overlapping top faces cause black/z-fighting patches in Cycles
    # and would obscure the proposed walking surfaces in the reference views.
    boxes=[o for o in all_bounds if o['role']!='ramp']
    for i,first in enumerate(boxes):
        for second in boxes[i+1:]:
            if abs(first['bounds_max'][2]-second['bounds_max'][2])>.001:continue
            overlap=[min(first['bounds_max'][axis],second['bounds_max'][axis])-
                     max(first['bounds_min'][axis],second['bounds_min'][axis]) for axis in (0,1)]
            assert not all(v>.001 for v in overlap),(first['id'],second['id'],'coplanar overlap')
    register=json.loads((PACK/'AssetRegister.json').read_text())
    assets=register['assets'];ids=[a['id'] for a in assets]
    assert len(ids)==len(set(ids))
    required={'id','family','purpose','dimensions','requiredViews','materials','states','collisionRole','approvalStatus'}
    for asset in assets:
        assert required.issubset(asset),asset['id']
        assert all(asset[k] for k in required),asset['id']
        assert asset['approvalStatus']!='approved' or asset['approvalEvidence'],asset['id']
        assert all((ROOT/r).is_file() for r in asset['references']),asset['id']
    images=[png_info(path) for path in sorted(PACK.rglob('*.png'))]
    links=0
    for path in [ROOT/'docs/DESIGN.md',ROOT/'docs/DESIGN-IMPLEMENTATION.md',*PACK.glob('*.md')]:
        for target in re.findall(r'\]\(([^)]+)\)',path.read_text(encoding='utf-8')):
            if target.startswith(('https://','#')):continue
            resolved=(path.parent/target.split('#')[0]).resolve()
            assert resolved.is_file() or resolved==(PACK/'manifest.json').resolve(),(path,target)
            links+=1
    report=dict(version=1,technicalValidation='passed',gameplayValidation='not performed',
                creatorApproval='pending per family',runtimeChanged=False,
                assetCount=len(assets),approvalCounts=dict(Counter(a['approvalStatus'] for a in assets)),
                greyboxRoleCounts=dict(roles),localLinks=links,images=images,
                blockedAssets=[a['id'] for a in assets if a['approvalStatus'].startswith('blocked')],
                limitations=['No controller traversal or competitive balance proof',
                             'Concepts are not calibrated shared-mesh projections',
                             'Detailed modeling and runtime migration require subsequent gates'])
    (PACK/'manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:report[k] for k in ('technicalValidation','assetCount','approvalCounts','localLinks')}))


if __name__=='__main__':
    main()
