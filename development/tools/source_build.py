"""Shared, DLL-independent source build and validation helpers (Python stdlib)."""
from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import xml.etree.ElementTree as ET

DEVELOPMENT = Path(__file__).resolve().parents[1]
REPOSITORY = DEVELOPMENT.parent
BASELINE_COMMIT = 'f8d8dd8610f20ccc323d8c3e0386bc5b98bcaeec'
BASELINE_SHA256 = '485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def record(path):
    path = Path(path)
    return {'bytes': path.stat().st_size, 'sha256': digest(path)}


def build_inputs():
    roots = [DEVELOPMENT / name for name in ('src', 'tests', 'tools')]
    paths = [path for root in roots for path in root.rglob('*')
             if path.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(path.parts)]
    paths.extend([DEVELOPMENT / 'Directory.Build.props', DEVELOPMENT / 'reference-contract.json',
                  REPOSITORY / 'config/DiceMaster.cfg'])
    return {str(path.relative_to(REPOSITORY)): digest(path) for path in sorted(paths)}


def run(command, log=None):
    print('+', ' '.join(str(item) for item in command), flush=True)
    result = subprocess.run([str(item) for item in command], text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
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
    contract = json.loads((DEVELOPMENT / 'reference-contract.json').read_text())
    for relative, expected in contract['files'].items():
        file = root / relative
        if not file.is_file():
            raise RuntimeError('Missing read-only external reference: ' + str(file))
        actual = record(file)
        if actual != expected:
            raise RuntimeError('Wrong reference version/hash: ' + str(file) +
                               '\nexpected ' + str(expected) + '\nactual ' + str(actual))
    project = ET.parse(DEVELOPMENT / 'src/DiceMaster/DiceMaster.csproj')
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
    run([dotnet, 'build', DEVELOPMENT / project, '-c', 'Release', '-o', output,
         '--nologo', '--disable-build-servers', '-p:UseSharedCompilation=false',
         '-p:NuGetAudit=false',
         '-p:BaseIntermediateOutputPath=' + str(Path(work) / 'obj' / name) + os.sep,
         *extra], log)
    return output


def behavior_tests(dotnet, candidate, reference_root, work, log):
    """Execute the actual candidate parser/adapter IL with existing game boundaries."""
    outputs = {}
    for name in ('LogRefreshFixtures', 'LogRefreshTests', 'LiveLogEngineTests',
                 'LifecycleTests', 'LiveLogIntegrationFixtures', 'LiveLogIntegrationTests',
                 'RealGameApiFixtures', 'RealGameApiTests', 'GamepadTests',
                 'SourceBehaviorFixtures', 'SourceBehaviorTests'):
        outputs[name] = build(dotnet, 'tests/' + name + '/' + name + '.csproj',
                              work, name, log)
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
