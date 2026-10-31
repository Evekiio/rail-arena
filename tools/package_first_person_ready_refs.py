"""Compose measured weapon-only construction references for the ready action."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady'
spec = json.loads((folder / 'ReadyAction.json').read_text(encoding='utf-8'))
views = json.loads((folder / 'measured-views.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder / spec['master']).read_bytes()).hexdigest() == spec['masterSha256'] == views['masterSha256']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
sheet = Image.new('RGB', (2304, 2730), '#102029')
draw = ImageDraw.Draw(sheet)
draw.text((25, 15), 'RAIL ARENA / FIRST-PERSON READY / 18 FRAMES / 60 HZ / 0.3 SECONDS', font=font, fill='white')
draw.text((25, 52), 'Canonical rifle geometry / weapon-only construction poses. Native arms, camera and ADS reviewed separately.', font=small, fill='#b4d9d2')
for row, view in enumerate(('front', 'rear', 'left', 'right', 'top', 'bottom')):
    for column, frame in enumerate(views['frames']):
        x, y = column * 384, 95 + row * 425
        with Image.open(folder / 'References' / view / f'{frame:02d}.png') as source:
            sheet.paste(source.convert('RGB').resize((384, 384)), (x, y + 25))
        draw.text((x + 8, y), f'{view.upper()} / {frame}f / {frame/60:.3f}s', font=small, fill='#b4d9d2')
        draw.line((x + 12, y + 395, x + 12 + round(384 / 1.6), y + 395), fill='#48cdd1', width=3)
        draw.text((x + 12, y + 402), '1 m / 1.6 m shared view span', font=small, fill='#b4d9d2')
draw.text((25, 2665), 'FPS-READY-v1 / ' + spec['masterSha256'][:32] + ' / original rifle / calibrated endpoint', font=small, fill='white')
sheet.save(folder / 'Ready-Measured-Sheet.png')
(folder / 'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>Rail Arena / First-person ready</title>
<style>body{background:#102029;color:#dfeee9;font:18px system-ui;margin:32px}a{color:#6ed7d2}img,video{max-width:100%}</style>
<h1>First-person ready / original Blender action</h1><p>An original 0.3-second rifle raise settles at the exact calibrated rest pose. The visual track does not delay firing, change accuracy, move the player or alter the physical ADS alignment. Original calibrated hands follow the weapon grips.</p>
<p><a href="Carbine-FirstPerson-Ready-v1.blend">Editable Blender master</a> / <a href="ReadyAction.json">Unity-ready evaluated track and provenance</a> / <a href="measured-views.json">Metre-scaled reference frames</a></p>
<img src="Ready-Measured-Sheet.png"><p>Six equal-scale views at six moments. These source images show weapon construction and motion only. Native contact, interruption and camera acceptance are recorded separately. Shipping integration and final artistic acceptance remain pending.</p>''', encoding='utf-8')
print('FPS_READY_MEASURED_PACKAGE_READY', spec['masterSha256'])
