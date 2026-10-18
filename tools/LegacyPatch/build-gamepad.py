"""Build and test the embedded gamepad feature using the .NET 8 SDK."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import sys
import tempfile

root = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(root / 'build'))
from baseline import get_baseline, protect_baseline_output

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path,
                        help='Input DLL; default: verified alpha.3 DLL recreated from local Git.')
    parser.add_argument('--output', type=Path,
                        default=root / 'artifacts/legacy-candidate/gamepad/DiceMaster.dll')
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
    input_bytes = args.input.read_bytes()
    with tempfile.TemporaryDirectory(prefix='dicemaster-gamepad-') as directory:
        work = Path(directory)
        def build(project, folder):
            out = work/folder
            subprocess.run([dotnet, 'build', str(root/project), '-c', 'Release', '-o', str(out), '--nologo',
                            '-p:UseArtifactsOutput=false',
                            '-p:BaseIntermediateOutputPath='+str(work/'obj'/folder)+os.sep], check=True)
            return out
        payload = build('tools/LegacyPatch/Payloads/DiceMaster.Gamepad.csproj', 'payload')
        injector = build('tools/LegacyPatch/GamepadInjector/GamepadInjector.csproj', 'injector')
        tests = build('tests/GamepadTests/GamepadTests.csproj', 'tests')
        original = work/'input.dll'
        original.write_bytes(input_bytes)
        output = work/'DiceMaster.dll'
        subprocess.run([dotnet, str(injector/'GamepadInjector.dll'), str(original),
                        str(payload/'DiceMaster.GamepadPayload.dll'), str(root/'src/DiceMaster/Resources/native'),
                        str(output)], check=True)
        subprocess.run([dotnet, str(tests/'GamepadTests.dll'), str(payload/'DiceMaster.GamepadPayload.dll'),
                        str(output)], check=True)
        # Failed builds/tests leave any existing candidate untouched.
        if args.output.resolve() == args.input.resolve() and args.input.read_bytes() != input_bytes:
            raise RuntimeError('The input DLL changed while building; refusing to overwrite it.')
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_bytes(output.read_bytes())
        print('Updated', args.output)

if __name__ == '__main__':
    main()
