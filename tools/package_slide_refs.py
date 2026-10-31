"""Package measured slide references with explicit source/native/installation boundaries."""
import csv
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root/'ArtSource/Original/ArtPass-v1/Animation/Slide'
spec = json.loads((folder/'slide-production.json').read_text(encoding='utf-8'))
evidence = json.loads((folder/'baked-contact-review.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/spec['master']).read_bytes()).hexdigest() == spec['masterSha256'] == evidence['masterSha256']
assert evidence['rendersUpdated']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 23)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
poster = Image.new('RGB', (1600, 1300), '#102029')
poster_draw = ImageDraw.Draw(poster)
poster_draw.text((25, 20), 'RAIL ARENA / ORIGINAL SLIDE / CANONICAL SOURCE POSES', font=font, fill='white')
cards = []
for index, clip in enumerate(spec['clips']):
    record = next(item for item in evidence['clips'] if item['clip'] == clip['name'])
    assert record['boundMeshCount'] == 216 and record['minimumWholeSkinHeightMetres'] >= -.005
    sheet = Image.new('RGB', (2304, 2730), '#102029')
    draw = ImageDraw.Draw(sheet)
    draw.text((25, 15), f"{clip['name']} / 60 Hz / {clip['durationSeconds']} s / {'gliding pose loop' if clip['loop'] else 'transition'}", font=font, fill='white')
    draw.text((25, 52), 'Canonical Blender source; unchanged shared skeleton. Live motion/capsule remain authoritative. Unarmed reference.', font=small, fill='#b4d9d2')
    for row, view in enumerate(record['views']):
        for column, frame in enumerate(record['poseFrames']):
            x, y = column*384, 95+row*425
            with Image.open(folder/'References'/clip['name']/f'{view}-{column}.png') as image:
                sheet.paste(image.convert('RGB').resize((384, 384)), (x, y+25))
            draw.text((x+8, y), f'{view.upper()} / {frame}f / {frame/60:.3f}s', font=small, fill='#b4d9d2')
            draw.line((x+12, y+395, x+12+round(384/2.2), y+395), fill='#48cdd1', width=3)
            draw.text((x+12, y+402), '1 m / 2.2 m shared view span', font=small, fill='#b4d9d2')
    draw.text((25, 2665), 'ANIM-SLIDE-v1 / '+spec['masterSha256'][:32]+' / no actor root motion', font=small, fill='white')
    sheet.save(folder/'References'/clip['name']/'measured-poses.png')
    for column, view in enumerate(('front', 'right')):
        with Image.open(folder/'References'/clip['name']/f'{view}-3.png') as image:
            poster.paste(image.convert('RGB').resize((390, 390)), (column*800+190, 95+index*400))
    poster_draw.text((25, 80+index*400), clip['name']+' / '+clip['posePurpose'], font=small, fill='white')
    cards.append('<h2>'+clip['name']+'</h2><img src="References/'+clip['name']+'/measured-poses.png">')
poster.save(folder/'Slide-Family-Poster.png')
native = ''
result_path = root/'Validation/motion-slide-native.xml'
if result_path.exists():
    results = ET.parse(result_path).getroot()
    assert results.attrib['result'] == 'Passed'
    rows = list(csv.DictReader((root/'Validation/SlideCandidatePlayback/Playback.csv').open(newline='', encoding='utf-8')))
    assert len(rows) == 120 and {int(row['characterId']) for row in rows} == {0, 1, 2, 3}
    assert not (root/'Validation/SlideCandidatePlayback/Failures.txt').read_text(encoding='utf-8').strip()
    native_record = {'scope': 'Isolated candidate clips on all four imported characters with live rifle grips. Simulation held and standing capsule retained for pose inspection. Not installed gameplay, native input/audio, moving-world clearance or final acceptance.',
        'sourceMasterSha256': spec['masterSha256'], 'frames': 30, 'characterSamples': len(rows),
        'minimumSkinYMetres': min(float(row['minimumSkinY']) for row in rows),
        'maximumSkinYMetres': max(float(row['maximumSkinY']) for row in rows),
        'maximumWristErrorMetres': max(float(row[key]) for row in rows for key in ('leftWristError', 'rightWristError')),
        'results': 'Validation/motion-slide-native.xml', 'installed': False}
    (folder/'native-candidate-review.json').write_text(json.dumps(native_record, indent=2)+'\n', encoding='utf-8')
    with Image.open(root/'Validation/SlideCandidatePlayback/frame-0007.png') as image:
        image.save(folder/'Native-Slide-Hold.png')
    native = '<h2>Native candidate pose / all four characters</h2><p><a href="native-candidate-review.json">Whole-skin, wrist and authority evidence</a>. This is isolated candidate staging, not live sliding.</p><img src="Native-Slide-Hold.png">'
installed_path = folder/'review-evidence.json'
installed = installed_path.exists() and json.loads(installed_path.read_text(encoding='utf-8'))['installed']
status = 'Three source-integrated actions follow remaining slide ticks at native cadence. Matching legacy/authored runway movement, configurable durations and lifecycle reviews pass. The current verified Windows milestone and performance exceptions are documented in <a href="../../../../../docs/ART-PASS-REVIEW.md">the current Windows handoff</a>. Native input/audio, broader terrain/combat interruptions and final acceptance remain open.' if installed else 'These candidates are not installed in the gameplay controller. Runtime timing/interruption, moving-world contacts and native acceptance remain open.'
playback = '<h2>Actual controller movement / matching commands</h2><p><a href="review-evidence.json">Installed clock, contacts, trajectory and lifecycle evidence</a>. Silent six-second staged movement; native-input and sound acceptance remain open.</p><video controls preload="metadata" style="width:100%" src="Slide-Before-After.mp4"></video><img src="Slide-Before-After.png"><p><a href="Slide-Playback.mp4">Authored motion at native 60 Hz</a></p>' if installed else ''
page = '<!doctype html><meta charset="utf-8"><title>Rail Arena slide production</title><style>body{background:#102029;color:#edf0df;font:18px Arial;margin:3vw}a{color:#48cdd1}img{max-width:100%}p{max-width:950px;line-height:1.6}</style><h1>Original feet-first slide / measured source and native movement</h1><p>Three original actions on the unchanged shared skeleton. Entry and recovery connect to compact crouch; gliding translation and steering belong to the simulation. '+status+'</p><p><a href="slide-production.json">Canonical source, cadence and ankle paths</a> / <a href="baked-contact-review.json">Complete 216-part source floor evidence</a></p>'+playback+'<img src="Slide-Family-Poster.png">'+native+''.join(cards)
(folder/'index.html').write_text(page, encoding='utf-8')
print('SLIDE_REFERENCE_PACKAGE_READY', len(cards), 'sheets;', 'source-integrated motion scope' if installed else 'candidate scope')
