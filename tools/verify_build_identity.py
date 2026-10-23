"""Verify source compatibility against the same normalized inputs used by Unity's generator."""
import argparse,hashlib,re,struct
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,default=Path('RailArena'));args=parser.parse_args();root=args.project.resolve()
protocol=(root/'Assets/RailArena/Runtime/Networking/DuelProtocol.cs').read_text()
version=int(re.search(r'const (?:byte|int|ushort) Version\s*=\s*(\d+)',protocol).group(1))
files=[]
for directory in ('Assets','Packages','ProjectSettings'):
 for path in (root/directory).rglob('*'):
  if not path.is_file():continue
  name=path.relative_to(root).as_posix()
  if name.startswith(('Assets/Resources/BuildIdentity.txt','Assets/Validation')) or '/Editor/' in name or '/Tests/' in name or name.endswith(('/Editor.meta','/Tests.meta')):continue
  if name.startswith('Packages/') and name not in ('Packages/manifest.json','Packages/packages-lock.json'):continue
  files.append((name,path))
digest=hashlib.sha256();digest.update(f'RailArena/UDP/{version}/Windows64\n'.encode())
for name,path in sorted(files):
 encoded=name.encode();digest.update(struct.pack('<i',len(name.encode('utf-16-le'))//2));digest.update(encoded);data=path.read_bytes()
 if data.startswith(b'%YAML') or path.suffix.lower() in ('.cs','.json','.asmdef','.txt','.meta','.shader','.hlsl','.uss','.uxml','.inputactions'):
  data=data.decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n').encode()
 digest.update(struct.pack('<q',len(data)));digest.update(data)
recorded=(root/'Assets/Resources/BuildIdentity.txt').read_text().strip()
assert digest.hexdigest()==recorded,(digest.hexdigest(),recorded)
print('SOURCE_IDENTITY_VERIFIED '+recorded)
