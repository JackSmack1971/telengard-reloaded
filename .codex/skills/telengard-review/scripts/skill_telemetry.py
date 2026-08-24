#!/usr/bin/env python3
"""Durable, non-gating telemetry for the telengard-review Codex skill."""
from __future__ import annotations
import argparse, collections, datetime as dt, hashlib, json, os, re, shlex, subprocess, sys, time, uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]; TD=ROOT/'telemetry'; EVENTS=TD/'events.jsonl'; SESSION=TD/'current-session.json'; REPORT=TD/'report.md'
SENSITIVE=re.compile(r'(?i)(token|secret|password|passwd|api[-_]?key|authorization|bearer)')

def now(): return dt.datetime.now(dt.timezone.utc).isoformat().replace('+00:00','Z')
def fp():
    h=hashlib.sha256()
    for p in (ROOT/'SKILL.md',Path(__file__).resolve()):
        try:h.update(p.read_bytes())
        except OSError:pass
    return h.hexdigest()[:16]
def warn(x): print(f'[telengard-review telemetry] {x}',file=sys.stderr)
def state():
    try:return json.loads(SESSION.read_text(encoding='utf-8'))
    except (OSError,json.JSONDecodeError):return {}
def sid(): return str(state().get('session_id','unscoped'))
def write_state(v):
    try:
        TD.mkdir(parents=True,exist_ok=True); t=SESSION.with_suffix('.json.tmp'); t.write_text(json.dumps(v,sort_keys=True),encoding='utf-8'); os.replace(t,SESSION)
    except OSError as e: warn(f'state unavailable: {type(e).__name__}: {e}')
def emit(name,session_id=None,**fields):
    r={'schema_version':1,'timestamp':now(),'skill':'telengard-review','skill_fingerprint':fp(),'session_id':session_id or sid(),'event':name,**{k:v for k,v in fields.items() if v is not None}}
    try:
        TD.mkdir(parents=True,exist_ok=True)
        with EVENTS.open('a',encoding='utf-8') as f:f.write(json.dumps(r,sort_keys=True,ensure_ascii=False)+'\n')
    except OSError as e: warn(f'event log unavailable: {type(e).__name__}: {e}')
def load():
    out=[]; bad=0
    try: lines=EVENTS.read_text(encoding='utf-8').splitlines()
    except OSError:return out,bad
    for line in lines:
        try:
            v=json.loads(line)
            if isinstance(v,dict):out.append(v)
            else:bad+=1
        except json.JSONDecodeError:bad+=1
    return out,bad
def attempt(name,keys):
    es,_=load(); s=sid(); return 1+sum(1 for e in es if e.get('event')==name and e.get('session_id')==s and all(e.get(k)==v for k,v in keys.items()))
def sanitize(argv):
    out=[]; redact=False
    for i,a in enumerate(argv):
        if redact:v='<redacted>'; redact=False
        elif a.startswith('-') and SENSITIVE.search(a):
            v=f"{a.split('=',1)[0]}=<redacted>" if '=' in a else a; redact='=' not in a
        elif SENSITIVE.search(a):v='<redacted>'
        else:v=re.sub(r'(://)[^/@:\s]+:[^/@\s]+@',r'\1<redacted>@',a); v='<multiline>' if '\n' in v or '\r' in v else v[:240]
        if i==0:v=Path(v).name or v
        out.append(v)
    try:return shlex.join(out)
    except Exception:return ' '.join(out)
def cint(v):
    try:return int(v)
    except Exception:return 0
def pct(n,d): return None if not d else round(n/d,4)

def start(a):
    old=state()
    if old.get('session_id') and not old.get('ended_at'):emit('session_abandoned',session_id=str(old['session_id']),reason='new_session_started_before_end')
    s=str(uuid.uuid4()); v={'session_id':s,'started_at':now(),'ticket':a.ticket,'base_ref':a.base_ref,'head_ref':a.head_ref,'skill_fingerprint':fp()}; write_state(v); emit('skill_start',session_id=s,ticket=a.ticket,base_ref=a.base_ref,head_ref=a.head_ref); print(s); return 0
def event(a):
    fields={}
    for raw in a.field:
        if '=' not in raw:continue
        k,v=raw.split('=',1); fields[k]='<redacted>' if SENSITIVE.search(k) else v[:500]
    emit(a.name,**fields); return 0
def probe(a):
    p=Path(a.path); found=p.is_file() if a.expect=='file' else p.is_dir() if a.expect=='directory' else p.exists(); n=attempt('lookup_attempt',{'target_kind':a.kind,'target':a.target}); emit('lookup_attempt',target_kind=a.kind,target=a.target,candidate=str(p),strategy=a.strategy,attempt=n,result='found' if found else 'miss');
    if found:print(p)
    return 0 if found else 1
