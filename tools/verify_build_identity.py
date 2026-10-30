"""Verify source compatibility against the same normalized inputs used by Unity's generator."""
import argparse,hashlib,re,struct
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,default=Path('RailArena'));parser.add_argument('--inputs',type=Path,help='Exact Unity InputPaths manifest, required for active Validation dependencies');parser.add_argument('--identity',type=Path,help='Packaged BuildIdentity.txt when the source identity was restored after building');args=parser.parse_args();root=args.project.resolve()
protocol=(root/'Assets/RailArena/Runtime/Networking/DuelProtocol.cs').read_text()
version=int(re.search(r'const (?:byte|int|ushort) Version\s*=\s*(\d+)',protocol).group(1))
files=[]
declared=set(args.inputs.read_text().splitlines()) if args.inputs else None
for directory in ('Assets','Packages','ProjectSettings'):
 for path in (root/directory).rglob('*'):
  if not path.is_file():continue
  name=path.relative_to(root).as_posix()
  if declared is not None:
   if name in declared:files.append((name,path))
   continue
  if name.startswith('Assets/Validation'):
   raise SystemExit('Use --inputs from Unity BuildIdentityGenerator.InputPaths; active Validation assets cannot be inferred from folder names.')
  if name.startswith('Assets/Resources/BuildIdentity.txt') or '/Editor/' in name or '/Tests/' in name or name.endswith(('/Editor.meta','/Tests.meta')):continue
  if name.startswith('Packages/') and name not in ('Packages/manifest.json','Packages/packages-lock.json'):continue
  files.append((name,path))
digest=hashlib.sha256();digest.update(f'RailArena/UDP/{version}/Windows64\n'.encode())
for name,path in sorted(files):
 encoded=name.encode();digest.update(struct.pack('<i',len(name.encode('utf-16-le'))//2));digest.update(encoded);data=path.read_bytes()
 if data.startswith(b'%YAML') or path.suffix.lower() in ('.cs','.json','.asmdef','.txt','.meta','.shader','.hlsl','.uss','.uxml','.inputactions'):
  data=data.decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n').encode()
 digest.update(struct.pack('<q',len(data)));digest.update(data)
if declared is not None:assert {name for name,_ in files}==declared,'Manifest contains missing files'
recorded=(args.identity or root/'Assets/Resources/BuildIdentity.txt').read_text().strip()
assert digest.hexdigest()==recorded,(digest.hexdigest(),recorded)
print('SOURCE_IDENTITY_VERIFIED '+recorded)
