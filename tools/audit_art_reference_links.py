"""Audit manifest link integrity; linked evidence is not measured-reference certification."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
output = args.output.resolve()
assert output.is_relative_to(root) and not output.exists(), 'Use a new repository-local evidence folder'
source = root / 'ArtSource/Original/ArtPass-v1/current-asset-manifest.json'
manifest = json.loads(source.read_text(encoding='utf-8'))
prefixes = ('ArtSource/', 'RailArena/', 'Validation/', 'docs/', 'ThirdParty/', 'tools/')
rows = []
for asset in manifest['assets']:
    links = []
    for field in ('canonicalMaster', 'dimensionAuthority', 'dependencies', 'references', 'screenshots'):
        values = asset.get(field) or []
        if isinstance(values, str): values = [values]
        for value in values:
            if not isinstance(value, str): continue
            normalized = value.replace('\\', '/')
            if not normalized.startswith(prefixes): continue
            target = (root / normalized).resolve()
            links.append({'field': field, 'path': normalized, 'exists': target.is_relative_to(root) and target.is_file()})
    master = asset.get('canonicalMaster')
    rows.append({'id': asset['id'], 'path': asset['path'], 'category': asset['category'], 'state': asset['evidenceState'],
                 'canonicalMasterLinked': bool(master and any(item['field'] == 'canonicalMaster' and item['exists'] for item in links)),
                 'dimensionsRecorded': asset.get('dimensions') is not None, 'referenceCount': len(asset.get('references') or []),
                 'openReferenceException': asset.get('referenceException'), 'links': links})
broken = [{'asset': row['path'], 'state': row['state'], **link} for row in rows for link in row['links'] if not link['exists']]
counts = Counter(row['state'] for row in rows)
report = {'scope': 'Structural link and field audit only. Existing links, family-shared masters and dimension fields do not prove correct scale, six mutually consistent views, contact evidence, final approval or complete per-asset certification. Narrative reference requirements are not fabricated into paths.',
          'manifestSha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'entries': len(rows), 'states': dict(counts),
          'brokenLinks': broken, 'assets': rows}
output.mkdir(parents=True)
(output / 'audit.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
lines = ['# Art reference link audit', '', report['scope'], '', f'Inventory entries: {len(rows)}. Missing linked files: {len(broken)}.', '', '| Evidence state | Entries | Master linked | Dimensions recorded | References recorded |', '|---|---:|---:|---:|---:|']
for state, count in sorted(counts.items()):
    subset = [row for row in rows if row['state'] == state]
    lines.append(f'| {state} | {count} | {sum(row["canonicalMasterLinked"] for row in subset)} | {sum(row["dimensionsRecorded"] for row in subset)} | {sum(row["referenceCount"] > 0 for row in subset)} |')
if broken:
    lines += ['', '## Missing linked evidence', '']
    lines += [f'- {row["state"]}: `{row["asset"]}` links missing {row["field"]} `{row["path"]}`.' for row in broken]
(output / 'audit.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print(json.dumps({'entries': len(rows), 'brokenLinks': len(broken), 'states': dict(counts)}))
