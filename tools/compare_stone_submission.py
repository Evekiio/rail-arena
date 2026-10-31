"""Compare one seeded same-executable off/on stone experiment without claiming native frame pacing."""
import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('directory', type=Path)
args = parser.parse_args()
folder = args.directory.resolve()
reports = {mode: json.loads((folder / f'{mode}.json').read_text(encoding='utf-8-sig')) for mode in ('off', 'on')}
runs = {mode: json.loads((folder / f'{mode}.json.run.json').read_text(encoding='utf-8-sig')) for mode in reports}
off, on = reports['off'], reports['on']
for field in ('fingerprint', 'gpu', 'api', 'warmupFrames', 'sampleFrames', 'width', 'height', 'simulationHz', 'randomSeed', 'apertureCropping'):
    assert off[field] == on[field], f'Workload mismatch: {field}'
assert runs['off']['executableSha256'] == runs['on']['executableSha256'], 'Executable mismatch'
assert off['stonePaletteRenderers'] == 0 and on['stonePaletteRenderers'] > 20
assert all(item['passed'] and item['exceptions'] == 0 for item in reports.values())
assert [view['view'] for view in off['views']] == [view['view'] for view in on['views']]

def counter(view, category, name):
    matches = [item for item in view['counters'] if item['category'] == category and item['name'] == name and item['available']]
    assert len(matches) == 1, f'Expected exactly one {category}/{name} counter'
    return matches[0]['mean']

def pair(before, after):
    return {'off': before, 'on': after, 'changePercent': (after / before - 1) * 100 if before else None}

views = []
for before, after in zip(off['views'], on['views']):
    for field in ('frames', 'portalRenders'):
        assert before[field] == after[field], f'{before["view"]}: {field} mismatch'
    item = {'view': before['view'], 'validGpuSamplesOff': before['validGpuSamples'], 'validGpuSamplesOn': after['validGpuSamples']}
    for field in ('meanWallMs', 'p95WallMs', 'meanSubmitMs', 'meanReadbackMs'):
        item[field] = pair(before[field], after[field])
    for name in ('Draw Calls Count', 'SetPass Calls Count', 'Triangles Count'):
        item[name] = pair(counter(before, 'Render', name), counter(after, 'Render', name))
    views.append(item)
result = {'scope': 'Single off/on ordered pair; same executable and seeded fixed simulation. Forced offscreen submission/readback is not native presentation, isolated GPU timing or sustained 60 FPS acceptance. Counters are inclusive and non-additive. No automatic promotion.',
          'fingerprint': off['fingerprint'], 'executableSha256': runs['off']['executableSha256'],
          'inputSha256': {mode: hashlib.sha256((folder / f'{mode}.json').read_bytes()).hexdigest() for mode in reports},
          'rendererInstancesOn': on['stonePaletteRenderers'], 'sourceMaterialSlotsOn': on['stonePaletteSourceSlots'],
          'runOrder': sorted(runs, key=lambda mode: runs[mode]['startedUtc']),
          'frameTimingFeatureEnabled': {mode: report['frameTimingFeatureEnabled'] for mode, report in reports.items()}, 'views': views}
output = folder / 'comparison.json'
assert not output.exists(), 'Preserve previous comparison evidence'
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
lines = ['# Stone submission controlled comparison', '', result['scope'], '', '| View | Draws off / on | Mean wall ms off / on | P95 wall ms off / on |', '|---|---:|---:|---:|']
for view in views:
    draws, wall, p95 = (view[key] for key in ('Draw Calls Count', 'meanWallMs', 'p95WallMs'))
    lines.append(f'| {view["view"]} | {draws["off"]:.1f} / {draws["on"]:.1f} | {wall["off"]:.3f} / {wall["on"]:.3f} | {p95["off"]:.3f} / {p95["on"]:.3f} |')
(folder / 'comparison.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print('\n'.join(lines))
