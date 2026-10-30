"""Compose the measured construction and in-engine prototype review; offline verification only."""
import hashlib
from html.parser import HTMLParser
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent.parent
folder = root / 'ArtSource/Original/ArtPass-v1/Gate2'
cover = folder / 'StoneCover'
captures = folder / 'look-prototype'
font_path = Path('C:/Windows/Fonts/segoeui.ttf')
font = ImageFont.truetype(str(font_path), 26)
small = ImageFont.truetype(str(font_path), 19)
sheet = Image.new('RGB', (2368, 1820), '#101c23')
draw = ImageDraw.Draw(sheet)
draw.text((32, 24), 'RAIL ARENA / SG-COVER-2M-v1 / canonical geometry', font=font, fill='#e8e8db')
draw.text((32, 65), 'Blender metres: X 2.00 / Y 1.00 / Z 1.20 | Unity: X 2.00 / Y 1.20 / Z 1.00 | pivot: envelope center', font=small, fill='#90d8d5')
for index, name in enumerate(('front', 'rear', 'left', 'right', 'top', 'bottom')):
    x, y = 16 + index % 3 * 784, 120 + index // 3 * 800
    with Image.open(cover / (name + '.png')) as image:
        assert image.size == (768, 768)
        sheet.paste(image.convert('RGB'), (x, y))
    draw.text((x + 18, y + 12), name.upper(), font=font, fill='white')
    width = round(768 / 2.6)
    draw.line((x + 30, y + 720, x + 30 + width, y + 720), fill='#50dfdb', width=3)
    draw.text((x + 30, y + 680), '1.00 m / same 2.60 m view span', font=small, fill='#50dfdb')
draw.text((32, 1750), '19 editable parts / four material families / original Blender source / construction candidate, Gate 2 review pending', font=small, fill='#c4cfcc')
sheet.save(cover / 'measured-sheet.png')
comparison = Image.new('RGB', (2560, 800), '#101c23')
draw = ImageDraw.Draw(comparison)
for x, name, title in [(0, 'stone-before', 'BEFORE / current source geometry'), (1280, 'stone-after', 'AFTER / authored cover + cel surface prototype')]:
    with Image.open(captures / (name + '.png')) as image:
        assert image.size == (1280, 720)
        comparison.paste(image.convert('RGB'), (x, 80))
    draw.text((x + 24, 22), title, font=font, fill='#e8e8db')
comparison.save(folder / 'cover-comparison.png')
record = json.loads((cover / 'reference.json').read_text())
for key, field in [('masterSha256', 'master'), ('exportSha256', 'export')]:
    assert hashlib.sha256((root / record[field]).read_bytes()).hexdigest() == record[key]
assert len(record['views']) == 6
assert record['unityDimensionsMetresXYZ'] == [2, 1.2, 1]
html = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rail Arena / Gate 2 production</title><style>body{margin:0;background:#101c23;color:#e8e8db;font:17px/1.6 system-ui}main{max-width:1440px;margin:auto;padding:32px}h1{font-size:42px}a{color:#73ded9}img{width:100%;height:auto;border:1px solid #35545a}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:24px}.tag{color:#73ded9}figure{margin:24px 0}figcaption{color:#bdccc9}</style><main><p class="tag">V1 ALPHA / GATE 1 APPROVED / GATE 2 IN PRODUCTION</p><h1>Cel surface and stone construction prototype</h1><p>This is the first production batch, not the complete playable slice. <a href="PRODUCTION-STATUS.md">Remaining slice work and reproduction commands</a> · <a href="../Gate1/APPROVAL-RECORD.json">Sanders approval record</a></p><figure><img src="cover-comparison.png" alt="Same camera before and after the authored stone cover"><figcaption>Same camera, geometry footprint and scene. Static captures disable motion blur in the temporary session. Final animation, art and performance acceptance remain open.</figcaption></figure><figure><img src="StoneCover/measured-sheet.png" alt="Measured six-view cover construction sheet"><figcaption><a href="StoneCover/StoneCover-v1.blend">Editable canonical Blender master</a> · <a href="StoneCover/reference.json">Dimensions, provenance and hashes</a></figcaption></figure><div class="grid">'''
for name, caption in [('fps-hud', 'First-person material prototype; HUD refinement pending'), ('player', 'Existing player with shared cel response; geometry refinement pending'), ('garden-bridge', 'Current garden-to-bridge baseline under the prototype'), ('portal', 'Portal specialist shader retained'), ('waterfall', 'Current water shader retained; slice treatment pending'), ('pad', 'Current jump pad specialist retained')]:
    html += f'<figure><img src="look-prototype/{name}.png" alt="{caption}"><figcaption>{caption}</figcaption></figure>'
html += '</div></main></html>'
(folder / 'index.html').write_text(html, encoding='utf-8')


class Links(HTMLParser):
    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if key in ('src', 'href'):
                assert (folder / value).resolve().is_file(), value


Links().feed(html)
report = {'passed': True, 'measuredViews': 6, 'sourceAndExportHashesVerified': True, 'comparisonCamera': 'Identical staged camera, presentation removed/reapplied; no simulated gameplay acceptance', 'browserRendering': 'Not established; offline link checks and direct image inspection only', 'gate2Complete': False}
(folder / 'review-verification.json').write_text(json.dumps(report, indent=2) + '\n')
asset_paths = []
for directory in ['RailArena/Assets/RailArena/Art/ArtSlice', 'RailArena/Assets/Resources/Art', 'RailArena/Assets/RailArena/Art/ArtPass', 'ArtSource/Original/ArtPass-v1/Characters', 'ArtSource/Original/ArtPass-v1/Gate2/RifleActions']:
    asset_paths.extend(p for p in (root / directory).rglob('*') if p.is_file() and p.suffix not in ('.meta', '.blend1'))
for relative in ['RailArena/Assets/Resources/Effects/CelSurface.shader', 'RailArena/Assets/RailArena/Runtime/CelArtProfile.cs', 'RailArena/Assets/RailArena/Runtime/ArtSlicePresentation.cs', 'RailArena/Assets/RailArena/Editor/ArtSliceBuilder.cs', 'ArtSource/Original/ArtPass-v1/Gate2/StoneCover/StoneCover-v1.blend']:
    asset_paths.append(root / relative)
entries = []
for path in asset_paths:
    relative = path.relative_to(root).as_posix()
    entries.append({'id': 'art-slice-' + hashlib.sha256(relative.encode()).hexdigest()[:16], 'path': relative, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'disposition': 'refine', 'approval': 'Gate 2 candidate; direction approved, finished production pending', 'provenance': 'Original Rail Arena art or code authored for this pass', 'reference': 'StoneCover/reference.json' if 'StoneCover' in path.name else '../Gate1/cel-material-spec.json', 'runtimeScope': 'ArtSliceBuilder review build only; SkyGardensBasement; ordinary builds unchanged'})
(folder / 'asset-manifest-update.json').write_text(json.dumps({'schema': 'rail-arena-art-inventory-delta/1', 'base': '../Gate1/asset-manifest.json', 'mapArea': 'Sky Gardens wing covers and shared slice surface treatment', 'entries': entries, 'requirementsRemaining': 'PRODUCTION-STATUS.md'}, indent=2) + '\n')
print(json.dumps(report))
