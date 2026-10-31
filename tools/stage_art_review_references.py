"""Stage versioned art evidence beside a validated player without altering runtime files."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--review', required=True)
parser.add_argument('--previous-review', required=True)
args = parser.parse_args()
assert args.review.isdigit() and args.previous_review.isdigit()
build = root/f'Builds/RailArena-v1Alpha-ArtPass-Review-{args.review}'
previous = root/f'Builds/RailArena-v1Alpha-ArtPass-Review-{args.previous_review}'
destination = build/'ValidationEvidence'
assert (destination/'integrated-final.json').is_file(), 'Validate and join the shipping-player evidence first'
previous_verification = json.loads((root/f'Validation/art-package-verification-{args.previous_review}.json').read_text(encoding='utf-8'))
archive_path = root/previous_verification['package']
assert hashlib.sha256(archive_path.read_bytes()).hexdigest() == previous_verification['sha256']
with zipfile.ZipFile(archive_path) as archive:
    prefix = previous.name+'/'
    manifest = dict(line.split('  ', 1)[::-1] for line in archive.read(prefix+'PackageFiles.sha256').decode('utf-8-sig').splitlines() if line)
inherited = ['CombatReactions', 'DirectionalMotion-Reference', 'Underbridge-Reference', 'BridgeStone', 'SummitStone', 'GroundedCrouch']
if int(args.review) >= 20:
    inherited.append('SlideMotion')
if int(args.review) >= 22:
    inherited.append('ArenaStone')
if int(args.review) >= 23:
    inherited.append('GardenNature')
if int(args.review) >= 25:
    inherited.append('ReadyMotion')
for name in inherited:
    source = previous/'ValidationEvidence'/name
    assert source.is_dir() and not (destination/name).exists(), name
    for path in source.rglob('*'):
        if path.is_file():
            assert hashlib.sha256(path.read_bytes()).hexdigest() == manifest[path.relative_to(previous).as_posix()], path
    shutil.copytree(source, destination/name)
family = 'BasementDecks' if int(args.review) >= 25 else 'ReadyMotion' if int(args.review) >= 23 else 'GardenNature' if int(args.review) >= 22 else 'ArenaStone' if int(args.review) >= 20 else 'SlideMotion'
if family == 'BasementDecks':
    source = root/'ArtSource/Original/ArtPass-v1/Environment/BasementDecks'
    spec = json.loads((source/'production.json').read_text(encoding='utf-8'))
    digest = hashlib.sha256((source/spec['master']).read_bytes()).hexdigest()
    assert digest == spec['masterSha256'] == json.loads((source/'measured-construction.json').read_text(encoding='utf-8'))['masterSha256']
    assert digest == json.loads((source/'review-evidence.json').read_text(encoding='utf-8'))['masterSha256']
    current = destination/family
    assert not current.exists(), 'Never overwrite a staged reference batch'
    current.mkdir()
    files = [(path, path.relative_to(source).as_posix()) for path in sorted(source.rglob('*')) if path.is_file() and path.suffix in ('.blend','.json','.png','.html')]
    exports = root/'RailArena/Assets/RailArena/Art/ArtPass/Environment/BasementDecks'
    files += [(exports/Path(lod['export']).name, 'Exports/'+Path(lod['export']).name) for lod in spec['lods']]
    for lod in spec['lods']:
        assert hashlib.sha256((root/lod['export']).read_bytes()).hexdigest() == lod['exportSha256']
    files += [(root/'Validation/stone-deck-inset-native.xml', 'stone-deck-inset-native.xml'), (root/'docs/BASEMENT-STONE-KIT.md', 'BASEMENT-STONE-KIT.md')]
elif family == 'ReadyMotion':
    current = destination/family
    assert not current.exists(), 'Never overwrite a staged reference batch'
    current.mkdir()
    files = []
    for name in ('Standing','FirstPersonReady'):
        source = root/'ArtSource/Original/ArtPass-v1/Animation'/name
        files += [(path, name+'/'+path.relative_to(source).as_posix()) for path in sorted(source.rglob('*')) if path.is_file() and path.suffix in ('.blend','.json','.csv','.mp4','.png','.html')]
    files += [(root/'Validation'/name,name) for name in ['standing-candidate.xml','standing-integrated.xml','standing-blends.xml','motion-standing-import.json','motion-standing-graph-repeatability.json','fps-ready-native.xml','fps-ready-active-native.xml','fps-ready-eye-native.xml']]
    files += [(root/'docs'/name,name) for name in ['AUTHORED-STANDING.md','AUTHORED-FIRST-PERSON-READY.md']]
elif family == 'GardenNature':
    current = destination/family
    assert not current.exists(), 'Never overwrite a staged reference batch'
    current.mkdir()
    nature = root/'ArtSource/Original/ArtPass-v1/Nature/GardenNature'
    files = [(nature/name, name) for name in ['Garden-Nature-v2.blend','Garden-Painted-Palette.png','Garden-Wind.mp4','Garden-Wind.csv','production.json','nature-source.json','measured-views.json','site-views.json','native-review-evidence.json','README.md']]
    files += [(path, path.name) for path in sorted(nature.glob('*-Measured-Sheet.png'))]
    files += [(path, path.name) for path in sorted(nature.glob('*-comparison.png'))]
    files += [(nature/'UnityReview/installed-review.json', 'installed-review.json')]
    files += [(root/'Validation'/name, name) for name in ['garden-nature-vines.xml','garden-nature-import.json','garden-nature-source-package.json','garden-nature-native-axes.json']]
    files += [(root/'docs/GARDEN-NATURE.md', 'GARDEN-NATURE.md')]
elif family == 'ArenaStone':
    source_root = root/'ArtSource/Original/ArtPass-v1/Environment/ArenaStone'
    current = destination/family
    assert not current.exists(), 'Never overwrite a staged reference batch'
    current.mkdir()
    files = [(source_root/name, name) for name in ['production.json', 'arena-source.json', 'measured-views.json', 'comparison-evidence.json']]
    files += [(source_root/(name+'-Measured-Sheet.png'), name+'-Measured-Sheet.png') for name in ['OuterX', 'OuterN', 'Spine', 'Wing', 'Landing', 'Underpass', 'Ramp']]
    files += [(path, path.name) for path in sorted((source_root/'UnityReview').glob('*.png'))]
    files += [(source_root/'UnityReview/installed-review.json', 'installed-review.json')]
    files += [(root/'Validation'/name, name) for name in ['arena-stone-stable-lod-native.xml', 'arena-stone-near-preservation.json', 'arena-stone-import-axes.json']]
    files += [(root/'docs/ARENA-STONE-KIT.md', 'ARENA-STONE-KIT.md')]
else:
    slide = root/'ArtSource/Original/ArtPass-v1/Animation/Slide'
    current = destination/'SlideMotion'
    assert not current.exists(), 'Never overwrite a staged reference batch'
    current.mkdir()
    files = [(slide/name, name) for name in ['Slide-Playback.mp4', 'Slide-Before-After.mp4', 'Slide-Before-After.png',
        'slide-production.json', 'baked-contact-review.json', 'review-evidence.json', 'Moving-Playback.csv', 'Integrated-Poses.csv', 'Lifecycle.csv']]
    files += [(slide/'References'/name/'measured-poses.png', name+'-Measured.png') for name in ['SlideEnter', 'SlideHold', 'SlideRecover']]
    files += [(root/'Validation'/name, name) for name in ['motion-slide-native.xml', 'motion-slide-integrated.xml', 'motion-slide-moving-regression.xml', 'motion-slide-graph-repeatability.json', 'motion-slide-duel-driver-fixed.xml', 'art-review-18-exception.json']]
    files += [(root/'docs/AUTHORED-SLIDE.md', 'AUTHORED-SLIDE.md')]
records = []
for source, name in files:
    assert source.is_file(), source
    (current/name).parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, current/name)
    records.append({'source': source.relative_to(root).as_posix(), 'packaged': (current/name).relative_to(build).as_posix(), 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
(destination/'reference-source-index.json').write_text(json.dumps({'inheritedVerifiedReview': args.previous_review,
    'inheritedFamilies': inherited, 'currentFamily': family, 'currentReferences': records,
    'scope': 'Reference source-stage documents may describe a pending Windows package. The adjacent integrated-final.json and root BuildIdentity.txt identify the actual shipping-player validation. Native input/audio and final artistic acceptance remain open.'}, indent=2)+'\n', encoding='utf-8')
(destination/'REFERENCE-CONTEXT.txt').write_text(
    'Rail Arena art references / Review '+args.review+'\n\n'
    'Prior family references are copied from hash-verified Review '+args.previous_review+' evidence. '
    +family+' adds current measured source and focused native evidence. '
    'Reference documents retain their source-stage wording; integrated-final.json and BuildIdentity.txt identify actual shipping validation. '
    'Staged reels and offscreen views do not establish native input, sound, sustained frame pacing or Sanders artistic approval. '
    'Full-body crouch enter/exit remain references. The overall asset-production plan remains incomplete.\n', encoding='utf-8')
print('ART_REFERENCES_STAGED', len(inherited), 'verified prior families;', len(records), family+' references')
