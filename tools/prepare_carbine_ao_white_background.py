"""Create an unoccluded-background AO candidate without altering covered texels."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / "Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedAudited"
output = folder / "AO-white-background.png"
if output.exists():
    raise RuntimeError("Candidate output already exists")
ao = Image.open(folder / "AO.png").convert("RGB")
mask = Image.open(folder / "Coverage.png").convert("L")
if ao.size != mask.size:
    raise RuntimeError("AO and coverage dimensions differ")
# Covered texels, including the bake margin, retain their exact baked values.
# Only fully uncovered texels become neutral white.
binary = mask.point(lambda value: 255 if value else 0)
result = Image.composite(ao, Image.new("RGB", ao.size, "white"), binary)
result.save(output)
covered = sum(mask.histogram()[1:])
report = {"size": list(ao.size), "coveredTexelsPreserved": covered,
          "uncoveredTexelsSetToWhite": ao.width * ao.height - covered,
          "sourceAOUnchanged": True}
(folder / "white-background.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report))
