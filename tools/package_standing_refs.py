"""Compose measured geometry references for the original standing-ready candidate."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/Standing'
spec = json.loads((folder / 'standing-production.json').read_text(encoding='utf-8'))
evidence = json.loads((folder / 'baked-contact-review.json').read_text(encoding='utf-8'))
assert evidence['rendersUpdated']
assert hashlib.sha256((folder / spec['master']).read_bytes()).hexdigest() == spec['masterSha256'] == evidence['masterSha256']
clip = evidence['clips'][0]
assert clip['clip'] == 'StandingReady' and clip['boundMeshCount'] == 216
sheet = Image.new('RGB', (2304, 2730), '#102029')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
draw.text((25, 15), 'RAIL ARENA / STANDING READY / 4 SECONDS / 60 HZ / LOOP', font=font, fill='white')
draw.text((25, 52), 'Canonical geometry; unarmed pose reference. Original shared rig. Native weapon contacts reviewed separately.', font=small, fill='#b4d9d2')
for row, view in enumerate(clip['views']):
    for column, frame in enumerate(clip['poseFrames']):
        x, y = column * 384, 95 + row * 425
        with Image.open(folder / 'References/StandingReady' / f'{view}-{column}.png') as source:
            sheet.paste(source.convert('RGB').resize((384, 384)), (x, y + 25))
        draw.text((x + 8, y), f'{view.upper()} / {frame}f / {frame/60:.3f}s', font=small, fill='#b4d9d2')
        draw.line((x + 12, y + 395, x + 12 + round(384 / 2.2), y + 395), fill='#48cdd1', width=3)
        draw.text((x + 12, y + 402), '1 m / 2.2 m shared view span', font=small, fill='#b4d9d2')
draw.text((25, 2665), 'ANIM-STANDING-v1 / ' + spec['masterSha256'][:32] + ' / metre-scaled master / no actor root motion', font=small, fill='white')
sheet.save(folder / 'References/StandingReady/measured-poses.png')
(folder / 'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>Rail Arena / Standing Ready</title>
<style>body{background:#102029;color:#dfeee9;font:18px system-ui;margin:32px}a{color:#6ed7d2}img,video{max-width:100%}</style>
<h1>Standing-ready / original source candidate</h1><p>Four-second loop with planted ankle targets, soft knees and restrained breathing. The shared original skeleton and actor root are preserved. Directional movement, weapon grips and head/capsule timing remain gameplay-authoritative.</p>
<p><a href="Contender-Standing-v1.blend">Editable Blender master</a> / <a href="standing-production.json">Source and timing</a> / <a href="baked-contact-review.json">All 216 bound meshes / contact evidence</a></p>
<img src="References/StandingReady/measured-poses.png"><p>Six equal-scale views and six time samples; unarmed source references. Native four-character retarget, contacts, transition review and final acceptance are recorded separately.</p>''', encoding='utf-8')
print('STANDING_MEASURED_SHEET_READY', spec['masterSha256'])
