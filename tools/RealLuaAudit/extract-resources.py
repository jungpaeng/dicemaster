"""Export private TextAssets and verified Resources keys without running game/Lua.

Requires UnityPy==1.25.4 in a separate environment. Outputs must be under the
repository's ignored artifacts directory. Never supplies dependency directories.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import UnityPy

REPOSITORY = Path(__file__).resolve().parents[2]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--resources', type=Path, required=True)
    parser.add_argument('--manager', type=Path, required=True)
    parser.add_argument('--resources-sha256', required=True)
    parser.add_argument('--manager-sha256', required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    output = args.output_dir.resolve()
    artifacts = (REPOSITORY / 'artifacts').resolve()
    if output == artifacts or not output.is_relative_to(artifacts) or output.exists():
        parser.error('--output-dir must be a new directory below artifacts')
    resources, manager = args.resources.resolve(), args.manager.resolve()
    for source in (resources, manager):
        if source.is_relative_to(output) or output.is_relative_to(source.parent):
            parser.error('outputs and read-only input directories must be separate')
    source_bytes, manager_bytes = resources.read_bytes(), manager.read_bytes()
    if digest(source_bytes) != args.resources_sha256.lower() or digest(manager_bytes) != args.manager_sha256.lower():
        parser.error('original input SHA-256 mismatch')
    # Byte input prevents automatic discovery/loading of other game files.
    resource_env, manager_env = UnityPy.load(source_bytes), UnityPy.load(manager_bytes)
    objects = {obj.path_id: obj for obj in resource_env.objects}
    managers = [obj for obj in manager_env.objects if obj.type.name == 'ResourceManager']
    if len(managers) != 1:
        raise ValueError('Expected exactly one ResourceManager')
    resource_manager = managers[0]
    container = resource_manager.read_typetree()['m_Container']
    asset = resource_manager.assets_file
    # Verify the container prefix independently of UnityPy's tree decoder.
    raw = resource_manager.get_raw_data()
    count = struct.unpack_from('<i', raw)[0]
    offset, decoded = 4, []
    if count != len(container):
        raise ValueError('ResourceManager count mismatch')
    for _ in range(count):
        size = struct.unpack_from('<i', raw, offset)[0]
        offset += 4
        if not 0 <= size <= len(raw) - offset:
            raise ValueError('Resource key length is invalid')
        key = raw[offset:offset + size].decode('utf-8', 'strict')
        offset = (offset + size + 3) & ~3
        file_id, path_id = struct.unpack_from('<iq', raw, offset)
        offset += 12
        decoded.append((key, {'m_FileID': file_id, 'm_PathID': path_id}))
    if decoded != [(key, ptr) for key, ptr in container]:
        raise ValueError('Raw ResourceManager map mismatch')
    output.mkdir(parents=True)
    (output / 'text').mkdir()
    entries, by_id = [], {}
    for path_id, obj in sorted(objects.items()):
        if obj.type.name != 'TextAsset':
            continue
        tree = obj.read_typetree()
        if set(tree) != {'m_Name', 'm_Script'}:
            raise ValueError('Unsupported TextAsset schema')
        body = tree['m_Script'].encode('utf-8', 'surrogateescape')
        text = body.decode('utf-8', 'strict')
        raw = obj.get_raw_data()
        name_size = struct.unpack_from('<i', raw)[0]
        if not 0 <= name_size <= len(raw) - 4:
            raise ValueError('TextAsset name length is invalid')
        if raw[4:4 + name_size].decode('utf-8', 'surrogateescape') != tree['m_Name']:
            raise ValueError('Raw TextAsset name mismatch')
        length_offset = (4 + name_size + 3) & ~3
        size = struct.unpack_from('<i', raw, length_offset)[0]
        first = length_offset + 4
        if size != len(body) or raw[first:first + size] != body or (first + size + 3) & ~3 != len(raw):
            raise ValueError('Raw TextAsset payload mismatch')
        path = 'text/id_' + str(path_id).replace('-', 'n') + '.txt'
        (output / path).write_bytes(body)
        if digest((output / path).read_bytes()) != digest(body):
            raise ValueError('Exported payload mismatch')
        markers = {key: text.count(key) for key in ('--index:', 'luamanager.', 'statmodifymanager.', 'checkpointmanager.', 'choose(')}
        entry = {'key': 'textasset_' + str(path_id).replace('-', 'n'), 'path_id': path_id,
                 'path': path, 'sha256': digest(body), 'bytes': len(body), 'valid_utf8': True,
                 'lua_candidate': any(markers.values()), 'resource_key': None,
                 'resource_key_verified': False, 'marker_counts': markers}
        entries.append(entry)
        by_id[path_id] = entry
    mapped, resolutions = [], []
    mapped_keys = set()
    for key, ptr in container:
        fid, pid = ptr['m_FileID'], ptr['m_PathID']
        external = asset.externals[fid - 1].path if 0 < fid <= len(asset.externals) else None
        resolved = external == resources.name and pid in objects
        record = {'resource_key': key, 'file_id': fid, 'external_path': external,
                  'path_id': pid, 'resolved': resolved, 'type': objects[pid].type.name if resolved else None}
        resolutions.append(record)
        if resolved and pid in by_id:
            if key in mapped_keys:
                raise ValueError('Ambiguous TextAsset resource key')
            mapped_keys.add(key)
            entry = dict(by_id[pid], key=key, resource_key=key, resource_key_verified=True)
            mapped.append(entry)
    story = [entry for entry in mapped if entry['key'].startswith('story/chinesetraditional/')]
    modules = [entry for entry in mapped if entry['key'].startswith('lua/')]
    write(output / 'index.json', entries)
    write(output / 'lua-candidate-index.json', [entry for entry in entries if entry['lua_candidate']])
    write(output / 'mapped-textasset-index.json', mapped)
    write(output / 'story-chinesetraditional-index.json', story)
    write(output / 'lua-module-index.json', modules)
    write(output / 'resource-path-map.json', resolutions)
    if resources.read_bytes() != source_bytes or manager.read_bytes() != manager_bytes:
        raise ValueError('Read-only source changed during extraction')
    report = {'tool': 'UnityPy', 'version': UnityPy.__version__, 'resources_sha256': digest(source_bytes),
              'manager_sha256': digest(manager_bytes), 'textassets': len(entries),
              'container_entries': len(container), 'resolved_objects': sum(r['resolved'] for r in resolutions),
              'mapped_textassets': len(mapped), 'story_chinesetraditional': len(story), 'lua_modules': len(modules),
              'unmapped_textasset_path_ids': sorted(set(by_id) - {entry['path_id'] for entry in mapped}),
              'raw_fields_independently_verified': True, 'source_files_unchanged': True,
              'game_or_lua_executed': False, 'raw_source_in_diagnostics': False}
    write(output / 'summary.json', report)
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
