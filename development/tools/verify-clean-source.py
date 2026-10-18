"""Build a fresh source-only snapshot with no preexisting mod DLL or build outputs."""
from pathlib import Path
import argparse
import datetime
import json
import os
import shutil
import sys
import tempfile
from source_build import DEVELOPMENT, REPOSITORY, run, record, write_json


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', required=True, type=Path)
    parser.add_argument('--report-dir', type=Path, default=DEVELOPMENT / 'releases/source-candidate')
    args = parser.parse_args()
    report_dir = args.report_dir.resolve()
    if not report_dir.is_relative_to((DEVELOPMENT / 'releases').resolve()):
        parser.error('--report-dir must be inside development/releases.')
    report_dir.mkdir(parents=True, exist_ok=True)
    report = report_dir / 'clean-build.json'
    report.unlink(missing_ok=True)
    log = report_dir / 'clean-build.log'
    log.write_text('')
    with tempfile.TemporaryDirectory(prefix='dicemaster-clean-source-') as directory:
        snapshot = Path(directory) / 'source'
        snapshot.mkdir()
        # Explicit allowlist: no plugins, installed DLL, git database, prior generated
        # outputs, old temporary extracts, recovery DLL, or external game files.
        shutil.copytree(DEVELOPMENT, snapshot / 'development',
                        ignore=shutil.ignore_patterns('bin', 'obj', 'releases', '__pycache__'))
        shutil.copytree(REPOSITORY / 'config', snapshot / 'config')
        forbidden = list(snapshot.rglob('DiceMaster.dll'))
        if forbidden:
            raise RuntimeError('Preexisting mod DLL leaked into clean snapshot: ' + str(forbidden))
        run([sys.executable, snapshot / 'development/tools/build-source.py',
             '--references', args.references.resolve()], log)
        result = snapshot / 'development/releases/source-candidate'
        manifest = json.loads((result / 'build-manifest.json').read_text())
        write_json(report, {
            'status': 'pass', 'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'method': 'fresh source snapshot; plugins/, .git/, bin/, obj/, releases/ excluded',
            'preexisting_dicemaster_dll_count': len(forbidden),
            'dll': record(result / 'DiceMaster.dll'), 'zip': record(result / 'DiceMaster-source-candidate.zip'),
            'tests': manifest['tests'], 'sdk': manifest['sdk'],
            'windows_validation': 'not authorized / not performed',
        })
    print('CLEAN SOURCE BUILD PASS', report)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('CLEAN SOURCE BUILD FAILED: ' + str(error))
