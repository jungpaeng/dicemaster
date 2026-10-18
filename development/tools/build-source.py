"""Compile and test the complete mod from managed source; never read an existing mod DLL."""
from pathlib import Path
import argparse
import datetime
import json
import os
import shutil
import tempfile
import zipfile
from source_build import (DEVELOPMENT, REPOSITORY, BASELINE_COMMIT, BASELINE_SHA256,
                          sdk, references, build, behavior_tests, build_inputs, digest, record, write_json)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', required=True, type=Path, help='Read-only root containing Mortal_Data/Managed and BepInEx/core.')
    parser.add_argument('--output-dir', type=Path, default=DEVELOPMENT / 'releases/source-candidate')
    args = parser.parse_args()
    output = args.output_dir.resolve()
    # This candidate tool has no path that replaces a deployed plugin/default build.
    releases = (DEVELOPMENT / 'releases').resolve()
    if output == releases or not output.is_relative_to(releases):
        parser.error('--output-dir must be a child directory inside development/releases (candidate outputs only).')
    reference_root = args.references.resolve()
    if output.is_relative_to(reference_root) or reference_root.is_relative_to(output):
        parser.error('Candidate output and read-only references must be separate.')
    if output.exists() and any(output.iterdir()):
        marker = output / 'build-manifest.json'
        try:
            previous_manifest = json.loads(marker.read_text())
            if (previous_manifest['status'] != 'local-source-candidate' or
                    record(output / 'DiceMaster.dll') != previous_manifest['dll']):
                raise ValueError('not a verified prior source-candidate directory')
        except (OSError, ValueError, KeyError) as error:
            parser.error('Refusing to replace existing files in an unmanaged output directory: ' + str(error))
        owned = {'DiceMaster.dll', 'DiceMaster-source-candidate.zip', 'build-manifest.json', 'build.log',
                 'compatibility.json', 'compatibility.log', 'clean-build.json', 'clean-build.log',
                 'failure-paths.json'}
        unexpected = [path.name for path in output.iterdir()
                      if path.name not in owned and not (path.name.startswith('failure-') and path.suffix == '.log')]
        if unexpected:
            parser.error('Refusing to remove unrecognized candidate-directory files: ' + ', '.join(unexpected))
    dotnet, version = sdk()
    inputs = references(reference_root)
    input_hashes = build_inputs()
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='dicemaster-source-') as directory:
        work = Path(directory)
        log = work / 'build.log'
        compiled = build(dotnet, 'src/DiceMaster/DiceMaster.csproj', work, 'mod', log,
                         ['-p:GameReferences=' + str(reference_root)])
        candidate = compiled / 'DiceMaster.dll'
        if not candidate.is_file():
            raise RuntimeError('Compiler did not produce DiceMaster.dll; no package generated.')
        unexpected = {path.name for path in compiled.glob('*.dll')} - {'DiceMaster.dll'}
        if unexpected:
            raise RuntimeError('External DLLs leaked into mod output: ' + ', '.join(sorted(unexpected)))
        results = behavior_tests(dotnet, candidate, reference_root, work, log)
        stage = work / 'candidate'
        stage.mkdir()
        shutil.copyfile(candidate, stage / 'DiceMaster.dll')
        shutil.copyfile(log, stage / 'build.log')
        archive = stage / 'DiceMaster-source-candidate.zip'
        config = REPOSITORY / 'config/DiceMaster.cfg'
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as package:
            for name, source in [('config/DiceMaster.cfg', config), ('plugins/DiceMaster/DiceMaster.dll', candidate)]:
                entry = zipfile.ZipInfo(name, (2026, 10, 18, 0, 0, 0))
                entry.compress_type = zipfile.ZIP_DEFLATED
                entry.external_attr = 0o100644 << 16
                package.writestr(entry, source.read_bytes())
        with zipfile.ZipFile(archive) as package:
            if package.namelist() != ['config/DiceMaster.cfg', 'plugins/DiceMaster/DiceMaster.dll'] or package.testzip():
                raise RuntimeError('Installation ZIP layout/CRC failed.')
            if package.read('plugins/DiceMaster/DiceMaster.dll') != candidate.read_bytes() or package.read('config/DiceMaster.cfg') != config.read_bytes():
                raise RuntimeError('Installation ZIP bytes differ from fresh outputs.')
        # Refuse a build whose read-only inputs changed while compilation/tests ran.
        references(reference_root)
        if build_inputs() != input_hashes:
            raise RuntimeError('Source, resource, configuration, test or build input changed during the build; no candidate published.')
        source_files = sorted(set((DEVELOPMENT / 'src/DiceMaster').rglob('*')) |
                              set((DEVELOPMENT / 'src/DiceMaster.Gamepad').rglob('*')) |
                              set((DEVELOPMENT / 'src/DiceMaster.LiveLog').rglob('*')))
        source_hashes = {str(path.relative_to(REPOSITORY)): digest(path) for path in source_files
                         if path.is_file() and not {'bin', 'obj'}.intersection(path.parts)}
        manifest = {
            'status': 'local-source-candidate', 'default_build_switched': False,
            'windows_validation': 'not authorized / not performed',
            'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
            'recovery_baseline': {'commit': BASELINE_COMMIT, 'sha256': BASELINE_SHA256,
                                  'used_as_normal_build_input': False},
            'sdk': version, 'references': inputs, 'source_sha256': source_hashes,
            'build_input_sha256': input_hashes,
            'dll': record(candidate), 'zip': record(archive), 'config': record(config),
            'tests': results, 'compatibility_comparison': 'separate verify-source.py required',
            'nuget_audit': 'disabled for repeatable offline builds; no vulnerability audit claimed',
        }
        write_json(stage / 'build-manifest.json', manifest)
        # Publish only a fully successful fresh generation. A failed run leaves previous
        # candidates intact and exits nonzero; it never presents them as new results.
        pending = output.with_name(output.name + '.pending')
        previous = output.with_name(output.name + '.previous')
        if pending.exists() or previous.exists():
            raise RuntimeError('Interrupted candidate promotion; inspect .pending/.previous before retrying.')
        shutil.copytree(stage, pending)
        try:
            if output.exists():
                output.rename(previous)
            pending.rename(output)
        except BaseException:
            if previous.exists() and not output.exists():
                previous.rename(output)
            raise
        if previous.exists():
            try:
                shutil.rmtree(previous)
            except OSError as error:
                print('Candidate published; prior generated backup cleanup needs inspection:', previous, error)
        print('SOURCE CANDIDATE', output)
        print('DLL SHA256', manifest['dll']['sha256'])
        print('ZIP SHA256', manifest['zip']['sha256'])


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        raise SystemExit('SOURCE BUILD FAILED (no new candidate published): ' + str(error))
