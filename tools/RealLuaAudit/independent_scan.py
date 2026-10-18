"""Limited independent lexer comparison. Reads raw Lua but never executes it.

This scanner does not import production parser code. It proves only explicitly
listed lexical invariants, not Lua control flow, path mapping or game outcomes.
No source text, arguments or description strings are written to the report.
"""
from collections import Counter
from pathlib import Path
import argparse
import hashlib
import json
import re
import time

TOKEN = re.compile(
    r'(?P<space>\s+)|(?P<longcomment>--\[(?P<ceq>=*)\[.*?\](?P=ceq)\])|'
    r'(?P<comment>--[^\r\n]*)|(?P<quoted>"(?:\\[\s\S]|[^"\\])*"|\'(?:\\[\s\S]|[^\'\\])*\')|'
    r'(?P<longstring>\[(?P<seq>=*)\[.*?\](?P=seq)\])|'
    r'(?P<identifier>[^\W\d]\w*|_\w*)|(?P<number>[0-9][A-Za-z0-9.]*)|'
    r'(?P<operator>==|~=|<=|>=|\.\.)|(?P<other>.)', re.DOTALL | re.UNICODE)
MARKER = re.compile(r'--index:(O_\w+)')
OPTION = re.compile(r'O_\w+\Z')


def sha(value):
    return hashlib.sha256(value.encode('utf-8')).hexdigest()


def safe_option(value):
    return value if re.fullmatch(r'[A-Za-z0-9_]+', value) else 'sha256:' + sha(value)


