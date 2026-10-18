"""Patch and verify live log display using the .NET 8 SDK."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile


def main():
    root = Path(__file__).resolve().parents[1]
    mod_root = root.parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, default=mod_root / 'plugins/DiceMaster/DiceMaster.dll')
    parser.add_argument('--output', type=Path, default=mod_root / 'plugins/DiceMaster/DiceMaster.dll')
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
                '-p:BaseIntermediateOutputPath=' + str(work / 'obj' / name) + os.sep,
            ], check=True)
            return output

        injector = build('tools/LogRefreshInjector/LogRefreshInjector.csproj', 'injector')
        fixtures = build('tests/LogRefreshFixtures/LogRefreshFixtures.csproj', 'fixtures')
        tests = build('tests/LogRefreshTests/LogRefreshTests.csproj', 'tests')
        original = work / 'input.dll'
        original.write_bytes(input_bytes)
        output = work / 'DiceMaster.dll'
        subprocess.run([dotnet, str(injector / 'LogRefreshInjector.dll'), str(original), str(output)], check=True)
        subprocess.run([
            dotnet, str(tests / 'LogRefreshTests.dll'), str(output), str(fixtures / 'LogRefreshFixtures.dll'),
        ], check=True)
        if args.output.resolve() == args.input.resolve() and args.input.read_bytes() != input_bytes:
            raise RuntimeError('The input DLL changed while building; refusing to overwrite it.')
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_bytes(output.read_bytes())
        print('Updated', args.output)


if __name__ == '__main__':
    main()
