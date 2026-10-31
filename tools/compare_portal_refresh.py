"""Compare full-rate and bounded distant portal refresh in one unchanged candidate."""
import argparse,json
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('baseline',type=Path);parser.add_argument('adaptive',type=Path);parser.add_argument('output',type=Path);args=parser.parse_args()
a=json.loads(args.baseline.read_text(encoding='utf-8-sig'));b=json.loads(args.adaptive.read_text(encoding='utf-8-sig'))
for key in ['schema','fingerprint','gpu','api','width','height','warmupFrames','sampleFrames','simulationHz','randomSeed','apertureCropping']:assert a[key]==b[key],key
assert a['passed'] and b['passed'] and a['exceptions']==b['exceptions']==0 and not a['adaptivePortalRefresh'] and b['adaptivePortalRefresh']
def metric(view,name,unit,category=None):
 matches=[v for v in view['counters'] if v['name']==name and v['unit']==unit and v['available'] and (category is None or v['category']==category)]
 assert len(matches)==1,(name,category,matches)
 return {key:matches[0][key] for key in ['mean','p95','maximum']}
rows=[]
for before,after in zip(a['views'],b['views']):
 assert before['view']==after['view'] and before['frames']==after['frames']==360
 assert min(before['nonBlackSamples'],after['nonBlackSamples'])>100
 row={'view':before['view']}
 for label,view in [('full',before),('adaptive',after)]:
  row[label]={key:view[key] for key in ['meanWallMs','p95WallMs','meanSubmitMs','meanReadbackMs','portalRenders','validGpuSamples','gc0','gc1','gc2','portalTargets']}
  row[label]['portalCpuNanoseconds']=metric(view,'RailArena.PortalView','TimeNanoseconds','Render');row[label]['draws']=metric(view,'Draw Calls Count','Count','Render')
 row['meanWallReductionPercent']=100*(1-after['meanWallMs']/before['meanWallMs']);row['portalCpuReductionPercent']=100*(1-row['adaptive']['portalCpuNanoseconds']['mean']/row['full']['portalCpuNanoseconds']['mean']);row['portalRenderReductionPercent']=100*(1-after['portalRenders']/before['portalRenders'])
 if row['view']=='portal':
  near_a=next(p for p in before['portalTargets'] if p['id']==0);near_b=next(p for p in after['portalTargets'] if p['id']==0);assert near_a['renders']==near_b['renders']==360 and near_b['skipped']==0 and near_b['interval']==1
 rows.append(row)
record={'schema':'rail-arena-portal-refresh-comparison/1','fingerprint':a['fingerprint'],'baseline':args.baseline.as_posix(),'adaptive':args.adaptive.as_posix(),'views':rows,'scope':a['scope'],'acceptance':'Controlled offscreen throughput evidence. Distant destination views intentionally reuse a bounded cache; nearby/scoped/moving views refresh immediately. Not native frame pacing, isolated GPU time or final artistic acceptance.'}
args.output.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
for row in rows:print(f"{row['view']}: wall {row['full']['meanWallMs']:.2f}->{row['adaptive']['meanWallMs']:.2f} ms; portal CPU {row['portalCpuReductionPercent']:.1f}% lower; portal renders {row['portalRenderReductionPercent']:.1f}% fewer")
