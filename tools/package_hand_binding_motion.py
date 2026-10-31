"""Encode the frozen native hand-review frames into a new, bounded review package."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from PIL import Image, ImageDraw, ImageFont

parser = argparse.ArgumentParser()
parser.add_argument('--frames', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
source = args.frames.resolve()
output = args.output.resolve()
assert not output.exists(), 'Preserve frozen reference media; use a new output directory'
report = json.loads((source/'review.json').read_text(encoding='utf-8'))
assert report['passed'] and report['framesPerStage'] == 180 and report['jointSamples'] == 7200
assert report['afterMaximumGap'] < .0005
for stage in ('before', 'after'):
    assert len(list((source/stage).glob('frame-*.png'))) == 180
output.mkdir(parents=True)
flags = {'creationflags': subprocess.CREATE_NO_WINDOW} if hasattr(subprocess, 'CREATE_NO_WINDOW') else {}

def run(arguments):
    return subprocess.run(arguments, check=True, capture_output=True, timeout=120, **flags)

clips = []
for stage in ('before', 'after'):
    path = output/('Hand-'+stage+'.mp4')
    run(['ffmpeg', '-v', 'error', '-threads', '4', '-framerate', '60', '-i', str(source/stage/'frame-%04d.png'), '-frames:v', '180', '-c:v', 'libx264', '-threads', '4', '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(path)])
    probe = json.loads(run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height,avg_frame_rate,duration,nb_frames', '-of', 'json', str(path)]).stdout)['streams'][0]
    assert (probe['width'], probe['height'], probe['avg_frame_rate'], probe['nb_frames']) == (1280, 720, '60/1', '180')
    clips.append({'path': path.name, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), **probe})
comparison = output/'Hand-before-after.mp4'
run(['ffmpeg', '-v', 'error', '-i', str(output/'Hand-before.mp4'), '-i', str(output/'Hand-after.mp4'), '-filter_complex', 'hstack=inputs=2', '-c:v', 'libx264', '-threads', '4', '-crf', '20', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(comparison)])
sheet = Image.new('RGB', (2560, 3080), '#14252c')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 23)
for row, frame in enumerate((15, 42, 90, 150)):
    for column, stage in enumerate(('before', 'after')):
        x, y = column*1280, row*770
        label = 'legacy replacement bind' if stage == 'before' else 'authored finger bind retained'
        draw.text((x+18, y+12), f'{stage.upper()} / frame {frame} / {label}', font=font, fill='white')
        with Image.open(source/stage/f'frame-{frame:04d}.png') as image:
            assert image.size == (1280, 720)
            sheet.paste(image.convert('RGB'), (x, y+50))
sheet.save(output/'Hand-motion-comparison.png')
metadata = {'scope': report['scope'], 'framesSource': str(source), 'jointSamplesTotal': report['jointSamples'], 'correctedJointSamples': 3600, 'beforeMaximumGapMetres': report['beforeMaximumGap'], 'afterMaximumGapMetres': report['afterMaximumGap'], 'maximumWristErrorMetres': report['maximumWristError'], 'clips': clips, 'comparisonClip': comparison.name, 'comparisonClipSha256': hashlib.sha256(comparison.read_bytes()).hexdigest(), 'sound': 'Silent controlled cosmetic review; not native audio/input acceptance'}
(output/'motion-review.json').write_text(json.dumps(metadata, indent=2)+'\n', encoding='utf-8')
print('HAND_MOTION_MEDIA_VERIFIED', output)
