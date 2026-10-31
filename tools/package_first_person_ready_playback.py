"""Join player-eye native evidence, source identity and normal-speed ready reels."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady'
raw = root / 'Validation/FirstPersonReadyEyePlayback'
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
spec = json.loads((folder / 'ReadyAction.json').read_text(encoding='utf-8'))
assert digest(folder / spec['master']) == spec['masterSha256']
test = ET.parse(root / 'Validation/fps-ready-eye-native.xml').getroot()
assert test.attrib['result'] == 'Passed' and int(test.attrib['passed']) == 1
initial = ET.parse(root / 'Validation/fps-ready-native.xml').getroot()
legacy = [case for case in initial.iter('test-case') if 'FirstPersonWeaponMotionTests.' in case.attrib['fullname']]
assert len(legacy) == 3 and all(case.attrib['result'] == 'Passed' for case in legacy)
asset = root / 'RailArena/Assets/Resources/Art/FirstPersonEquip.asset'
assert spec['masterSha256'] in asset.read_text(encoding='utf-8')
rows = list(csv.DictReader((raw / 'Playback.csv').open(newline='')))
assert len(rows) == 720 and len({row['simulationTick'] for row in rows}) == len({row['rounds'] for row in rows}) == 1
assert len({row['character'] for row in rows}) == 4 and {row['mode'] for row in rows} == {'0', '1', '2'}
assert max(float(row[key]) for row in rows for key in ('leftWristError', 'rightWristError')) < .025
full_ads = [row for row in rows if row['mode'] == '1' or row['mode'] == '2' and float(row['sourceTime']) >= .18]
assert len(full_ads) == 400 and max(float(row['sightError']) for row in full_ads) < .0001
shutil.copy2(raw / 'Playback.csv', folder / 'Playback.csv')
movies = {}
for name, size in [('Ready-Playback.mp4', (1280, 720)), ('Ready-Before-After.mp4', (2560, 720))]:
    path = folder / name
    data = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-of', 'json', str(path)]))
    assert len(data['streams']) == 1
    stream = data['streams'][0]
    assert stream['nb_frames'] == '360' and stream['r_frame_rate'] == '60/1' and float(stream['duration']) == 6
    assert (stream['width'], stream['height']) == size
    movies[name] = {'sha256': digest(path), 'frames': 360, 'fps': 60, 'seconds': 6, 'audio': False}
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 26)
comparisons = []
for label, frame in [('Raise', 4), ('Settle', 9), ('ADS', 40), ('ADS-Entry', 64)]:
    output = Image.new('RGB', (2560, 792), '#102029')
    draw = ImageDraw.Draw(output)
    for index, version in enumerate(('before', 'after')):
        with Image.open(raw / version / f'frame-{frame:04d}.png') as source:
            output.paste(source.convert('RGB'), (index * 1280, 72))
        draw.text((index * 1280 + 20, 20), ('PRIOR PROCEDURAL RAISE' if index == 0 else 'BLENDER-AUTHORED READY') + ' / ' + label, font=font, fill='white')
    name = label + '-Comparison.png'
    output.save(folder / name)
    comparisons.append({'path': name, 'frame': frame, 'sha256': digest(folder / name)})
record = {'masterSha256': spec['masterSha256'], 'sourceAssetSha256': digest(asset),
    'scope': 'Player-eye native camera, four arm appearances, hip/ADS/ADS-entry, matching camera/pose/frame samples. Background atmosphere time is not held identical. Overlay HUD/reticle, native input/audio and performance are excluded.',
    'sourceIntegrated': True, 'shippingBuildPending': True, 'currentVerifiedWindowsReview': 22,
    'samples': len(rows), 'fullAdsSamples': len(full_ads),
    'maximumRecordedWristError': max(float(row[key]) for row in rows for key in ('leftWristError', 'rightWristError')),
    'maximumRecordedFullAdsAxisError': max(float(row['sightError']) for row in full_ads),
    'simulationTickRoundsRootVelocityCapsuleAndShotTimingPreserved': True,
    'resetAndArtProfileRemovalChecked': True, 'legacyMotionRegressionsPassed': [case.attrib['name'] for case in legacy],
    'nativeTest': 'Validation/fps-ready-eye-native.xml', 'csvSha256': digest(folder / 'Playback.csv'),
    'movies': movies, 'comparisons': comparisons,
    'provisionalEvidence': {'Validation/fps-ready-native.xml': 'Initial fixture froze before the inactive view weapon was activated; one null-reference failure, three legacy regressions passed.',
                            'Validation/fps-ready-active-native.xml': 'Numeric/contact review passes; menu aerial camera was unsuitable for final gameplay-view evidence. Superseded by player-eye review.'}}
(folder / 'review-evidence.json').write_bytes((json.dumps(record, indent=2) + '\n').encode('utf-8'))
page = folder / 'index.html'
html = page.read_text(encoding='utf-8').split('<!-- NATIVE_READY -->')[0]
html += '<!-- NATIVE_READY --><h2>Player-eye native comparison</h2><p>Prior procedural raise on the left; authored ready on the right. Four arm appearances, hip, ADS and ADS entry. Six seconds at 60 Hz, silent. Camera and weapon samples match; background atmosphere time is not identical. The main-camera capture excludes the HUD and illuminated overlay reticle.</p><video controls preload="metadata" src="Ready-Before-After.mp4"></video>'
html += ''.join('<h3>' + item['path'].split('-Comparison')[0] + '</h3><img src="' + item['path'] + '">' for item in comparisons)
html += '<p><a href="review-evidence.json">Source and native evidence</a> / <a href="Playback.csv">720 samples</a> / <a href="Ready-Playback.mp4">Authored playback only</a>. Source integrated; the next Windows milestone and final artistic acceptance remain pending.</p>'
page.write_text(html, encoding='utf-8')
print('FPS_READY_NATIVE_PACKAGED', record['samples'], 'samples;', record['fullAdsSamples'], 'full ADS;', record['maximumRecordedWristError'], 'maximum wrist error')
