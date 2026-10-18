"""Compare metadata-only actual-IL corpus audit reports."""
from collections import Counter
from pathlib import Path
import json
import sys

baseline_path, candidate_path, output_path = map(Path, sys.argv[1:])
baseline_doc, candidate_doc = (json.loads(p.read_text()) for p in (baseline_path,candidate_path))
baseline, candidate = (d['analysis'] for d in (baseline_doc,candidate_doc))
def by_option(report):
    return {(r['source'],r['option']):r for r in report['option_results']}
def effects(row):
    return Counter(json.dumps(e,sort_keys=True) for e in row['effects'])
CALL_IDENTITY_FIELDS = ('source','owner','name','argument_sha256','followup')
def calls(row, followup=None):
    # Preserve multiplicity. Display/validation/branch metadata and ordering are
    # intentionally absent; argument material in this report is already hashed.
    return Counter(json.dumps({k:e[k] for k in CALL_IDENTITY_FIELDS},sort_keys=True)
                   for e in row['effects'] if followup is None or e['followup']==followup)
def call_deltas(b,c):
    result = {}
    for label,followup in (('',None),('root_',False),('followup_',True)):
        before,after = calls(b,followup),calls(c,followup)
        result[label+'call_identity_records_added'] = sum((after-before).values())
        result[label+'call_identity_records_removed'] = sum((before-after).values())
    return result
def support(row):
    return row.get('static_support') or row.get('root_static_support')
brows, crows = map(by_option,(baseline,candidate))
bkeys, ckeys = map(set,(brows,crows))
changed = []
aggregate = Counter()
call_aggregate = Counter({k:0 for k in (
    'call_identity_records_added','call_identity_records_removed',
    'root_call_identity_records_added','root_call_identity_records_removed',
    'followup_call_identity_records_added','followup_call_identity_records_removed',
    'options_with_added_call_identities','options_with_removed_call_identities',
    'options_with_added_root_call_identities','options_with_removed_root_call_identities',
    'options_with_added_followup_call_identities','options_with_removed_followup_call_identities')})
for key in sorted(bkeys & ckeys):
    b,c = brows[key],crows[key]
    bfx,cfx = effects(b),effects(c)
    added,removed = sum((cfx-bfx).values()),sum((bfx-cfx).values())
    call_changes = call_deltas(b,c)
    call_aggregate.update(call_changes)
    for origin in ('','root_','followup_'):
        for direction in ('added','removed'):
            if call_changes[origin+'call_identity_records_'+direction]:
                call_aggregate['options_with_'+direction+'_'+origin+'call_identities'] += 1
    bissues = Counter(i['function']+'|'+i['reason'] for i in b['issues'])
    cissues = Counter(i['function']+'|'+i['reason'] for i in c['issues'])
    if added or removed or bissues!=cissues or support(b)!=support(c):
        changed.append(dict(source=key[0],option=key[1],baseline_effect_count=len(b['effects']),candidate_effect_count=len(c['effects']),
                            added_or_changed_effect_records=added,removed_or_changed_effect_records=removed,
                            **call_changes,
                            baseline_support=support(b),candidate_support=support(c),
                            issues_added=dict(cissues-bissues),issues_removed=dict(bissues-cissues)))
    aggregate.update(effect_records_added_or_changed=added,effect_records_removed_or_changed=removed)
fields = ['corpus_scripts','corpus_characters','scripts_with_options','options','incomplete_discovery_scripts',
          'collected_effects_including_routing_and_display','formatter_unexpected_failures','forbidden_boundary_calls','script_read_calls']
report=dict(status='metadata-only-actual-IL-comparison',baseline_dll_sha256=baseline_doc['released_dll_sha256'],
            candidate_dll_sha256=candidate_doc['released_dll_sha256'],
            baseline={k:baseline[k] for k in fields},candidate={k:candidate[k] for k in fields},
            added_option_keys=[dict(source=s,option=o) for s,o in sorted(ckeys-bkeys)],
            removed_option_keys=[dict(source=s,option=o) for s,o in sorted(bkeys-ckeys)],
            issues_delta={k:candidate['issues'].get(k,0)-baseline['issues'].get(k,0) for k in sorted(set(baseline['issues'])|set(candidate['issues'])) if candidate['issues'].get(k,0)!=baseline['issues'].get(k,0)},
            baseline_metrics=baseline['metrics'],candidate_metrics=candidate['metrics'],
            changed_options=changed,effect_record_deltas=dict(aggregate),
            call_identity_fields=list(CALL_IDENTITY_FIELDS),
            call_identity_deltas=dict(scope='option keys present in both reports',**dict(call_aggregate)),
            unmatched_option_call_identity_counts=dict(
                added_option_keys=len(ckeys-bkeys),removed_option_keys=len(bkeys-ckeys),
                call_identity_records_in_added_options=sum(len(crows[k]['effects']) for k in ckeys-bkeys),
                root_call_identity_records_in_added_options=sum(sum(calls(crows[k],False).values()) for k in ckeys-bkeys),
                followup_call_identity_records_in_added_options=sum(sum(calls(crows[k],True).values()) for k in ckeys-bkeys),
                call_identity_records_in_removed_options=sum(len(brows[k]['effects']) for k in bkeys-ckeys),
                root_call_identity_records_in_removed_options=sum(sum(calls(brows[k],False).values()) for k in bkeys-ckeys),
                followup_call_identity_records_in_removed_options=sum(sum(calls(brows[k],True).values()) for k in bkeys-ckeys)),
            limitations=['Effects differing in branch metadata are recorded as changed, not automatically added or removed gameplay effects.',
                         'Call identity deltas preserve multiplicity and followup origin but exclude condition, sequence, branch and display/validation metadata; they do not establish execution semantics.',
                         'Call identity deltas cover shared option keys; added and removed options are counted separately.',
                         'Runtime cost figures include fixture/reflection overhead and cannot certify Unity frame budgets.',
                         'ResourceManager mapping and followup resolution depend on the explicitly recorded input mode.',
                         'This comparison does not prove option ownership, condition outcomes, omitted effects or complete gameplay rewards.'])
output_path.write_text(json.dumps(report,ensure_ascii=False,indent=2))
print(json.dumps({k:v for k,v in report.items() if k not in ('changed_options','added_option_keys','removed_option_keys','baseline_metrics','candidate_metrics')},ensure_ascii=False))
