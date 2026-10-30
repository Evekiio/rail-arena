"""Create a more visible cyan candidate while retaining the original."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / "RailArena/Assets/Validation/TraversalVfxCandidate"
source = folder / "TraversalBurstCyan.vfx"
destination = folder / "TraversalBurstCyanVisible.vfx"
if destination.exists():
    raise RuntimeError("Visible candidate already exists")
text = source.read_text(encoding="utf-8")

def update(text, anchor, expected, transform):
    start = text.index("--- !u!114 &" + anchor + "\n")
    end = text.index("--- !u!", start + 1)
    block = text[start:end]
    match = re.search(r"m_SerializableObject: '([^\n]+)'", block)
    if match is None or expected not in block:
        raise RuntimeError("Unexpected slot " + anchor)
    data = json.loads(match.group(1))
    transform(data)
    value = "m_SerializableObject: '" + json.dumps(data, separators=(",", ":")) + "'"
    return text[:start] + block[:match.start()] + value + block[match.end():] + text[end:]

def brighten(gradient):
    for key in gradient["alphaKeys"]:
        key["alpha"] = min(1.0, key["alpha"] * 2)

def enlarge(curve):
    for frame in curve["frames"]:
        for key in ("value", "inTangent", "outTangent"):
            frame[key] *= 2

text = update(text, "8926484042661614793", "name: Color", brighten)
text = update(text, "8926484042661614953", "name: Size", enlarge)
destination.write_text(text, encoding="utf-8", newline="\n")
print("CREATED", destination)
