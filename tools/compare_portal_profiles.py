"""Compare controlled full-frame and aperture-cropped runs of the same player."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("full", type=Path)
parser.add_argument("cropped", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
full = json.loads(args.full.read_text(encoding="utf-8-sig"))
cropped = json.loads(args.cropped.read_text(encoding="utf-8-sig"))
for field in ("schema", "fingerprint", "gpu", "api", "width", "height", "warmupFrames", "sampleFrames", "simulationHz", "randomSeed"):
    if full[field] != cropped[field]:
        raise SystemExit(f"Workload mismatch: {field}")
if not full["passed"] or not cropped["passed"] or full["exceptions"] or cropped["exceptions"]:
    raise SystemExit("Both profiles must pass without exceptions")
if full["apertureCropping"] or not cropped["apertureCropping"]:
    raise SystemExit("Expected full-frame first and cropped second")
if [view["view"] for view in full["views"]] != [view["view"] for view in cropped["views"]]:
    raise SystemExit("View order mismatch")


def metric(view, name, unit):
    matches = [item for item in view["counters"] if item["name"] == name and item["unit"] == unit and item["available"] and item["mean"] > 0]
    if len(matches) != 1:
        raise SystemExit(f"Expected one positive {name}/{unit} counter in {view['view']}: {matches}")
    return {key: matches[0][key] for key in ("name", "category", "unit", "mean", "p95")}


rows = []
for before, after in zip(full["views"], cropped["views"]):
    for field in ("frames", "portalRenders"):
        if before[field] != after[field]:
            raise SystemExit(f"{before['view']}: {field} differs")
    if min(before["nonBlackSamples"], after["nonBlackSamples"]) <= 100:
        raise SystemExit("Rendered images unavailable")
    row = {"view": before["view"], "frames": before["frames"], "portalRenders": before["portalRenders"]}
    for label, view in (("full", before), ("cropped", after)):
        row[label] = {key: view[key] for key in ("meanWallMs", "p95WallMs", "meanReadbackMs", "validGpuSamples", "gc0", "gc1", "gc2", "portalTargets")}
        row[label]["portalCpu"] = metric(view, "RailArena.PortalView", "TimeNanoseconds")
        row[label]["drawCalls"] = metric(view, "Draw Calls Count", "Count")
        row[label]["triangles"] = metric(view, "Triangles Count", "Count")
    row["meanWallReductionPercent"] = 100 * (1 - after["meanWallMs"] / before["meanWallMs"])
    row["portalCpuReductionPercent"] = 100 * (1 - row["cropped"]["portalCpu"]["mean"] / row["full"]["portalCpu"]["mean"])
    rows.append(row)
record = {"schema": "rail-arena-portal-profile-comparison/1", "fingerprint": full["fingerprint"], "full": args.full.as_posix(), "cropped": args.cropped.as_posix(), "scope": full["scope"], "views": rows, "nativeFramePacingAccepted": False}
args.output.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
for row in rows:
    print(f"{row['view']}: wall {row['full']['meanWallMs']:.2f} -> {row['cropped']['meanWallMs']:.2f} ms; portal CPU {row['portalCpuReductionPercent']:.1f}% lower")
