"""Join the baseline and production delta by path while retaining Unity identities and evidence limits."""
import csv
import hashlib
import json
from pathlib import Path
import re

root=Path(__file__).resolve().parent.parent
art=root/'ArtSource/Original/ArtPass-v1'
baseline=json.loads((art/'Gate1/asset-manifest.json').read_text())
production=json.loads((art/'integrated-manifest.json').read_text())
base={entry['path'].replace('\\','/'):entry for entry in baseline['assets']}
delta={entry['path'].replace('\\','/'):entry for entry in production['entries']}
assert len(base)==len(baseline['assets']) and len(delta)==len(production['entries'])
records=[]
for path in sorted(base.keys()|delta.keys()):
    old=base.get(path);new=delta.get(path);identity=old['id'] if old else new['id']
    if old is None and path.startswith('RailArena/Assets/'):
        meta=root/(path+'.meta')
        if meta.is_file():
            guid=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(),re.M)
            if guid:identity='unity:'+guid.group(1)
    file=root/path
    record={'id':identity,'path':path,'baselineId':old['id'] if old else None,'productionId':new['id'] if new else None,'category':new['category'] if new else old['category'],'mapArea':new['mapArea'] if new else old['area'],'disposition':new['disposition'] if new else old['disposition'],'existsNow':file.is_file(),'baselineUsage':old.get('scope') if old else None,'productionUsage':new.get('runtimeUsage') if new else None,'runtimeEvidenceLimit':'Baseline usage is the original Unity audit snapshot. Production inclusion is the conditional Sky Gardens art profile; source/reference evidence files are not runtime artwork. No deletion or installation is inferred from this join.','canonicalMaster':new.get('sourceMaster') if new else None,'dimensionAuthority':new.get('dimensionAuthority') if new else old.get('dimensionsEvidence'),'dimensions':new.get('dimensionsMetresBlenderXYZ') if new else old.get('dimensions'),'dependencies':new.get('dependencies',[]) if new else old.get('dependencies',[]),'references':new.get('referenceSheets',[]) if new else old.get('references',[]),'screenshots':[new['currentScreenshot']] if new else old.get('screenshots',[]),'provenance':new['provenance'] if new else old['provenance'],'approval':'Direction approved; integrated native/artistic acceptance pending','evidenceState':'production-evidence-present' if new else 'baseline-only','referenceException':new.get('referenceException') if new else 'Current per-asset reference requirements remain to be completed or explicitly excepted; baseline candidates are not certified masters.'}
    if new and new.get('disposition')=='archive-reference':
        record['approval']=new['approval'];record['evidenceState']='archived-research';record['runtimeEvidenceLimit']='Explicitly archived research; not installed production content or final art acceptance.'
    if new and new.get('researchStatus')=='unpromoted':
        record['approval']=new['approval'];record['evidenceState']='unpromoted-research';record['runtimeEvidenceLimit']='Opt-in Editor/development research only; default disabled. Not installed production artwork or an accepted performance improvement.'
    if new:
        actual=hashlib.sha256(file.read_bytes()).hexdigest() if file.is_file() else None
        assert actual==new['sha256'],path
        record['productionSha256']=actual
    records.append(record)
assert len({entry['id'] for entry in records})==len(records),'Stable inventory IDs must remain unique'
summary={'baselineEntries':len(base),'productionEntries':len(delta),'overlappingPaths':len(base.keys()&delta.keys()),'currentUniquePaths':len(records),'productionEvidencePaths':sum(entry['evidenceState']=='production-evidence-present' for entry in records),'unpromotedResearchPaths':sum(entry['evidenceState']=='unpromoted-research' for entry in records),'archivedResearchPaths':sum(entry['evidenceState']=='archived-research' for entry in records),'baselineOnlyPaths':sum(entry['evidenceState']=='baseline-only' for entry in records),'missingPaths':sum(not entry['existsNow'] for entry in records),'finalApprovedEntries':0,'referenceCoverage':'Incomplete; evidence-present is not complete geometry/contact/reference certification.'}
(art/'current-asset-manifest.json').write_text(json.dumps({'schema':'rail-arena-art-current/1','baseline':'Gate1/asset-manifest.json','productionDelta':'integrated-manifest.json','summary':summary,'assets':records},indent=2)+'\n')
columns=list(records[0]);columns += [key for record in records for key in record if key not in columns]
columns=list(dict.fromkeys(columns))
with (art/'current-asset-register.csv').open('w',newline='',encoding='utf-8') as stream:
    writer=csv.DictWriter(stream,columns);writer.writeheader();writer.writerows({key:json.dumps(value) if isinstance(value,(list,dict)) else value for key,value in record.items()} for record in records)
text='# Current inventory coverage and production gaps\n\nThe Gate 1 snapshot is preserved. This path join keeps baseline IDs, uses Unity GUIDs for new imported assets, and links the current production evidence. Production evidence is not final asset/reference acceptance.\n\n'
text+='\n'.join(f'- **{key}:** {value}' for key,value in summary.items())
text+='''

## Next production priorities

1. Complete body stance, full directional movement transitions, traversal, interrupted hit/death stance combinations and remaining first-person actions with contact evidence. The current action matrix retains all 34 original action requirements; cardinal movement, basic airborne/reactions, grounded crouch and third-person slide families now have integrated review evidence. First-person actions and complete transition/interruption coverage remain unfinished.
2. Extend measured stone production beyond the completed staging/basement, bridge, summit and lower arena/ramp kits to remaining structural framing and cover. Preserve tested movement envelopes.
3. Refine portal/pad silhouettes, active/depleted states, launch cues and destination readability; complete their measured assembly references.
4. Complete waterfall lips/foam, cloudscape and distant ruins, plus broader nature placement/variant/wind/contact coverage. Eleven garden modules now have canonical geometry, measured sheets, matched native views and a scoped wind reel; shipping and final acceptance are recorded separately.
5. Refine the lowest rifle LOD barrel/vent silhouette and complete per-family rig/socket/pivot/collision/construction-layer evidence, UI state/animation references, audio/provenance review and complete legacy/archive references.
6. Resolve measured performance exceptions, then native sound/input, sustained match and artistic acceptance with Sanders. The archived rifle batch reduced draw calls only 1.1â€“3.8% and did not establish a frame-time improvement; it is not installed. Prioritize the larger environment/material budget and stronger native GPU/pacing evidence. No public release until final acceptance.

See integrated-review.html, animation-production-matrix.csv and docs/ART-PASS-REVIEW.md for actual in-game/reel/build evidence. No intermediate approval stop is required.
'''
(art/'CURRENT-GAPS.md').write_text(text)
print('CURRENT_INVENTORY_JOINED',json.dumps(summary))