def run(a):
    argv=list(a.command); argv=argv[1:] if argv and argv[0]=='--' else argv
    if not argv:return 2
    n=attempt('command_attempt',{'kind':a.kind,'label':a.label}); preview=sanitize(argv); exe=Path(argv[0]).name; emit('command_attempt',kind=a.kind,label=a.label,attempt=n,executable=exe,command_preview=preview,command_signature=hashlib.sha256(preview.encode()).hexdigest()[:16])
    if exe.lower() in {'powershell','powershell.exe','pwsh','pwsh.exe'} and any(x.lower() in {'-command','-c'} for x in argv[1:]):emit('wrapper_risk',kind=a.kind,label=a.label,risk='inline_powershell_command'); warn('prefer PowerShell -File or run complex inline PowerShell unwrapped')
    t=time.monotonic()
    try: rc=subprocess.Popen(argv).wait()
    except OSError as e: emit('command_result',kind=a.kind,label=a.label,attempt=n,result='launch_error',exit_code=127,error_type=type(e).__name__,duration_ms=round((time.monotonic()-t)*1000)); return 127
    ok=rc==0 or rc in a.ok_exit; emit('command_result',kind=a.kind,label=a.label,attempt=n,result='success' if rc==0 else 'expected_nonzero' if ok else 'failure',exit_code=rc,duration_ms=round((time.monotonic()-t)*1000)); return rc
def end(a):
    emit('skill_end',outcome=a.outcome,ticket=a.ticket); v=state(); v.update(ended_at=now(),outcome=a.outcome); write_state(v); return 0
def report(a):
    es,bad=load(); starts=[e for e in es if e.get('event')=='skill_start']; ends=[e for e in es if e.get('event')=='skill_end']; selected=[e for e in es if e.get('event')=='lane_selected']; results=[e for e in es if e.get('event')=='lane_result']; actionable=[e for e in results if sum(cint(e.get(k)) for k in ('p0','p1','p2'))>0]; cmd=[e for e in es if e.get('event')=='command_result']; first=[e for e in cmd if cint(e.get('attempt'))==1]; firstok=[e for e in first if e.get('result') in {'success','expected_nonzero'}]; look=[e for e in es if e.get('event')=='lookup_attempt']; lf=[e for e in look if cint(e.get('attempt'))==1]; lfok=[e for e in lf if e.get('result')=='found']; fps={}
    for f in sorted({str(e.get('skill_fingerprint')) for e in es if e.get('skill_fingerprint')}):
        c=[e for e in es if str(e.get('skill_fingerprint'))==f]; fps[f]={'runs_started':sum(e.get('event')=='skill_start' for e in c),'runs_ended':sum(e.get('event')=='skill_end' for e in c),'lanes_selected':sum(e.get('event')=='lane_selected' for e in c)}
    s={'skill':'telengard-review','skill_fingerprint':fp(),'generated_at':now(),'runs_started':len(starts),'runs_ended':len(ends),'outcomes':dict(collections.Counter(str(e.get('outcome','unknown')) for e in ends)),'fingerprint_stats':fps,'command_failures':sum(e.get('result') in {'failure','launch_error'} for e in cmd),'command_first_attempt_success_rate':pct(len(firstok),len(first)),'lookup_misses':sum(e.get('result')=='miss' for e in look),'lookup_first_attempt_success_rate':pct(len(lfok),len(lf)),'lanes_selected':len(selected),'lane_distribution':dict(collections.Counter(str(e.get('lane','unknown')) for e in selected)),'trigger_distribution':dict(collections.Counter(str(e.get('trigger','unknown')) for e in selected)),'actionable_lane_yield':pct(len(actionable),len(results)),'lane_escalations':sum(e.get('event')=='lane_escalated' for e in es),'profile_fallbacks':sum(e.get('event')=='lane_profile_fallback' for e in es),'lane_context_missing':sum(e.get('event')=='lane_context_missing' for e in es),'unexpected_lane_context':sum(e.get('event')=='unexpected_lane_context' for e in es),'lane_retries':sum(e.get('event')=='lane_retry' for e in es),'deduplicated_findings':sum(cint(e.get('duplicates')) for e in es if e.get('event')=='finding_deduplication'),'malformed_lines':bad}
    out='# telengard-review Telemetry Report\n\n```json\n'+json.dumps(s,indent=2,sort_keys=True)+'\n```\n'
    if a.write:
        try:TD.mkdir(parents=True,exist_ok=True); REPORT.write_text(out,encoding='utf-8'); print(REPORT)
        except OSError as e:warn(str(e)); print(out); return 1
    else:print(out)
    return 0

def parser():
    p=argparse.ArgumentParser(); sub=p.add_subparsers(dest='cmd',required=True)
    q=sub.add_parser('start'); q.add_argument('--ticket',default='unspecified'); q.add_argument('--base-ref'); q.add_argument('--head-ref'); q.set_defaults(fn=start)
    q=sub.add_parser('event'); q.add_argument('name'); q.add_argument('--field',action='append',default=[]); q.set_defaults(fn=event)
    q=sub.add_parser('probe'); q.add_argument('--kind',required=True); q.add_argument('--target',required=True); q.add_argument('--path',required=True); q.add_argument('--strategy',default='expected-path'); q.add_argument('--expect',choices=['file','directory','any'],default='any'); q.set_defaults(fn=probe)
    q=sub.add_parser('run'); q.add_argument('--kind',required=True); q.add_argument('--label',required=True); q.add_argument('--ok-exit',action='append',type=int,default=[]); q.add_argument('command',nargs=argparse.REMAINDER); q.set_defaults(fn=run)
    q=sub.add_parser('end'); q.add_argument('--outcome',choices=['pass','changes-required','blocked','failed','inconclusive'],required=True); q.add_argument('--ticket'); q.set_defaults(fn=end)
    q=sub.add_parser('report'); q.add_argument('--write',action='store_true'); q.set_defaults(fn=report)
    return p
if __name__=='__main__':
    a=parser().parse_args(); raise SystemExit(a.fn(a))