def scan(source):
    tokens, markers = [], []
    for match in TOKEN.finditer(source):
        kind = match.lastgroup
        # lastgroup may be the inner named equal-sign group; distinguish by
        # inspecting which complete token alternative actually matched.
        if match.group('space') is not None or match.group('longcomment') is not None:
            continue
        if match.group('comment') is not None:
            before = source[source.rfind('\n', 0, match.start()) + 1:match.start()]
            marker = MARKER.match(match.group()) if before.strip() == '' else None
            if marker:
                markers.append((marker.group(1), match.start(), match.end()))
            continue
        is_string = match.group('quoted') is not None or match.group('longstring') is not None
        tokens.append((match.group(), match.start(), match.end(), is_string))
    pairs, stack, unbalanced = {}, [], 0
    for index, (text, _, _, string) in enumerate(tokens):
        if string:
            continue
        if text in ('(', '[', '{'):
            stack.append((text, index))
        elif text in (')', ']', '}'):
            if not stack or stack[-1][0] != {')':'(', ']':'[', '}':'{'}[text]:
                unbalanced += 1
            else:
                _, opening = stack.pop()
                pairs[opening] = index
    unbalanced += len(stack)
    calls = []
    direct_option_assignments = []
    for i, token in enumerate(tokens):
        if token[3] or not re.fullmatch(r'[^\W\d]\w*|_\w*', token[0]):
            continue
        if i and tokens[i-1][0] in ('.', ':'):
            continue
        owner, name, opening = '', token[0], i + 1
        if i + 3 < len(tokens) and tokens[i+1][0] in ('.', ':'):
            owner, name, opening = token[0], tokens[i+2][0], i + 3
        if opening >= len(tokens) or tokens[opening][0] != '(' or opening not in pairs:
            continue
        closing = pairs[opening]
        spans, first, j = [], opening + 1, opening + 1
        while j < closing:
            if j in pairs:
                j = pairs[j] + 1
                continue
            if tokens[j][0] == ',' and not tokens[j][3]:
                spans.append((first, j))
                first = j + 1
            j += 1
        if first < closing or spans:
            spans.append((first, closing))
        argument_hashes = [sha(' '.join(t[0] for t in tokens[a:b])) for a, b in spans]
        calls.append({'owner':owner, 'name':name, 'argument_hashes':argument_hashes,
                      'position':token[1], 'opening':opening, 'closing':closing, 'spans':spans})
    # A second discovery oracle: direct table[index]="O_key" assignments
    # whose table is later supplied to an unqualified choose call. This is a
    # deliberately wider candidate set than proven branch-bound option keys.
    for i in range(len(tokens) - 5):
        t = tokens[i:i+6]
        if t[1][0] == '[' and t[3][0] == ']' and t[4][0] == '=' and t[5][3]:
            raw = t[5][0]
            if len(raw) >= 2 and raw[0] == raw[-1] and raw[0] in ('"', "'") and OPTION.fullmatch(raw[1:-1]):
                direct_option_assignments.append((t[0][0], raw[1:-1], t[0][1]))
    choose_candidates = set()
    for call in calls:
        if call['owner'] or call['name'] != 'choose' or not call['spans']:
            continue
        first, last = call['spans'][0]
        if last - first != 1:
            continue
        table = tokens[first][0]
        choose_candidates.update(key for var, key, position in direct_option_assignments if var == table and position < call['position'])
    return tokens, markers, calls, choose_candidates, unbalanced


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('index', type=Path)
    parser.add_argument('released_report', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    index = json.loads(args.index.read_text())
    released = json.loads(args.released_report.read_text())['analysis']
    by_file = {f['source']:f for f in released['files']}
    rows = {}
    for row in released['option_results']:
        rows.setdefault(row['source'], []).append(row)
    counts = Counter()
    functions = Counter()
    records = []
    started = time.perf_counter()
    for entry in index:
        body = (args.index.parent / entry['path']).read_bytes().decode('utf-8')
        token_list, markers, calls, choose_candidates, unbalanced = scan(body)
        marker_keys = {safe_option(m[0]) for m in markers}
        actual_keys = set(by_file[entry['key']].get('option_keys', []))
        lexical_candidates = marker_keys | {safe_option(k) for k in choose_candidates}
        marker_missing = sorted(marker_keys - actual_keys)
        unproven_discovered = sorted(actual_keys - lexical_candidates)
        signatures = Counter((c['owner'], c['name'], tuple(c['argument_hashes'])) for c in calls)
        absent = []
        observed_effects = 0
        for row in rows.get(entry['key'], []):
            for effect in row['effects']:
                observed_effects += 1
                signature = (effect['owner'], effect['name'], tuple(effect['argument_sha256']))
                if effect['source'] != entry['key'] or effect['followup']:
                    raise RuntimeError('ROOT-ONLY result contains a followed or foreign-source effect')
                if signatures[signature] == 0:
                    absent.append({'option':row['option'], 'function':effect['owner']+'.'+effect['name'], 'argument_count':effect['argument_count']})
        for call in calls:
            if call['owner'] in ('statmodifymanager', 'luamanager'):
                functions[call['owner'] + '.' + call['name']] += 1
        counts.update(files=1, tokens=len(token_list), index_markers=len(markers), distinct_marker_keys=len(marker_keys),
                      choose_candidate_keys=len(choose_candidates), released_option_keys=len(actual_keys),
                      marker_keys_not_discovered=len(marker_missing), discovered_keys_without_lexical_evidence=len(unproven_discovered),
                      released_root_effects=observed_effects, released_effects_without_source_call=len(absent),
                      unbalanced_delimiters=unbalanced, choose_calls=sum(c['owner']=='' and c['name']=='choose' for c in calls))
        if marker_missing or unproven_discovered or absent or unbalanced:
            records.append({'source':entry['key'], 'sha256':entry['sha256'], 'incomplete_discovery':by_file[entry['key']].get('incomplete_option_discovery'),
                            'markers_not_discovered':marker_missing, 'unproven_discovered_keys':unproven_discovered,
                            'effects_without_lexical_call':absent, 'unbalanced_delimiters':unbalanced,
                            'source_characters':len(body), 'independent_tokens':len(token_list)})
    report = {'status':'limited-independent-lexical-comparison-complete', 'counts':dict(counts),
              'manager_call_frequency_all_candidate_source':dict(functions.most_common()), 'differences':records,
              'elapsed_seconds':time.perf_counter()-started,
              'proof_scope':['genuine short line comment index IDs compared against released option discovery',
                             'direct choose table assignments are a wider lexical candidate set, not path proof',
                             'every released root effect must have matching owner/name/normalized argument hashes somewhere in its original TextAsset',
                             'all released effects must remain in their root source with followup=false'],
              'not_proven':['exact option effect ownership or all omitted effects', 'control-flow outcomes or branch conditions',
                            'Story/ChineseTraditional resource identity', 'SetNextScript resource resolution', 'final trace limits', 'Windows/Unity runtime'],
              'lua_or_game_executed':False, 'raw_source_or_arguments_in_report':False}
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2))
    print(json.dumps({'status':report['status'], 'counts':report['counts'], 'elapsed_seconds':report['elapsed_seconds']}))


if __name__ == '__main__':
    main()
