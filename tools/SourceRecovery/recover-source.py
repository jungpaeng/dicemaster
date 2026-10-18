"""Recreate the reviewed alpha.3 source component from its pinned mod DLL.

This is an audit/recovery tool, never part of the ordinary source build. It
decompiles into a new temporary directory and never consumes old extractions.
"""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile


REPOSITORY = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPOSITORY / 'build'))
from baseline import BASELINE_COMMIT, BASELINE_SHA256, get_baseline

SOURCE = REPOSITORY / 'src/DiceMaster'
ILSPY_VERSION = '9.1.0.7988'
GIT_ATTRIBUTES = '''*.cs text eol=lf
*.csproj text eol=lf
recovery-manifest.json text eol=lf
.gitattributes text eol=lf
Resources/** -text
'''
NATIVE_RESOURCES = {
    'DiceMaster.Gamepad.Native.MinHook.x64.dll': 'MinHook.x64.dll',
    'DiceMaster.Gamepad.Native.MinHook.x86.dll': 'MinHook.x86.dll',
    'DiceMaster.Gamepad.Native.LICENSE.txt': 'MinHook.LICENSE.txt',
}
MOD_TRANSLATION_SHA256 = '6fc71ac1780be28b78e5ee193835a38e47d1526cbb9a143d05ff5206eceef1ec'
TRANSLATION_HEADINGS = {
    1: '// 활협전 내장 UI 번역',
    38: '// DiceMaster 플래그 설명',
    89: '// 플래그 참고문 번역',
}


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def replace_once(text, old, new, label):
    if text.count(old) != 1:
        raise RuntimeError('Recovery transform did not match exactly once: ' + label)
    return text.replace(old, new)


def clean_translation_headings(data):
    """Replace three legacy comment headings, preserving all translation bytes."""
    if hashlib.sha256(data).hexdigest() != MOD_TRANSLATION_SHA256:
        raise RuntimeError('Translation headings require the pinned resource bytes.')
    lines = data.splitlines(keepends=True)
    for number, heading in TRANSLATION_HEADINGS.items():
        if not lines[number - 1].startswith(b'//') or not lines[number - 1].endswith(b'\r\n'):
            raise RuntimeError('Unexpected translation comment at line ' + str(number))
        lines[number - 1] = heading.encode('utf-8') + b'\r\n'
    return b''.join(lines)


