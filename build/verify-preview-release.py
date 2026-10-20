"""Rebuild an explicitly selected preview commit and verify the reviewed DLL/config.

This prerelease-only path retains the original source-restoration verifier. The
default --scope prd2 keeps its original candidate contract. Select --scope prd4
explicitly for the reviewed log GUI/XInput candidate and its 12 test programs.
Select --scope prd5 for the current-action event popup and its 14 test programs.
Select --scope prd6 for read-only map/one-shot/branch/shop information including bounded map routes.
No scope claims historical runtime equivalence, feature completion or a
Windows run of the candidate.
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

from information_contract import (PRD6_VALIDATION, WINDOWS_VALIDATION as PRD6_WINDOWS_VALIDATION,
                                  RUNTIME_BOUNDARY as PRD6_RUNTIME_BOUNDARY,
                                  IMPLEMENTATION_BASELINE as PRD6_IMPLEMENTATION_BASELINE,
                                  REQUIRED_TESTS as PRD6_REQUIRED_TESTS)
from source_build import (ARTIFACTS, BUILD, artifact_directory, digest, record,
                          run, write_json)


PRD4_VALIDATION = {
    'scope': 'PRD 4 log GUI and XInput navigation local candidate',
    'windows_runtime': 'not-run; maintainer confirmation required',
    'real_lua_comparison': 'PRD 2 analysis unchanged; see existing evidence',
    'final_trace_limits': 'PRD 2 limits preserved',
    'historical_windows_confirmation_applies_to_this_candidate': False,
}
PRD4_RUNTIME_BOUNDARY = (
    'PRD 4 changes GUI and input behavior; previous Windows confirmation does not '
    'validate this candidate. Maintainer Windows verification is pending.')
PRD4_REQUIRED_TESTS = frozenset({
    'LogRefreshTests', 'LiveLogEngineTests', 'LifecycleTests',
    'LiveLogIntegrationTests', 'RealGameApiTests', 'GamepadTests',
    'SourceBehaviorTests', 'PreviewIntegrationTests', 'LogUiModelTests',
    'LogUiWiringTests', 'LogUiKeyboardTests', 'LogOpenerIntegrationTests',
})
PRD5_VALIDATION = {
    'scope': 'PRD 5 current-action event selection popup local candidate',
    'windows_runtime': 'not-run; maintainer confirmation required',
    'real_lua_comparison': 'PRD 2 analysis preserved; PRD 5 parsed literal Position coroutine gates tested separately',
    'final_trace_limits': 'PRD 2 limits preserved',
    'historical_windows_confirmation_applies_to_this_candidate': False,
}
PRD5_RUNTIME_BOUNDARY = (
    'PRD 5 replaces event weight reservations with current-action result selection. '
    'User-confirmed v1.0.5 is the starting baseline, not Windows validation of this candidate.')
PRD5_IMPLEMENTATION_BASELINE = {
    'release': 'v1.0.5',
    'source_commit': '8886309e2a7248fa13544f81546e3c4795ff9611',
    'working_head_at_start': '40813f9',
    'windows_confirmation': 'User confirmed v1.0.5 Windows verification in this chat on 2026-10-20 Asia/Seoul; no per-scenario or environment detail supplied.',
}
PRD5_WINDOWS_VALIDATION = {
    'status': 'pending',
    'new_release_dll_directly_windows_tested': False,
    'confirmation_source': None,
    'environment_note': 'PRD 5 candidate has not been run in Windows; v1.0.5 user confirmation is an implementation baseline only.',
}
PRD5_REQUIRED_TESTS = PRD4_REQUIRED_TESTS | frozenset({
    'EventSelectionRuntimeTests', 'EventPopupTests',
})


def _exact_contract(actual, expected):
    # bool is a subclass of int in Python; ordinary dict equality would accept
    # 0 as False. Evidence boundaries require exact JSON value types as well.
    if type(actual) is not type(expected):
        return False
    if isinstance(expected, dict):
        return actual.keys() == expected.keys() and all(
            _exact_contract(actual[key], expected[key]) for key in expected)
    if isinstance(expected, list):
        return len(actual) == len(expected) and all(
            _exact_contract(left, right) for left, right in zip(actual, expected))
    return actual == expected


def _valid_prd6_test_evidence(name, evidence):
    """Recognize the actual suite's success records without accepting metadata alone."""
    if not isinstance(evidence, list) or not evidence:
        return False
    succeeded = False
    for line in evidence:
        if not isinstance(line, str):
            return False
        text = line.strip()
        if text.startswith('PASS:') and text[5:].strip():
            succeeded = True
        elif name == 'RealGameApiTests' and text.startswith('METADATA CONTRACT:') and text[18:].strip():
            # This supplementary record is not proof the behavior suite passed.
            continue
        elif name == 'LifecycleTests' and re.fullmatch(r'Lifecycle assertions passed: [1-9][0-9]*', text):
            succeeded = True
        else:
            return False
    return succeeded


