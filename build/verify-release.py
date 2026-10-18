"""Build and verify a local release bundle; never publish or change Git refs."""
from pathlib import Path, PurePosixPath
import argparse
import datetime
import hashlib
import io
import json
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import zipfile

from baseline import BASELINE_COMMIT, BASELINE_SHA256, get_baseline
from source_build import (REPOSITORY, ARTIFACTS, artifact_directory, build_inputs,
                          build, digest, record, root_build_files, run, sdk, snapshot_source, write_json)


def committed_snapshot(commit, destination):
    """Read only the requested commit's build inputs, excluding historical DLLs."""
    if not re.fullmatch(r'[0-9a-fA-F]{40}', commit):
        raise ValueError('--source-commit must be a full 40-character commit SHA.')
    selected = subprocess.check_output(
        ['git', '--no-replace-objects', '-C', str(REPOSITORY), 'rev-parse', '--verify', commit + '^{commit}'], text=True).strip()
    if selected.lower() != commit.lower():
        raise RuntimeError('Source commit did not resolve to the requested commit.')
    archive = subprocess.check_output(['git', '--no-replace-objects', '-C', str(REPOSITORY), 'archive', '--format=tar', selected])
    allowed_roots = {'src', 'tests', 'tools', 'build', 'packaging'}
    allowed_files = {path.name for path in root_build_files()}
    destination.mkdir()
    with tarfile.open(fileobj=io.BytesIO(archive), mode='r:') as source:
        for member in source.getmembers():
            path = PurePosixPath(member.name)
            if path.is_absolute() or '..' in path.parts:
                raise RuntimeError('Unsafe committed archive path: ' + member.name)
            if not path.parts or (path.parts[0] not in allowed_roots and member.name not in allowed_files):
                continue
            if {'bin', 'obj', '__pycache__'}.intersection(path.parts) or path.name == '.DS_Store':
                continue
            if member.isdir():
                continue
            if not member.isfile():
                raise RuntimeError('Only regular committed build inputs are supported: ' + member.name)
            target = destination / member.name
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(source.extractfile(member).read())
    return selected


