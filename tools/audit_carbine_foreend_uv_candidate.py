"""Audit a disposable fore-end unwrap for triangle overlaps and texture changes."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / "Validation/SkyGardens-Playable-2026-10-29"
data = json.loads((folder / "CarbineForeendUVCompare.json").read_text())
triangles = data["candidate"]

def cross(a, b, p):
    return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])

def area(poly):
    return abs(sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))) / 2 if poly else 0

def intersection_area(subject, clip):
    sign = 1 if cross(clip[0], clip[1], clip[2]) >= 0 else -1
    for i in range(3):
        a, b = clip[i], clip[(i + 1) % 3]
        old, subject = subject, []
        if not old:
            break
        prev = old[-1]
        pv = sign * cross(a, b, prev)
        for cur in old:
            cv = sign * cross(a, b, cur)
            if (pv >= 0) != (cv >= 0):
                t = pv / (pv - cv)
                subject.append([prev[0] + t * (cur[0] - prev[0]), prev[1] + t * (cur[1] - prev[1])])
            if cv >= 0:
                subject.append(cur)
            prev, pv = cur, cv
    return area(subject)

bounds = [(min(p[0] for p in t), min(p[1] for p in t), max(p[0] for p in t), max(p[1] for p in t)) for t in triangles]
overlaps = []
for i, first in enumerate(triangles):
    a = bounds[i]
    for j in range(i + 1, len(triangles)):
        b = bounds[j]
        if a[0] >= b[2] or b[0] >= a[2] or a[1] >= b[3] or b[1] >= a[3]:
            continue
        overlap = intersection_area(first, triangles[j])
        if overlap > 1e-12:
            overlaps.append({"triangles": [i, j], "area": overlap})

asset = root / "RailArena/Assets/RailArena/Art/PrecisionCarbine"
weights = [(1/3, 1/3, 1/3), (0.8, 0.1, 0.1), (0.1, 0.8, 0.1), (0.1, 0.1, 0.8)]
texture_results = {}
for filename in ("BaseColor.png", "MetallicSmoothness.png", "Emission.png", "Normal.png", "MetalDetailMask.png"):
    image = Image.open(asset / filename).convert("RGBA")
    pixels = image.load()
    changed = 0
    for original, candidate in zip(data["original"], triangles):
        for weight in weights:
            def sample(tri):
                u = sum(tri[i][0] * weight[i] for i in range(3))
                v = sum(tri[i][1] * weight[i] for i in range(3))
                x = min(image.width - 1, max(0, int(u * image.width)))
                y = image.height - 1 - min(image.height - 1, max(0, int(v * image.height)))
                return pixels[x, y]
            changed += sample(original) != sample(candidate)
    texture_results[filename] = {"samples": len(triangles) * len(weights), "differentSamples": changed}

report = {"name": data["name"], "triangles": len(triangles), "overlapPairsAboveTolerance": len(overlaps),
          "overlapAreaUV": sum(x["area"] for x in overlaps), "largestOverlaps": sorted(overlaps, key=lambda x: x["area"], reverse=True)[:10],
          "textures": texture_results, "sourceBlendSaved": False}
path = folder / "CarbineForeendUVAudit.json"
if path.exists():
    raise RuntimeError("Audit output already exists")
path.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report, indent=2))
