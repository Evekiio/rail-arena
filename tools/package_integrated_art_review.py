"""Offline source/reference gallery; no browser or native acceptance claims."""
import hashlib
import json
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
html = '<!doctype html><meta charset="utf-8"><title>Rail Arena integrated art review</title><style>body{background:#102029;color:#eef0df;font:17px system-ui;margin:32px}a{color:#6ce0d0}img{max-width:100%;height:auto}figure{margin:24px 0}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(480px,1fr));gap:20px}</style><h1>Rail Arena v1 alpha / integrated art review</h1><p>Direction approved. Production continues without intermediate approval holds. Final native input, sound and artistic acceptance: Sanders playtest. Public release held.</p><p><a href="Gate1/index.html">Full baseline inventory</a> · <a href="Gate2/RifleActions/index.html">Reload motion and contacts</a> · <a href="Gate2/PRODUCTION-STATUS.md">Production status and exceptions</a></p>'
for title, directory in sections:
    html += f'<h2>{title}</h2><section>'
    for p in sorted(directory.rglob('*.png')):
        html += f'<figure><img loading="lazy" src="{p.relative_to(art).as_posix()}"><figcaption>{p.stem}</figcaption></figure>'
    html += '</section>'
html += '<h2>Measured canonical families</h2>'
for directory, _, name in families:
    html += f'<h3>{name}</h3><img loading="lazy" src="{(directory/"measured-sheet.png").relative_to(art).as_posix()}">'
(art/'integrated-review.html').write_text(html,encoding='utf-8')
paths=[]
for directory in [root/'RailArena/Assets/RailArena/Art/ArtPass',root/'RailArena/Assets/Resources/Art',art/'Characters',art/'Nature',art/'Devices',art/'Environment',art/'Gate2/RifleActions']:
    paths += [p for p in directory.rglob('*') if p.is_file() and p.suffix not in ('.meta','.blend1','.blend2')]
entries=[{'id':'art-'+hashlib.sha256(p.relative_to(root).as_posix().encode()).hexdigest()[:16], 'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'disposition':'refine','approval':'Art direction approved; final integrated playtest pending'} for p in sorted(set(paths))]
(art/'integrated-manifest.json').write_text(json.dumps({'schema':'rail-arena-art-delta/2','baseline':'Gate1/asset-manifest.json','entries':entries,'coverage':'Baseline inventory plus production delta; remaining reference and animation exceptions in PRODUCTION-STATUS.md'},indent=2)+'\n')
print('INTEGRATED_ART_PACKAGE',len(entries),'assets;',len(families)*6,'measured views')
