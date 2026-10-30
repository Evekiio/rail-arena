"""Verify every portable-package file against its shipped SHA-256 manifest."""
import hashlib
import json
from pathlib import Path
import zipfile

root=Path(__file__).resolve().parent.parent
package=root/'Builds/RailArena-v1Alpha-ArtPass-Review-07.zip'
extraction=root/'Builds/ArtPass-Portable-Verify-07'
assert not extraction.exists(),'Choose a new verification directory; never overwrite a prior package'
with zipfile.ZipFile(package) as archive:
    names=archive.namelist();prefix=package.stem+'/'
    assert all(name.startswith(prefix) and 'BackUpThisFolder' not in name and 'DoNotShip' not in name for name in names)
    assert all((extraction/name).resolve().is_relative_to(extraction.resolve()) for name in names)
    manifest=dict(line.split('  ',1)[::-1] for line in archive.read(prefix+'PackageFiles.sha256').decode('utf-8-sig').splitlines() if line)
    assert len(names)==len(manifest)+1
    for name,expected in manifest.items():
        digest=hashlib.sha256()
        with archive.open(prefix+name) as stream:
            while block:=stream.read(1024*1024):digest.update(block)
        assert digest.hexdigest()==expected,name
    for required in ('RailArena.exe','Run-Checks.ps1','Test-Duel.ps1','BuildIdentity.txt','BuildInputs.txt','LICENSE','Licenses/Barlow-OFL.txt','ValidationEvidence/integrated-final.json'):
        assert required in manifest,required
    archive.extractall(extraction)
record={'package':package.relative_to(root).as_posix(),'sha256':hashlib.sha256(package.read_bytes()).hexdigest(),'bytes':package.stat().st_size,'verifiedFiles':len(manifest),'shippingBackupFoldersExcluded':True,'extractedTo':extraction.relative_to(root).as_posix(),'nativeInputAudioAcceptance':False}
(root/'Validation/art-package-verification.json').write_text(json.dumps(record,indent=2)+'\n')
print(json.dumps(record))
