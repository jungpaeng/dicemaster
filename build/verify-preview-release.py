"""Rebuild an explicitly selected preview commit and verify the reviewed DLL/config.

This prerelease-only path retains the original source-restoration verifier. It
does not claim alpha.4 runtime equivalence or a Windows run of PRD 2 changes.
No Git refs, network resources, installed plugins or game references are written.
"""
from pathlib import Path
import argparse
import datetime
import importlib.util
import json
import re
import shutil
import sys
import tempfile
import zipfile

from source_build import (ARTIFACTS, BUILD, artifact_directory, digest, record,
                          run, write_json)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', type=Path, required=True)
    parser.add_argument('--source-commit', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--expected-dll-sha256', required=True)
    parser.add_argument('--expected-config-sha256', required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    if not re.fullmatch(r'v\d+\.\d+\.\d+-[A-Za-z0-9]+(?:[.-][A-Za-z0-9]+)*', args.version):
        parser.error('This command accepts a prerelease version only.')
    for value in (args.expected_dll_sha256, args.expected_config_sha256):
        if not re.fullmatch(r'[0-9a-f]{64}', value):
            parser.error('Expected hashes must be full lowercase SHA-256 values.')
    try:
        output = artifact_directory(args.output_dir, '--output-dir', args.references)
    except ValueError as error:
        parser.error(str(error))
    if output.exists() and any(output.iterdir()):
        parser.error('Output must be absent or empty; previous artifacts are preserved.')
    # Reuse the existing commit allowlist and archive safety checks without
    # weakening its historical full-equivalence/Windows-reference policy.
    spec = importlib.util.spec_from_file_location('release_verifier', BUILD / 'verify-release.py')
    verifier = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(verifier)
    with tempfile.TemporaryDirectory(prefix='dicemaster-preview-release-') as directory:
        work = Path(directory)
        source = work / 'source'
        commit = verifier.committed_snapshot(args.source_commit, source)
        if any((source / name).exists() for name in ('plugins', 'config', '.git', 'artifacts')):
            raise RuntimeError('Non-source distribution or Git data entered the snapshot.')
        allowed = {'src/DiceMaster/Resources/native/MinHook.x64.dll',
                   'src/DiceMaster/Resources/native/MinHook.x86.dll'}
        forbidden = [path.relative_to(source).as_posix() for path in source.rglob('*')
                     if path.is_file() and path.suffix.lower() == '.dll'
                     and path.relative_to(source).as_posix() not in allowed]
        if forbidden:
            raise RuntimeError('Preexisting managed or unapproved DLL in source snapshot.')
        hashes = {path.relative_to(source).as_posix(): digest(path)
                  for path in source.rglob('*') if path.is_file()}
        log = work / 'verification.log'
        try:
            run([sys.executable, source / 'build/build-source.py', '--references',
                 args.references.resolve()], log)
            candidate = source / 'artifacts/source-candidate'
            manifest = json.loads((candidate / 'build-manifest.json').read_text())
            dll = candidate / 'DiceMaster.dll'
            archive = candidate / 'DiceMaster-source-candidate.zip'
            config = source / 'packaging/config/DiceMaster.cfg'
            if (manifest['status'] != 'local-source-candidate' or record(dll) != manifest['dll']
                    or digest(dll) != args.expected_dll_sha256
                    or digest(config) != args.expected_config_sha256
                    or record(archive) != manifest['zip']):
                raise RuntimeError('Fresh commit build differs from the reviewed DLL/config/package.')
            if manifest['build_input_sha256'] != {name: hashes[name] for name in manifest['build_input_sha256']}:
                raise RuntimeError('Built inputs differ from the selected committed source.')
            with zipfile.ZipFile(archive) as package:
                if (package.namelist() != ['config/DiceMaster.cfg', 'plugins/DiceMaster/DiceMaster.dll']
                        or package.testzip() is not None
                        or package.read('config/DiceMaster.cfg') != config.read_bytes()
                        or package.read('plugins/DiceMaster/DiceMaster.dll') != dll.read_bytes()):
                    raise RuntimeError('Two-file ZIP, CRC or committed artifact bytes do not match.')
            validation = manifest['candidate_validation']
            if (validation['windows_runtime'] != 'not-run'
                    or validation['real_lua_comparison'] != 'not-run'
                    or validation['historical_windows_confirmation_applies_to_this_candidate']):
                raise RuntimeError('Preview validation boundaries must remain explicit.')
            stage = work / 'verified'
            stage.mkdir()
            for name in ('DiceMaster.dll', 'build-manifest.json', 'build.log'):
                shutil.copyfile(candidate / name, stage / name)
            package_name = 'DiceMaster-' + args.version + '.zip'
            shutil.copyfile(archive, stage / package_name)
            shutil.copyfile(log, stage / 'verification.log')
            write_json(stage / 'release-verification.json', {
                'status': 'prerelease-commit-build-verified', 'published': False,
                'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'source_commit': commit, 'version': args.version,
                'preexisting_managed_dll_count': 0,
                'build_input_sha256': manifest['build_input_sha256'],
                'dll': manifest['dll'], 'zip': manifest['zip'], 'config': manifest['config'],
                'package_name': package_name, 'tests': manifest['tests'],
                'reviewed_dll_sha256': args.expected_dll_sha256,
                'reviewed_config_sha256': args.expected_config_sha256,
                'zip_file_list_and_crc': 'pass', 'candidate_validation': validation,
                'historical_full_runtime_equivalence_claimed': False,
                'feature_or_issue_completion_claimed': False,
            })
            output.parent.mkdir(parents=True, exist_ok=True)
            if output.exists():
                if any(output.iterdir()):
                    raise RuntimeError('Output became nonempty during verification.')
                output.rmdir()
            shutil.move(str(stage), str(output))
        except BaseException:
            output.mkdir(parents=True, exist_ok=True)
            if log.exists():
                shutil.copyfile(log, output / 'verification.failed.log')
            raise
    print('PRERELEASE COMMIT BUILD VERIFIED', output / 'release-verification.json')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('PRERELEASE VERIFICATION FAILED: ' + str(error))
