"""Independent lexical evidence for exact mapped root and followup effects."""
from collections import Counter
from pathlib import Path
import hashlib
import json
import re
import sys
import time
from independent_scan import scan, safe_option

index_path, report_path, output_path = map(Path,sys.argv[1:])
index=json.loads(index_path.read_text())
report_doc=json.loads(report_path.read_text())
actual=report_doc['analysis']
if not actual.get('exact_resource_map_enabled'):
    raise ValueError('Exact resource map mode required')
prefix='story/chinesetraditional/'
signatures={};expected_keys={};counts=Counter();problems=[]
started=time.perf_counter()
for entry in index:
    if not entry['resource_key_verified'] or entry['resource_key']!=entry['key'] or not entry['key'].startswith(prefix) or entry['key'] in signatures:
        raise ValueError('Index resource identity not verified or unique')
    data=(index_path.parent/entry['path']).read_bytes()
    if hashlib.sha256(data).hexdigest()!=entry['sha256']:
        raise ValueError('Source hash mismatch')
    tokens,markers,calls,choose_candidates,unbalanced=scan(data.decode('utf-8'))
    signatures[entry['key']]=Counter((c['owner'],c['name'],tuple(c['argument_hashes'])) for c in calls)
    expected_keys[entry['key']]={safe_option(m[0]) for m in markers}
    counts.update(files=1,source_tokens=len(tokens),index_markers=len(markers),unbalanced_delimiters=unbalanced)
for row in actual['option_results']:
    root=row['source']
    if root not in signatures:raise ValueError('Unexpected root resource')
    for effect in row['effects']:
        source=effect['source']
        sig=(effect['owner'],effect['name'],tuple(effect['argument_sha256']))
        counts['collected_effects']+=1
        counts['followup_effects']+=bool(effect['followup'])
        absent=source not in signatures or signatures[source][sig]==0
        wrong_followup_flag=(source!=root)!=bool(effect['followup'])
        counts['effects_without_original_source_call']+=absent
        counts['effects_with_wrong_root_followup_flag']+=wrong_followup_flag
        if absent or wrong_followup_flag:
            problems.append(dict(root=root,option=row['option'],source=source,function=effect['owner']+'.'+effect['name'],
                                 original_call_absent=absent,wrong_followup_flag=wrong_followup_flag))
files={f['source']:f for f in actual['files']}
missing_markers=[]
for source,keys in expected_keys.items():
    missing=keys-set(files[source].get('option_keys',[]))
    counts['index_keys_not_discovered']+=len(missing)
    if missing:missing_markers.append(dict(source=source,keys=sorted(missing)))
for source in actual['resolved_literal_target_reads']:
    if source not in signatures:raise ValueError('Resolved reader used unverified resource')
verified_absent=[];unassessed_absent=[]
for target,count in actual['unresolved_literal_target_reads'].items():
    if re.fullmatch(r'[A-Za-z0-9_]+',target):
        if prefix+target.lower() in signatures:raise ValueError('Reader labeled known exact resource absent')
        verified_absent.append(dict(target=target,reads=count))
    else:unassessed_absent.append(dict(target_hash=target,reads=count))
output=dict(status='limited-independent-exact-resource-lexical-comparison-complete',
            audited_dll_sha256=report_doc['released_dll_sha256'],counts=dict(counts),problems=problems,
            missing_index_keys=missing_markers,reader_resolved_distinct_resources=len(actual['resolved_literal_target_reads']),
            reader_verified_absent_identifier_targets=verified_absent,reader_hashed_absent_targets_unassessed=unassessed_absent,
            elapsed_seconds=time.perf_counter()-started,lua_or_game_executed=False,raw_source_or_arguments_in_report=False,
            proof_scope=['Input exact resource keys and source byte hashes verified.',
                         'Every recorded root/followup effect has an original call with matching owner/name/normalized-argument hashes in its exact source resource.',
                         'Followup flags agree with root-versus-other source identity.',
                         'Literal identifier targets labeled absent are absent from the supplied verified Traditional Chinese map.'],
            not_proven=['Exact option-effect ownership or all omitted effects.',
                        'Control-flow outcomes, branch truth, actual reachability or current-player estimates.',
                        'Real Unity resource loading, running game state, Windows behavior or final trace limits.'])
output_path.write_text(json.dumps(output,ensure_ascii=False,indent=2))
print(json.dumps({k:output[k] for k in ('status','counts','reader_resolved_distinct_resources','elapsed_seconds')}))
