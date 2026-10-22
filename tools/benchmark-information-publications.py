"""Same publication probe against current source or a read-only Git archive.

Measures managed fixture scheduling/allocations, not Unity/Windows frame times.
"""
import argparse
import json
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline')
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    repository = Path(__file__).resolve().parent.parent
    args.output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='dicemaster-publication-probe-') as folder:
        folder = Path(folder)
        root = repository
        if args.baseline:
            root = folder / 'baseline'
            root.mkdir()
            archive = subprocess.run(['git', '-C', str(repository), 'archive', args.baseline,
                                      'src', 'tests', 'Directory.Build.props', 'global.json'], check=True, stdout=subprocess.PIPE).stdout
            subprocess.run(['tar', '-xf', '-', '-C', str(root)], input=archive, check=True)
        project = root / 'tests/InformationRuntimeTests/InformationRuntimeTests.csproj'
        tree = ET.parse(project)
        xml = tree.getroot()
        properties = xml.find('PropertyGroup')
        ET.SubElement(properties, 'StartupObject').text = 'PublicationProbe'
        ET.SubElement(properties, 'EnableDefaultCompileItems').text = 'false'
        group = xml.find('ItemGroup')
        for item in xml.findall('.//Compile'):
            item.set('Include', str((project.parent / item.get('Include')).resolve()))
        for source in ('Boundary.cs', 'Program.cs'):
            ET.SubElement(group, 'Compile', {'Include': str(project.parent / source)})
        ET.SubElement(group, 'Compile', {'Include': str(repository / 'tools/Benchmarks/InformationPublications.cs')})
        target = folder / 'Probe.csproj'
        tree.write(target)
        build = subprocess.run([args.dotnet, 'build', str(target), '-c', 'Release', '-o', str(folder / 'bin'),
                                '--nologo', '--disable-build-servers', '-p:NuGetAudit=false'],
                               text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        (args.output / 'build.log').write_text(build.stdout)
        build.check_returncode()
        rounds = []
        for _ in range(5):
            result = subprocess.run([args.dotnet, str(folder / 'bin/Probe.dll')], check=True, text=True, stdout=subprocess.PIPE)
            rounds.append(json.loads(result.stdout.strip().splitlines()[-1]))
        report = {'source': args.baseline or 'working-tree', 'boundary': 'Host .NET8 fixtures; no Unity/Windows FPS', 'rounds': rounds}
        (args.output / 'measurements.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps(report))


if __name__ == '__main__':
    main()
