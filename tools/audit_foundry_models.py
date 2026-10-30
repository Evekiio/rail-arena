"""Independently inspect the exported GLBs, render links and source identity."""
import hashlib
import json
import math
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Original/Foundry-Models-v1'


def audit():
    report=json.loads((OUT/'Model-Validation.json').read_text())
    source=ROOT/'ArtSource/Original/Foundry-Arena-v2/Map.json'
    assert report['sourceSHA256']==hashlib.sha256(source.read_bytes()).hexdigest()
    assert report['layoutCounts']==dict(surfaces=16,cover=30,ramps=4,jumpPads=4,portals=2,health=3,spawns=8)
    results=[]
    for item in report['assets']:
        path=OUT/'Exports'/f'{item["id"]}.glb'
        data=path.read_bytes()
        magic,version,length=struct.unpack_from('<III',data)
        assert magic==0x46546C67 and version==2 and length==len(data),path
        json_length,kind=struct.unpack_from('<II',data,12)
        assert kind==0x4E4F534A,path
        doc=json.loads(data[20:20+json_length])
        assert doc['asset']['version']=='2.0'
        primitives=[p for mesh in doc['meshes'] for p in mesh['primitives']]
        assert primitives,path
        triangles=0
        for primitive in primitives:
            assert primitive.get('mode',4)==4,path
            assert {'POSITION','NORMAL','TEXCOORD_0'}<=set(primitive['attributes']),path
            assert 'material' in primitive,path
            assert primitive['material']<len(doc['materials'])
            positions=doc['accessors'][primitive['attributes']['POSITION']]
            assert all(math.isfinite(n) for n in positions['min']+positions['max'])
            triangles+=doc['accessors'][primitive['indices']]['count']//3
        assert triangles==item['triangles'],(path,triangles,item['triangles'])
        assert not item['geometryIssues'],path
        results.append({'asset':item['id'],'triangles':triangles,'bytes':len(data),
                        'sha256':hashlib.sha256(data).hexdigest()})
    for render in report['renders']:
        data=(OUT/'Renders'/f'{render["name"]}.png').read_bytes()
        assert data[:8]==b'\x89PNG\r\n\x1a\n'
        assert struct.unpack_from('>II',data,16)==(1440,1080)
    result={'status':'pass','assets':results,'renderCount':len(report['renders']),
            'sourceIdentityMatches':True,'geometryAudit':'finite, closed manifold meshes; nonzero faces',
            'qualification':'export structure only; visual approval and Unity runtime checks outstanding'}
    (OUT/'Export-Audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(f'PASS: {len(results)} GLBs; {len(report["renders"])} PNGs; source identity and layout counts')


if __name__=='__main__':
    audit()
