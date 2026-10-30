"""Offline source/reference gallery; no browser or native acceptance claims."""
import hashlib
import json
import csv
from html.parser import HTMLParser
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
art = root / 'ArtSource/Original/ArtPass-v1'
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 23)
views = ('front', 'rear', 'left', 'right', 'top', 'bottom')
families = [(art / 'Characters/References' / n, 2.2, n) for n in ('Player', 'Bot01', 'Bot02', 'Bot03')]
families += [(art / 'Nature/Canopy', 1.35, 'SG-CANOPY-v1'), (art / 'Devices/HealthCore', 1.25, 'SG-MEDICAL-v1')]
for directory, span, name in families:
    sheet = Image.new('RGB', (2352, 1730), '#102029'); draw = ImageDraw.Draw(sheet)
    draw.text((25, 15), f'RAIL ARENA / {name} / v1 / geometry-derived orthographic references', font=font, fill='white')
    for i, view in enumerate(views):
        x, y = (i % 3) * 784, 70 + (i // 3) * 800
        with Image.open(directory / f'{view}.png') as im: sheet.paste(im.convert('RGB'), (x, y))
        draw.text((x+15, y+10), view.upper(), font=font, fill='#9be2d5')
        draw.line((x+25,y+725,x+25+round(768/span),y+725), fill='#6ce0d0',width=3)
        draw.text((x+25,y+680), f'1 metre / view span {span:.2f} m', font=font,fill='#9be2d5')
    draw.text((25,1680), 'Master dimensions and provenance: adjacent JSON. Character rig rest pose; final Unity shading shown in gameplay gallery.',font=font,fill='white')
    sheet.save(directory / 'measured-sheet.png')

sections = [('Gameplay / integrated captures', art/'Gate2/look-prototype'), ('Menus and HUD / three resolutions', art/'UiReview')]
if (art/'Environment/BasementDecks/UnityReview').exists():sections.insert(0,('Authored basement stone / matched cameras and LODs',art/'Environment/BasementDecks/UnityReview'))
if (art/'StandaloneReview').exists():sections.insert(0,('Standalone Windows player / forced offscreen render; native swapchain and HUD excluded',art/'StandaloneReview'))
comparisons=art/'Comparisons';comparisons.mkdir(exist_ok=True)
before_views={v['id']:v for v in json.loads((art/'Gate1/baseline-views/index.json').read_text())['views']}
after_views={v['id']:v for v in json.loads((art/'Gate2/look-prototype/index.json').read_text())['views']}
comparison_records=[]
for name in ('garden-bridge','summit','basement','waterfall'):
    for field in ('position','euler'):
        assert all(abs(before_views[name][field][axis]-after_views[name][field][axis]) < .001 for axis in ('x','y','z')),(name,field)
    pair=Image.new('RGB',(2560,790),'#102029');draw=ImageDraw.Draw(pair)
    for x,label,directory in [(0,'BEFORE / serialized baseline',art/'Gate1/baseline-views'),(1280,'AFTER / integrated art',art/'Gate2/look-prototype')]:
        draw.text((x+20,18),name.upper()+' / '+label,font=font,fill='white')
        with Image.open(directory/f'{name}.png') as im:pair.paste(im.convert('RGB'),(x,70))
    pair.save(comparisons/f'{name}-before-after.png')
    comparison_records.append({'view':name,'cameraPosition':after_views[name]['position'],'cameraEuler':after_views[name]['euler'],'poseMatchTolerance':.001,'scope':'Same staged environment camera. Baseline PC pipeline versus integrated art/atmosphere; lighting changes are part of the pass. Not native gameplay acceptance.'})
(comparisons/'comparison-evidence.json').write_text(json.dumps(comparison_records,indent=2)+'\n')
sections.insert(0,('Before and after / matching environment cameras',comparisons))
html = '<!doctype html><meta charset="utf-8"><title>Rail Arena integrated art review</title><style>body{background:#102029;color:#eef0df;font:17px system-ui;margin:32px}a{color:#6ce0d0}img{max-width:100%;height:auto}figure{margin:24px 0}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(480px,1fr));gap:20px}</style><h1>Rail Arena v1 alpha / integrated art review</h1><p>Direction approved. Production continues without intermediate approval holds. Final native input, sound and artistic acceptance: Sanders playtest. Public release held.</p><p><a href="Gate1/index.html">Full baseline inventory</a> · <a href="Gate2/RifleActions/index.html">Reload motion and contacts</a> · <a href="Gate2/PRODUCTION-STATUS.md">Production status and exceptions</a></p>'
html += '<p><a href="current-asset-register.csv">Joined current inventory</a> · <a href="current-asset-manifest.json">Machine-readable current manifest</a> · <a href="CURRENT-GAPS.md">Coverage and remaining production gaps</a> · <a href="Environment/BasementDecks/index.html">Measured basement stone kit and construction layers</a></p>'
html += '<p><a href="Performance/UnderbridgeMounted/index.html">Corrected under-bridge destinations and measured wall mounting</a></p>'
for title, directory in sections:
    html += f'<h2>{title}</h2><section>'
    for p in sorted(directory.rglob('*.png')):
        html += f'<figure><img loading="lazy" src="{p.relative_to(art).as_posix()}"><figcaption>{p.stem}</figcaption></figure>'
    html += '</section>'
html += '<h2>Authored animation</h2><p><a href="Animation/index.html">Backward gaits</a> · <a href="Animation/Forward/index.html">Forward gaits</a> · <a href="Animation/Airborne/index.html">Takeoff, suspension and landing</a>: measured poses and armed/controller-driven playback. <a href="animation-production-matrix.csv">Complete action production matrix</a>.</p><h2>Measured canonical families</h2>'
for directory, _, name in families:
    html += f'<h3>{name}</h3><img loading="lazy" src="{(directory/"measured-sheet.png").relative_to(art).as_posix()}">'
(art/'integrated-review.html').write_text(html,encoding='utf-8')
class Links(HTMLParser):
    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if key in ('href','src'):
                assert (art/value).resolve().is_file(), value
Links().feed(html)
paths=[]
for directory in [root/'RailArena/Assets/RailArena/Art/ArtPass',root/'RailArena/Assets/Resources/Art',root/'RailArena/Assets/Resources/UI/ArtPassPortraits',art/'Characters',art/'Nature',art/'Devices',art/'Environment',art/'Gate2/RifleActions',art/'Animation']:
    paths += [p for p in directory.rglob('*') if p.is_file() and p.suffix not in ('.meta','.blend1','.blend2')]
paths += [root/'RailArena/Assets/Resources/Effects'/name for name in ('CelSurface.shader','CelWater.shader','CelOutline.shader','SkyGardenClouds.shader','WorldGlyph.shader','ArtAtmosphere.shader')]
entries=[{'id':'art-'+hashlib.sha256(p.relative_to(root).as_posix().encode()).hexdigest()[:16], 'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'disposition':'refine','approval':'Art direction approved; final integrated playtest pending'} for p in sorted(set(paths))]
for entry in entries:
    path = entry['path']; family = 'environment'; category = 'supporting'
    identity = next((n for n in ('Player','Bot01','Bot02','Bot03') if n in path), None)
    if '/ArtPassPortraits/' in path:
        identity=('Player','Bot01','Bot02','Bot03')[int(Path(path).stem.split('-')[-1])]
    if identity:
        family='character-'+identity; category='characters'
        spec=art/'Characters'/f'{identity}-Production.json'
        data=json.loads(spec.read_text()); dimensions=data['boundsMetresBlenderXYZ']
        master=art/'Characters'/f'{identity}-ArtPass-v1.blend'
        references=art/'Characters/References'/identity/'measurements.json'
        sheet=references.parent/'measured-sheet.png'; screenshot=art/'Gate2/look-prototype'/('player-face.png' if identity=='Player' else identity.lower()+'-face.png')
        rig=data.get('bones',[]); provenance='Existing CC0 foundation rig with upstream AI-assisted disclosure; original geometry refinements, UVs and paint. See Characters/README.md and ThirdParty/Grab3D/LICENSE.txt.'
    else:
        rig=[]; dimensions=None; provenance='Original Rail Arena code-authored paint, Blender construction or promoted existing project graphics; existing source retained.'
        master=art/'Environment/SkyGardens-Stone-Art-v1.blend'; references=art/'Environment/Environment-Master.json'; sheet=None; screenshot=art/'Gate2/look-prototype/garden-bridge.png'
        if '/BasementDecks/' in path or any(name in path for name in ('StagingStoneDeck','BasementStoneDeck')):
            family='basement-stone-decks';category='environment';directory=art/'Environment/BasementDecks';master=directory/'Basement-Stone-Decks-v1.blend';references=directory/'production.json';sheet=directory/'References/Basement/measured-sheet.png';screenshot=directory/'UnityReview/basement-top-after.png';provenance='Original layered stone, inlaid crest, underside and LOD geometry authored from exact existing collision hulls; original periodic limestone paint.'
            variant='Staging' if Path(path).name.startswith('Staging') or '/References/Staging/' in path else 'Basement' if Path(path).name.startswith('Basement') or '/References/Basement/' in path else None
            if variant:
                variant_spec=next(lod for lod in json.loads(references.read_text())['lods'] if lod['profile']==variant and lod['lod']==0)
                dimensions=[variant_spec['diameterMetres'],variant_spec['diameterMetres'],variant_spec['thicknessMetres']]
        elif '/Airborne/' in path or any(name in path for name in ('Takeoff','AirborneHold','LandingRecover','ContenderMotion.controller')):
            family='contender-airborne';category='animation';directory=art/'Animation/Airborne';master=directory/'Contender-Airborne-v1.blend';references=directory/'airborne-production.json';sheet=directory/'References/Takeoff/measured-poses.png';screenshot=directory/'Airborne-Family-Poster.png';rig=json.loads((art/'Characters/Player-Production.json').read_text()).get('bones',[]);provenance='Original authored takeoff, suspension and impact-recovery motion on the unchanged shared skeleton; simulation owns root translation.'
        elif '/Forward/' in path or 'Advance' in path:
            family='contender-advance';category='animation';directory=art/'Animation/Forward';master=directory/'Contender-Advance-v1.blend';references=directory/'forward-production.json';sheet=directory/'References/AdvanceWalk/measured-poses.png';screenshot=directory/'Advance-Family-Poster.png';rig=json.loads((art/'Characters/Player-Production.json').read_text()).get('bones',[]);provenance='Original authored forward support/swing contact paths baked on the unchanged shared contender skeleton.'
        elif '/Animation/' in path or 'ContenderRetreat' in path:
            family='contender-retreat';category='animation';directory=art/'Animation';master=directory/'Contender-Retreat-v1.blend';references=directory/'retreat-production.json';sheet=directory/'References/RetreatWalk/measured-poses.png';screenshot=directory/'Retreat-Family-Poster.png';rig=json.loads((art/'Characters/Player-Production.json').read_text()).get('bones',[]);provenance='Original authored contact paths and baked bone animation on the unchanged shared contender skeleton; inherited CC0 rig provenance retained.'
        elif 'Canopy' in path or '/Nature/' in path:
            family='canopy';category='nature';directory=art/'Nature/Canopy';master=directory/'SkyGarden-Canopy-v1.blend';references=directory/'production.json';sheet=directory/'measured-sheet.png';dimensions=[1,1,1]
        elif 'Medical' in path or 'HealthCore' in path:
            family='medical-core';category='pickups';directory=art/'Devices/HealthCore';master=directory/'Medical-Core-v1.blend';references=directory/'production.json';sheet=directory/'measured-sheet.png';dimensions=json.loads(references.read_text())['dimensionsBlenderXYZ'];screenshot=art/'Gate2/look-prototype/health.png'
        elif 'Rifle' in path or 'Reload' in path or 'Carbine' in path:
            family='carbine';category='weapons';master=art/'Gate2/RifleActions/Carbine-Reload-v1.blend';references=art/'Gate2/RifleActions/ReloadAction.json';screenshot=art/'Gate2/look-prototype/fps-hud.png'
        elif 'StoneCover' in path:
            family='stone-cover';category='environment';directory=art/'Gate2/StoneCover';master=directory/'StoneCover-v1.blend';references=directory/'reference.json';sheet=directory/'measured-sheet.png';dimensions=[2,1,1.2]
        elif 'Contenders.asset' in path or 'CharacterContour' in path:
            family='shared-contender-surface';category='characters';master=art/'Characters/Player-ArtPass-v1.blend';references=art/'Characters/Player-Production.json';sheet=art/'Characters/References/Player/measured-sheet.png';screenshot=art/'Gate2/look-prototype/player-face.png';provenance='Shared original cel presentation over the four refined contenders; existing CC0 rig provenance is preserved in Characters/README.md.'
        elif 'Atmosphere' in path or 'SkyGarden' in path or 'Cel' in path:
            family='sky-gardens-lighting';category='VFX / lighting';references=art/'Gate1/cel-material-spec.json'
    assert master.is_file(),master
    relative=lambda p:p.relative_to(root).as_posix() if p else None
    if '/ArtPassPortraits/' in path:category='UI / UX'
    entry.update({'category':category,'family':family,'mapArea':'Sky Gardens / summit / four basement routes','runtimeUsage':'Production review build' if path.startswith('RailArena/') else 'Editable source or reference evidence; not runtime content','sourceMaster':relative(master),'sourceExportLocations':[path],'dimensionsMetresBlenderXYZ':dimensions,'dimensionAuthority':relative(references),'dependencies':[relative(references)],'referenceSheets':[relative(sheet)] if sheet else [],'rigBones':rig,'clips':['Carbine reload / 120 ticks'] if family=='carbine' else [],'provenance':provenance,'currentScreenshot':relative(screenshot),'referenceException':'Family-shared specifications apply to supporting textures/materials. Remaining per-asset construction/variant evidence is explicitly incomplete, pending production rather than silently approved.'})
    if family in ('contender-retreat','contender-advance','contender-airborne'):
        entry['clips']=[f"{clip['name']} / {clip['frames']} frames / {clip['fps']} Hz" for clip in json.loads(references.read_text())['clips']]
        entry['dependencies'] += ['RailArena/Assets/Resources/Art/ContenderRetreat.overrideController','RailArena/Assets/Resources/Art/ContenderMotion.controller','ArtSource/Original/ArtPass-v1/Characters/Player-Production.json']
        entry['runtimeUsage']='Sky Gardens art profile / four contenders' if path.startswith('RailArena/') else 'Editable animation master or measured playback evidence; not runtime content'
    if family=='basement-stone-decks':
        entry['mapArea']='Sky Gardens / four staging and four basement decks'
        entry['referenceSheets']=[relative(art/'Environment/BasementDecks/References'/name/'measured-sheet.png') for name in ('Staging','Basement')]
        entry['dependencies'] += [relative(art/'Environment/BasementDecks/deck-source.json'),relative(art/'Environment/BasementDecks/paint-spec.json'),'RailArena/Assets/RailArena/Editor/BasementRouteBuilder.cs']
        entry['runtimeUsage']='Conditional Sky Gardens art profile / eight visual decks; original colliders retained' if path.startswith('RailArena/') else 'Canonical source, paint or measured/reference evidence; not installed artwork by itself'
    if Path(path).suffix.lower()=='.png':
        with Image.open(root/path) as texture:entry['textureDimensionsPixelsWH']=list(texture.size)
(art/'integrated-manifest.json').write_text(json.dumps({'schema':'rail-arena-art-delta/2','baseline':'Gate1/asset-manifest.json','entries':entries,'coverage':'Baseline inventory plus production delta; remaining reference and animation exceptions in PRODUCTION-STATUS.md'},indent=2)+'\n')
with (art/'integrated-asset-register.csv').open('w',newline='',encoding='utf-8') as stream:
    columns=['id','path','category','family','mapArea','runtimeUsage','sourceMaster','dimensionsMetresBlenderXYZ','dimensionAuthority','dependencies','referenceSheets','rigBones','clips','provenance','approval','currentScreenshot','disposition','referenceException','sha256']
    writer=csv.DictWriter(stream,columns,extrasaction='ignore');writer.writeheader();writer.writerows({k:json.dumps(v) if isinstance(v,(dict,list)) else v for k,v in e.items()} for e in entries)
print('INTEGRATED_ART_PACKAGE',len(entries),'assets;',len(families)*6,'measured views')
