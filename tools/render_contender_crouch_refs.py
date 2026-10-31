"""Compatibility entry point for canonical crouch references."""
from pathlib import Path
import runpy

runpy.run_path(str(Path(__file__).with_name("render_contender_motion_refs.py")), run_name="__main__")
