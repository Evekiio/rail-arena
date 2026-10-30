"""Check the disposable carbine AO bake against the editable source UVs."""
import bpy
import json
import sys
from pathlib import Path

root = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
source = root / "ArtSource/Original/PrecisionCarbine-Production-v1/Precision-Carbine.blend"
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineAO"
bpy.ops.wm.open_mainfile(filepath=str(source))
image = bpy.data.images.load(str(folder / "Carbine AO.png"), check_existing=False)
image.colorspace_settings.name = "Non-Color"
pixels = list(image.pixels)
coverage_image = bpy.data.images.load(str(folder / "CoverageWhite.png"), check_existing=False)
coverage_image.colorspace_settings.name = "Non-Color"
coverage = list(coverage_image.pixels)
width, height = image.size
rows = []
triangle_count = 0
triangle_dark = 0
triangle_dark_flipped = 0
subpixel = 0
large_triangles = 0
large_dark = 0
covered = 0
covered_dark = 0
for obj in sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name):
    uv = obj.data.uv_layers.get("SurfaceAtlas")
    if uv is None:
        raise RuntimeError("Missing SurfaceAtlas on " + obj.name)
    values = []
    for poly in obj.data.polygons:
        coords = [uv.data[i].uv for i in poly.loop_indices]
        u = sum(v.x for v in coords) / len(coords)
        v = sum(v.y for v in coords) / len(coords)
        x = min(width - 1, max(0, int(u * width)))
        y = min(height - 1, max(0, int(v * height)))
        values.append(pixels[(y * width + x) * 4 + 1])
    rows.append({"name": obj.name, "faces": len(values), "nearBlack": sum(x < .02 for x in values), "mean": sum(values) / len(values) if values else 0})
    obj.data.calc_loop_triangles()
    for tri in obj.data.loop_triangles:
        coords = [uv.data[i].uv for i in tri.loops]
        u = sum(v.x for v in coords) / 3
        v = sum(v.y for v in coords) / 3
        x = min(width - 1, max(0, int(u * width)))
        y = min(height - 1, max(0, int(v * height)))
        area = abs((coords[1].x - coords[0].x) * (coords[2].y - coords[0].y) - (coords[1].y - coords[0].y) * (coords[2].x - coords[0].x)) * width * height * .5
        triangle_count += 1
        subpixel += area < 1
        triangle_dark += pixels[(y * width + x) * 4 + 1] < .02
        is_covered = coverage[(y * width + x) * 4 + 1] > .5
        covered += is_covered
        covered_dark += is_covered and pixels[(y * width + x) * 4 + 1] < .02
        triangle_dark_flipped += pixels[((height - 1 - y) * width + x) * 4 + 1] < .02
        if area >= 4:
            large_triangles += 1
            large_dark += pixels[(y * width + x) * 4 + 1] < .02
report = {"objects": len(rows), "faces": sum(x["faces"] for x in rows), "nearBlack": sum(x["nearBlack"] for x in rows), "triangles": triangle_count, "triangleNearBlack": triangle_dark, "triangleNearBlackFlipped": triangle_dark_flipped, "subpixelTriangles": subpixel, "trianglesAtLeast4Pixels": large_triangles, "largeNearBlack": large_dark, "coverageWhite": covered, "coveredNearBlack": covered_dark, "parts": rows}
(folder / "uv-coverage-mask.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print("CARBINE_AO_COVERAGE " + json.dumps({k: report[k] for k in ("objects", "triangles", "triangleNearBlack", "subpixelTriangles", "trianglesAtLeast4Pixels", "largeNearBlack", "coverageWhite", "coveredNearBlack")}))
