"""Build the browsable, source-linked portal refresh performance evidence package."""
import hashlib,json,os,shutil,subprocess
from pathlib import Path
from html.parser import HTMLParser
root=Path(__file__).resolve().parents[1];folder=root/'ArtSource/Original/ArtPass-v1/Performance/PortalRefresh';folder.mkdir(parents=True,exist_ok=True)
comparisons=[json.loads((root/f'Validation/PortalRefresh-Profile-{i:02}/comparison.json').read_text(encoding='utf-8')) for i in (1,2)]
assert comparisons[0]['fingerprint']==comparisons[1]['fingerprint'];native=root/'Validation/PortalRefreshNative-04';review=json.loads((native/'review.json').read_text(encoding='utf-8'));assert review['passed'] and len(review['cases'])==6 and all(c['changedPixels']==0 and c['meanAbsoluteRgb']==0 and c['nonBlackPixels']>100 for c in review['cases'])
candidate='57e94e9a180dbe462bb2c78f1a193745f83508fe';hashes={}
for path in ['RailArena/Assets/RailArena/Runtime/PortalView.cs','RailArena/Assets/RailArena/Runtime/PortalRefreshPolicy.cs','RailArena/Assets/RailArena/Runtime/ArtFrameProfileValidation.cs']:
 committed=subprocess.check_output(['git','-C',str(root),'show',candidate+':'+path]).replace(b'\r\n',b'\n');current=(root/path).read_bytes().replace(b'\r\n',b'\n');assert current==committed,path;hashes[path]=hashlib.sha256(current).hexdigest()
expected=None
for pair in (1,2):
 for mode in ['off','on']:
  source=root/f'Validation/PortalRefresh-Profile-{pair:02}/{mode}.json.run.json';record=json.loads(source.read_text(encoding='utf-8-sig'));assert record['fingerprint']==comparisons[0]['fingerprint']
  if expected is None:expected=record['payloadHashes']
  assert record['payloadHashes']==expected
for i,comparison in enumerate(comparisons,1):(folder/f'comparison-{i:02}.json').write_text(json.dumps(comparison,indent=2)+'\n',encoding='utf-8')
for path in native.glob('*.png'):shutil.copy2(path,folder/path.name)
shutil.copy2(native/'review.json',folder/'native-review.json')
record={'schema':'rail-arena-portal-refresh-evidence/1','candidateCommit':candidate,'candidateFingerprint':comparisons[0]['fingerprint'],'commonImplementationSha256':hashes,'payloadHashes':expected,'orders':['OffThenOn','OnThenOff'],'nativeResult':'Validation/portal-refresh-native-04.xml','nativeReview':'Validation/PortalRefreshNative-04/review.json','policyResult':'Validation/portal-refresh-policy.xml','comparisons':[f'Validation/PortalRefresh-Profile-{i:02}/comparison.json' for i in [1,2]],'decision':'Promote bounded distant refresh in Sky Gardens art presentation only. Older maps retain full-rate defaults. Close/scoped and changed camera views refresh immediately.','scope':'Same candidate payload in both measurement orders; common implementation hashes verified against integrated source. Offscreen wall times include readback/driver/scheduling and are not native frame pacing. GPU timing unavailable. No new geometry, Blender masters or full-art completion is claimed.'}
(folder/'review-evidence.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
html='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena portal refresh performance</title><style>body{max-width:1400px;margin:30px auto;padding:24px;background:#14252c;color:#eef1df;font:18px/1.6 system-ui}a{color:#83e0cd}table{border-collapse:collapse;width:100%}td,th{padding:10px;border:1px solid #567}img{width:256px;max-width:48%;height:auto}section{margin:30px 0}</style><h1>Rail Arena / bounded distant portal refresh</h1><p>Sky Gardens reuses very small distant destination images for two or four rendered frames, with an age limit of 1/15 second. Nearby (within 12 m), large and scoped apertures remain full rate. Projection/layer changes, re-entry, resize and substantial movement/turning force a refresh. Actual cached crop coordinates stay paired with the texture. Older-map defaults and all traversal/gameplay authority are preserved.</p><p><a href="review-evidence.json">Source, payload and validation evidence</a> · <a href="native-review.json">All six direction checks</a></p><p>Timings below are forced 1080p rendering with synchronous one-pixel readback on RTX 2080/D3D12, High tier, one automated human and three bots. Inclusive wall time is not an isolated GPU cost or native frame pacing. No valid GPU samples were returned. Mean improvements repeat in both orders; some P95 misses remain. Final native input/audio/artistic acceptance remains open.</p>'''
for i,comparison in enumerate(comparisons,1):
 html+=f'<h2>Pair {i} / '+('off then on' if i==1 else 'on then off')+f'</h2><p><a href="comparison-{i:02}.json">Raw comparison and counter summaries</a></p><table><tr><th>View</th><th>Full mean / P95 ms</th><th>Adaptive mean / P95 ms</th><th>Mean wall reduction</th><th>Portal renders</th></tr>'
 for row in comparison['views']:
  a,b=row['full'],row['adaptive'];html+=f'<tr><td>{row["view"]}</td><td>{a["meanWallMs"]:.3f} / {a["p95WallMs"]:.3f}</td><td>{b["meanWallMs"]:.3f} / {b["p95WallMs"]:.3f}</td><td>{row["meanWallReductionPercent"]:.1f}%</td><td>{a["portalRenders"]} → {b["portalRenders"]}</td></tr>'
 html+='</table>'
html+='<h2>Frozen destination images / original and cached</h2><p>Native render targets are shown enlarged for inspection. Every pair has zero changed pixels. This preserves a frozen scene; distant moving views intentionally have bounded temporal reuse. Nearby/scoped tests refreshed on every sampled frame, with explicit projection, layer, re-entry and crop checks.</p>'
for i,case in enumerate(review['cases']):html+=f'<section><h3>{case["portal"]}</h3><p>{case["width"]} × {case["height"]} pixels / {case["nonBlackPixels"]} non-black pixels / {case["changedPixels"]} changed</p><img src="portal-{i}-reference.png" alt="Original full-rate destination"><img src="portal-{i}-cached.png" alt="Cached destination"></section>'
html+='</html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ['href','src']:assert (folder/value).is_file(),value
Links().feed(html);(folder/'index.html').write_text(html,encoding='utf-8');print('PORTAL_REFRESH_EVIDENCE_PACKAGED two orders / same payload / six zero-difference destination pairs')
