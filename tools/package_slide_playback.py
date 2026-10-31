"""Join installed slide motion, trajectory comparison and lifecycle evidence."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]
folder = root/'ArtSource/Original/ArtPass-v1/Animation/Slide'
raw = root/'Validation/SlideMovementPlayback'
for name, count in [('motion-slide-integrated.xml', 1), ('motion-slide-moving-regression.xml', 2)]:
    result = ET.parse(root/'Validation'/name).getroot()
    assert result.attrib['result'] == 'Passed' and int(result.attrib['total']) == count
assert not (raw/'Failures.txt').read_text(encoding='utf-8').strip()
assert not (root/'Validation/SlideIntegration/Failures.txt').read_text(encoding='utf-8').strip()
rows = list(csv.DictReader((raw/'Playback.csv').open(newline='', encoding='utf-8')))
poses = list(csv.DictReader((root/'Validation/SlideIntegration/Poses.csv').open(newline='', encoding='utf-8')))
lifecycle = list(csv.DictReader((root/'Validation/SlideIntegration/Lifecycle.csv').open(newline='', encoding='utf-8')))
assert len(rows) == 2880 and len(poses) == 1176 and len(lifecycle) == 12
assert len(list((raw/'legacy').glob('frame-*.png'))) == len(list((raw/'authored').glob('frame-*.png'))) == 360
source = json.loads((folder/'slide-production.json').read_text(encoding='utf-8'))
assert hashlib.sha256((folder/source['master']).read_bytes()).hexdigest() == source['masterSha256']
graph = json.loads((root/'Validation/motion-slide-graph-repeatability.json').read_text(encoding='utf-8'))
assert graph['identical']
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 28)
header = Image.new('RGB', (2560, 60), '#102029')
draw = ImageDraw.Draw(header)
draw.text((25, 12), 'BEFORE / legacy procedural slide / same commands', font=font, fill='white')
draw.text((1305, 12), 'AFTER / original feet-first slide / same commands', font=font, fill='#6ce0d0')
header.save(raw/'comparison-header.png')
poster = Image.new('RGB', (2560, 780), '#102029')
poster.paste(header, (0, 0))
for x, name in [(0, 'legacy'), (1280, 'authored')]:
    with Image.open(raw/name/'frame-0018.png') as image:
        poster.paste(image.convert('RGB'), (x, 60))
poster.save(folder/'Slide-Before-After.png')

def run_encoder(arguments, name):
    with (root/'Validation'/name).open('w', encoding='utf-8') as log:
        process = subprocess.Popen(['ffmpeg', '-v', 'error', '-y']+arguments, stdout=log, stderr=log,
                                   creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        (root/'Validation'/(name+'.pid')).write_text(str(process.pid), encoding='utf-8')
        try:
            assert process.wait(timeout=90) == 0, name
        finally:
            if process.poll() is None:
                process.kill(); process.wait()

encoding = ['-an', '-c:v', 'libx264', '-preset', 'fast', '-crf', '18', '-pix_fmt', 'yuv420p', '-frames:v', '360', '-movflags', '+faststart']
run_encoder(['-framerate', '60', '-i', str(raw/'authored/frame-%04d.png')]+encoding+[str(folder/'Slide-Playback.mp4')], 'motion-slide-reel.log')
run_encoder(['-framerate', '60', '-i', str(raw/'legacy/frame-%04d.png'), '-framerate', '60', '-i', str(raw/'authored/frame-%04d.png'),
    '-loop', '1', '-framerate', '60', '-i', str(raw/'comparison-header.png'), '-filter_complex',
    '[0:v][1:v]hstack=inputs=2,pad=2560:780:0:60:color=0x102029[paired];[paired][2:v]overlay=0:0:shortest=1[v]', '-map', '[v]']+
    encoding+[str(folder/'Slide-Before-After.mp4')], 'motion-slide-comparison-reel.log')
videos = []
for name, width, height in [('Slide-Playback.mp4', 1280, 720), ('Slide-Before-After.mp4', 2560, 780)]:
    probe = subprocess.run(['ffprobe', '-v', 'error', '-show_streams', '-of', 'json', str(folder/name)], capture_output=True, check=True,
                           timeout=20, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    stream = next(item for item in json.loads(probe.stdout)['streams'] if item['codec_type'] == 'video')
    assert int(stream['nb_frames']) == 360 and stream['r_frame_rate'] == '60/1' and abs(float(stream['duration'])-6) < .01
    assert int(stream['width']) == width and int(stream['height']) == height
    videos.append({'path': name, 'width': width, 'height': height, 'fps': 60, 'frames': 360, 'seconds': 6, 'audio': False})
for original, destination in [(raw/'Playback.csv', 'Moving-Playback.csv'), (root/'Validation/SlideIntegration/Poses.csv', 'Integrated-Poses.csv'),
                              (root/'Validation/SlideIntegration/Lifecycle.csv', 'Lifecycle.csv')]:
    shutil.copy2(original, folder/destination)
moving = [row for row in rows if row['pass'] == 'authored' and int(row['remainingTicks']) > 0]
record = {'scope': 'Four-character installed body animation on a controlled flat runway using actual ArenaActor.Move and original slide physics. Six cycles per character under identical legacy/authored commands; simulation sampled at 60 Hz. Not native input/audio, all terrain/traversal cases or final artistic acceptance.',
    'sourceMasterSha256': source['masterSha256'], 'controllerSha256': graph['controllerSha256'], 'graphPreparationByteStable': True,
    'installed': True, 'installedClips': ['SlideEnter', 'SlideHold', 'SlideRecover'], 'integratedPoseSamples': len(poses), 'lifecycleScenarios': len(lifecycle),
    'movingCharacterSamples': len(rows)//2, 'pairedMovementComparisons': len(rows)//2, 'activeMovingSlideSamples': len(moving),
    'movementComparisonToleranceMetres': .0001, 'authority': 'Actor root, velocity, remaining ticks and capsule heights matched for identical scripted movement. Presentation never changes simulation ticks or root position.',
    'minimumMovingSlideSkinYMetres': min(float(row['minimumSkinY']) for row in moving),
    'minimumIntegratedSkinYMetres': min(float(row['minimumSkinY']) for row in poses),
    'maximumWristErrorMetres': max(float(row[key]) for row in moving+poses for key in ('leftWristError', 'rightWristError')),
    'configuredDurationsTested': [2, 8, 16, 36, 120], 'aimAngles': '0/90 degrees yaw, -30/0/30 degrees pitch in pose review; moving steering and +/-15 degree pitch in the runway comparison',
    'lifecycle': 'Standing, held crouch, jump, grounded warp, death, respawn and legacy fallback at entry/hold/recovery; collider definitions and local attachments retained.',
    'results': ['Validation/motion-slide-native.xml', 'Validation/motion-slide-integrated.xml', 'Validation/motion-slide-moving-regression.xml'],
    'videos': videos, 'windowsBuild': 'Review 17 is the current packaged build; slide Windows integration pending',
    'remaining': 'Native input/audio, first-person slide actions, firing/reload/hit interruption combinations, all terrain and portal/pad trajectories, dynamic tuning changes and Sanders artistic acceptance.'}
(folder/'review-evidence.json').write_text(json.dumps(record, indent=2)+'\n', encoding='utf-8')
print('SLIDE_INSTALLED_PLAYBACK_PACKAGED', record['movingCharacterSamples'], record['minimumMovingSlideSkinYMetres'])
