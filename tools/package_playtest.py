"""Package a Windows player and verify every archived file before reporting success."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile


def package(build, output, fingerprint):
    build = build.resolve(strict=True)
    output = output.resolve()
    if not re.fullmatch(r"[a-f0-9]{64}", fingerprint):
        raise ValueError("Expected the full 64-character build fingerprint.")
    if not (build / "RailArena.exe").is_file() or not (build / "RailArena_Data").is_dir():
        raise ValueError("Select the complete Rail Arena Windows player folder.")
    if output.is_relative_to(build):
        raise ValueError("Write the package outside the player folder.")
    if output.exists():
        raise FileExistsError(f"Refusing to replace an existing package: {output}")
    files = {}
    for path in sorted(build.rglob("*")):
        relative = path.relative_to(build)
        if any("BackUpThisFolder_ButDontShipItWithYourGame" in part for part in relative.parts):
            continue
        if path.is_symlink():
            raise ValueError(f"Unexpected symbolic link: {relative}")
        if path.is_file():
            files[relative.as_posix()] = path.read_bytes()
    root = Path(__file__).resolve().parent.parent
    for name in ("LICENSE", "THIRD-PARTY-NOTICES.md"):
        files[name] = (root / name).read_bytes()
    files["START-HERE.txt"] = (
        "Rail Arena - pre-alpha 0.1.0-pre-alpha.1\n\n"
        "Extract this entire ZIP, then run RailArena.exe. Keep all adjacent files.\n"
        "Windows keyboard/mouse: WASD move, mouse aim, left click fire, hold right mouse 1.5x zoom, Space jump,\n"
        "Tab scoreboard, Escape pause/settings. Choose PRACTICE and 0 // SOLO\n"
        "or 1-3 bots for the intended four-contender scope. Offline play needs no account, server or internet.\n\n"
        "This is unfinished: character/action polish, native control feel and real\n"
        "internet multiplayer acceptance remain open. Local/simulated tests are\n"
        "documented separately from human acceptance.\n\n"
        "Build fingerprint: " + fingerprint + "\n"
        "Source and evaluation guide:\n"
        "https://github.com/Evekiio/rail-arena/blob/main/docs/PRE-ALPHA.md\n"
    ).encode("utf-8")
    manifest = {name: hashlib.sha256(data).hexdigest() for name, data in files.items()}
    files["package-manifest.json"] = (json.dumps(
        {"fingerprint": fingerprint, "sha256": manifest}, indent=2) + "\n").encode("utf-8")
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in files.items():
            archive.writestr(name, data)
    with zipfile.ZipFile(output) as archive:
        if set(archive.namelist()) != set(files):
            raise ValueError("Archive entry inventory mismatch.")
        for name, data in files.items():
            if archive.read(name) != data:
                raise ValueError(f"Archive verification failed: {name}")
    return {"fingerprint": fingerprint, "filesVerified": len(files),
            "bytes": output.stat().st_size,
            "sha256": hashlib.sha256(output.read_bytes()).hexdigest(), "passed": True}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--fingerprint", required=True)
    args = parser.parse_args()
    print(json.dumps(package(args.build, args.output, args.fingerprint), indent=2))
