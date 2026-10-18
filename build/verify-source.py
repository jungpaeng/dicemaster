"""Compare a source-built candidate with the fixed alpha.3 baseline (separate from building)."""
from pathlib import Path
import argparse
import datetime
import tempfile
from baseline import get_baseline
from source_build import (ARTIFACTS, BASELINE_COMMIT, BASELINE_SHA256,
                          sdk, references, compatibility, record, write_json, artifact_directory, release_policy)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', required=True, type=Path)
    parser.add_argument('--baseline', type=Path,
                        help='Explicit alpha.3 DLL (SHA-256 checked); default: recreate from fixed local Git commit.')
    parser.add_argument('--candidate', type=Path, default=ARTIFACTS / 'source-candidate/DiceMaster.dll')
    args = parser.parse_args()
    candidate = args.candidate.resolve()
    try:
        artifact_directory(candidate.parent, '--candidate parent', args.references)
    except ValueError as error:
        parser.error(str(error))
    report = candidate.parent / 'compatibility.json'
    log = candidate.parent / 'compatibility.log'
    # A failed comparison must not leave a prior PASS receipt for this path.
    report.unlink(missing_ok=True)
    log.write_text('')
    baseline = args.baseline.resolve() if args.baseline is not None else get_baseline()
    baseline_info = record(baseline)
    if baseline_info['sha256'] != BASELINE_SHA256:
        raise RuntimeError('Compatibility baseline must be the fixed v1.0.3-alpha.3 DLL: ' + BASELINE_SHA256)
    dotnet, version = sdk()
    reference_info = references(args.references)
    before = record(candidate)
    with tempfile.TemporaryDirectory(prefix='dicemaster-source-compare-') as directory:
        checks = compatibility(dotnet, baseline, candidate, Path(directory), log)
    if record(candidate) != before:
        raise RuntimeError('Candidate changed during compatibility comparison.')
    write_json(report, {'status': 'pass', 'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                        'baseline_commit': BASELINE_COMMIT, 'baseline': baseline_info,
                        'candidate': before, 'sdk': version, 'references': reference_info,
                        'checks': checks, 'windows_validation': release_policy()['windows_validation'],
                        'default_build_switched': True})
    print('COMPATIBILITY PASS', report)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('SOURCE COMPARISON FAILED: ' + str(error))
