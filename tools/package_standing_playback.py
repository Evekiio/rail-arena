"""Join native standing evidence without claiming unrun shipping or input acceptance."""
import csv
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from art_shipping_evidence import shipping_followup

root = Path(__file__).resolve().parents[1]
folder = root / 'ArtSource/Original/ArtPass-v1/Animation/Standing'
spec = json.loads((folder / 'standing-production.json').read_text(encoding='utf-8'))
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
assert digest(folder / spec['master']) == spec['masterSha256']
for name, count in [('standing-candidate.xml', 1), ('standing-integrated.xml', 3), ('standing-blends.xml', 1)]:
    test = ET.parse(root / 'Validation' / name).getroot()
    assert test.attrib['result'] == 'Passed' and int(test.attrib['passed']) == count
checks = {}
for kind, directory in [('candidate', 'StandingCandidatePlayback'), ('installed', 'StandingInstalledPlayback')]:
    source = root / 'Validation' / directory
    assert not (source / 'Failures.txt').read_text().strip()
    rows = list(csv.DictReader((source / 'Playback.csv').open(newline='')))
    assert len(rows) == 964 and len({row['characterId'] for row in rows}) == 4
    assert {int(row['sourceFrame']) for row in rows} == set(range(241))
    assert len({(row['actorX'], row['actorY'], row['actorZ']) for row in rows}) == 4
    minimum = min(float(row['minimumSkinY']) for row in rows)
    maximum = max(float(row['minimumSkinY']) for row in rows)
    wrist = max(float(row[key]) for row in rows for key in ('leftWristError', 'rightWristError'))
    assert -.005 <= minimum <= maximum <= .035 and wrist <= .025
    shutil.copy2(source / 'Playback.csv', folder / (kind.title() + '-Playback.csv'))
    checks[kind] = {'samples': len(rows), 'characters': 4, 'framesIncludingLoopEndpoint': 241,
                    'minimumSkinY': minimum, 'maximumSkinFloorY': maximum, 'maximumWristError': wrist,
                    'actorRootPositionsUnchanged': True, 'standingCapsuleHeightUnchanged': True}
shutil.copy2(root / 'Validation/StandingInstalledPlayback/frame-0000.png', folder / 'Standing-Family-Poster.png')
movie = folder / 'Standing-Playback.mp4'
probe = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-of', 'json', str(movie)]))
assert len(probe['streams']) == 1
stream = probe['streams'][0]
assert stream['codec_type'] == 'video' and stream['nb_frames'] == '240' and stream['r_frame_rate'] == '60/1'
assert float(stream['duration']) == 4 and (stream['width'], stream['height']) == (1280, 720)
record = {'masterSha256': spec['masterSha256'], 'scope': 'Native stationary four-character candidate and installed idle review. Simulation frozen for geometry/contact inspection; not native input, audio, moving-world transitions, performance or final artistic acceptance.',
          'sourceIntegrated': True, 'shippingBuildPending': True, 'currentVerifiedWindowsReview': 22,
          'checks': checks, 'nativeTests': ['Validation/standing-candidate.xml', 'Validation/standing-integrated.xml'],
          'regressions': ['DirectionalBlendsPreserveAimStanceAndAuthority', 'GroundedCrouchPreservesImmediateStanceAndContacts'],
          'movie': {'path': movie.name, 'sha256': digest(movie), 'frames': 240, 'seconds': 4, 'fps': 60, 'audio': False},
          'sourceFrameEndpointExcludedFromMovie': 240,
          'csv': {name: digest(folder / name) for name in ('Candidate-Playback.csv', 'Installed-Playback.csv')}}
blend_source = root / 'Validation/StandingBlendReview'
assert not (blend_source / 'Failures.txt').read_text().strip()
blend_rows = list(csv.DictReader((blend_source / 'BlendChecks.csv').open(newline='')))
assert len(blend_rows) == 5184
shutil.copy2(blend_source / 'BlendChecks.csv', folder / 'BlendChecks.csv')
record['blendReview'] = {'samples': len(blend_rows), 'characters': 4, 'directions': 8,
                        'yawAngles': 2, 'speeds': 9, 'phases': 9,
                        'minimumSkinY': min(float(row['skinFloorY']) for row in blend_rows),
                        'maximumSkinFloorY': max(float(row['skinFloorY']) for row in blend_rows),
                        'maximumWristError': max(float(row[key]) for row in blend_rows for key in ('leftWristError', 'rightWristError')),
                        'scope': 'Stationary blend snapshots; actor root, velocity and capsule unchanged. Not moving-world or temporal start/stop validation.'}
record['nativeTests'].append('Validation/standing-blends.xml')
record['csv']['BlendChecks.csv'] = digest(folder / 'BlendChecks.csv')
shipping = shipping_followup(root, folder / 'standing-production.json', spec['masterSha256'])
if shipping:
    record.update(shippingBuildPending=False, currentVerifiedWindowsReview=shipping['review'], shippingFollowup=shipping)
(folder / 'review-evidence.json').write_bytes((json.dumps(record, indent=2) + '\n').encode('utf-8'))
page = folder / 'index.html'
text = page.read_text(encoding='utf-8').split('<!-- NATIVE_STANDING -->')[0]
status = f'This master is included in verified Windows Review{shipping["review"]}; final acceptance remains open.' if shipping else 'Source integrated; shipping verification remains pending.'
text += '<!-- NATIVE_STANDING --><h2>Installed four-character playback</h2><img src="Standing-Family-Poster.png"><video controls preload="metadata" src="Standing-Playback.mp4"></video><p>Four seconds at native 60 Hz; silent. All four identities share the compatible skeleton. '+status+'</p><p><a href="review-evidence.json">Native scope and numeric evidence</a> / <a href="Installed-Playback.csv">964 installed samples</a></p>'
page.write_text(text, encoding='utf-8')
print('STANDING_NATIVE_PACKAGED', json.dumps(checks))
