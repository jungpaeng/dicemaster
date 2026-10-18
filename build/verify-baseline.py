"""Verify pinned Git baseline recovery and failure handling in disposable repositories."""
from pathlib import Path
import argparse
import datetime
import os
import subprocess
import tempfile
from unittest import mock

import baseline
from source_build import (ARTIFACTS, REPOSITORY, artifact_directory, compatibility,
                          digest, record, write_json)


def git(*arguments):
    result = subprocess.run(['git', *map(str, arguments)], text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode:
        raise RuntimeError('Git test setup failed: ' + result.stderr.strip())
    return result.stdout.strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--report-dir', type=Path, default=ARTIFACTS / 'baseline-checks')
    args = parser.parse_args()
    try:
        report_dir = artifact_directory(args.report_dir, '--report-dir')
    except ValueError as error:
        parser.error(str(error))
    report_dir.mkdir(parents=True, exist_ok=True)
    report = report_dir / 'baseline-checks.json'
    report.unlink(missing_ok=True)
    results = []

    def passed(name, **evidence):
        results.append({'case': name, 'status': 'pass', **evidence})
        print('PASS', name, flush=True)

    def reject(name, operation, expected):
        try:
            operation()
        except RuntimeError as error:
            if expected.lower() not in str(error).lower():
                raise RuntimeError(name + ' failed with an unexpected diagnostic: ' + str(error))
            passed(name, diagnostic=str(error))
            return
        raise RuntimeError(name + ' unexpectedly succeeded.')

    for name in ('config', 'plugins'):
        if (REPOSITORY / name).exists():
            raise RuntimeError('Repository root ' + name + '/ must be absent before baseline verification.')
    recovered = baseline.get_baseline(REPOSITORY)
    if digest(recovered) != baseline.BASELINE_SHA256:
        raise RuntimeError('Recovered baseline does not match the pinned DLL.')
    original = recovered.read_bytes()
    passed('real-repository-without-root-config-or-plugins', baseline=record(recovered))

    common = Path(git('-C', REPOSITORY, 'rev-parse', '--git-common-dir'))
    if not common.is_absolute():
        common = REPOSITORY / common
    objects = (common / 'objects').resolve()
    if not objects.is_dir():
        raise RuntimeError('Cannot locate read-only Git object store for disposable tests.')

    with tempfile.TemporaryDirectory(prefix='dicemaster-baseline-checks-') as directory:
        work = Path(directory)

        def repository(name, history=True):
            root = work / name
            git('init', '--quiet', root)
            if history:
                alternates = root / '.git/objects/info/alternates'
                alternates.write_text(str(objects) + '\n')
            return root

        # These repositories have no checkout of config/, plugins/, or an old
        # mod DLL. Their only baseline provenance is the fixed Git object.
        fresh = repository('fresh')
        cache = baseline.baseline_cache_path(fresh)
        if cache.exists():
            raise RuntimeError('Disposable baseline cache was not empty.')
        restored = baseline.get_baseline(fresh)
        if restored != cache or restored.read_bytes() != original:
            raise RuntimeError('A fresh repository did not reproduce the exact fixed Git blob.')
        passed('fresh-cache-from-fixed-git-blob', baseline=record(restored))

        cache.unlink()
        if baseline.get_baseline(fresh).read_bytes() != original:
            raise RuntimeError('Missing cache was not regenerated from Git.')
        passed('deleted-cache-regenerated')

        cache.write_bytes(b'corrupted cached baseline')
        if baseline.get_baseline(fresh).read_bytes() != original:
            raise RuntimeError('Corrupt cache was not replaced with the verified Git blob.')
        passed('corrupt-cache-repaired-from-git')

        # Even a correct prior cache must not impersonate unavailable history.
        missing = repository('missing-history', history=False)
        missing_cache = baseline.baseline_cache_path(missing)
        missing_cache.parent.mkdir(parents=True, exist_ok=True)
        missing_cache.write_bytes(original)
        reject('missing-history-rejects-valid-cache-fallback',
               lambda: baseline.get_baseline(missing), 'Git')
        if missing_cache.read_bytes() != original:
            raise RuntimeError('Failed Git lookup modified an existing baseline cache.')

        wrong = repository('wrong-git-blob')
        wrong_cache = baseline.baseline_cache_path(wrong)
        wrong_blob = subprocess.CompletedProcess(args=['git'], returncode=0,
                                                stdout=b'wrong fixed-commit DLL blob', stderr=b'')
        with mock.patch.object(baseline.subprocess, 'run', return_value=wrong_blob) as query:
            reject('wrong-git-blob-hash-rejected', lambda: baseline.get_baseline(wrong), 'SHA-256')
            if query.call_count == 0:
                raise RuntimeError('Wrong Git blob injection did not exercise the Git lookup.')
        if wrong_cache.exists():
            raise RuntimeError('Unverified Git blob was written into the baseline cache.')
        passed('wrong-git-blob-never-cached')

        # Explicit compatibility input is subject to the same pinned hash even
        # when a caller bypasses the default baseline helper.
        custom = work / 'custom-wrong-baseline.dll'
        custom.write_bytes(b'explicit wrong baseline')
        reject('explicit-wrong-baseline-rejected-before-compile',
               lambda: compatibility(None, custom, work / 'absent-candidate.dll',
                                     work / 'not-built', None),
               baseline.BASELINE_SHA256)

        symlink_repo = repository('symlink-cache')
        symlink_cache = baseline.baseline_cache_path(symlink_repo)
        symlink_cache.parent.mkdir(parents=True, exist_ok=True)
        untouched = work / 'symlink-target.dll'
        untouched.write_bytes(b'symlink target must remain unchanged')
        symlink_cache.symlink_to(untouched)
        reject('symlink-cache-rejected', lambda: baseline.get_baseline(symlink_repo), 'Unsafe baseline cache file')
        if untouched.read_bytes() != b'symlink target must remain unchanged':
            raise RuntimeError('Baseline recovery followed a cache symlink.')

        directory_repo = repository('directory-cache')
        directory_cache = baseline.baseline_cache_path(directory_repo)
        directory_cache.mkdir(parents=True)
        reject('directory-instead-of-dll-rejected',
               lambda: baseline.get_baseline(directory_repo), 'file')

        protected = baseline.baseline_cache_path(fresh)
        reject('legacy-output-cannot-overwrite-baseline-cache',
               lambda: baseline.protect_baseline_output(protected, fresh), 'baseline')
        alias = work / 'hardlink-baseline.dll'
        os.link(protected, alias)
        reject('legacy-output-cannot-overwrite-baseline-hardlink',
               lambda: baseline.protect_baseline_output(alias, fresh), 'baseline')
        linked_output = work / 'symlink-baseline.dll'
        linked_output.symlink_to(protected)
        reject('legacy-output-cannot-overwrite-baseline-symlink',
               lambda: baseline.protect_baseline_output(linked_output, fresh), 'baseline')
        if cache.read_bytes() != original:
            raise RuntimeError('Protection checks modified the disposable verified cache.')

    write_json(report, {'status': 'pass',
                        'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                        'baseline_commit': baseline.BASELINE_COMMIT,
                        'baseline_git_path': baseline.BASELINE_GIT_PATH,
                        'baseline': record(recovered), 'cases': results,
                        'scope': 'Root folders absent; disposable cache/history failures; actual Git history unchanged.'})
    print('BASELINE VERIFICATION PASS', len(results), 'cases:', report)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('BASELINE VERIFICATION FAILED: ' + str(error))
