from pathlib import Path
import shutil,hashlib,json
r=Path(r"F:/Rail Arena");p=r/'Builds/Automatic-Rifle-2026-10-29/Project';v=r/'Validation/Automatic-Rifle-2026-10-29'
assert not p.exists(), 'Refusing existing isolated project'
p.mkdir(parents=True)
for d in ('Assets','Packages','ProjectSettings'):shutil.copytree(r/'RailArena'/d,p/d)
shutil.copytree(r/'Builds/Slide-2026-10-29/Project/Library',p/'Library')
for name in ('CharacterPicker/Portraits.json',):
 target=p.parent/'Validation'/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(r/'Validation'/name,target)
old=r/'Builds/Sprint-Slide-2026-10-29';count=0
for line in (old/'SHA256SUMS.txt').read_text().splitlines():
 digest,name=line.split('  ',1);assert hashlib.sha256((old/name).read_bytes()).hexdigest()==digest;count+=1
(v/'preservation.json').write_text(json.dumps({'previousSlideBuildFilesVerified':count,'isolatedProject':str(p)},indent=2))
print('ISOLATED_PROJECT_READY; prior slide build verified:',count)
