"""Verify and index the produced benchmark files without launching graphics apps."""
import hashlib
import html
import json
from pathlib import Path
from validate_design_package import png_info

ROOT=Path(__file__).resolve().parents[1]
PACK=ROOT/'ArtSource/Original/ProductionBenchmarks-v1'
ASSETS=ROOT/'RailArena/Assets/RailArena/Art/ProductionBenchmarks'
report=json.loads((PACK/'geometry.json').read_text())
files=[]
assert report['builderSha256']==hashlib.sha256((ROOT/'tools/build_production_benchmarks.py').read_bytes()).hexdigest()
for asset in ('RailRifle','LowWingCover'):
    for level in (0,1):
        lod=report[asset]['lod'+str(level)]
        assert lod['degenerateTriangles']==0 and lod['nonManifoldEdges']==0
        assert 0<lod['triangles']<18000
        p=ASSETS/(asset+'_LOD'+str(level)+'.fbx')
        assert p.stat().st_size>10000
        files.append(p)
    assert report[asset]['lod1']['triangles']<report[asset]['lod0']['triangles']
    assert (ASSETS/(asset+'.prefab')).is_file()
    files.append(ASSETS/(asset+'.prefab'))
assert all(abs(a-b)<.0001 for a,b in zip(report['LowWingCover']['dimensions'],[2,1,1.2]))
master=PACK/'Production-Benchmarks.blend';assert master.stat().st_size>100000;files.append(master)
required=report['views']+['RailRifle-Unity.png','LowWingCover-Unity.png']
images=[]
for name in required:
    p=PACK/name
    images.append(png_info(p));files.append(p)
unity=ROOT/'Validation/production-benchmarks-unity.txt'
assert 'PASS:' in unity.read_text()
for name in ['FoundryPalette.png','FoundryPalette.mat','CyanInlay.mat','RifleFireRecovery.anim','BenchmarkReview.unity']:
    p=ASSETS/name; assert p.is_file();files.append(p)
images.append(png_info(ASSETS/'FoundryPalette.png'))
body=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Rail Arena / reference comparison</title>',
      '''<style>body{font:17px system-ui;background:#18232b;color:#f4eedf;margin:30px auto;max-width:1600px;padding:20px}a{color:#64dbe0}img{width:100%;height:auto}section{margin:40px 0}h1{font-size:clamp(28px,4vw,48px)}p{max-width:1000px;line-height:1.6}.scroll{overflow-x:auto}.compare{display:grid;grid-template-columns:1fr 1fr;gap:16px;min-width:600px}figure{margin:0;background:#10191f;border:1px solid #45515b;border-radius:8px;overflow:hidden}figcaption{padding:14px;font-weight:650}.frame{aspect-ratio:16/11;display:flex;align-items:center;justify-content:center;background:#28333a}.frame>img{height:100%;object-fit:contain}.crop{position:relative;width:100%;aspect-ratio:1312/1200;overflow:hidden}.crop img{position:absolute;left:0;top:0;width:300%;max-width:none;height:auto}label{display:block;margin:12px 0}select{font:inherit;color:inherit;background:#10191f;padding:8px;border:1px solid #687a87;border-radius:5px}summary{cursor:pointer;padding:16px;background:#23323d}.note{color:#bdcbd1;font-size:15px}</style>''',
      '<h1>Rail Arena / reference vs model</h1><p>Compare the supplied visual target on the left with the actual model on the right. Change the render view below each heading. References are illustrations with different perspective and lighting; these panels are not scale-matched overlays.</p><p><a href="Dimensional-References-v1/index.html">Dimensioned six-view approval sheets</a> Â· <a href="README.md">Modeling handoff</a> Â· <a href="VALIDATION.md">Validation and open acceptance checks</a></p>']
body.append('<p><strong>Current rifle review:</strong> <a href="../PrecisionCarbine-References-v3/index.html">Open the current precision carbine proposal</a>. The old skeletal stock below was rejected. These benchmark renders retain the old geometry for history; the revised reference study still needs creator approval.</p>')
views=[('Unity','Unity capture'),('Studio','Blender studio'),('Left','Blender left'),('Right','Blender right'),('Top','Blender top'),('Bottom','Blender underside'),('Front','Blender front / muzzle or end'),('Rear','Blender rear / end')]
references=[('RailRifle','Ivory rail rifle','../Design-v1/Sources/User-Rifle-Reference.png','Supplied rifle reference',False),
            ('LowWingCover','Low wing cover','../Foundry-Arena-v2/References-v1/Concepts/02-Cover-Kit-v2.png','Cover concept / card A',True)]
for asset,title,ref,label,crop in references:
    assert (PACK/ref).is_file()
    options=''.join(f'<option value="{asset}-{suffix}.png">{html.escape(text)}</option>' for suffix,text in views)
    reference=(f'<div class="crop"><img src="{ref}" alt="Low wing cover concept card A, front and back"></div>' if crop else
               f'<div class="frame"><img src="{ref}" alt="{html.escape(label)}"></div>')
    body.append(f'''<section><h2>{title}</h2><label>Model view <select aria-label="{title} render view" data-image="{asset}-image" data-link="{asset}-link">{options}</select></label>
      <div class="scroll"><div class="compare"><figure><figcaption>{label}</figcaption>{reference}</figure>
      <figure><figcaption>Actual model render</figcaption><a id="{asset}-link" href="{asset}-Unity.png"><div class="frame"><img id="{asset}-image" src="{asset}-Unity.png" alt="{title} / Unity capture"></div></a></figure></div></div>
      <p class="note"><a href="{ref}">Open full original reference</a> Â· Click the render for its full-size image.</p></section>''')
body.append('<p class="note">The rifle reference is a small supplied image. Unseen construction remains an authored interpretation. The cover panel isolates card A from the full sheet without changing the source image. On narrow screens, scroll each pair horizontally to keep both panels side by side.</p><details><summary>Browse all model renders individually</summary>')
for name in required:
    body.append(f'<section><h2>{html.escape(Path(name).stem)}</h2><img loading="lazy" src="{name}" alt="{html.escape(Path(name).stem)}"></section>')
body.append('''</details><script>
document.querySelectorAll('select[data-image]').forEach(select => {
 select.addEventListener('change', () => {
  const image = document.getElementById(select.dataset.image);
  image.src = select.value;
  image.alt = select.getAttribute('aria-label') + ' / ' + select.selectedOptions[0].text;
  document.getElementById(select.dataset.link).href = select.value;
 });
});
</script></html>''')
(PACK/'index.html').write_text('\n'.join(body),encoding='utf-8')
manifest={'status':'creator review candidates; not integrated into gameplay','images':images,
          'files':[{'path':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size,
                    'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files],
          'validated':['source and LOD mesh geometry','cover envelope','headless Unity import and prefab build',
                       'Unity material mapping and muzzle axis','sampled recoil/reset','PNG integrity'],
          'open':['creator visual approval','hand/finger grip fit','gameplay collision and LOD review','combat performance']}
(PACK/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps({'verifiedImages':len(images),'checkedFiles':len(files),'status':manifest['status']}))
