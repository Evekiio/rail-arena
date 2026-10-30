"""Create a smaller cyan VFX candidate from the validated large burst."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / "RailArena/Assets/Validation/TraversalVfxCandidate"
source = folder / "TraversalBurstLarge.vfx"
destination = folder / "TraversalBurstCyan.vfx"
if destination.exists():
    raise RuntimeError("Cyan candidate already exists")
text = source.read_text(encoding="utf-8")

def update_slot(text, anchor_id, expected, transform):
    start = text.index("--- !u!114 &" + anchor_id + "\n")
    end = text.index("--- !u!", start + 1)
    block = text[start:end]
    match = re.search(r"m_SerializableObject: '([^\n]+)'", block)
    if match is None or expected not in block:
        raise RuntimeError("Unexpected serialized slot " + anchor_id)
    data = json.loads(match.group(1))
    transform(data)
    replacement = "m_SerializableObject: '" + json.dumps(data, separators=(",", ":")) + "'"
    return text[:start] + block[:match.start()] + replacement + block[match.end():] + text[end:]

def tint(gradient):
    for key in gradient["colorKeys"]:
        key["color"].update({"r": .05, "g": .75, "b": 1.0, "a": 1.0})
    for key in gradient["alphaKeys"]:
        key["alpha"] *= .22

def shrink(curve):
    for frame in curve["frames"]:
        for key in ("value", "inTangent", "outTangent"):
            frame[key] *= .12

text = update_slot(text, "8926484042661614793", "name: Color", tint)
text = update_slot(text, "8926484042661614953", "name: Size", shrink)
destination.write_text(text, encoding="utf-8", newline="\n")
print("CREATED", destination)
