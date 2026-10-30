"""Audit all repacked rifle UV triangles against tile bounds and source textures."""
import json
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / "Validation/SkyGardens-Playable-2026-10-29"
fix_muzzle = "--muzzle-fix" in sys.argv
rows = json.loads((folder / ("CarbineAllUVMuzzleAngle.json" if fix_muzzle else "CarbineAllUVCompare.json")).read_text())["parts"]

def cross(a, b, p):
    return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])

def intersection_area(subject, clip):
    sign = 1 if cross(clip[0], clip[1], clip[2]) >= 0 else -1
    for i in range(3):
        a, b = clip[i], clip[(i + 1) % 3]
        old, subject = subject, []
        if not old:
            break
        prev, pv = old[-1], sign * cross(a, b, old[-1])
        for cur in old:
            cv = sign * cross(a, b, cur)
            if (pv >= 0) != (cv >= 0):
                t = pv / (pv - cv)
                subject.append([prev[0] + t * (cur[0] - prev[0]), prev[1] + t * (cur[1] - prev[1])])
            if cv >= 0:
                subject.append(cur)
            prev, pv = cur, cv
    return abs(sum(subject[i][0] * subject[(i + 1) % len(subject)][1] - subject[(i + 1) % len(subject)][0] * subject[i][1] for i in range(len(subject)))) / 2 if subject else 0

images = {name: Image.open(root / "RailArena/Assets/RailArena/Art/PrecisionCarbine" / name).convert("RGBA") for name in ("BaseColor.png", "MetallicSmoothness.png", "Emission.png", "Normal.png", "MetalDetailMask.png")}
weights = [(1/3, 1/3, 1/3), (.8, .1, .1), (.1, .8, .1), (.1, .1, .8)]
results = []
for row in rows:
    triangles = row["candidate"]
    bounds = [(min(p[0] for p in t), min(p[1] for p in t), max(p[0] for p in t), max(p[1] for p in t)) for t in triangles]
    overlaps = 0
    for i, first in enumerate(triangles):
        a = bounds[i]
        for j in range(i + 1, len(triangles)):
            b = bounds[j]
            if a[0] >= b[2] or b[0] >= a[2] or a[1] >= b[3] or b[1] >= a[3]:
                continue
            overlaps += intersection_area(first, triangles[j]) > 1e-12
    tile = row["tile"]
    left, bottom = tile["column"] / 9, tile["row"] / 9
    outside = sum(not (left <= p[0] <= left + 1/9 and bottom <= p[1] <= bottom + 1/9) for tri in triangles for p in tri)
    changed = {}
    for name, image in images.items():
        pixels = image.load()
        def sample(tri, weight):
            u = sum(tri[i][0] * weight[i] for i in range(3))
            v = sum(tri[i][1] * weight[i] for i in range(3))
            x = min(image.width - 1, max(0, int(u * image.width)))
            y = image.height - 1 - min(image.height - 1, max(0, int(v * image.height)))
            return pixels[x, y]
        changed[name] = sum(sample(old, w) != sample(new, w) for old, new in zip(row["original"], triangles) for w in weights)
    results.append({"part": row["name"], "triangles": len(triangles), "overlapPairs": overlaps, "verticesOutsideTile": outside, "textureSamplesDifferent": changed})
report = {"parts": len(results), "triangles": sum(r["triangles"] for r in results), "overlapPairs": sum(r["overlapPairs"] for r in results),
          "verticesOutsideTile": sum(r["verticesOutsideTile"] for r in results), "textureSamplesPerMap": sum(r["triangles"] for r in results) * len(weights),
          "textureSamplesDifferent": {name: sum(r["textureSamplesDifferent"][name] for r in results) for name in images}, "details": results}
path = folder / ("CarbineAllUVAuditMuzzleAngle.json" if fix_muzzle else "CarbineAllUVAudit.json")
if path.exists():
    raise RuntimeError("Audit output already exists")
path.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({k: v for k, v in report.items() if k != "details"}, indent=2))
