"""Compatibility entry point for pinned source recovery."""
from pathlib import Path
import runpy


if __name__ == '__main__':
    runpy.run_path(str(Path(__file__).resolve().parents[2] /
                       'tools/SourceRecovery/recover-source.py'), run_name='__main__')
