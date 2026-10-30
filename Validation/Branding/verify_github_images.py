"""Verify that published LFS images are real PNGs and match their local hashes."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import subprocess
import urllib.parse
import urllib.request

root=Path(__file__).resolve().parents[2]
report=json.loads((root/'Validation/Branding/Publication.json').read_text(encoding='utf-8'))
commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip()

def verify(entry):
    path=urllib.parse.quote(entry['path'],safe='/')
    url=f'https://media.githubusercontent.com/media/Evekiio/rail-arena/{commit}/{path}'
    with urllib.request.urlopen(url,timeout=45) as response:
        data=response.read()
        content_type=response.headers.get('Content-Type','')
    assert content_type.startswith('image/png'),(entry['path'],content_type)
    assert data.startswith(b'\x89PNG\r\n\x1a\n'),entry['path']
    assert hashlib.sha256(data).hexdigest()==entry['sha256'],entry['path']
    return {'path':entry['path'],'status':'verified PNG; exact SHA-256 match','bytes':len(data),'url':url}

with ThreadPoolExecutor(max_workers=4) as pool:
    results=list(pool.map(verify,report['images']))
output={'passed':True,'commit':commit,'imagesVerified':len(results),'images':results}
(root/'Validation/Branding/GitHub-Images.json').write_text(json.dumps(output,indent=2)+'\n',encoding='utf-8')
print(json.dumps({key:value for key,value in output.items() if key!='images'},indent=2))