def recover(baseline, references, ilspycmd, output):
    if digest(baseline) != BASELINE_SHA256:
        raise RuntimeError('Recovery input is not the pinned v1.0.3-alpha.3 DLL.')
    version = subprocess.run([str(ilspycmd), '--version'], check=True,
                             text=True, capture_output=True).stdout
    if ('ilspycmd: ' + ILSPY_VERSION + '\n') not in version.replace('\r', ''):
        raise RuntimeError('Recovery requires ilspycmd ' + ILSPY_VERSION + ': ' + version)
    if output.exists() and any(output.iterdir()):
        raise RuntimeError('Recovery output must be absent or empty; no files are removed.')
    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='dicemaster-source-recovery-') as folder:
        temporary = Path(folder)
        reference_path = temporary / 'references'
        reference_path.mkdir()
        # Only disposable external-reference copies enter this temporary folder.
        # None are copied into the recovered source tree or build distribution.
        for subfolder in ['Mortal_Data/Managed', 'BepInEx/core']:
            directory = references / subfolder
            if not directory.is_dir():
                raise RuntimeError('Missing external reference directory: ' + str(directory))
            for dll in sorted(directory.glob('*.dll')):
                target = reference_path / dll.name
                if target.exists() and digest(target) != digest(dll):
                    raise RuntimeError('Conflicting external assembly name: ' + dll.name)
                if not target.exists():
                    shutil.copyfile(dll, target)
        # Stable filename prevents the caller's path or filename entering output.
        input_path = temporary / 'DiceMaster.dll'
        shutil.copyfile(baseline, input_path)
        raw = temporary / 'raw'
        subprocess.run([str(ilspycmd), '--disable-updatecheck', '-p',
                        '--nested-directories', '-r', str(reference_path),
                        '-o', str(raw), str(input_path)], check=True)
        for source in sorted(raw.rglob('*.cs')):
            relative = source.relative_to(raw)
            # Existing reviewed sources already contain every type in these two
            # namespaces (including GamepadBridge). Compiling copies duplicates
            # type identities and risks installing duplicate hook paths.
            if relative.parts[:2] in [('DiceMaster', 'Gamepad'), ('DiceMaster', 'LiveLog')]:
                continue
            text = source.read_text(encoding='utf-8')
            if relative.as_posix() == 'Properties/AssemblyInfo.cs':
                text = replace_once(text, '[module: RefSafetyRules(11)]\n', '',
                                    'compiler-generated RefSafetyRules module attribute')
            elif relative.as_posix() == 'DiceMaster/DataHelper.cs':
                text = replace_once(text, 'item2 != LiveMissionFilter',
                                    '(object)item2 != LiveMissionFilter',
                                    'explicit raw reference comparison')
            # Namespace directories are an ILSpy convention. The ordinary C#
            # project keeps its sources directly under the project root.
            if relative.parts[0] == 'DiceMaster':
                relative = Path(*relative.parts[1:])
                if relative.as_posix() in ['DicePatch.cs', 'OptionInfoPatch.cs', 'DebugPatch.cs']:
                    relative = Path('Patches') / relative
            target = output / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            # Normalize ILSpy's platform-specific text newlines to checked-in LF.
            target.write_bytes(text.encode('utf-8'))
        for resource in sorted(raw.glob('DiceMaster.*')):
            if resource.suffix == '.csproj' or not resource.is_file():
                continue
            if resource.name in NATIVE_RESOURCES:
                original = SOURCE / 'Resources/native' / NATIVE_RESOURCES[resource.name]
                if digest(resource) != digest(original):
                    raise RuntimeError('Existing native resource differs from baseline: ' + resource.name)
                destination = output / 'Resources/native' / NATIVE_RESOURCES[resource.name]
            else:
                destination = output / 'Resources' / resource.name
            destination.parent.mkdir(parents=True, exist_ok=True)
            if resource.name == 'DiceMaster.Translations.ModTranslation.txt':
                destination.write_bytes(clean_translation_headings(resource.read_bytes()))
            else:
                shutil.copyfile(resource, destination)
        # Authored Gamepad/LiveLog sources are the canonical definitions. Copy
        # them into a complete standalone source tree rather than re-decompiling
        # those namespaces or linking files from sibling production projects.
        for namespace in ['Gamepad', 'LiveLog']:
            for source in sorted((SOURCE / namespace).glob('*.cs')):
                destination = output / namespace / source.name
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(source.read_text(encoding='utf-8').encode('utf-8'))
        template = REPOSITORY / 'tools/SourceRecovery/DiceMaster.csproj.template'
        (output / 'DiceMaster.csproj').write_bytes(template.read_text(encoding='utf-8').encode('utf-8'))
        (output / '.gitattributes').write_bytes(GIT_ATTRIBUTES.encode('utf-8'))
        manifest = {
            'baseline_commit': BASELINE_COMMIT,
            'baseline_sha256': BASELINE_SHA256,
            'ilspycmd_version': ILSPY_VERSION,
            'transforms': [
                'Exclude decompiled Gamepad and LiveLog namespaces; copy canonical reviewed sources into the same project once.',
                'Flatten the decompiled DiceMaster namespace directory; group DicePatch, OptionInfoPatch and DebugPatch under Patches.',
                'Let the C# 12 compiler regenerate RefSafetyRules(11) and embedded support attributes.',
                'Make GetMissions raw object reference inequality explicit (same baseline IL semantics).',
                'Retain decompiler-escaped legacy compiler identifiers; do not rewrite compiled IL.',
                'Use authored net48 project template; extract byte-identical MinHook resources under Resources/native.',
                'Replace three legacy external-plugin comment headings in ModTranslation (lines 1, 38 and 89); preserve every translation and CRLF byte.',
            ],
            'source_text_newlines': 'LF (linked managed source hashes normalize CRLF to LF)',
            'recovered_files': {p.relative_to(output).as_posix(): digest(p)
                                for p in sorted(output.rglob('*')) if p.is_file()},
            'linked_managed_sources': {
                p.relative_to(REPOSITORY).as_posix(): hashlib.sha256(p.read_text(encoding='utf-8').encode('utf-8')).hexdigest()
                for namespace in ['Gamepad', 'LiveLog']
                for p in sorted((SOURCE / namespace).glob('*.cs'))
            },
            'linked_native_resources': {
                name: digest(SOURCE / 'Resources/native' / filename)
                for name, filename in NATIVE_RESOURCES.items()
            },
        }
        (output / 'recovery-manifest.json').write_bytes(
            (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', type=Path,
                        help='Explicit alpha.3 DLL (SHA-256 checked); default: recreate from fixed local Git commit.')
    parser.add_argument('--references', type=Path, required=True,
                        help='Read-only root containing Mortal_Data/Managed and BepInEx/core.')
    parser.add_argument('--ilspycmd', type=Path, default=shutil.which('ilspycmd'),
                        help='Pinned ilspycmd 9.1.0.7988 executable.')
    destination = parser.add_mutually_exclusive_group(required=True)
    destination.add_argument('--output', type=Path, help='Absent or empty recovered component directory.')
    destination.add_argument('--check', action='store_true', help='Compare fresh recovery with checked-in source component.')
    args = parser.parse_args()
    if args.ilspycmd is None:
        parser.error('Install ilspycmd 9.1.0.7988 or pass --ilspycmd.')
    baseline = args.baseline.resolve() if args.baseline is not None else get_baseline()
    if args.check:
        with tempfile.TemporaryDirectory(prefix='dicemaster-source-recovery-check-') as folder:
            generated = Path(folder) / 'DiceMaster'
            recover(baseline, args.references.resolve(), args.ilspycmd.resolve(), generated)
            actual = SOURCE
            expected_files = {p.relative_to(generated) for p in generated.rglob('*') if p.is_file()}
            actual_files = {p.relative_to(actual) for p in actual.rglob('*') if p.is_file()
                            and not {'bin', 'obj'}.intersection(p.relative_to(actual).parts)}
            changes = sorted(str(p) for p in expected_files ^ actual_files)
            changes.extend(str(p) for p in sorted(expected_files & actual_files)
                           if (generated / p).read_bytes() != (actual / p).read_bytes())
            if changes:
                raise RuntimeError('Fresh recovery differs: ' + ', '.join(changes))
        print('Fresh pinned source/resource recovery matches checked-in component.')
    else:
        recover(baseline, args.references.resolve(), args.ilspycmd.resolve(), args.output.resolve())
        print('Recovered pinned source component:', args.output.resolve())


if __name__ == '__main__':
    main()
