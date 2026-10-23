"""Prepare review-only pose targets and provenance for the completed v3 study."""
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/Original/PrecisionCarbine-References-v3'
data=json.loads((OUT/'Calibration.json').read_text())
def vector(values):return dict(zip('xyz',values))
pose={name:vector([-source[1],source[2],source[0]]) for name,source in data['anchors'].items()}
pose.update(RightEuler=vector([-8,-90,-5]),LeftEuler=vector([-5,90,-80]),
            FirstPersonMount=vector([.20,1.52,.48]),FirstPersonBody=vector([0,.02,.33]),
            ThirdPersonMount=vector([.15,1.37,.40]))
pose.update(RightWristOffset=vector([-.030,.090,0]),LeftWristOffset=vector([0,0,0]))
(OUT/'ReviewPose.json').write_text(json.dumps(pose,indent=2)+'\n')
roles=['Compact optic and overall carbine balance','Receiver/stock vocabulary; no vertical foregrip retained',
       'Precision silhouette only; no bipod retained','Low-profile angled support cue; palette remains ivory/graphite/cyan',
       'Long vented fore-end and precision-carbine proportions']
ids=['a55dc1df-2e7a-4682-980c-0c43a1fc52d7','e42e8b34-18c5-4e9a-8a55-098778446bf9','5b87b813-ada0-4023-a587-29702c8ffff6',
     'ee937913-1b06-4573-a063-834a6071719c','df904cf9-2aac-4856-8961-2d119be6f9c0']
sources=[]
for i,(identifier,role) in enumerate(zip(ids,roles),1):
    file=OUT/f'Sources/Additional-Reference-{i}.png'
    source=Path('C:/Users/User/AppData/Local/Temp')/('codex-clipboard-'+identifier+'.png')
    digest=hashlib.sha256(file.read_bytes()).hexdigest()
    if source.exists():assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
    sources.append(dict(file=file.relative_to(OUT).as_posix(),originalFilename=source.name,sha256=digest,role=role,
                        alteration='None. Original pixels and any watermarks preserved.',use='Private design inspiration; not a game texture or model'))
(OUT/'Provenance.json').write_text(json.dumps(dict(sources=sources,meshSource='New original carbine reference geometry',
    palette='Original Rail Arena palette reused',previousPackages='Historical, retained',approved=False),indent=2)+'\n')
print('Review pose and five-source provenance prepared')
