"""Offline data/link/JavaScript checks for the Gate 1 gallery; does not open or automate a browser."""
from html.parser import HTMLParser
import json
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

root = Path(__file__).resolve().parent.parent
folder = root / "ArtSource/Original/ArtPass-v1/Gate1"
page = (folder / "index.html").read_text(encoding="utf-8")
manifest = json.loads((folder / "asset-manifest.json").read_text(encoding="utf-8"))


class Links(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []
    def handle_starttag(self, tag, attributes):
        for key, value in attributes:
            if key in {"href", "src"} and value and not value.startswith("#"):
                self.links.append(value)


links = Links()
links.feed(page)
for link in links.links:
    parsed = urlsplit(link)
    if not parsed.scheme:
        assert (folder / unquote(parsed.path)).resolve().exists(), "Missing gallery link: " + link
embedded = re.search(r'<script type="application/json" id="asset-data">(.*?)</script>', page, re.S).group(1)
records = json.loads(embedded)
assert len(records) == manifest["summary"]["inventoryEntries"]
assert all((root / record["path"]).exists() for record in records), "Missing inventory file"
assert all((root / view["path"]).exists() for view in manifest["baselineCaptures"]["views"])
script = re.findall(r"<script>(.*?)</script>", page, re.S)[0]
test = r'''
const assert = require('node:assert/strict'); const elements = new Map();
class Element {
  constructor(tag='div'){this.tag=tag;this.textContent='';this.children=[];this.value='';this.listeners={};this.disabled=false;}
  appendChild(e){this.children.push(e);return e;} append(...e){this.children.push(...e);} replaceChildren(...e){this.children=e;}
  addEventListener(name,fn){this.listeners[name]=fn;}
}
const document = { getElementById(id){if(!elements.has(id))elements.set(id,new Element());return elements.get(id);}, createElement(tag){return new Element(tag);} };
document.getElementById('asset-data').textContent=EMBEDDED;
GALLERY
assert.equal(document.getElementById('inventory').children.length,80);
document.getElementById('next').onclick(); assert.match(document.getElementById('page').textContent,/Page 2 /);
document.getElementById('previous').onclick(); assert.match(document.getElementById('page').textContent,/Page 1 /);
category.value='Weapons'; category.listeners.input(); assert.match(document.getElementById('count').textContent,/^WEAPON_COUNT matching/);
search.value='precisioncarbine'; search.listeners.input(); assert.ok(document.getElementById('inventory').children.length>0);
search.value='NO_SUCH_ASSET_G1'; search.listeners.input(); assert.equal(document.getElementById('inventory').children.length,0); assert.equal(document.getElementById('next').disabled,true);
search.value=''; category.value=''; disposition.value='archive-reference'; disposition.listeners.input();
assert.ok(document.getElementById('inventory').children.length>0);
console.log(JSON.stringify({pagination:true,categoryFilter:true,textSearch:true,emptyState:true,dispositionFilter:true}));
'''.replace("EMBEDDED", json.dumps(embedded)).replace("GALLERY", script).replace("WEAPON_COUNT", str(manifest["summary"]["categories"]["Weapons"]))
temporary = root / "Validation/art-gate1-gallery-offline.js"
temporary.write_text(test, encoding="utf-8")
node = sys.argv[1] if len(sys.argv) > 1 else "node"
result = subprocess.run([node, str(temporary)], check=True, capture_output=True, text=True)
report = dict(passed=True, galleryLinksVerified=len(links.links), inventoryFilesVerified=len(records), baselineCapturesLinked=len(manifest["baselineCaptures"]["views"]), logic=json.loads(result.stdout), browserVisualReview="Not established: browser tool blocked local file URL. This verifier performs offline data/logic checks only.")
(folder / "gallery-verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2))
