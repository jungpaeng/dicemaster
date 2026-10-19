"""Shared, DLL-independent source build and validation helpers (Python stdlib)."""
from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import xml.etree.ElementTree as ET
from baseline import BASELINE_COMMIT, BASELINE_SHA256

BUILD = Path(__file__).resolve().parent
REPOSITORY = BUILD.parent
ARTIFACTS = REPOSITORY / 'artifacts'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def record(path):
    path = Path(path)
    return {'bytes': path.stat().st_size, 'sha256': digest(path)}


def release_policy():
    """Describe the selected build path and the exact user-tested artifact."""
    policy = json.loads((BUILD / 'release-policy.json').read_text())
    if policy.get('schema_version') != 1 or policy.get('default_build') != 'source':
        raise RuntimeError('Unsupported source release policy.')
    validation = policy.get('windows_validation', {})
    if validation.get('status') != 'user-confirmed' or not validation.get('release_equivalence_required'):
        raise RuntimeError('Source release policy must identify the user-confirmed Windows reference.')
    return policy


def build_inputs():
    roots = [REPOSITORY / name for name in ('src', 'tests', 'tools', 'build', 'packaging')]
    paths = [path for root in roots for path in root.rglob('*')
             if path.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(path.parts)]
    paths.extend(path for path in root_build_files() if path.is_file())
    return {str(path.relative_to(REPOSITORY)): digest(path) for path in sorted(paths)}


def root_build_files():
    return [REPOSITORY / name for name in
            ('DiceMaster.sln', 'global.json', 'Directory.Build.props', 'Directory.Build.targets')]


def artifact_directory(path, option, reference_root=None):
    """Keep candidate publications/reports away from installed and SDK outputs."""
    output = Path(path).resolve()
    artifacts = ARTIFACTS.resolve()
    if output == artifacts or not output.is_relative_to(artifacts):
        raise ValueError(option + ' must be a child directory inside artifacts (candidate outputs only).')
    for name in ('bin', 'obj'):
        reserved = artifacts / name
        if output == reserved or output.is_relative_to(reserved):
            raise ValueError(option + ' must not use the reserved artifacts/' + name + ' SDK output directory.')
    if output.exists() and not output.is_dir():
        raise ValueError(option + ' must name a directory, not an existing file.')
    if reference_root is not None:
        reference_root = Path(reference_root).resolve()
        if output.is_relative_to(reference_root) or reference_root.is_relative_to(output):
            raise ValueError('Candidate output/reports and read-only references must be separate.')
    return output


def snapshot_source(destination):
    """Copy only reproducible source inputs; external/game/mod DLLs stay outside."""
    destination = Path(destination)
    destination.mkdir(parents=True, exist_ok=True)
    for name in ('src', 'tests', 'tools', 'build', 'packaging'):
        shutil.copytree(REPOSITORY / name, destination / name,
                        ignore=shutil.ignore_patterns('bin', 'obj', '__pycache__', '.DS_Store'))
    for path in root_build_files():
        if path.is_file():
            shutil.copyfile(path, destination / path.name)
    # MinHook is reviewed mod-owned native code embedded by the project. No
    # previously compiled managed mod/game/reference DLL belongs in this tree.
    allowed = {'src/DiceMaster/Resources/native/MinHook.x64.dll',
               'src/DiceMaster/Resources/native/MinHook.x86.dll'}
    forbidden = [path.relative_to(destination).as_posix() for path in destination.rglob('*')
                 if path.is_file() and path.suffix.lower() == '.dll' and
                 path.relative_to(destination).as_posix() not in allowed]
    if forbidden:
        raise RuntimeError('Preexisting managed or unapproved DLL leaked into source snapshot: ' + str(forbidden))
    return destination


def run(command, log=None):
    print('+', ' '.join(str(item) for item in command), flush=True)
    result = subprocess.run([str(item) for item in command], text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=REPOSITORY)
    print(result.stdout, end='', flush=True)
    if log is not None:
        with Path(log).open('a') as stream:
            stream.write('+ ' + ' '.join(str(item) for item in command) + '\n' + result.stdout)
    result.check_returncode()
    return result.stdout


def sdk():
    executable = shutil.which('dotnet')
    if executable is None:
        raise RuntimeError('The .NET 8 SDK is required; put dotnet on PATH.')
    version = run([executable, '--version']).strip()
    if not version.startswith('8.'):
        raise RuntimeError('Use .NET SDK 8.x (validated: 8.0.426); selected ' + version)
    return executable, version


