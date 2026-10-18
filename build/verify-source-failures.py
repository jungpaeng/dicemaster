"""Exercise build failure paths in disposable snapshots; preserve existing candidates."""
from pathlib import Path
import argparse
import datetime
import os
import subprocess
import sys
import tempfile
from source_build import ARTIFACTS, digest, record, write_json, artifact_directory, snapshot_source


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
    report = report_dir / 'failure-paths.json'
    report.unlink(missing_ok=True)
    results = []
    with tempfile.TemporaryDirectory(prefix='dicemaster-source-failures-') as directory:
        work = Path(directory)
        snapshot = snapshot_source(work / 'source')
        output = snapshot / 'artifacts/sentinel'
        output.mkdir(parents=True)
        sentinel = output / 'DiceMaster.dll'
        sentinel.write_bytes(b'prior candidate sentinel; never a successful compiler output')
        write_json(output / 'build-manifest.json', {'status': 'local-source-candidate', 'dll': record(sentinel)})
        expected = digest(sentinel)
        command = [sys.executable, str(snapshot / 'build/build-source.py'),
                   '--references', str(args.references.resolve()), '--output-dir', str(output)]

        def reject(name, expected_message, extra=(), env=None, replaced=None):
            result = subprocess.run(replaced or [*command, *extra], text=True,
                                    stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=env)
            if result.returncode == 0 or expected_message not in result.stdout:
                raise RuntimeError(name + ' did not fail as expected:\n' + result.stdout)
            if digest(sentinel) != expected or set(path.name for path in output.iterdir()) != {'DiceMaster.dll', 'build-manifest.json'}:
                raise RuntimeError(name + ' replaced prior candidate or published a stale package.')
            (report_dir / ('failure-' + name + '.log')).write_text(result.stdout)
            results.append({'case': name, 'exit_code': result.returncode,
                            'expected_diagnostic': expected_message,
                            'prior_candidate_preserved': True, 'no_package_published': True})
            print('PASS rejected', name, flush=True)

        missing_sdk = os.environ.copy()
        missing_sdk['PATH'] = str(work / 'empty-path')
        reject('missing-sdk', 'The .NET 8 SDK is required', env=missing_sdk)
        reject('missing-reference', 'Missing read-only external reference',
               extra=['--references', str(work / 'missing-refs')])
        wrong = work / 'wrong-refs/Mortal_Data/Managed'
        wrong.mkdir(parents=True)
        (wrong / 'UnityEngine.CoreModule.dll').write_bytes(b'wrong version')
        reject('wrong-reference', 'Wrong reference version/hash',
               extra=['--references', str(work / 'wrong-refs')])
        sdk_bin = work / 'wrong-sdk'
        sdk_bin.mkdir()
        sdk_file = sdk_bin / 'dotnet'
        sdk_file.write_text('#!/bin/sh\nprintf "9.0.0\\n"\n')
        sdk_file.chmod(0o755)
        wrong_sdk = os.environ.copy()
        wrong_sdk['PATH'] = str(sdk_bin)
        reject('wrong-sdk', 'Use .NET SDK 8.x', env=wrong_sdk)
        reject('unsafe-output-root', '--output-dir must be a child directory',
               extra=['--output-dir', str(snapshot / 'artifacts')])
        for reserved in ('bin', 'obj'):
            reject('reserved-' + reserved + '-output', 'must not use the reserved artifacts/' + reserved,
                   extra=['--output-dir', str(snapshot / 'artifacts' / reserved / 'candidate')])
        unmanaged = snapshot / 'artifacts/legacy-output'
        unmanaged.mkdir()
        (unmanaged / 'prior-release.zip').write_bytes(b'existing legacy release')
        reject('unmanaged-output', 'Refusing to replace existing files in an unmanaged output directory',
               extra=['--output-dir', str(unmanaged)])
        if (unmanaged / 'prior-release.zip').read_bytes() != b'existing legacy release':
            raise RuntimeError('Unmanaged release output changed.')
        broken = snapshot / 'src/DiceMaster/MyPluginInfo.cs'
        original = broken.read_bytes()
        try:
            broken.write_bytes(original + b'\n#error Intentional failure-path verification\n')
            reject('compiler-error', 'Intentional failure-path verification')
        finally:
            broken.write_bytes(original)
        test = snapshot / 'tests/LogRefreshTests/Program.cs'
        original = test.read_text()
        old = 'if (args.Length != 2)'
        if original.count(old) != 1:
            raise RuntimeError('Expected failure injection point changed.')
        test.write_text(original.replace(old, 'if (args.Length == 2) throw new InvalidOperationException("Intentional test failure-path verification");\n        ' + old))
        reject('test-error', 'Intentional test failure-path verification')
        compare = snapshot / 'artifacts/failed-comparison'
        compare.mkdir()
        (compare / 'DiceMaster.dll').write_bytes(b'comparison input')
        write_json(compare / 'compatibility.json', {'status': 'pass', 'stale': True})
        result = subprocess.run([sys.executable, str(snapshot / 'build/verify-source.py'),
                                 '--references', str(work / 'missing-refs'),
                                 '--baseline', str(sentinel), '--candidate', str(compare / 'DiceMaster.dll')],
                                text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        if result.returncode == 0 or (compare / 'compatibility.json').exists():
            raise RuntimeError('Comparison preflight failure left a stale PASS receipt.')
        (report_dir / 'failure-comparison-preflight.log').write_text(result.stdout)
        results.append({'case': 'comparison-preflight', 'exit_code': result.returncode,
                        'stale_pass_receipt_removed': True})
    write_json(report, {'status': 'pass', 'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                        'cases': results, 'scope': 'disposable source snapshot only; no installed/default DLL modified'})
    print('FAILURE PATHS PASS', report)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('FAILURE-PATH VERIFICATION FAILED: ' + str(error))
