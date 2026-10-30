"""Package the real Blender output beside its source references, offline."""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'ArtSource/Original/Foundry-Models-v1'
REF = '../Foundry-Arena-v2/References-v1/'


def main():
    report = json.loads((OUT/'Model-Validation.json').read_text())
    groups = {
        'Structure': ('01-Structural-Kit.png', ['FLOOR-4','FLOOR-4x2','LANDING','EDGE','BEAM','RAMP']),
        'Competitive cover': ('02-Cover-Kit-v2.png', [f'COVER-{i:02d}' for i in range(1,9)]),
        'Jump pad': ('03-Jump-Pad.png', ['PAD']),
        'Portal': ('04-Portal.png', ['PORTAL']),
        'Health': ('05-Health-Pickup.png', ['HEALTH']),
        'Environment dressing': ('06-Dressing-Landmarks.png', ['VENT','CONDUIT','FLOODLIGHT']),
    }
    by_id = {x['id']: x for x in report['assets']}
    def image(src, label):
        return f'<a href="{src}"><img loading="lazy" src="{src}" alt="{html.escape(label)}"></a><p>{label}</p>'
    cards = []
    for title, concept, names in [(k,*v) for k,v in groups.items()]:
        cards.append(f'<section><h2>{title}</h2><div class="comparison"><div class="reference">'+
                     image(REF+'Concepts/'+concept,'Concept reference · click to inspect full size')+'</div><div>')
        for name in names:
            item=by_id[name]
            dims=' × '.join(f'{n:.3f}' for n in item['dimensions'])
            cards.append(f'<article><h3>{name}</h3>'+image(f'Renders/{name}-Studio.png','Modeled candidate · same editable master')+
                         f'<p>{dims} m art bounds · {item["triangles"]:,} triangles · {item["parts"]} editable parts</p>'+
                         f'<a href="Exports/{name}.glb">Download model (.glb)</a>')
            views=[x for x in report['renders'] if x['name'].startswith(name+'-') and not x['name'].endswith('Studio')]
            if views:
                cards.append('<details><summary>Six construction views</summary><div class="views">'+''.join(
                    '<div>'+image('Renders/'+x['name']+'.png',x['name'].rsplit('-',1)[-1])+'</div>' for x in views)+'</div></details>')
            cards.append('</article>')
        cards.append('</div></div></section>')
    map_cards=[]
    for name,ref in [('Arena-SW','Overview-SW'),('Arena-NE','Overview-NE'),('Arena-Top','Top'),
                     ('Underpass','Player-Underpass'),('Crown','Player-Crown'),
                     ('West-Wing','Player-West-Wing'),('North-Gate','Player-North-Gate')]:
        map_cards.append('<h3>'+name+'</h3><div class="comparison"><div>'+image(REF+'Measured/'+ref+'.png','Measured placement reference')+
                         '</div><div>'+image('Renders/'+name+'.png','Modeled arena · matching camera')+'</div></div>')
    content='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rail Arena / Foundry model review</title><style>
