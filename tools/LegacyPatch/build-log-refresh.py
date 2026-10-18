"""Patch and verify live log display using the .NET 8 SDK."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'build'))
from baseline import get_baseline, protect_baseline_output
from source_build import references


def main():
    root = ROOT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path,
                        help='Input DLL; default: verified alpha.3 DLL recreated from local Git.')
    parser.add_argument('--output', type=Path,
                        default=root / 'artifacts/legacy-candidate/live-log/DiceMaster.dll')
    parser.add_argument('--game-managed', type=Path, required=True, help='Read-only game Managed folder; PRD 2 compiles the pinned full-source payload and checks actual-game contracts.')
    args = parser.parse_args()
    try:
        protect_baseline_output(args.output, root)
    except RuntimeError as error:
        parser.error(str(error))
    dotnet = shutil.which('dotnet')
    if dotnet is None:
        parser.error('The .NET 8 SDK is required. Install it before running this script.')
    if args.input is None:
        args.input = get_baseline(root)
    reference_root = args.game_managed.resolve().parent.parent
    references(reference_root)
    input_bytes = args.input.read_bytes()
    with tempfile.TemporaryDirectory(prefix='dicemaster-log-refresh-') as directory:
        work = Path(directory)

        def build(project, name, extra=()):
            output = work / name
            subprocess.run([
                dotnet, 'build', str(root / project), '-c', 'Release', '-o', str(output), '--nologo',
                '--disable-build-servers', '-p:UseSharedCompilation=false',
                '-p:UseArtifactsOutput=false',
                '-p:NuGetAudit=false',
                '-p:BaseIntermediateOutputPath=' + str(work / 'obj' / name) + os.sep,
                *extra,
            ], check=True)
            return output

        injector = build('tools/LegacyPatch/LogRefreshInjector/LogRefreshInjector.csproj', 'injector')
        payload = build('src/DiceMaster/DiceMaster.csproj', 'payload', ['-p:GameReferences=' + str(reference_root)])
        fixtures = build('tests/LogRefreshFixtures/LogRefreshFixtures.csproj', 'fixtures')
        tests = build('tests/LogRefreshTests/LogRefreshTests.csproj', 'tests')
        engine_tests = build('tests/LiveLogEngineTests/LiveLogEngineTests.csproj', 'engine-tests')
        integration_fixtures = build('tests/LiveLogIntegrationFixtures/LiveLogIntegrationFixtures.csproj', 'integration-fixtures')
        integration_tests = build('tests/LiveLogIntegrationTests/LiveLogIntegrationTests.csproj', 'integration-tests')
        preview_fixtures = build('tests/PreviewIntegrationFixtures/PreviewIntegrationFixtures.csproj', 'preview-fixtures')
        preview_tests = build('tests/PreviewIntegrationTests/PreviewIntegrationTests.csproj', 'preview-tests')
        artifact_tests = build('tests/LiveLogArtifactTests/LiveLogArtifactTests.csproj', 'artifact-tests')
        lifecycle_tests = build('tests/LifecycleTests/LifecycleTests.csproj', 'lifecycle-tests')
        if args.game_managed is not None:
            real_fixtures = build('tests/RealGameApiFixtures/RealGameApiFixtures.csproj', 'real-fixtures')
            real_tests = build('tests/RealGameApiTests/RealGameApiTests.csproj', 'real-tests')
        original = work / 'input.dll'
        original.write_bytes(input_bytes)
        output = work / 'DiceMaster.dll'
        subprocess.run([dotnet, str(injector / 'LogRefreshInjector.dll'), str(original),
                        str(payload / 'DiceMaster.dll'), str(output)], check=True)
        subprocess.run([
            dotnet, str(tests / 'LogRefreshTests.dll'), str(output), str(fixtures / 'LogRefreshFixtures.dll'),
        ], check=True)
        subprocess.run([dotnet, str(engine_tests / 'LiveLogEngineTests.dll')], check=True)
        subprocess.run([dotnet, str(lifecycle_tests / 'LifecycleTests.dll')], check=True)
        subprocess.run([
            dotnet, str(integration_tests / 'LiveLogIntegrationTests.dll'), str(output),
            str(integration_fixtures / 'LiveLogIntegrationFixtures.dll'),
        ], check=True)
        subprocess.run([
            dotnet, str(artifact_tests / 'LiveLogArtifactTests.dll'), str(original), str(output),
        ], check=True)
        subprocess.run([dotnet, str(preview_tests / 'PreviewIntegrationTests.dll'), str(output),
                        str(preview_fixtures / 'PreviewIntegrationFixtures.dll')], check=True)
        def verify_real(candidate, name):
            if args.game_managed is not None:
                subprocess.run([
                    dotnet, str(real_tests / 'RealGameApiTests.dll'), str(args.game_managed),
                    str(real_fixtures / 'RealGameApiFixtures.dll'), str(work / name), str(candidate),
                ], check=True)
        verify_real(output, 'real-first')
        repeated = work / 'DiceMaster.repeated.dll'
        subprocess.run([dotnet, str(injector / 'LogRefreshInjector.dll'), str(output),
                        str(payload / 'DiceMaster.dll'), str(repeated)], check=True)
        subprocess.run([
            dotnet, str(integration_tests / 'LiveLogIntegrationTests.dll'), str(repeated),
            str(integration_fixtures / 'LiveLogIntegrationFixtures.dll'),
        ], check=True)
        subprocess.run([
            dotnet, str(artifact_tests / 'LiveLogArtifactTests.dll'), str(output), str(repeated),
        ], check=True)
        subprocess.run([dotnet, str(preview_tests / 'PreviewIntegrationTests.dll'), str(repeated),
                        str(preview_fixtures / 'PreviewIntegrationFixtures.dll')], check=True)
        verify_real(repeated, 'real-repeated')
        print('Repeated log patch verified.')
        if args.output.resolve() == args.input.resolve() and args.input.read_bytes() != input_bytes:
            raise RuntimeError('The input DLL changed while building; refusing to overwrite it.')
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_bytes(output.read_bytes())
        print('Updated', args.output)


if __name__ == '__main__':
    main()
