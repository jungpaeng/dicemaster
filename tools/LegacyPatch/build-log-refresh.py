"""Patch and verify live log display using the .NET 8 SDK."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile


def main():
    root = Path(__file__).resolve().parents[2]
    mod_root = root
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, default=mod_root / 'plugins/DiceMaster/DiceMaster.dll')
    parser.add_argument('--output', type=Path, default=mod_root / 'plugins/DiceMaster/DiceMaster.dll')
    parser.add_argument('--game-managed', type=Path, help='Read-only game Managed folder for actual-game IL and hook contract checks.')
    args = parser.parse_args()
    dotnet = shutil.which('dotnet')
    if dotnet is None:
        parser.error('The .NET 8 SDK is required. Install it before running this script.')
    input_bytes = args.input.read_bytes()
    with tempfile.TemporaryDirectory(prefix='dicemaster-log-refresh-') as directory:
        work = Path(directory)

        def build(project, name):
            output = work / name
            subprocess.run([
                dotnet, 'build', str(root / project), '-c', 'Release', '-o', str(output), '--nologo',
                '--disable-build-servers', '-p:UseSharedCompilation=false',
                '-p:UseArtifactsOutput=false',
                '-p:BaseIntermediateOutputPath=' + str(work / 'obj' / name) + os.sep,
            ], check=True)
            return output

        injector = build('tools/LegacyPatch/LogRefreshInjector/LogRefreshInjector.csproj', 'injector')
        payload = build('tools/LegacyPatch/Payloads/DiceMaster.LiveLog.csproj', 'payload')
        fixtures = build('tests/LogRefreshFixtures/LogRefreshFixtures.csproj', 'fixtures')
        tests = build('tests/LogRefreshTests/LogRefreshTests.csproj', 'tests')
        engine_tests = build('tests/LiveLogEngineTests/LiveLogEngineTests.csproj', 'engine-tests')
        integration_fixtures = build('tests/LiveLogIntegrationFixtures/LiveLogIntegrationFixtures.csproj', 'integration-fixtures')
        integration_tests = build('tests/LiveLogIntegrationTests/LiveLogIntegrationTests.csproj', 'integration-tests')
        artifact_tests = build('tests/LiveLogArtifactTests/LiveLogArtifactTests.csproj', 'artifact-tests')
        lifecycle_tests = build('tests/LifecycleTests/LifecycleTests.csproj', 'lifecycle-tests')
        if args.game_managed is not None:
            real_fixtures = build('tests/RealGameApiFixtures/RealGameApiFixtures.csproj', 'real-fixtures')
            real_tests = build('tests/RealGameApiTests/RealGameApiTests.csproj', 'real-tests')
        original = work / 'input.dll'
        original.write_bytes(input_bytes)
        output = work / 'DiceMaster.dll'
        subprocess.run([dotnet, str(injector / 'LogRefreshInjector.dll'), str(original),
                        str(payload / 'DiceMaster.LiveLogPayload.dll'), str(output)], check=True)
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
        def verify_real(candidate, name):
            if args.game_managed is not None:
                subprocess.run([
                    dotnet, str(real_tests / 'RealGameApiTests.dll'), str(args.game_managed),
                    str(real_fixtures / 'RealGameApiFixtures.dll'), str(work / name), str(candidate),
                ], check=True)
        verify_real(output, 'real-first')
        repeated = work / 'DiceMaster.repeated.dll'
        subprocess.run([dotnet, str(injector / 'LogRefreshInjector.dll'), str(output),
                        str(payload / 'DiceMaster.LiveLogPayload.dll'), str(repeated)], check=True)
        subprocess.run([
            dotnet, str(integration_tests / 'LiveLogIntegrationTests.dll'), str(repeated),
            str(integration_fixtures / 'LiveLogIntegrationFixtures.dll'),
        ], check=True)
        subprocess.run([
            dotnet, str(artifact_tests / 'LiveLogArtifactTests.dll'), str(output), str(repeated),
        ], check=True)
        verify_real(repeated, 'real-repeated')
        print('Repeated log patch verified.')
        if args.output.resolve() == args.input.resolve() and args.input.read_bytes() != input_bytes:
            raise RuntimeError('The input DLL changed while building; refusing to overwrite it.')
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_bytes(output.read_bytes())
        print('Updated', args.output)


if __name__ == '__main__':
    main()
