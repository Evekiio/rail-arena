"""Compare two recorded on/off profiles of the same rifle-batching player."""
import argparse
import json
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('directory',type=Path)
args=parser.parse_args()
folder=args.directory
runs={mode:json.loads((folder/(mode+'.json.run.json')).read_text(encoding='utf-8-sig')) for mode in ('off','on')}
reports={mode:json.loads((folder/(mode+'.json')).read_text(encoding='utf-8-sig')) for mode in ('off','on')}
assert runs['off']['executableSha256']==runs['on']['executableSha256']
for mode in ('off','on'):
    assert runs[mode]['exitCode']==0 and reports[mode]['passed'] and reports[mode]['exceptions']==0
    assert ('--profile-rifle-batch-off' in runs[mode]['arguments'])==(mode=='off')
for key in ('schema','fingerprint','gpu','api','width','height','warmupFrames','sampleFrames','simulationHz','randomSeed','apertureCropping'):
    assert reports['off'][key]==reports['on'][key],key
assert [view['view'] for view in reports['off']['views']]==[view['view'] for view in reports['on']['views']]
def metric(view,name):
    values=[item for item in view['counters'] if item['name']==name and item['available'] and item['mean']>0]
    assert len(values)==1,(view['view'],name)
    return {key:values[0][key] for key in ('category','unit','mean','p95')}
rows=[]
for before,after in zip(reports['off']['views'],reports['on']['views']):
    for key in ('frames','portalRenders'):assert before[key]==after[key],(before['view'],key)
    assert min(before['nonBlackSamples'],after['nonBlackSamples'])>100
    row={'view':before['view'],'frames':before['frames'],'portalRenders':before['portalRenders']}
    for mode,view in [('off',before),('on',after)]:
        row[mode]={key:view[key] for key in ('meanWallMs','p95WallMs','meanSubmitMs','meanReadbackMs','validGpuSamples','gc0','gc1','gc2')}
        row[mode].update({key:metric(view,name) for key,name in [('drawCalls','Draw Calls Count'),('triangles','Triangles Count'),('portalCpu','RailArena.PortalView')]})
    row['drawCallReductionPercent']=100*(1-row['on']['drawCalls']['mean']/row['off']['drawCalls']['mean'])
    row['meanWallReductionPercent']=100*(1-after['meanWallMs']/before['meanWallMs'])
    rows.append(row)
output=folder/'comparison.json'
assert not output.exists(),'Preserve prior comparison evidence'
record={'schema':'rail-arena-rifle-batch-profile-comparison/1','fingerprint':reports['on']['fingerprint'],'executableSha256':runs['on']['executableSha256'],'scope':reports['on']['scope'],'order':'off then on, once each; no repeated selection of passing timings','views':rows,'nativeFramePacingAccepted':False,'gpuCostIsolated':False}
output.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
for row in rows:print(row['view'],f"draw calls {row['off']['drawCalls']['mean']:.2f} -> {row['on']['drawCalls']['mean']:.2f}; reduction {row['drawCallReductionPercent']:.1f}%; wall {row['off']['meanWallMs']:.2f} -> {row['on']['meanWallMs']:.2f} ms")
