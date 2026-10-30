"""Join final gameplay, rendered-frame and reproducibility evidence without overstating acceptance."""
import hashlib
import argparse
import json
from pathlib import Path
import shutil
import subprocess

root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser()
parser.add_argument('--review',default='07')
parser.add_argument('--validated-review',default='06')
args=parser.parse_args()
assert args.review.isdigit() and args.validated_review.isdigit(),'Review identifiers must be numeric'
build=root/f'Builds/RailArena-v1Alpha-ArtPass-Review-{args.review}'
validated=root/f'Builds/RailArena-v1Alpha-ArtPass-Review-{args.validated_review}'
clean=root/'Builds/ArtPass-CleanSource-01'
evidence=root/f'Validation/ArtPass-Review-{args.validated_review}'
identity=(build/'BuildIdentity.txt').read_text().strip()
assert identity==(validated/'BuildIdentity.txt').read_text().strip()
reports={name:json.loads((evidence/(name+'.json')).read_text()) for name in ('mechanics','bots','performance')}
reports.update({role:json.loads((evidence/'Duel'/(role+'.json')).read_text()) for role in ('host','client')})
assert all(report['passed'] and report['fingerprint']==identity for report in reports.values())
assert len(reports['mechanics']['checks'])==39 and all(check['passed'] for check in reports['mechanics']['checks'])
assert len(reports['performance']['views'])==5 and all(view['nonBlackSamples']>100 for view in reports['performance']['views'])
files=['RailArena.exe','UnityPlayer.dll','RailArena_Data/Managed/RailArena.Runtime.dll','RailArena_Data/Managed/RailArena.Simulation.dll','RailArena_Data/resources.assets','RailArena_Data/resources.assets.resS','RailArena_Data/sharedassets0.assets','RailArena_Data/sharedassets0.assets.resS','RailArena_Data/level0']
digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
comparisons=[]
for name in files:
    a,b=digest(build/name),digest(validated/name)
    assert a==b,name
    comparisons.append({'path':name,'sha256':a,'byteIdenticalToValidatedBuild':True})
source_commit=subprocess.check_output(['git','-C',str(clean),'rev-parse','HEAD'],text=True).strip()
for name in ('RailArena/ProjectSettings/QualitySettings.asset','RailArena/Assets/Resources/BuildIdentity.txt'):
    original=subprocess.check_output(['git','-C',str(clean),'show','HEAD:'+name])
    assert original.replace(b'\r\n',b'\n')==(clean/name).read_bytes().replace(b'\r\n',b'\n'),name
record={'schema':'rail-arena-integrated-art-validation/1','build':build.relative_to(root).as_posix(),'sourceCommit':source_commit,'fingerprint':identity,'validatedPlayer':'Review-06; final Review-07 changes only Editor settings restoration','runtimeByteComparisons':comparisons,'sourceQualityAndIdentityRestored':True,'mechanicsChecks':39,'mechanicsPassed':True,'botMatch':{'seconds':reports['bots']['elapsedSeconds'],'bots':3,'telemetry':reports['bots']['telemetry'],'exceptions':reports['bots']['exceptions'],'voids':reports['bots']['statisticsVoids']},'duel':{role:{field:reports[role][field] for field in ('passed','errors','slideSamples','statisticsPassed')} for role in ('host','client')},'performance':reports['performance'],'discardedEvidence':'Review-05 hidden swapchain timings are invalid for graphics performance: screenshots were black. They are excluded from final evidence.','acceptanceRemaining':['Native visible-window input, audio and sustained playtest by Sanders','Real-internet multiplayer acceptance','Broader authored animation replacement and complete inventory reference coverage; see production status']}
record['validatedPlayer']=f'Review-{args.validated_review}; shipped Review-{args.review} runtime files verified below'
record['performanceTargetMet']=all(view['frameBudgetMet'] for view in reports['performance']['views'])
record['performanceExceptions']=[{'view':view['view'],'meanFrameMs':view['meanFrameMs'],'p95FrameMs':view['p95FrameMs'],'targetMs':1000/60} for view in reports['performance']['views'] if not view['frameBudgetMet']]
output=root/f'Validation/art-integrated-final-{args.review}.json';output.write_text(json.dumps(record,indent=2)+'\n')
shipping=build/'ValidationEvidence';shipping.mkdir(exist_ok=True)
for name in ('mechanics','bots','performance'):shutil.copy2(evidence/(name+'.json'),shipping/(name+'.json'))
for role in ('host','client'):shutil.copy2(evidence/'Duel'/(role+'.json'),shipping/('duel-'+role+'.json'))
shutil.copy2(output,shipping/'integrated-final.json')
art=root/'ArtSource/Original/ArtPass-v1/StandaloneReview';art.mkdir(exist_ok=True)
for view in reports['performance']['views']:
    source=evidence/('performance.json.'+view['view']+'.png')
    shutil.copy2(source,art/(view['view']+'-1080.png'))
shutil.copy2(evidence/'performance.json',art/'render-performance.json')
(art/'README.md').write_text('''# Standalone rendered-frame evidence

Actual Windows player, RTX 2080 / Direct3D12, 1920x1080, High tier, one automated human and three bots. Hidden swapchains do not reliably render; these views force offscreen camera rendering and synchronize a one-pixel GPU readback each frame. Frame times include that readback overhead. Each view has 60 warmup frames followed by 180 samples. The five exported images are checked for non-black content and inspected directly.

GPU timers returned no valid samples, so isolated GPU cost is unavailable. Overlay HUD, native swapchain/input/audio and sustained visible play remain Sanders' QA gate. Automated runs used -noaudio. Earlier black-swapchain readings are discarded.
''')
print('FINAL_ART_EVIDENCE_VERIFIED',identity,'39 mechanics checks; bots; both duel peers; 5 rendered views; 9 byte-identical runtime files')
