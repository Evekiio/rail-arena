"""Compare black and neutral AO backgrounds on exported rifle LOD UVs."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
base = root / "Validation/SkyGardens-Playable-2026-10-29"
folder = base / "CarbineRepackedAudited"
lods = json.loads((base / "CarbineRepackedModels/lod-uv-samples.json").read_text())
black = [Image.open(folder / "AO.png").convert("L")]
neutral = [Image.open(folder / "AO-white-background.png").convert("L")]
for _ in range(6):
    for chain in (black, neutral):
        img = chain[-1]
        chain.append(img.resize((img.width // 2, img.height // 2), Image.Resampling.BOX))
weights = [(1/3, 1/3, 1/3), (.8, .1, .1), (.1, .8, .1), (.1, .1, .8)]

def sample(image, u, v):
    x = max(0, min(image.width - 1, u * image.width - .5))
    y = max(0, min(image.height - 1, (1 - v) * image.height - .5))
    x0, y0 = int(x), int(y)
    x1, y1 = min(x0 + 1, image.width - 1), min(y0 + 1, image.height - 1)
    dx, dy, pixels = x - x0, y - y0, image.load()
    return ((1-dx)*(1-dy)*pixels[x0,y0] + dx*(1-dy)*pixels[x1,y0] + (1-dx)*dy*pixels[x0,y1] + dx*dy*pixels[x1,y1]) / 255

report = []
for row in lods:
    triangles = [tri for obj in row["objects"] if not obj["name"].endswith("Energy") for tri in obj["triangles"]]
    samples = [(sum(tri[i][0] * w[i] for i in range(3)), sum(tri[i][1] * w[i] for i in range(3))) for tri in triangles for w in weights]
    mips = []
    for mip, (a, b) in enumerate(zip(black, neutral)):
        diffs = [sample(b, u, v) - sample(a, u, v) for u, v in samples]
        mips.append({"mip": mip, "size": a.width, "samples": len(diffs), "meanIncrease": round(sum(diffs) / len(diffs), 6),
                     "increaseOverFivePercent": sum(d > .05 for d in diffs), "minimumDifference": round(min(diffs), 6)})
    report.append({"lod": row["level"], "mips": mips})
output = folder / "mip-background-comparison.json"
if output.exists():
    raise RuntimeError("Comparison output already exists")
output.write_text(json.dumps({"method": "box mip, bilinear sampling, four points per non-emissive triangle", "lods": report}, indent=2) + "\n")
print(json.dumps([{ "lod": row["lod"], "mip3": row["mips"][3], "mip4": row["mips"][4]} for row in report], indent=2))
