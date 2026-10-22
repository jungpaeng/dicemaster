"""Compare cold coverage and warm light-marker queries using managed fixtures.

Run with the .NET 8 SDK on PATH:
    python3 tools/benchmark-guidance-markers.py
    python3 tools/benchmark-guidance-markers.py --baseline c850006

This does not execute Unity, Lua, Windows input or a game frame.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', help='Read-only Git ref to compare with the current checkout')
    parser.add_argument('--dotnet', default='dotnet', help='.NET 8 SDK executable')
    args = parser.parse_args()
    repository = Path(__file__).resolve().parent.parent
    output = repository / 'artifacts/issue-6/guidance-badges'
    output.mkdir(parents=True, exist_ok=True)
    environment = os.environ.copy()
    with tempfile.TemporaryDirectory(prefix='dicemaster-guidance-benchmark-') as temporary:
        temporary = Path(temporary)
        root = repository
        if args.baseline:
            root = temporary / 'baseline'
            root.mkdir()
            archive = subprocess.run(
                ['git', '-C', str(repository), 'archive', args.baseline,
                 'src', 'tests', 'Directory.Build.props', 'global.json'],
                check=True, stdout=subprocess.PIPE).stdout
            subprocess.run(['tar', '-xf', '-', '-C', str(root)], input=archive, check=True)
        project = root / 'tests/MapGuideRouteIntegrationTests/MapGuideRouteIntegrationTests.csproj'
        tree = ET.parse(project)
        xml = tree.getroot()
        properties = xml.find('PropertyGroup')
        ET.SubElement(properties, 'StartupObject').text = 'GuideProbe'
        ET.SubElement(properties, 'EnableDefaultCompileItems').text = 'false'
        symbols = []
        if 'BeginScheduledReads' in (root / 'src/DiceMaster/Information/MapGuideSource.cs').read_text():
            symbols.append('SCHEDULED_SCOPE')
        if (root / 'src/DiceMaster/Information/MapDefinitionAssets.cs').is_file():
            symbols.append('GUIDANCE_NATIVE_DEFINITIONS')
        ET.SubElement(properties, 'DefineConstants').text = '$(DefineConstants);' + ';'.join(symbols)
        for item in xml.findall('.//Compile'):
            item.set('Include', str((project.parent / item.get('Include')).resolve()))
        ET.SubElement(xml.find('ItemGroup'), 'Compile', {
            'Include': str(repository / 'tools/Benchmarks/GuidanceMarkers.cs')})
        ET.indent(tree)
        target = temporary / 'Probe.csproj'
        tree.write(target)
        build = subprocess.run([
            args.dotnet, 'build', str(target), '-c', 'Release', '-o', str(temporary / 'bin'),
            '--nologo', '--disable-build-servers', '-p:UseSharedCompilation=false', '-p:NuGetAudit=false'],
            env=environment, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
        # Ref syntax may contain slashes; use a fixed output prefix rather than
        # treating a Git ref as a path. The exact ref stays in the report.
        label = 'baseline' if args.baseline else 'final'
        (output / (label + '-benchmark-build.log')).write_text(build.stdout)
        build.check_returncode()
        report = {'baseline': args.baseline, 'measurements': {}}
        for mode in ('unselected', 'selected'):
            result = subprocess.run([args.dotnet, str(temporary / 'bin/Probe.dll'), mode],
                env=environment, check=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
            report['measurements'][mode] = json.loads(result.stdout.strip().splitlines()[-1])
        (output / (label + '-benchmark.json')).write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps(report))


if __name__ == '__main__':
    main()
