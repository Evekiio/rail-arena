"""Make a higher-capacity copy of the imported Unity burst template."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / "RailArena/Assets/Validation/TraversalVfxCandidate"
source = folder / "TraversalBurst.vfx"
destination = folder / "TraversalBurstLarge.vfx"
if destination.exists():
    raise RuntimeError("Large candidate already exists")
text = source.read_text(encoding="utf-8")
anchor = "--- !u!114 &8926484042661614704\n"
start = text.index(anchor)
end = text.index("--- !u!", start + len(anchor))
block = text[start:end]
if "    name: Count\n" not in block or block.count("m_SerializableObject: 128") != 1:
    raise RuntimeError("Unexpected template burst count slot")
block = block.replace("m_SerializableObject: 128", "m_SerializableObject: 2048")
text = text[:start] + block + text[end:]
if text.count("  capacity: 128\n") != 1:
    raise RuntimeError("Unexpected template particle capacity")
text = text.replace("  capacity: 128\n", "  capacity: 4096\n")
destination.write_text(text, encoding="utf-8", newline="\n")
print("CREATED", destination)