*{box-sizing:border-box}body{margin:0;background:#101a20;color:#e5e7df;font:16px/1.55 system-ui,sans-serif}
main{max-width:1600px;margin:auto;padding:40px}h1{font-size:clamp(30px,4vw,64px);line-height:1.1}
h2{padding-top:25px;border-top:1px solid #36505a}a{color:#6ce3ed}img{display:block;width:100%;height:auto;background:#26343b;border-radius:8px}
.comparison{display:grid;grid-template-columns:1fr 1fr;gap:22px;align-items:start}.reference{position:sticky;top:15px}
article{margin-bottom:35px;padding:16px;background:#192830;border:1px solid #324a55;border-radius:10px}
p{color:#b9c9ce}.views{display:grid;grid-template-columns:1fr 1fr;gap:10px}summary{cursor:pointer;padding:15px 0}
.status{border-left:4px solid #54d8e4;background:#20323b;padding:18px}nav{display:flex;gap:20px;flex-wrap:wrap;margin:25px 0}
@media(max-width:850px){.comparison{grid-template-columns:1fr}.reference{position:static}main{padding:20px}}
</style><main><p>RAIL ARENA · SANDERS RIDDLE</p><h1>Foundry / modeled environment</h1>
<p class="status">Creator-review candidates. Built headlessly from the 56 × 40 metre v2 placement master.
The playable Unity arena has not been replaced. Visual fidelity, route feel and competitive balance await review.</p>
<nav><a href="Foundry-Environment.blend">Editable Blender master</a><a href="Model-Validation.json">Geometry and layout report</a>
<a href="Export-Audit.json">Export audit</a><a href="Measured-Comparison.png">Layout comparison board</a>
<a href="Master-Audit.json">Saved Blender master audit</a>
<a href="README.md">Scope and limitations</a><a href="../Foundry-Arena-v2/References-v1/index.html">Full original reference gallery</a></nav>
<h2>Whole arena / matching cameras</h2>'''+''.join(map_cards)+''.join(cards)
    content+='<h2>Unplaced prop proposals</h2><p>These do not add cover to the competitive layout. Their detailing is an original interpretation; no dedicated approved sheet exists.</p><div class="comparison">'
    for name in ('CRATE','CABINET'):
        content+='<div>'+image(f'Renders/{name}-Studio.png',name)+f'<a href="Exports/{name}.glb">Download model</a></div>'
    content+='</div><h2>Review boundaries</h2><ul>'+''.join('<li>'+html.escape(x)+'</li>' for x in report['limitations'])+'</ul></main></html>'
    (OUT/'index.html').write_text(content,encoding='utf-8')
    readme=f'''# Foundry environment / modeled review candidates

[Side-by-side review gallery](index.html) · [Editable master](Foundry-Environment.blend) · [Geometry report](Model-Validation.json)

Created at the creator's request to begin modeling. This authorizes model review work; it does not record approval of the exact layout, completed gameplay integration or competitive qualification.

## Delivered

- {len(report['assets'])} editable asset assemblies, individual Y-up metre-scale GLB exports, {len(report['renders'])} actual Blender renders.
- Exact v2 deck placements, thirty cover envelopes, four ramps, four pads, two linked gate housings and three health mounts in a shared art assembly.
- Six additional orthographic construction views for floor, low cover, ramp, pad, portal and health.
- Closed hidden surfaces, single-facet bevels, material separation and generated UV islands. No photographic textures.
- Independently selectable original collision boxes. Art does not silently become collision.
- Shared ivory ceramic, graphite, teal, copper, restrained cyan and amber materials.

## Construction decisions and remaining work

Map.json is the dimensional authority. Floor bays use linked reusable meshes, with scaled closure bays where needed. Cover remains opaque within its source gameplay volume; the current elbow variants use that envelope rather than introducing a new see-through corner. Small applied face plates extend beyond collision by up to approximately 4cm and need intersection review before integration. This is a modeled first pass, not a claim of perfect concept reproduction.

Portal artwork retains a floor-level 1.4 × 2.2m opaque field and solid rear. Gate state VFX, A/B labels, portal traversal and jump-pad impulses are not implemented by these static meshes. The 2.4 × .8 × 2.8m portal source envelope is offset vertically in Map.json; its bottom is -.1m. The art retains that placement. Health pedestal is .8 × .8 × .25m; its separate hovering marker is .35 × .5m. Amber identifies health consistently.

The arena assembly uses source geometry and source cameras. No additional wall bays, railings or supports are placed where they could change routes or sightlines. Beam and edge assets are available in the library but await explicit placement review. Dressing objects need final inset mounting checks. Crate and cabinet are unplaced proposals.

Cloud sky artwork, distant islands/citadels, directional landmarks/signs, state animation, final texture atlases, LODs, collision/occlusion qualification and actual Unity import/playtesting remain outstanding. The renders use bright neutral world lighting, not a final skybox. UVs are authoring islands, not a final baked atlas. No performance or release readiness claim is made.

## Review

Compare the whole arena against matching measured cameras, then review each modeled asset beside its concept sheet. Approve or revise silhouettes, panel construction, material proportions and device clarity before integration. The six construction views and GLBs are generated from the same mesh collections as the editable master.

## Reproduce headlessly

Run `tools/build_foundry_models.py` through background Blender 5.2.2 with four threads and a bounded owned-process wrapper, then `python tools/package_foundry_models.py`. The script writes only this new package; it does not modify Unity runtime assets or earlier source references.
'''
    (OUT/'README.md').write_text(readme,encoding='utf-8')
    assert all(not x['geometryIssues'] for x in report['assets'])
    assert all((OUT/'Renders'/f'{r["name"]}.png').is_file() for r in report['renders'])
    print(f'Packaged {len(report["assets"])} assets and {len(report["renders"])} renders')


if __name__ == '__main__':
    main()