def verify_windows_reference(snapshot, candidate, manifest, references, work, log):
    """Rebuild the exact user-tested release, then strictly compare runtime data.

    The reference is regenerated from its immutable source commit in a new
    directory. It is never an input to the ordinary source compilation, and a
    static comparison never impersonates a Windows run of the new DLL.
    """
    validation = manifest['windows_validation']
    if not isinstance(validation, dict) or not validation.get('release_equivalence_required'):
        return None  # Historical commit reproduction retains its own policy.
    if validation.get('status') != 'user-confirmed' or not manifest['default_build_switched']:
        raise RuntimeError('Source transition requires recorded user Windows confirmation.')
    reference_source = work / 'windows-reference-source'
    reference_commit = committed_snapshot(validation['tested_source_commit'], reference_source)
    dotnet, _ = sdk()
    compiled = build(dotnet, reference_source / 'src/DiceMaster/DiceMaster.csproj', work,
                     'windows-reference', log, ['-p:GameReferences=' + str(references)])
    reference_dll = compiled / 'DiceMaster.dll'
    if record(reference_dll)['sha256'] != validation['tested_dll_sha256']:
        raise RuntimeError('Rebuilt Windows reference differs from the exact user-tested DLL.')
    reference_config = reference_source / 'packaging/config/DiceMaster.cfg'
    if (record(reference_config)['sha256'] != validation['tested_config_sha256'] or
            reference_config.read_bytes() != (snapshot / 'packaging/config/DiceMaster.cfg').read_bytes()):
        raise RuntimeError('Release configuration differs from the user-tested alpha.4 configuration.')
    # Recreate the historical archive encoding and require its already published
    # digest, connecting the reference config as well as its DLL to that ZIP.
    archive_bytes = io.BytesIO()
    with zipfile.ZipFile(archive_bytes, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, path in [('config/DiceMaster.cfg', reference_config),
                           ('plugins/DiceMaster/DiceMaster.dll', reference_dll)]:
            entry = zipfile.ZipInfo(name, (2026, 10, 18, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            archive.writestr(entry, path.read_bytes())
    reference_zip_hash = hashlib.sha256(archive_bytes.getvalue()).hexdigest()
    if reference_zip_hash != validation['tested_zip_sha256']:
        raise RuntimeError('Rebuilt Windows reference ZIP differs from the published alpha.4 ZIP.')
    audit = build(dotnet, snapshot / 'tools/ReleaseAudit/ReleaseAudit.csproj', work, 'runtime-audit', log)
    report = work / 'runtime-equivalence.json'
    run([dotnet, audit / 'ReleaseAudit.dll', reference_dll, candidate / 'DiceMaster.dll',
         '--self-test', report], log)
    comparison = json.loads(report.read_text())
    if (comparison['status'] != 'pass' or comparison['candidate'] != manifest['dll'] or
            comparison['alpha4'] != record(reference_dll)):
        raise RuntimeError('Runtime equivalence does not identify the user-tested and new DLLs.')
    comparison['user_tested_source_commit'] = reference_commit
    comparison['reference_method'] = 'Fresh source-commit build verified against the user-tested published alpha.4 DLL SHA-256.'
    comparison['new_dll_direct_windows_run'] = False
    comparison['configuration'] = {'comparison': 'byte-identical to the user-tested alpha.4 ZIP',
                                   **record(reference_config)}
    comparison['reproduced_user_tested_zip_sha256'] = reference_zip_hash
    return comparison


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', required=True, type=Path)
    parser.add_argument('--output-dir', type=Path, default=ARTIFACTS / 'release-candidate',
                        help='Absent or empty directory under artifacts; existing releases are never overwritten.')
    parser.add_argument('--source-commit', help='Optional full commit SHA; otherwise verify the current working source.')
    parser.add_argument('--version', help='Optional package version, e.g. v1.0.3-alpha.5; creates no tag or Release.')
    args = parser.parse_args()
    try:
        output = artifact_directory(args.output_dir, '--output-dir', args.references)
    except ValueError as error:
        parser.error(str(error))
    if output.exists() and any(output.iterdir()):
        parser.error('Release verification output must be absent or empty; existing artifacts are preserved.')
    if args.version and not re.fullmatch(r'v\d+\.\d+\.\d+(?:-[A-Za-z0-9]+(?:[.-][A-Za-z0-9]+)*)?', args.version):
        parser.error('--version must be a version such as v1.0.3-alpha.5.')
    baseline = get_baseline(REPOSITORY)
    references = args.references.resolve()
    inputs = build_inputs() if args.source_commit is None else None
    package_name = 'DiceMaster-' + (args.version or 'release-candidate') + '.zip'
    with tempfile.TemporaryDirectory(prefix='dicemaster-release-verify-') as directory:
        work = Path(directory)
        snapshot = work / 'source'
        if args.source_commit:
            selected = committed_snapshot(args.source_commit, snapshot)
            origin = {'kind': 'git-commit', 'commit': selected}
        else:
            snapshot_source(snapshot)
            origin = {'kind': 'working-source', 'commit': None,
                      'note': 'Includes current local source changes; this is not a committed-source release.'}
        if any((snapshot / name).exists() for name in ('config', 'plugins', '.git', 'artifacts')):
            raise RuntimeError('A baseline distribution, Git database or previous output leaked into the snapshot.')
        allowed_dlls = {'src/DiceMaster/Resources/native/MinHook.x64.dll',
                        'src/DiceMaster/Resources/native/MinHook.x86.dll'}
        forbidden = [p.relative_to(snapshot).as_posix() for p in snapshot.rglob('*')
                     if p.is_file() and p.suffix.lower() == '.dll' and
                     p.relative_to(snapshot).as_posix() not in allowed_dlls]
        if forbidden:
            raise RuntimeError('Preexisting managed or unapproved DLL in release source: ' + str(forbidden))
        source_hashes = {p.relative_to(snapshot).as_posix(): digest(p)
                         for p in snapshot.rglob('*') if p.is_file()}
        log = work / 'verification.log'
        try:
            run([sys.executable, snapshot / 'build/build-source.py', '--references', references], log)
            run([sys.executable, snapshot / 'build/verify-source.py', '--references', references,
                 '--baseline', baseline], log)
        except BaseException:
            output.mkdir(parents=True, exist_ok=True)
            if log.exists():
                shutil.copyfile(log, output / 'verification.failed.log')
            raise
        candidate = snapshot / 'artifacts/source-candidate'
        manifest = json.loads((candidate / 'build-manifest.json').read_text())
        comparison = json.loads((candidate / 'compatibility.json').read_text())
        if (manifest['status'] != 'local-source-candidate' or comparison['status'] != 'pass' or
                record(candidate / 'DiceMaster.dll') != manifest['dll'] or comparison['candidate'] != manifest['dll']):
            raise RuntimeError('Release build and compatibility receipts do not identify the same successful DLL.')
        runtime_equivalence = verify_windows_reference(snapshot, candidate, manifest, references, work, log)
        if manifest['build_input_sha256'] != {name: source_hashes[name] for name in manifest['build_input_sha256']}:
            raise RuntimeError('Built inputs differ from the selected source snapshot.')
        if inputs is not None and (build_inputs() != inputs or manifest['build_input_sha256'] != inputs):
            raise RuntimeError('Working source changed during release verification; no new release bundle published.')
        archive = candidate / 'DiceMaster-source-candidate.zip'
        if record(archive) != manifest['zip']:
            raise RuntimeError('Release ZIP changed after build verification.')
        config = snapshot / 'packaging/config/DiceMaster.cfg'
        with zipfile.ZipFile(archive) as package:
            if package.namelist() != ['config/DiceMaster.cfg', 'plugins/DiceMaster/DiceMaster.dll'] or package.testzip():
                raise RuntimeError('Release installation ZIP file list or CRC failed.')
            if (package.read('config/DiceMaster.cfg') != config.read_bytes() or
                    package.read('plugins/DiceMaster/DiceMaster.dll') != (candidate / 'DiceMaster.dll').read_bytes()):
                raise RuntimeError('Release installation ZIP differs from the selected config or fresh DLL.')
        if record(baseline)['sha256'] != BASELINE_SHA256 or comparison['baseline'] != record(baseline):
            raise RuntimeError('Git baseline changed during release verification.')
        stage = work / 'verified'
        stage.mkdir()
        for name in ('DiceMaster.dll', 'build-manifest.json', 'build.log', 'compatibility.json', 'compatibility.log'):
            shutil.copyfile(candidate / name, stage / name)
        shutil.copyfile(archive, stage / package_name)
        shutil.copyfile(log, stage / 'verification.log')
        if runtime_equivalence is not None:
            write_json(stage / 'runtime-equivalence.json', runtime_equivalence)
        receipt = {
            'status': 'local-release-verified',
            'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'source': origin, 'build_input_sha256': manifest['build_input_sha256'],
            'source_snapshot_preexisting_dicemaster_dll_count': 0,
            'source_snapshot_excludes': ['config/', 'plugins/', '.git/', 'artifacts/', 'external game files'],
            'baseline': {'commit': BASELINE_COMMIT, 'git_path': 'plugins/DiceMaster/DiceMaster.dll',
                         **record(baseline), 'used_as_normal_build_input': False},
            'dll': manifest['dll'], 'zip': manifest['zip'], 'package_name': package_name,
            'config': manifest['config'], 'sdk': manifest['sdk'], 'references': manifest['references'],
            'tests': manifest['tests'], 'compatibility_checks': comparison['checks'],
            'zip_file_list_and_crc': 'pass', 'published': False,
            'windows_validation': manifest['windows_validation'],
            'runtime_equivalence': runtime_equivalence,
            'default_build_switched': manifest['default_build_switched'],
        }
        write_json(stage / 'release-verification.json', receipt)
        # Refuse a directory populated while the long-running checks were in flight.
        output.parent.mkdir(parents=True, exist_ok=True)
        if output.exists():
            if any(output.iterdir()):
                raise RuntimeError('Release output became nonempty during verification; no files replaced.')
            output.rmdir()
        shutil.move(str(stage), str(output))
    print('LOCAL RELEASE VERIFICATION PASS', output / 'release-verification.json')
    print('DLL SHA256', receipt['dll']['sha256'])
    print('ZIP SHA256', receipt['zip']['sha256'])


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('LOCAL RELEASE VERIFICATION FAILED: ' + str(error))
