import os,runpy
from pathlib import Path
os.environ['RAIL_BLUEPRINT_PACKAGE']='SkyGardens-Blueprint-v2'
runpy.run_path(str(Path(__file__).with_name('build_sky_construction.py')),run_name='__main__')
