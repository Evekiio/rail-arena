"""Fetch the upstream Barlow fonts and preserve their OFL notice and content hashes."""
from pathlib import Path
import hashlib
import json
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'RailArena/Assets/Resources/UI/Brand/Fonts'
LICENSE = ROOT / 'RailArena/Assets/ThirdParty/Barlow'
OUTPUT.mkdir(parents=True, exist_ok=True)
LICENSE.mkdir(parents=True, exist_ok=True)
base = 'https://raw.githubusercontent.com/google/fonts/main/ofl/'
records = []
for remote, output in (
    ('barlow/Barlow-Regular.ttf', OUTPUT / 'Barlow-Regular.ttf'),
    ('barlow/Barlow-Medium.ttf', OUTPUT / 'Barlow-Medium.ttf'),
    ('barlowcondensed/BarlowCondensed-SemiBold.ttf', OUTPUT / 'BarlowCondensed-SemiBold.ttf'),
    ('barlow/OFL.txt', LICENSE / 'OFL.txt'),
    ('barlowcondensed/OFL.txt', LICENSE / 'OFL-Condensed.txt'),
):
    url = base + remote
    with urllib.request.urlopen(url, timeout=30) as response:
        data = response.read()
    if output.suffix == '.ttf' and data[:4] != b'\x00\x01\x00\x00':
        raise ValueError('Not a TrueType font: ' + remote)
    output.write_bytes(data)
    records.append(dict(url=url, file=output.relative_to(ROOT).as_posix(), sha256=hashlib.sha256(data).hexdigest()))
provenance = ROOT / 'ArtSource/Original/UI-Brand-v1/Fonts.json'
provenance.parent.mkdir(parents=True, exist_ok=True)
provenance.write_text(json.dumps(records, indent=2) + '\n')
print('Fetched three Barlow fonts and both upstream OFL notices.')
