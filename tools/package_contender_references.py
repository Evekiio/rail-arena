"""Verify and package the selected Rail Arena concept reference sheets.

Uses only the Python standard library. Raster artwork is copied unchanged;
the viewer lays out the original files without resampling or retouching them.
"""
import argparse
import hashlib
import html
import json
from pathlib import Path
import struct
import zipfile
import zlib


def verify_png(path):
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError(f'Invalid PNG signature: {path}')
    offset, width, height, compressed, ended = 8, 0, 0, bytearray(), False
    while offset < len(data):
        length = struct.unpack_from('>I', data, offset)[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + length]
        crc = struct.unpack_from('>I', data, offset + 8 + length)[0]
        if zlib.crc32(kind + payload) & 0xffffffff != crc:
            raise ValueError(f'PNG chunk CRC mismatch: {path} {kind!r}')
        if kind == b'IHDR':
            width, height = struct.unpack_from('>II', payload)
        elif kind == b'IDAT':
            compressed.extend(payload)
        elif kind == b'IEND':
            ended = True
            break
        offset += 12 + length
    if not ended or min(width, height) < 768 or width*height < 1_000_000 or not zlib.decompress(compressed):
        raise ValueError(f'Incomplete or undersized reference: {path}')
    return dict(file=path.name, width=width, height=height, bytes=len(data),
                sha256=hashlib.sha256(data).hexdigest())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()
    folder = root / 'ArtSource/Original/StylizedContenders/References-v1/Final'
    characters = ['Player', 'Bot01', 'Bot02', 'Bot03']
    kinds = ['Turnaround', 'Head', 'Details', 'Axial']
    images = [verify_png(folder / f'{name}-{kind}.png')
              for name in characters for kind in kinds]
    concept = verify_png(folder / 'Approved-Concept.png')
    original = root / 'ArtSource/Original/StylizedContenders/Approved-Concept.png'
    if concept['sha256'] != hashlib.sha256(original.read_bytes()).hexdigest():
        raise ValueError('Pack concept must preserve the approved original unchanged.')
    if len({entry['sha256'] for entry in images}) != 16:
        raise ValueError('The sixteen selected sheets must be distinct files.')
    for filename in ('MODELING-GUIDE.md', 'PromptSet.json', 'Axial-Prompts.json'):
        if not (folder / filename).is_file():
            raise FileNotFoundError(filename)
    json.loads((folder / 'PromptSet.json').read_text(encoding='utf-8'))
    manifest = dict(
        version=1, purpose='Expanded modeling concept references for four Rail Arena characters',
        status='review candidate; new unseen-surface extensions await creator approval',
        generationTool='built-in image_gen.imagegen',
        approvedSource=concept, characters=characters,
        coverage=dict(sheets=16, bodyTurnaroundViews=32, bodyAxialViews=8, largeHeadInspections=24,
                      equipmentDetailSheets=4),
        imageValidation='PNG signatures, every chunk CRC, IDAT decompression, native dimensions and SHA-256',
        visualReview='See Visual-Review.json; qualitative concept consistency, not calibrated 3D registration',
        exactSharedMeshProjection=False, gameAssetsModified=False,
        poleGuides='Headless Blender orthographic top/bottom projections; guides constrain camera/occlusion, not final appearance',
        selectedSheets=images,
        excluded=['All files outside Final; especially Axial-Rejected-Perspective.png'])
    (folder / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')

    title_map = dict(Player='Player', Bot01='Bot 01', Bot02='Bot 02', Bot03='Bot 03')
    gallery_data = json.dumps(title_map)
    gallery = '''<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Rail Arena | Character references v1</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#171c22;color:#eef1f3;font:16px/1.5 system-ui,sans-serif}
header,nav,main,footer{max-width:1500px;margin:auto;padding:14px 24px}h1{font-size:28px;margin:0}
header p{color:#b8c3cd;margin:4px 0}nav{display:flex;gap:10px;flex-wrap:wrap;padding-top:0}
button,a{font:inherit}button{border:1px solid #5c6b78;background:#252f39;color:#e9eef3;border-radius:6px;padding:8px 18px;cursor:pointer}
button[aria-pressed=true]{background:#a5d9df;color:#14232a;border-color:#a5d9df}button:focus-visible,a:focus-visible{outline:3px solid #efbd76;outline-offset:4px}
a{color:#a5d9df}main{padding-top:0}figure{margin:0;background:#c9c9c7;border-radius:8px;overflow:hidden}
figure a{display:block}img{display:block;max-width:100%;max-height:calc(100vh - 300px);width:auto;height:auto;margin:auto;object-fit:contain}
figcaption{padding:10px 14px;background:#252f39;color:#d3dde5;font-size:14px}footer{color:#b8c3cd;font-size:14px}
.links{display:flex;gap:20px;flex-wrap:wrap}.badge{font-size:12px;letter-spacing:.1em;text-transform:uppercase;color:#efbd76}
@media(max-width:600px){header,nav,main,footer{padding-left:12px;padding-right:12px}h1{font-size:22px}button{padding:7px 12px}img{max-height:none}}
</style>
<header><span class="badge">Design references v1 · Review candidate</span><h1>Rail Arena / Contenders</h1>
<p>Four characters. Ten body directions each, larger head studies and construction details.</p></header>
<nav aria-label="Character" id="characters"></nav>
<nav aria-label="Reference sheet" id="sheets"></nav>
<main><figure><a id="full" target="_blank" rel="noopener"><img id="sheet" alt=""></a><figcaption id="caption"></figcaption></figure></main>
<footer><div class="links"><a href="MODELING-GUIDE.md">Modeling guide and fixed side details</a><a href="Approved-Concept.png" target="_blank" rel="noopener">Original approved concept</a><a href="manifest.json">File manifest</a><a href="PromptSet.json">Prompts and provenance</a></div>
<p>Click the sheet for its full native image. These are concept illustrations; high/low and under-jaw inspections are not calibrated camera projections. New unseen-surface details remain proposed until reviewed.</p>
<noscript><p>JavaScript is disabled. Open a sheet directly:</p>__FALLBACK__</noscript></footer>
<script>
const names=__DATA__;let character='Player',kind='Turnaround';
const types={Turnaround:'Body · 8 views',Head:'Head · 6 views',Details:'Equipment and details',Axial:'Top / bottom'};
function draw(){
 const file=character+'-'+kind+'.png';document.getElementById('sheet').src=file;
 document.getElementById('sheet').alt=names[character]+' — '+types[kind];
 document.getElementById('full').href=file;
 document.getElementById('caption').textContent=names[character]+' / '+types[kind]+' · '+file;
 document.querySelectorAll('#characters button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.key===character));
 document.querySelectorAll('#sheets button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.key===kind));
}
function buttons(id,items,select){for(const [key,label] of Object.entries(items)){
 const b=document.createElement('button');b.type='button';b.dataset.key=key;b.textContent=label;
 b.onclick=()=>{select(key);draw()};document.getElementById(id).appendChild(b);
}}
buttons('characters',names,k=>character=k);buttons('sheets',types,k=>kind=k);draw();
</script></html>'''
    fallback = '<ul>' + ''.join(
        f'<li><a href="{html.escape(entry["file"])}">{html.escape(entry["file"])}</a></li>'
        for entry in images) + '</ul>'
    gallery = gallery.replace('__DATA__', gallery_data).replace('__FALLBACK__', fallback)
    (folder / 'index.html').write_text(gallery, encoding='utf-8')
    review = folder / 'Visual-Review.json'
    if not review.is_file():
        raise FileNotFoundError('A visual review is required before packaging.')
    output = folder.parent / 'RailArena-Character-References-v1.zip'
    allowed = [entry['file'] for entry in images] + [
        'Approved-Concept.png', 'MODELING-GUIDE.md', 'PromptSet.json', 'Axial-Prompts.json',
        'manifest.json', 'Visual-Review.json', 'index.html']
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for filename in allowed:
            archive.write(folder / filename, arcname='RailArena-Character-References-v1/' + filename)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None or len(archive.namelist()) != len(allowed):
            raise ValueError('Archive verification failed.')
    print(json.dumps(dict(passed=True, selectedSheets=len(images), totalPngs=17,
                          gallery=str(folder / 'index.html'), archive=str(output),
                          archiveBytes=output.stat().st_size), indent=2))


if __name__ == '__main__':
    main()