def validate_preview_candidate(manifest, scope):
    """Reject a mismatched scope or evidence that overstates a preview build."""
    validation = manifest.get('candidate_validation')
    if not isinstance(validation, dict):
        raise RuntimeError('Preview candidate validation boundaries are missing.')
    if scope == 'prd2':
        if (validation.get('windows_runtime') != 'not-run'
                or validation.get('real_lua_comparison') != 'not-run'
                or validation.get('historical_windows_confirmation_applies_to_this_candidate') is not False):
            raise RuntimeError('PRD 2 preview validation boundaries must remain explicit.')
    elif scope == 'prd4':
        if (validation != PRD4_VALIDATION
                or validation.get('historical_windows_confirmation_applies_to_this_candidate') is not False
                or manifest.get('runtime_equivalence') != PRD4_RUNTIME_BOUNDARY
                or manifest.get('windows_validation', {}).get('new_release_dll_directly_windows_tested') is not False):
            raise RuntimeError('PRD 4 preview must preserve its exact scope and pending Windows boundaries.')
        tests = manifest.get('tests')
        if not isinstance(tests, dict) or not PRD4_REQUIRED_TESTS.issubset(tests):
            raise RuntimeError('PRD 4 preview requires all 12 candidate test programs.')
        for name in PRD4_REQUIRED_TESTS:
            evidence = tests[name]
            if (not isinstance(evidence, list) or not evidence
                    or any(not isinstance(line, str) or not line.strip() for line in evidence)):
                raise RuntimeError('PRD 4 preview test evidence is empty or malformed: ' + name)
    elif scope == 'prd5':
        windows_validation = manifest.get('windows_validation')
        if (validation != PRD5_VALIDATION
                or validation.get('historical_windows_confirmation_applies_to_this_candidate') is not False
                or manifest.get('runtime_equivalence') != PRD5_RUNTIME_BOUNDARY
                or manifest.get('implementation_baseline') != PRD5_IMPLEMENTATION_BASELINE
                or windows_validation != PRD5_WINDOWS_VALIDATION
                or windows_validation.get('new_release_dll_directly_windows_tested') is not False):
            raise RuntimeError('PRD 5 preview must preserve its exact scope, baseline and pending Windows boundaries.')
        tests = manifest.get('tests')
        if not isinstance(tests, dict) or not PRD5_REQUIRED_TESTS.issubset(tests):
            raise RuntimeError('PRD 5 preview requires all 14 candidate test programs.')
        for name in PRD5_REQUIRED_TESTS:
            evidence = tests[name]
            if (not isinstance(evidence, list) or not evidence
                    or any(not isinstance(line, str) or not line.strip() for line in evidence)):
                raise RuntimeError('PRD 5 preview test evidence is empty or malformed: ' + name)
    elif scope == 'prd6':
        for key, expected in (
                ('candidate_validation', PRD6_VALIDATION),
                ('windows_validation', PRD6_WINDOWS_VALIDATION),
                ('implementation_baseline', PRD6_IMPLEMENTATION_BASELINE),
                ('runtime_equivalence', PRD6_RUNTIME_BOUNDARY)):
            if not _exact_contract(manifest.get(key), expected):
                raise RuntimeError('PRD 6 preview must preserve its exact scope, user-confirmed v1.0.6 baseline and pending new-candidate Windows boundaries: ' + key)
        for claim in ('windows_runtime_executed', 'historical_full_runtime_equivalence_claimed',
                      'feature_or_issue_completion_claimed'):
            if manifest.get(claim) is not False:
                raise RuntimeError('PRD 6 preview requires explicit false for runtime/completion claims: ' + claim)
        tests = manifest.get('tests')
        if not isinstance(tests, dict) or not PRD6_REQUIRED_TESTS.issubset(tests):
            raise RuntimeError('PRD 6 preview requires all 18 candidate test programs.')
        for name in PRD6_REQUIRED_TESTS:
            evidence = tests[name]
            if not _valid_prd6_test_evidence(name, evidence):
                raise RuntimeError('PRD 6 preview test evidence is empty, malformed or lacks a recognized suite-success record: ' + name)
    else:
        raise RuntimeError('Unsupported preview candidate scope: ' + str(scope))
    for claim in ('historical_full_runtime_equivalence_claimed', 'feature_or_issue_completion_claimed'):
        if claim in manifest and manifest[claim] is not False:
            raise RuntimeError('Preview must not claim runtime equivalence or feature/issue completion.')
    return validation


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scope', choices=('prd2', 'prd4', 'prd5', 'prd6'), default='prd2',
                        help='Candidate validation contract (default: prd2). PRD 4, PRD 5 and PRD 6 require explicit selection.')
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
            validation = validate_preview_candidate(manifest, args.scope)
            stage = work / 'verified'
            stage.mkdir()
            for name in ('DiceMaster.dll', 'build-manifest.json', 'build.log'):
                shutil.copyfile(candidate / name, stage / name)
            package_name = 'DiceMaster-' + args.version + '.zip'
            shutil.copyfile(archive, stage / package_name)
            shutil.copyfile(log, stage / 'verification.log')
            scope_boundaries = {}
            if args.scope in ('prd5', 'prd6'):
                scope_boundaries = {name: manifest[name] for name in (
                    'runtime_equivalence', 'implementation_baseline', 'windows_validation')}
                historical = 'historical_source_restoration_windows_validation'
                if historical in manifest:
                    scope_boundaries[historical] = manifest[historical]
            write_json(stage / 'release-verification.json', {
                'status': 'prerelease-commit-build-verified', 'published': False,
                'created_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'source_commit': commit, 'version': args.version, 'preview_scope': args.scope,
                'preexisting_managed_dll_count': 0,
                'build_input_sha256': manifest['build_input_sha256'],
                'dll': manifest['dll'], 'zip': manifest['zip'], 'config': manifest['config'],
                'package_name': package_name, 'tests': manifest['tests'],
                'reviewed_dll_sha256': args.expected_dll_sha256,
                'reviewed_config_sha256': args.expected_config_sha256,
                'zip_file_list_and_crc': 'pass', 'candidate_validation': validation,
                'historical_full_runtime_equivalence_claimed': False,
                'feature_or_issue_completion_claimed': False,
                **({'windows_runtime_executed': False} if args.scope == 'prd6' else {}),
                **scope_boundaries,
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