def references(root):
    root = Path(root).resolve()
    contract = json.loads((BUILD / 'reference-contract.json').read_text())
    for relative, expected in contract['files'].items():
        file = root / relative
        if not file.is_file():
            raise RuntimeError('Missing read-only external reference: ' + str(file))
        actual = record(file)
        if actual != expected:
            raise RuntimeError('Wrong reference version/hash: ' + str(file) +
                               '\nexpected ' + str(expected) + '\nactual ' + str(actual))
    project = ET.parse(REPOSITORY / 'src/DiceMaster/DiceMaster.csproj')
    declared = set()
    for reference in project.findall('.//Reference'):
        hint = reference.findtext('HintPath', '').replace('\\', '/')
        if not hint.startswith('$(GameReferences)/'):
            raise RuntimeError('Unapproved project reference: ' + hint)
        relative = hint.removeprefix('$(GameReferences)/')
        declared.add(relative)
        if relative not in contract['files'] or reference.findtext('Private', '').lower() != 'false':
            raise RuntimeError('Reference must be pinned and Private=false: ' + hint)
    if declared != set(contract['files']):
        raise RuntimeError('Project references differ from the pinned reference contract.')
    return {'root': str(root), 'files': contract['files']}


def build(dotnet, project, work, name, log, extra=()):
    output = Path(work) / name
    run([dotnet, 'build', REPOSITORY / project, '-c', 'Release', '-o', output,
         '--nologo', '--disable-build-servers', '-p:UseSharedCompilation=false',
         '-p:NuGetAudit=false', '-p:UseArtifactsOutput=false',
         '-p:BaseIntermediateOutputPath=' + str(Path(work) / 'obj' / name) + os.sep,
         *extra], log)
    return output


def behavior_tests(dotnet, candidate, reference_root, work, log):
    """Execute the actual candidate parser/adapter IL with existing game boundaries."""
    outputs = {}
    for name in ('LogRefreshFixtures', 'LogRefreshTests', 'LiveLogEngineTests',
                 'LifecycleTests', 'LiveLogIntegrationFixtures', 'LiveLogIntegrationTests',
                 'RealGameApiFixtures', 'RealGameApiTests', 'GamepadTests',
                 'SourceBehaviorFixtures', 'SourceBehaviorTests',
                 'PreviewIntegrationFixtures', 'PreviewIntegrationTests',
                 'LogUiModelTests', 'LogUiWiringTests', 'LogUiKeyboardTests', 'LogOpenerIntegrationTests',
                 'EventSelectionRuntimeTests', 'EventPopupTests'):
        outputs[name] = build(dotnet, 'tests/' + name + '/' + name + '.csproj',
                              work, name, log,
                              ['-p:GameReferences=' + str(reference_root)] if name == 'EventSelectionRuntimeTests' else ())
    results = {}

    def test(name, *args):
        output = run([dotnet, outputs[name] / (name + '.dll'), *args], log)
        results[name] = [line for line in output.splitlines() if line.startswith(('PASS:', 'METADATA CONTRACT:', 'Lifecycle assertions passed:'))]

    test('LogRefreshTests', candidate, outputs['LogRefreshFixtures'] / 'LogRefreshFixtures.dll')
    test('LiveLogEngineTests')
    test('LifecycleTests')
    test('LiveLogIntegrationTests', candidate,
         outputs['LiveLogIntegrationFixtures'] / 'LiveLogIntegrationFixtures.dll')
    test('RealGameApiTests', Path(reference_root) / 'Mortal_Data/Managed',
         outputs['RealGameApiFixtures'] / 'RealGameApiFixtures.dll', Path(work) / 'actual-il', candidate)
    test('GamepadTests', candidate)
    test('SourceBehaviorTests', candidate, outputs['SourceBehaviorFixtures'] / 'SourceBehaviorFixtures.dll',
         Path(work) / 'source-behavior')
    test('PreviewIntegrationTests', candidate,
         outputs['PreviewIntegrationFixtures'] / 'PreviewIntegrationFixtures.dll')
    test('LogUiModelTests')
    test('LogUiWiringTests', candidate)
    test('LogUiKeyboardTests')
    test('LogOpenerIntegrationTests')
    test('EventSelectionRuntimeTests')
    test('EventPopupTests')
    return results


def compatibility(dotnet, baseline, candidate, work, log):
    if digest(baseline) != BASELINE_SHA256:
        raise RuntimeError('Compatibility baseline must be the fixed v1.0.3-alpha.3 DLL: ' + BASELINE_SHA256)
    output = build(dotnet, 'tests/SourceBuildContractTests/SourceBuildContractTests.csproj',
                   work, 'source-contract', log)
    result = run([dotnet, output / 'SourceBuildContractTests.dll', '--self-test', baseline, candidate], log)
    fixtures = build(dotnet, 'tests/SourceBehaviorFixtures/SourceBehaviorFixtures.csproj',
                     work, 'behavior-fixtures', log)
    tests = build(dotnet, 'tests/SourceBehaviorTests/SourceBehaviorTests.csproj',
                  work, 'behavior-tests', log)
    behavior = run([dotnet, tests / 'SourceBehaviorTests.dll', candidate,
                    fixtures / 'SourceBehaviorFixtures.dll', Path(work) / 'behavior-compare', baseline], log)
    return [line for line in (result + behavior).splitlines()
            if line.startswith(('PASS:', 'CONTRACT:', 'ACCEPTED SOURCE REPRESENTATION:'))]


def write_json(path, data):
    Path(path).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')
