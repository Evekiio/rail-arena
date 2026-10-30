"""Original painted cel atlas construction from the established contender palettes."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parent.parent
output = root / 'RailArena/Assets/RailArena/Art/ArtPass/Characters'
source = root / 'ArtSource/Original/ArtPass-v1/Characters'
output.mkdir(parents=True, exist_ok=True)
source.mkdir(parents=True, exist_ok=True)
axis = (np.arange(512, dtype=np.float32) + .5) / 512
x, y = np.meshgrid(axis, axis)
edge = np.minimum.reduce([x, y, 1-x, 1-y])
for name in ('Player', 'Bot01', 'Bot02', 'Bot03'):
    original = root / f'RailArena/Assets/RailArena/Art/StylizedContenders/{name}-Palette.png'
    palette = Image.open(original).convert('RGB')
    canvas = np.zeros((2048, 2048, 3), dtype=np.uint8)
    for index in range(16):
        # Blender UV rows run upward, PNG rows run downward.
        color = np.array(palette.getpixel((index % 4 * 64 + 32, (3-index//4) * 64 + 32)), dtype=np.float32)
        factor = np.ones_like(x)
        if index in (0, 6, 12, 14):
            factor += .10 * (1-y) - .035*y
            factor += np.where((edge > .035) & (edge < .052), .14, 0)
            factor -= np.where(edge < .025, .12, 0)
            factor += .06 * np.maximum(0, 1 - abs(x-.27)*22) * (y < .34)
        elif index in (1, 2, 7):
            factor += .06 * (1-y) - .05*y
            factor += .025 * np.sin(x * np.pi * 8) * np.sin(y * np.pi * 3)
            factor -= np.where(edge < .022, .08, 0)
        elif index in (4, 11):
            factor += .14 * (1-y) + .07 * np.sin(x * np.pi * 3) * (1-y)
        elif index in (3, 10, 13):
            factor += .055 * (1-y) - .025*y
        tile = np.clip(color[None,None,:] * factor[:,:,None], 0, 255).astype(np.uint8)
        row, column = 3-index//4, index%4
        canvas[row*512:(row+1)*512, column*512:(column+1)*512] = tile
    image = Image.fromarray(canvas)
    image.save(output / f'{name}-Painted.png')
    image.save(source / f'{name}-Painted.png')
    (source / f'{name}-Texture-Provenance.json').write_text(json.dumps({'id': f'CHAR-{name.upper()}-PAINT-v1', 'provenance': 'Original code-authored cel texture painting from existing Rail Arena palette; no external assets', 'originalPalette': original.relative_to(root).as_posix(), 'originalPaletteSha256': hashlib.sha256(original.read_bytes()).hexdigest(), 'outputSha256': hashlib.sha256((output/f'{name}-Painted.png').read_bytes()).hexdigest(), 'size': [2048,2048], 'layout': '4x4 material families with projected per-part UVs; controlled enamel edge accents, cloth folds and hair color planes', 'approval': 'Final integrated user playtest pending'}, indent=2)+'\n')
    print('CONTENDER_ATLAS_PAINTED', name)
