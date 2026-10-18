"""Compatibility entry point for the preserved legacy gamepad patch workflow."""
from pathlib import Path
import runpy


if __name__ == '__main__':
    runpy.run_path(str(Path(__file__).resolve().parents[2] /
                       'tools/LegacyPatch/build-gamepad.py'), run_name='__main__')
