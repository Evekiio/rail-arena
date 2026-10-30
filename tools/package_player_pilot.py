"""Build an offline review gallery and a checksummed, editable pilot handoff."""
import hashlib
import html
import json
from pathlib import Path
import shutil
import zipfile
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
out=root/'ArtSource/Original/PlayerPilot-v1'
refs=root/'ArtSource/Original/StylizedContenders/References-v1/Final'
assets=root/'RailArena/Assets/RailArena/Art/PlayerPilot'
validation=root/'Validation/PlayerPilot-v1'


def write_json(path,value):
    path.write_text(json.dumps(value,indent=2)+'\n')


def main():
    required=['Player-Pilot.blend','Player-Pilot-Carbine-Fit.blend','Model.json',
        'Geometry-Validation.json','Unity-Validation.json','Blender-Fit-Validation.json',
        'Unity-Carbine-Fit.png','Player-Motion-Review.mp4']
    for name in required:
        if not (out/name).is_file():raise RuntimeError('Missing handoff evidence: '+name)
    protected=json.loads((validation/'Protected-Baseline.json').read_text(encoding='utf-8-sig'))
    for item in protected:
        item['currentSha256']=hashlib.sha256((root/item['path']).read_bytes()).hexdigest().upper()
        item['unchanged']=item['currentSha256']==item['sha256']
    if not all(item['unchanged'] for item in protected):raise RuntimeError('Protected asset changed')
    write_json(out/'Protected-Assets.json',protected)
    model=json.loads((out/'Model.json').read_text())
    geometry=json.loads((out/'Geometry-Validation.json').read_text())
    unity=json.loads((out/'Unity-Validation.json').read_text())
    if not geometry['passed'] or not unity['passed']:raise RuntimeError('Validation did not pass')
    tests=ET.parse(validation/'EditMode.xml').getroot()
    if tests.get('result')!='Passed' or int(tests.get('failed','1'))!=0:raise RuntimeError('Editor tests did not pass')
    samples=[s for s in unity['samples'] if s['phase']!='Death']
    metrics=dict(maximumWristErrorMetres=max(max(s['rightWristError'],s['leftWristError']) for s in samples),
        maximumShoulderErrorMetres=max(s['shoulderError'] for s in samples),
        animationSamples=len(unity['samples']),editorTestsPassed=int(tests.get('passed','0')),lods=geometry['lods'])
    write_json(out/'Review-Metrics.json',metrics)
    ref_out=out/'References';ref_out.mkdir(exist_ok=True)
    for path in [refs/'Approved-Concept.png',*refs.glob('Player-*.png'),refs/'MODELING-GUIDE.md']:
        shutil.copy2(path,ref_out/path.name)
    readme=f'''# Player character pilot v1

Creator-review candidate. The installed player, bots, carbine prefab and previous player Blender master are unchanged; their hashes are recorded in `Protected-Assets.json`.

## Review

Open `index.html` locally for the reference/model comparison, actual Unity captures, LODs and five-second motion sequence. Nothing opens automatically.

- `Player-Pilot.blend`: neutral editable master, {model['editableParts']} individually named mesh parts, a connected facial/neck volume, layered hair, suit, armor, fingerless gloves and treaded soles. Native materials and packed reference images are retained.
- `Player-Pilot-Carbine-Fit.blend`: separate editable assembly with the existing production carbine and a measured Unity idle pose keyed on frame 1. The weapon remains separated into editable parts.
- The licensed project skeleton is retained. Thirty finger deformation joints survive runtime export. Optional `CTRL-hand/foot` and pole bones are authoring controls; `Arm IK` and `Leg IK` properties default to zero. Runtime exports use deformation bones only. This is not a facial expression rig or a new animation library.
- Three FBX LODs use one runtime material and unique `SurfaceUV` islands. The 2048px base-color, metallic/smoothness and emission atlases encode authored surface constants. No high-resolution sculpted normal bake or cloth simulation is claimed.
- Unity review prefab and scene: `RailArena/Assets/RailArena/Art/PlayerPilot/Player-Pilot.prefab` and `Player-Pilot-Review.unity`. Presentation scripts are disabled for isolated review. This prefab is not registered as a playable appearance.

## Validation

Blender geometry checks pass: finite coordinates, nondegenerate exported triangles, UV presence, normalized skinning with at most four influences, human scale, thirty finger bones and descending LOD topology.

Unity {unity['unityVersion']} sampled five phases at 30 steps each and baked all three LODs at each step. Maximum wrist target error: {metrics['maximumWristErrorMetres']*1000:.3f} mm. Maximum shoulder anchor error: {metrics['maximumShoulderErrorMetres']*1000:.3f} mm. These measure skeletal anchors, not palm/trigger surface clearance. Exact results are in `Unity-Validation.json`. The separate motion video contains 120 fixed-step Unity captures at 24 fps: idle, run, backward, jump and death, one second each. Root translation/jump travel is not simulated. It is an animation review, not gameplay footage.

## Artistic comparison and limits

The approved concept is the primary design authority. The expanded player sheets are generated appearance/construction references, not calibrated views of a shared finished mesh. Source hashes and construction decisions are in `Model.json`.

The pilot preserves ivory/charcoal/teal/cyan identity, compact front/back armor, paired pouches, broad bordered thigh panels, fingerless hands and the specified right outer ankle inset. It adds actual undersides, sole tread and coherent editable construction. It remains an interpretation: the face, swept hair, shoulder-shell profile and scarf drape are visibly more procedural than the illustrations. Do not describe it as an exact reproduction or final artistic approval. Review those areas, finger contact, skin/armor intersections, silhouette under motion and LOD transitions before gameplay replacement.

Headless validation does not establish physical-input acceptance, gameplay feel, audio quality or measured four-contender performance. No release or GitHub publication is performed.

## Reproduce

Run sequentially from the repository root:

```powershell
.\\tools\\Build-PlayerPilot.ps1 -Stage Model
.\\tools\\Build-PlayerPilot.ps1 -Stage Audit
.\\tools\\Build-PlayerPilot.ps1 -Stage Render
.\\tools\\Build-PlayerPilot.ps1 -Stage Import
.\\tools\\Build-PlayerPilot.ps1 -Stage Tests
.\\tools\\Build-PlayerPilot.ps1 -Stage Capture
.\\tools\\Build-PlayerPilot.ps1 -Stage Fit
```

Blender 5.2.2 and Unity 6000.6.7f1 are used with bounded hidden/background processes and four workers. Capture needs an offscreen graphics device. Encode `Motion/frame-%04d.png` at 24 fps with FFmpeg to `Player-Motion-Review.mp4`, then run `tools/package_player_pilot.py` with Python to recreate the gallery/ZIP. The production carbine and original rig dependencies must already exist in the workspace.

## Methods and provenance

Reference planning, named interfaces and inspection/repair were adapted from powerhouse90/Blender-Superskill. Reference classification, comparison and explicit validation boundaries were informed by RobLe3/cc-blender-skill. These are workflow references, not bundled third-party code or a claim that a Blender MCP backend was installed. The project construction helpers and licensed CC0 skeleton/animation provenance remain applicable. No third-party visible character mesh was reused.
'''
    (out/'README.md').write_text(readme,encoding='utf-8')
    options=[('Player-Studio.png','Blender studio'),('Unity-ThreeQuarter.png','Unity neutral'),
        ('Unity-Carbine-Fit.png','Unity carbine fit'),('Blender-Carbine-Fit.png','Editable Blender carbine assembly'),
        ('Player-Face.png','Blender face'),('Unity-Face.png','Unity face'),
        *[(f'Turn-{name}.png',name) for name in ('Front','FrontRight','Right','BackRight','Back','BackLeft','Left','FrontLeft')],
        ('Top.png','Top'),('Underside.png','Underside'),('Head-Crown.png','Crown'),('Head-UnderJaw.png','Under jaw'),
        ('Boot-Sole.png','Sole'),*[(f'Unity-LOD{i}.png',f'Unity LOD {i}') for i in range(3)]]
    options.extend((f'Unity-Contact-{name}.png',f'Contact: {name}') for name in ('RightHand','LeftHand','Shoulder'))
    for name,label in options:
        if not (out/name).is_file():raise RuntimeError('Missing review view: '+label)
    select=''.join(f'<option value="{file}">{html.escape(label)}</option>' for file,label in options)
    reference_options=''.join(f'<option value="References/{name}">{label}</option>' for name,label in [
        ('Player-Turnaround.png','Player turnaround'),('Player-Head.png','Head reference'),
        ('Player-Details.png','Construction details'),('Player-Axial.png','Guided axial concepts'),('Approved-Concept.png','Approved starting concept')])
    page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rail Arena | Player pilot</title><style>
