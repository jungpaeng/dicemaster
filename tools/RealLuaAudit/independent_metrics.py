"""Source-size and structural metrics only; never executes Lua or logs source."""
from collections import Counter
from pathlib import Path
import hashlib
import json
import sys
import time

from independent_scan import scan


def quantiles(values):
    values = sorted(values)
    return dict(count=len(values), total=sum(values), p50=values[int((len(values)-1)*.5)],
                p95=values[int((len(values)-1)*.95)], p99=values[int((len(values)-1)*.99)], maximum=values[-1])


index_path, output_path = map(Path, sys.argv[1:])
records = []
started = time.perf_counter()
for entry in json.loads(index_path.read_text()):
    data = (index_path.parent / entry['path']).read_bytes()
    if hashlib.sha256(data).hexdigest() != entry['sha256']:
        raise ValueError('Source hash mismatch')
    source = data.decode('utf-8')
    tokens, markers, calls, choose_candidates, unbalanced = scan(source)
    words = Counter(t[0] for t in tokens if not t[3])
    # Counts are lexical evidence, not executed branch or path counts.
    records.append(dict(source=entry['key'], sha256=entry['sha256'], bytes=len(data),
                        utf16_characters=len(source.encode('utf-16-le'))//2, tokens=len(tokens),
                        if_keywords=words['if'], elseif_keywords=words['elseif'], else_keywords=words['else'],
                        index_markers=len(markers), distinct_marker_keys=len(set(m[0] for m in markers)),
                        choose_candidate_keys=len(choose_candidates), choose_calls=sum(c['owner']=='' and c['name']=='choose' for c in calls),
                        manager_calls=sum(c['owner'] in ('statmodifymanager','luamanager') for c in calls),
                        delimiter_problems=unbalanced))
fields = ['bytes','utf16_characters','tokens','if_keywords','elseif_keywords','else_keywords','index_markers',
          'distinct_marker_keys','choose_candidate_keys','choose_calls','manager_calls']
report = dict(status='independent-static-size-structure-metrics', lua_or_game_executed=False,
              actual_resource_paths_known=False, counts={k:quantiles([r[k] for r in records]) for k in fields},
              exceeded_released_root_limits=dict(characters=sum(r['utf16_characters']>262144 for r in records),
                                                  tokens=sum(r['tokens']>65536 for r in records)),
              largest_by_characters=sorted(records,key=lambda r:r['utf16_characters'],reverse=True)[:30],
              largest_by_tokens=sorted(records,key=lambda r:r['tokens'],reverse=True)[:30],
              largest_by_options=sorted(records,key=lambda r:r['distinct_marker_keys']+r['choose_candidate_keys'],reverse=True)[:30],
              files=records, elapsed_seconds=time.perf_counter()-started,
              limitations=['Independent tokenization may differ from production tokenization.',
                           'Keyword counts are lexical; they do not prove branch grouping or truth.',
                           'Work counts and cost estimates require the actual-IL report.',
                           'Root-only measurements cannot certify followup-chain limits.'])
output_path.write_text(json.dumps(report,ensure_ascii=False,indent=2))
print(json.dumps({k:report[k] for k in ('status','counts','exceeded_released_root_limits','elapsed_seconds')}))
