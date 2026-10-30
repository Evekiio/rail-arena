"""Estimate AO atlas mip coverage on exported LOD triangles using a white mask."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
base = root / "Validation/SkyGardens-Playable-2026-10-29"
models = base / "CarbineRepackedModels"
rows = json.loads((models / "lod-uv-samples.json").read_text())
image = Image.open(base / "CarbineRepackedAudited/Coverage.png").convert("L")
levels = [image]
for _ in range(6):
    previous = levels[-1]
    levels.append(previous.resize((previous.width // 2, previous.height // 2), Image.Resampling.BOX))
weights = [(1/3, 1/3, 1/3), (.8, .1, .1), (.1, .8, .1), (.1, .1, .8)]

def sample(image, u, v):
    # Bilinear filtering at normalized texel centers, with clamp addressing.
    x = max(0, min(image.width - 1, u * image.width - .5))
    y = max(0, min(image.height - 1, (1 - v) * image.height - .5))
    x0, y0 = int(x), int(y)
    x1, y1 = min(x0 + 1, image.width - 1), min(y0 + 1, image.height - 1)
    dx, dy = x - x0, y - y0
    pixels = image.load()
    return ((1-dx)*(1-dy)*pixels[x0,y0] + dx*(1-dy)*pixels[x1,y0] + (1-dx)*dy*pixels[x0,y1] + dx*dy*pixels[x1,y1]) / 255

report = []
for row in rows:
    triangles = [tri for obj in row["objects"] if not obj["name"].endswith("Energy") for tri in obj["triangles"]]
    samples = [(sum(tri[i][0] * w[i] for i in range(3)), sum(tri[i][1] * w[i] for i in range(3))) for tri in triangles for w in weights]
    results = []
    for mip, mask in enumerate(levels):
        values = [sample(mask, u, v) for u, v in samples]
        results.append({"mip": mip, "size": mask.width, "samples": len(values),
                        "below95PercentWhite": sum(v < .95 for v in values),
                        "below50PercentWhite": sum(v < .5 for v in values)})
    report.append({"lod": row["level"], "surfaceTriangles": len(triangles), "mips": results})
output = models / "mip-coverage.json"
if output.exists():
    raise RuntimeError("Audit output already exists")
output.write_text(json.dumps({"method": "box mip plus bilinear sampling; white coverage mask; 4 points per triangle", "lods": report}, indent=2) + "\n")
print(json.dumps(report, indent=2))