*{box-sizing:border-box}body{margin:0;background:#10161c;color:#edf0ee;font:16px/1.5 system-ui,sans-serif}main{max-width:1600px;margin:auto;padding:32px}h1{font-size:clamp(30px,4vw,56px);line-height:1.1;margin:12px 0}p{max-width:1000px;color:#c3cdd1}.tag{color:#65d7e6;letter-spacing:.16em;text-transform:uppercase;font-size:12px}select,a{font:inherit}select{width:100%;padding:12px;background:#25323b;color:white;border:1px solid #69808b;border-radius:8px}a{color:#75deeb}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px}.panel{background:#1a252d;padding:16px;border-radius:12px}.panel img{width:100%;height:680px;object-fit:contain;margin-top:12px;background:#11171b}video{width:100%;max-height:760px;background:#11171b}.links{display:flex;gap:24px;flex-wrap:wrap;margin:24px 0}.stats{display:flex;gap:32px;flex-wrap:wrap;padding:20px 0}.stats b{font-size:24px;color:#fff;display:block}small{color:#a7bac2}@media(max-width:900px){main{padding:18px}.grid{grid-template-columns:1fr}.panel img{height:540px}}</style>
<main><span class="tag">Rail Arena / original art pilot / creator review</span><h1>Player character pilot</h1>
<p>Editable Blender character, shared humanoid rig and production-carbine fitting, reviewed through actual Unity imports and animation samples. The current playable player and bots are preserved.</p>
<div class="stats">METRICS</div><div class="links"><a href="Player-Pilot.blend">Neutral Blender master</a><a href="Player-Pilot-Carbine-Fit.blend">Fitted Blender assembly</a><a href="README.md">Handoff and limits</a><a href="Unity-Validation.json">Unity measurements</a></div>
<p><strong>Comparison:</strong> reference sheets are generated design illustrations. Different poses and cameras make this a visual comparison, not a calibrated pixel overlay.</p>
<div class="grid"><section class="panel"><label for="reference">Design reference</label><select id="reference">REFERENCES</select><img id="refImage" src="References/Player-Turnaround.png" alt="Player design reference"></section>
<section class="panel"><label for="model">Actual model render</label><select id="model">OPTIONS</select><img id="modelImage" src="Player-Studio.png" alt="Actual player model render"></section></div>
<h2>Motion and contact review</h2><p>Five seconds at 24 fps: idle, forward run, backward movement, jump pose and death. Fixed-step in-engine animation samples; this is not a gameplay recording.</p><video controls preload="metadata" poster="Unity-Carbine-Fit.png" src="Player-Motion-Review.mp4"></video>
<h2>Review focus</h2><p>Facial likeness and hair flow; scarf drape and shoulder profile; fine trigger/palm contact; suit and armor intersections during motion; LOD silhouettes. The illustrations have more sculpted detail than this procedural pilot. Skeletal contact measurements do not certify surface contact or artistic approval.</p>
<small>No gameplay replacement, public release, physical-input acceptance or performance certification is implied.</small></main>
<script>document.getElementById('reference').addEventListener('change',e=>document.getElementById('refImage').src=e.target.value);document.getElementById('model').addEventListener('change',e=>document.getElementById('modelImage').src=e.target.value);</script></html>'''
    stats=f'<div><b>{model["editableParts"]}</b>editable source parts</div><div><b>{" / ".join(str(x["triangles"]) for x in geometry["lods"])}</b>LOD triangles</div><div><b>1</b>runtime surface material</div><div><b>{metrics["maximumWristErrorMetres"]*1000:.3f} mm</b>maximum wrist anchor error</div>'
    (out/'index.html').write_text(page.replace('METRICS',stats).replace('REFERENCES',reference_options).replace('OPTIONS',select),encoding='utf-8')
    files=[p for p in out.rglob('*') if p.is_file() and p.name!='manifest.json' and not p.name.startswith('Blockout-') and p.suffix not in ('.zip','.blend1') and 'Motion' not in p.parts]
    files.extend(p for p in assets.rglob('*') if p.is_file())
    files.extend(root/'tools'/name for name in ('build_player_pilot.py','Build-PlayerPilot.ps1','package_player_pilot.py','build_stylized_contenders.py'))
    files.extend(root/name for name in ('RailArena/Assets/RailArena/Editor/PlayerPilotBuilder.cs','RailArena/Assets/RailArena/Tests/Editor/PlayerPilotTests.cs'))
    files.extend(p for p in validation.glob('*') if p.is_file() and p.suffix in ('.xml','.json'))
    write_json(out/'manifest.json',dict(files=[dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files],state='Creator-review candidate; not installed as playable character'))
    files.append(out/'manifest.json')
    with zipfile.ZipFile(out/'RailArena-Player-Pilot-v1.zip','w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
        for path in files:archive.write(path,path.relative_to(root).as_posix())
    with zipfile.ZipFile(out/'RailArena-Player-Pilot-v1.zip') as archive:
        if archive.testzip() is not None:raise RuntimeError('ZIP integrity verification failed')
    print(json.dumps(dict(package=str(out/'RailArena-Player-Pilot-v1.zip'),files=len(files),metrics=metrics),indent=2))


if __name__=='__main__':main()
