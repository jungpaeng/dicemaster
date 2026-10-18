"""Build a fresh source-only snapshot with no preexisting mod DLL or build outputs."""
from pathlib import Path
import argparse
import datetime
import json
import sys
import tempfile
from source_build import ARTIFACTS, run, record, write_json, artifact_directory, snapshot_source


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', required=True, type=Path)
    parser.add_argument('--report-dir', type=Path, default=ARTIFACTS / 'source-candidate')
    args = parser.parse_args()
    try:
        report_dir = artifact_directory(args.report_dir, '--report-dir', args.references)
    except ValueError as error:
        parser.error(str(error))
    report_dir.mkdir(parents=True, exist_ok=True)
    report = report_dir / 'clean-build.json'
    report.unlink(missing_ok=True)
    log = report_dir / 'clean-build.log'
    log.write_text('')
    with tempfile.TemporaryDirectory(prefix='dicemaster-clean-source-') as directory:
        # Explicit allowlist: no plugins, installed DLL, git database, prior generated
        # outputs, old temporary extracts, recovery DLL, or external game files.
        snapshot = snapshot_source(Path(directory) / 'source')
        forbidden = list(snapshot.rglob('DiceMaster.dll'))
        if forbidden:
            raise RuntimeError('Preexisting mod DLL leaked into clean snapshot: ' + str(forbidden))
        run([sys.executable, snapshot / 'build/build-source.py',
             '--references', args.references.resolve()], log)
        result = snapshot / 'artifacts/source-candidate'
        manifest = json.loads((result / 'build-manifest.json').read_text())
        write_json(report, {
            'status': 'pass', 'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'method': 'allowlisted src/, tests/, tools/, build/, packaging/ and root build files; plugins/, config/, .git/, artifacts/, development/ excluded; approved embedded MinHook DLLs only',
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
