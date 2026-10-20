using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // Bounded abstract traversal of definitions. No Lua or gameplay method is
    // evaluated. Call Advance only while the owning map query is visible.
    public sealed class MapRouteDefinitions
    {
        private sealed class Value
        {
            internal string Script, Pool, Literal;
            internal PreviewCondition Condition;
            internal Dictionary<string, string> Table;
            internal Dictionary<string, PreviewCondition> TableGates;
            internal Value Copy() { return new Value { Script = Script, Pool = Pool, Literal = Literal, Condition = Condition,
                Table = Table == null ? null : new Dictionary<string, string>(Table, StringComparer.Ordinal),
                TableGates = TableGates == null ? null : new Dictionary<string, PreviewCondition>(TableGates, StringComparer.Ordinal) }; }
        }
        private sealed class Plan
        { internal MapRouteSyntax Syntax; internal bool Failed; internal float Retry; internal bool? PassiveProof; }
        private sealed class Frame
        {
            internal int At, End;
            internal readonly Dictionary<string, Value> Locals = new Dictionary<string, Value>(StringComparer.Ordinal);
        }
        private sealed class Job
        {
            internal string Script, Pool, Identity = "", Path = "", DialogueCharacter;
            internal int Depth, At, End, PoolCursor;
            internal bool Started, Uncertain, HasChoice, StoryChoice, EntryUncertain;
            internal PreviewCondition Gate = PreviewConditions.Constant(true);
            internal PreviewCondition EntryGate;
            internal List<MapChoiceStep> Steps = new List<MapChoiceStep>();
            internal Value Pending;
            internal Dictionary<string, Value> Bindings = new Dictionary<string, Value>(StringComparer.Ordinal);
            internal HashSet<string> InvalidManagers = new HashSet<string>(StringComparer.Ordinal);
            internal HashSet<string> Ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            internal List<Frame> Frames = new List<Frame>();
            internal IList<MapRouteEvent> PoolItems;
            internal Job Copy()
            {
                var n = new Job { Script = Script, Pool = Pool, Identity = Identity, Path = Path, Depth = Depth, At = At, End = End,
                    Started = Started, Uncertain = Uncertain, HasChoice = HasChoice, StoryChoice = StoryChoice, Gate = Gate, Pending = Pending, DialogueCharacter = DialogueCharacter,
                    EntryGate = EntryGate, EntryUncertain = EntryUncertain, Steps = new List<MapChoiceStep>(Steps),
                    InvalidManagers = new HashSet<string>(InvalidManagers, StringComparer.Ordinal),
                    Ancestors = new HashSet<string>(Ancestors, StringComparer.OrdinalIgnoreCase) };
                var values = new Dictionary<Value, Value>();
                foreach (var p in Bindings) n.Bindings.Add(p.Key, CloneValue(p.Value, values));
                foreach (var f in Frames)
                { var frame = new Frame { At = f.At, End = f.End }; foreach (var p in f.Locals) frame.Locals.Add(p.Key, CloneValue(p.Value, values)); n.Frames.Add(frame); }
                return n;
            }
            private static Value CloneValue(Value value, Dictionary<Value, Value> values)
            { if (value == null) return null; Value copy; if (!values.TryGetValue(value, out copy)) values.Add(value, copy = value.Copy()); return copy; }
        }
        private readonly Func<string, ScriptReadResult> reader;
        private readonly Func<string, IList<MapRouteEvent>> pools;
        private readonly MapRouteLimits limits;
        private readonly Queue<Job> jobs = new Queue<Job>();
        private readonly Dictionary<string, Plan> plans = new Dictionary<string, Plan>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> poolRetries = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, IList<MapRouteEvent>> poolDefinitions = new Dictionary<string, IList<MapRouteEvent>>(StringComparer.Ordinal);
        private readonly List<MapRouteEntry> entries = new List<MapRouteEntry>();
        private readonly List<MapStoryEntry> storyEntries = new List<MapStoryEntry>();
        private readonly Dictionary<string, MapStoryEntry> storyIndex = new Dictionary<string, MapStoryEntry>(StringComparer.Ordinal);
        private readonly List<MapChoiceStep> choiceSteps = new List<MapChoiceStep>();
        private readonly List<string> reasons = new List<string>();
        private int admitted, choices, characters, tokens;
        private int resourceBudget, poolBudget;
        private bool limited, cleared;
        public int WorkCount { get; private set; }
        public int ScriptReadCount { get; private set; }
        public int PoolReadCount { get; private set; }
        public int ParseCount { get; private set; }
        public IList<MapRouteEntry> Entries { get { return entries.AsReadOnly(); } }
        public IList<MapStoryEntry> StoryEntries { get { return storyEntries.AsReadOnly(); } }
        // Include utility/menu choices too: absence of a linked story cannot
        // establish that an identical visible label names a unique node.
        // Alternative guard definitions are deliberately retained. Consumers
        // must also respect preparation/partial state before claiming unique.
        public IList<MapChoiceStep> ChoiceSteps { get { return choiceSteps.AsReadOnly(); } }
        public bool HasReadFailure { get { if (poolRetries.Count > 0) return true; foreach (var p in plans.Values) if (p.Failed) return true; return false; } }
        public bool IsPreparing { get { return !cleared && jobs.Count > 0 && !HasReadFailure; } }
        public bool IsPartial { get { return limited || HasReadFailure || reasons.Count > 0; } }
        public string Diagnostic
        {
            get
            {
                string text = HasReadFailure ? "경로 자료 읽기 대기/실패 · 5초 뒤 재시도" : IsPreparing ? "경로 안내 준비 중" : "";
                if (reasons.Count > 0) text += (text.Length == 0 ? "" : " · ") + string.Join(" · ", reasons);
                return text;
            }
        }
        // The route reader has already incrementally prepared these tokens.
        // Only this small, closed, straight-line subset may enter the existing
        // synchronous proof analyzer during passive map display. It contains
        // no followup, choice, snapshot call, nested argument or user function.
        // Larger/unsupported results remain available to explicit log detail.
        public bool CanBuildBoundedProof(string script)
        {
            Plan plan;
            if (script == null || !plans.TryGetValue(script, out plan) || plan.Syntax == null) return false;
            if (plan.PassiveProof.HasValue) return plan.PassiveProof.Value;
            var syntax = plan.Syntax; var t = syntax.Tokens;
            if (!syntax.Ready) return false;
            return (plan.PassiveProof = SmallProof(syntax)).Value;
        }
        private static bool SmallProof(MapRouteSyntax syntax)
        {
            var t = syntax.Tokens;
            if (syntax.Limited || syntax.Malformed || syntax.Source.Length > 8192 || t.Count > 256 || Encoding.UTF8.GetByteCount(syntax.Source) > 8192) return false;
            bool flag = false;
            for (int at = 0; at < t.Count;)
            {
                if (t[at].Text == ";") { at++; continue; }
                if (at + 4 >= t.Count || t[at].Text != "statmodifymanager" || t[at + 1].Text != "." || t[at + 3].Text != "(" || !syntax.Closes.TryGetValue(at + 3, out int close)) return false;
                string name = t[at + 2].Text;
                if (name != "SetFlag" && name != "AddFlag" && name != "Player" && name != "Character" && name != "SetPlayer" && name != "SetCharacter" && name != "AddBook" && name != "AddMisc" && name != "AddSpecial" && name != "RemoveBook" && name != "RemoveMisc" && name != "RemoveSpecial" && name != "AddTalent") return false;
                flag |= name == "SetFlag";
                for (int i = at + 4; i < close; i++)
                    if (!t[i].IsString && t[i].Text != "," && t[i].Text != "-" && !int.TryParse(t[i].Text, NumberStyles.None, CultureInfo.InvariantCulture, out int number)) return false;
                at = close + 1;
            }
            return flag;
        }
        public MapRouteDefinitions(string rootScript, Func<string, ScriptReadResult> scriptReader,
            Func<string, IList<MapRouteEvent>> poolReader, MapRouteLimits policy = null)
        {
            reader = scriptReader; pools = poolReader; limits = policy ?? new MapRouteLimits();
            if (ValidScript(rootScript)) Admit(new Job { Script = rootScript, Path = rootScript, Identity = rootScript });
            else if (!string.IsNullOrEmpty(rootScript)) Reason("시작 스크립트 경로 확인 불가");
        }
        public void Clear()
        { cleared = true; jobs.Clear(); plans.Clear(); poolDefinitions.Clear(); poolRetries.Clear(); entries.Clear(); storyEntries.Clear(); storyIndex.Clear(); choiceSteps.Clear(); reasons.Clear(); }
        private void Reason(string value) { if (reasons.Count < 8 && !reasons.Contains(value)) reasons.Add(value); }
        private static bool ValidScript(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 256) return false;
            foreach (char c in value) if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')) return false;
            return true;
        }
        private void Admit(Job job)
        {
            if (admitted >= limits.MaxContexts || job.Depth > limits.MaxDepth)
            { limited = true; Reason("경로 깊이/대기열 상한 · 일부 연결 확인 불가"); return; }
            admitted++; jobs.Enqueue(job);
        }
        public void Advance(float now, int maxWork = 8192)
        {
            if (cleared || limited && WorkCount >= limits.MaxWork) return;
            resourceBudget = poolBudget = 1;
            if (float.IsNaN(now) || float.IsInfinity(now)) now = 0;
            int budget = Math.Min(Math.Max(0, maxWork), Math.Max(0, limits.MaxWork - WorkCount));
            int blocked = 0;
            while (budget > 0 && jobs.Count > 0)
            {
                Job job = jobs.Dequeue(); int used;
                bool done = job.Pool != null ? ProcessPool(job, now, out used) : Process(job, now, budget, out used);
                used = Math.Min(budget, Math.Max(0, used)); budget -= used; WorkCount += used;
                if (!done) jobs.Enqueue(job);
                if (used == 0) { if (++blocked >= jobs.Count) break; } else blocked = 0;
            }
            if (WorkCount >= limits.MaxWork && jobs.Count > 0)
            { limited = true; jobs.Clear(); Reason("경로 누적 작업 상한 · 일부 연결 확인 불가"); }
        }
        private Plan GetPlan(string script, float now, out bool blocked)
        {
            blocked = false; Plan plan;
            if (!plans.TryGetValue(script, out plan))
            {
                if (plans.Count >= limits.MaxScripts) { limited = true; Reason("스크립트 수 상한 · 일부 연결 확인 불가"); return null; }
                plans.Add(script, plan = new Plan());
            }
            if (plan.Syntax != null) return plan;
            if (plan.Failed && now < plan.Retry) { blocked = true; return plan; }
            if (resourceBudget <= 0) { blocked = true; return plan; }
            resourceBudget--;
            ScriptReadCount++;
            ScriptReadResult read;
            try { read = reader == null ? null : reader(script); } catch (Exception) { read = null; }
            if (read == null || read.State != ScriptReadState.Success)
            { plan.Failed = true; plan.Retry = now + 5; blocked = true; return plan; }
            plan.Failed = false;
            int chars = Math.Min(limits.MaxScriptCharacters, Math.Max(0, limits.MaxCharacters - characters));
            int count = Math.Min(limits.MaxScriptTokens, Math.Max(0, limits.MaxTokens - tokens));
            plan.Syntax = new MapRouteSyntax(read.Text, chars, count); ParseCount++;
            return plan;
        }
        private bool Process(Job job, float now, int budget, out int used)
        {
            used = 0;
            if (!job.Started && job.Ancestors.Contains(job.Script)) { Reason("경로 순환 · 이후 연결 확인 불가"); used = 1; return true; }
            bool blocked; Plan plan = GetPlan(job.Script, now, out blocked);
            if (plan == null) { used = 1; return true; }
            if (blocked) return false;
            var syntax = plan.Syntax;
            if (!syntax.Ready)
            {
                int before = syntax.Tokens.Count, charsBefore = syntax.Characters;
                used = syntax.Step(budget);
                tokens += syntax.Tokens.Count - before; characters += syntax.Characters - charsBefore;
                if (syntax.Ready && (syntax.Limited || syntax.Malformed)) Reason("구문 한계/미완결 · 일부 연결 확인 불가");
                return false;
            }
            used = 1;
            if (EnvironmentCells(job) < 0)
            { Reason("경로 환경/복사 상한 · 해당 경로 확인 불가"); return true; }
            if (syntax.Limited || syntax.Malformed) { job.Uncertain = true; }
            if (!job.Started) { job.Started = true; job.End = syntax.Tokens.Count; job.Ancestors.Add(job.Script); }
            if (job.At >= job.End)
            {
                if (job.Frames.Count > 0)
                {
                    var frame = job.Frames[job.Frames.Count - 1]; job.Frames.RemoveAt(job.Frames.Count - 1);
                    foreach (var p in frame.Locals) { if (p.Value == null) job.Bindings.Remove(p.Key); else job.Bindings[p.Key] = p.Value; }
                    job.At = frame.At; job.End = frame.End; return false;
                }
                Finish(job); return true;
            }
            var t = syntax.Tokens; int at = job.At; string text = t[at].Text;
            if (t[at].IsString) { job.At++; return false; }
            MapRouteSyntax.Control control;
            if (syntax.Controls.TryGetValue(at, out control))
            {
                if (control.Kind == "if") { Fork(job, syntax, control); return true; }
                if (control.Kind == "do")
                { job.Frames.Add(new Frame { At = control.End + 1, End = job.End }); job.At++; job.End = control.End; return false; }
                if (control.Kind != "function") { job.DialogueCharacter = null; job.Uncertain = true; Reason("반복/미지원 제어 흐름 · 경로 확인 필요"); }
                else if (at + 1 < t.Count)
                {
                    if (Manager(t[at + 1].Text)) { job.InvalidManagers.Add(t[at + 1].Text); job.DialogueCharacter = null; job.Uncertain = true; Reason("manager 바인딩 변경 · 호출 확인 불가"); }
                    if (t[at + 1].Text == "choose" || t[at + 1].Text == "say" || t[at + 1].Text == "setcharacter")
                    { job.Bindings[t[at + 1].Text] = new Value(); if (t[at + 1].Text == "setcharacter") job.DialogueCharacter = null; }
                }
                job.At = Math.Min(control.End + 1, job.End); return false;
            }
            if (text == "goto" || text == "break" || text == "continue" || text == ":" && at + 1 < job.End && t[at + 1].Text == ":")
            { Reason("미지원 이동 제어 · 해당 경로 확인 불가"); return true; }
            if (text == "return")
            {
                if (at + 1 < job.End && t[at + 1].Text != ";")
                { Reason("반환식 실행 확인 불가 · 해당 경로 중단"); return true; }
                Finish(job); return true;
            }
            if (text == "and" || text == "or" || text == "..") { job.Uncertain = true; Reason("미지원 값/표현식 · 현재 경로 확인 필요"); }
            bool local = text == "local";
            if (local && at + 1 < job.End && t[at + 1].Text == "function") { job.At++; return false; }
            int variable = local ? at + 1 : at;
            if (variable < job.End && t[variable].IsIdentifier && (variable == 0 || t[variable - 1].Text != "." && t[variable - 1].Text != ":"))
            {
                string name = t[variable].Text;
                if (MultipleAssignment(syntax, variable, job.End))
                { Reason("다중 대입 미지원 · 해당 경로 확인 불가"); return true; }
                if (local && variable + 1 < job.End && t[variable + 1].Text == "function") { job.At++; return false; }
                if (variable + 1 < job.End && t[variable + 1].Text == "=")
                {
                    if (name == "setcharacter" || name == "characters") job.DialogueCharacter = null;
                    if (Manager(name)) { job.InvalidManagers.Add(name); job.Uncertain = true; Reason("manager 바인딩 변경 · 호출 확인 불가"); }
                    if (local) Declare(job, name);
                    int rhs = variable + 2;
                    if (syntax.Controls.TryGetValue(rhs, out MapRouteSyntax.Control function) && function.Kind == "function")
                    { job.Bindings[name] = new Value(); job.At = function.End + 1; return false; }
                    if (rhs < job.End && t[rhs].Text == "choose" && TryCall(syntax, rhs, out string co, out string cn, out int op, out int cl))
                    {
                        if (job.Bindings.ContainsKey("choose")) { Reason("재정의된 choose · 선택 경로 확인 불가"); return true; }
                        Choose(job, syntax, name, op, cl); return true;
                    }
                    int end; Value value = ReadValue(job, syntax, rhs, out end);
                    if (end < job.End && (t[end].Text == ".." || t[end].Text == "+" || t[end].Text == "-" || t[end].Text == "*" || t[end].Text == "/" || t[end].Text == "and" || t[end].Text == "or" || t[end].Text == "," || t[end].Text == "["))
                    { value = new Value(); job.Uncertain = true; Reason("미지원 값/표현식 · 현재 경로 확인 필요"); }
                    job.Bindings[name] = value; job.At = Math.Max(variable + 2, end); return false;
                }
                if (variable + 4 < job.End && t[variable + 1].Text == "[" && syntax.Closes.TryGetValue(variable + 1, out int close) && close + 2 < job.End && t[close + 1].Text == "=")
                {
                    Value table;
                    int end = close + 3;
                    if (job.Bindings.TryGetValue(name, out table) && table.Table != null && close == variable + 3)
                    {
                        string rawKey = t[variable + 2].Text; Value keyValue;
                        if (!t[variable + 2].IsString && job.Bindings.TryGetValue(rawKey, out keyValue)) rawKey = keyValue.Literal;
                        string key = CanonicalLiteral(rawKey);
                        if (key == null) { Reason("동적 선택지 인덱스 · 해당 경로 확인 불가"); return true; }
                        if (t[close + 2].IsString && PreviewSyntax.TryString(t[close + 2].Text, out string label))
                        { table.Table[key] = label; if (table.TableGates != null) table.TableGates.Remove(key); }
                        else
                        {
                            // The game's enabled option form is predicate and
                            // "label" or "~x". Keep its predicate rather than
                            // dropping that arm or claiming it is enabled.
                            int first = close + 2, and = first;
                            while (and < job.End && and - first < 256 && t[and].Text != "and")
                            {
                                if (and > first && syntax.Source.Substring(t[and - 1].End, t[and].Start - t[and - 1].End).IndexOf('\n') >= 0) break;
                                and++;
                            }
                            if (and + 3 < job.End && t[and].Text == "and" && t[and + 1].IsString && t[and + 2].Text == "or" && t[and + 3].IsString &&
                                PreviewSyntax.TryString(t[and + 1].Text, out label) && PreviewSyntax.TryString(t[and + 3].Text, out string disabled) && disabled == "~x")
                            {
                                var bindings = Conditions(job);
                                table.Table[key] = label;
                                if (table.TableGates == null) table.TableGates = new Dictionary<string, PreviewCondition>(StringComparer.Ordinal);
                                table.TableGates[key] = PreviewConditions.ParseTokens(t, first, and, bindings);
                                end = and + 4;
                            }
                            // Opaque indexed values may expose the manager
                            // through an alias; its object provenance is not
                            // modeled, so later setters cannot prove speech.
                            else { table.Table.Remove(key); job.InvalidManagers.Add("characters"); job.DialogueCharacter = null; job.Uncertain = true; Reason("선택지 활성식 확인 불가"); }
                        }
                    }
                    else { if (Manager(name)) job.InvalidManagers.Add(name); job.InvalidManagers.Add("characters"); job.DialogueCharacter = null; job.Uncertain = true; Reason("미지원 인덱스 쓰기 · 현재 경로 확인 필요"); }
                    job.At = end; return false;
                }
                if (local)
                { Declare(job, name); job.Bindings[name] = new Value(); if (name == "setcharacter" || name == "characters") job.DialogueCharacter = null; if (Manager(name)) { job.InvalidManagers.Add(name); job.Uncertain = true; Reason("manager 바인딩 변경 · 호출 확인 불가"); } job.At = variable + 1; return false; }
                if (Manager(name) && variable + 3 < job.End && (t[variable + 1].Text == "." || t[variable + 1].Text == ":") && t[variable + 3].Text == "=")
                { job.InvalidManagers.Add(name); job.DialogueCharacter = null; job.Uncertain = true; Reason("manager 바인딩 변경 · 호출 확인 불가"); }
            }
            if (TryCall(syntax, at, out string owner, out string method, out int open, out int closing))
            {
                if (UsesColon(syntax, at, open))
                {
                    job.DialogueCharacter = null;
                    job.Uncertain = true; Reason("colon 호출 인수 계약 미확인 · 현재 경로 확인 필요");
                    if (owner == "luamanager" && (method == "SetNextScript" || method == "SetCurrentStoryScript")) job.Pending = new Value();
                    job.At = closing + 1; return false;
                }
                if (owner.Length == 0 && method == "setcharacter")
                {
                    job.DialogueCharacter = DialogueCharacter(job, syntax, open, closing);
                    // Speaker evidence is definition metadata, not permission
                    // to execute this UI call or strengthen a route verdict.
                    job.Uncertain = true; Reason("미지원 실행 호출 · 현재 경로 확인 필요");
                    job.At = closing + 1; return false;
                }
                if (owner == "characters" && method == "Focus" && !job.InvalidManagers.Contains(owner) &&
                    !job.Bindings.ContainsKey(owner) && closing == open + 2 && t[open + 1].IsString)
                {
                    // Focus moves a portrait, not the dialogue speaker. It
                    // preserves an existing witness but cannot create one.
                    job.Uncertain = true; Reason("미지원 실행 호출 · 현재 경로 확인 필요");
                    job.At = closing + 1; return false;
                }
                // Recognize only the game's literal localized dialogue form.
                // Merely registering a utility/menu script is not a story.
                if (owner.Length == 0 && method == "say" && !job.Bindings.ContainsKey("say") && !job.InvalidManagers.Contains("luamanager") &&
                    TryStory(syntax, open, closing, out string storyKey))
                {
                    AddStory(job, t[at].Start, storyKey);
                    // This audited dialogue syntax is a complete structural
                    // observation. It can yield before later code runs, so
                    // descendants retain uncertainty without falsely marking
                    // the definition itself unsupported or incomplete.
                    job.Uncertain = true; job.At = closing + 1; return false;
                }
                if (owner == "luamanager" && (method == "SetNextScript" || method == "SetCurrentStoryScript") && !job.InvalidManagers.Contains(owner))
                {
                    int end; job.Pending = ReadValue(job, syntax, open + 1, out end);
                    if (end != closing) job.Pending = new Value();
                    if (job.Pending.Script == null && job.Pending.Pool == null) Reason("동적/미지원 후속 대상 · 연결 확인 불가");
                }
                else if (owner == "checkpointmanager" && method == "Position" && !job.InvalidManagers.Contains(owner))
                { job.Uncertain = true; Reason("등록에 사용되지 않은 Position 반환값"); }
                else { job.DialogueCharacter = null; job.Uncertain = true; Reason("미지원 실행 호출 · 현재 경로 확인 필요"); }
                job.At = closing + 1; return false;
            }
            if (TrySugar(syntax, at, out owner, out method, out closing))
            {
                job.DialogueCharacter = null;
                job.Uncertain = true; Reason("미지원 축약 호출 · 현재 경로 확인 필요");
                if (owner == "luamanager" && (method == "SetNextScript" || method == "SetCurrentStoryScript")) job.Pending = new Value();
                job.At = closing + 1; return false;
            }
            if (text == "characters") { job.InvalidManagers.Add("characters"); job.DialogueCharacter = null; }
            if (text != ";") { job.Uncertain = true; Reason("미지원 명령/값 · 현재 경로 확인 필요"); }
            job.At++; return false;
        }
        private static bool Manager(string name) { return name == "checkpointmanager" || name == "luamanager" || name == "statmodifymanager" || name == "characters"; }
        private static void Declare(Job job, string name)
        {
            if (job.Frames.Count == 0) return;
            var locals = job.Frames[job.Frames.Count - 1].Locals;
            Value old; if (!locals.ContainsKey(name)) locals[name] = job.Bindings.TryGetValue(name, out old) ? old : null;
        }
        private Value ReadValue(Job job, MapRouteSyntax syntax, int at, out int end)
        {
            var t = syntax.Tokens; end = at + 1;
            if (at >= job.End) return new Value();
            if (t[at].IsString && PreviewSyntax.TryString(t[at].Text, out string literal))
                return new Value { Script = literal.Length == 0 || ValidScript(literal) ? literal : null, Literal = t[at].Text };
            if (t[at].Text == "{" && syntax.Closes.TryGetValue(at, out int tableEnd))
            {
                // Tables may hide a character-manager alias in any cell.
                // Inspect only a fixed token window; larger opaque tables
                // conservatively invalidate that manager instead of scanning.
                bool escaped = tableEnd - at > MapRouteLimits.MaxHeaderTokens;
                for (int i = at + 1; !escaped && i < tableEnd; i++) escaped = t[i].IsIdentifier && t[i].Text == "characters";
                if (escaped) { job.InvalidManagers.Add("characters"); job.DialogueCharacter = null; }
                end = tableEnd + 1; var table = new Dictionary<string, string>(StringComparer.Ordinal); int n = 1;
                for (int i = at + 1; i < tableEnd; i++)
                {
                    if (t[i].Text == "," || t[i].Text == ";") continue;
                    if (n > MapRouteLimits.MaxTableCells) { job.DialogueCharacter = null; job.Uncertain = true; Reason("선택 테이블 크기 상한"); return new Value(); }
                    if (!t[i].IsString || !PreviewSyntax.TryString(t[i].Text, out string value))
                    { job.DialogueCharacter = null; job.Uncertain = true; Reason("테이블 값/내부 호출 확인 불가"); return new Value(); }
                    table[(n++).ToString(CultureInfo.InvariantCulture)] = value;
                }
                return new Value { Table = table };
            }
            if (TryCall(syntax, at, out string owner, out string name, out int open, out int close))
            {
                end = close + 1;
                if (UsesColon(syntax, at, open))
                { job.DialogueCharacter = null; job.Uncertain = true; Reason("colon 호출 인수 계약 미확인 · 현재 경로 확인 필요"); return new Value(); }
                if (owner == "checkpointmanager" && !job.InvalidManagers.Contains(owner) && close == open + 2 && t[open + 1].IsString && PreviewSyntax.TryString(t[open + 1].Text, out string key))
                {
                    if (name == "Position") return new Value { Pool = key };
                    if (name == "Switch" || name == "Condition") return new Value { Condition = PreviewConditions.ParseTokens(t, at, close + 1) };
                }
                job.DialogueCharacter = null; job.Uncertain = true; Reason("미지원 실행 호출 · 현재 경로 확인 필요"); return new Value();
            }
            if (TrySugar(syntax, at, out owner, out name, out int sugarClose))
            { end = sugarClose + 1; job.DialogueCharacter = null; job.Uncertain = true; Reason("미지원 축약 호출 · 현재 경로 확인 필요"); return new Value(); }
            // No object/alias analysis is performed. Once the character
            // manager escapes as a value, a later method write through that
            // alias cannot be ruled out; do not accept new speaker witnesses.
            if (t[at].Text == "characters")
            { job.InvalidManagers.Add("characters"); job.DialogueCharacter = null; }
            Value found;
            if (job.Bindings.TryGetValue(t[at].Text, out found)) return found;
            if (t[at].Text == "true" || t[at].Text == "false" || int.TryParse(t[at].Text, out int number))
                return new Value { Literal = t[at].Text, Condition = PreviewConditions.ParseTokens(t, at, at + 1) };
            job.Uncertain = true; Reason("미지원 값/표현식 · 현재 경로 확인 필요"); return new Value();
        }
        private static bool TryCall(MapRouteSyntax syntax, int at, out string owner, out string name, out int open, out int close)
        {
            owner = name = ""; open = close = -1; var t = syntax.Tokens;
            if (at >= t.Count || !t[at].IsIdentifier || at > 0 && (t[at - 1].Text == "." || t[at - 1].Text == ":")) return false;
            int p = at; var parts = new List<string> { t[p++].Text };
            while (p + 1 < t.Count && (t[p].Text == "." || t[p].Text == ":") && t[p + 1].IsIdentifier && parts.Count < 16)
            { parts.Add(t[p + 1].Text); p += 2; }
            if (p >= t.Count || t[p].Text != "(" || !syntax.Closes.TryGetValue(p, out close)) return false;
            open = p; name = parts[parts.Count - 1]; parts.RemoveAt(parts.Count - 1); owner = string.Join(".", parts); return true;
        }
        private static bool TrySugar(MapRouteSyntax syntax, int at, out string owner, out string name, out int close)
        {
            owner = name = ""; close = -1; var t = syntax.Tokens;
            if (at >= t.Count || !t[at].IsIdentifier || at > 0 && (t[at - 1].Text == "." || t[at - 1].Text == ":")) return false;
            int p = at; var parts = new List<string> { t[p++].Text };
            while (p + 1 < t.Count && (t[p].Text == "." || t[p].Text == ":") && t[p + 1].IsIdentifier && parts.Count < 16)
            { parts.Add(t[p + 1].Text); p += 2; }
            if (p >= t.Count) return false;
            if (t[p].IsString) close = p;
            else if (t[p].Text != "{" || !syntax.Closes.TryGetValue(p, out close)) return false;
            name = parts[parts.Count - 1]; parts.RemoveAt(parts.Count - 1); owner = string.Join(".", parts); return true;
        }
        private static bool UsesColon(MapRouteSyntax syntax, int start, int open)
        { for (int i = start; i < open; i++) if (syntax.Tokens[i].Text == ":") return true; return false; }
        private static bool TryStory(MapRouteSyntax syntax, int open, int close, out string key)
        {
            key = null;
            var t = syntax.Tokens;
            return TryCall(syntax, open + 1, out string owner, out string method, out int inner, out int end) &&
                owner == "luamanager" && method == "GetStoryText" && !UsesColon(syntax, open + 1, inner) &&
                end == close - 1 && end == inner + 2 && t[inner + 1].IsString &&
                PreviewSyntax.TryString(t[inner + 1].Text, out key) && !string.IsNullOrEmpty(key);
        }
        private static string DialogueCharacter(Job job, MapRouteSyntax syntax, int open, int close)
        {
            if (job.Bindings.ContainsKey("setcharacter") || job.Bindings.ContainsKey("characters") || job.InvalidManagers.Contains("characters")) return null;
            var t = syntax.Tokens;
            if (!TryCall(syntax, open + 1, out string owner, out string method, out int inner, out int end) ||
                owner != "characters" || method != "Get" || UsesColon(syntax, open + 1, inner) || end != inner + 2 ||
                !t[inner + 1].IsString || !PreviewSyntax.TryString(t[inner + 1].Text, out string character) ||
                end >= close || end != close - 1 && t[end + 1].Text != "," || !ValidScript(character)) return null;
            if (character.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0 ||
                character.Equals("narrative", StringComparison.OrdinalIgnoreCase) || character.Equals("narrator", StringComparison.OrdinalIgnoreCase)) return null;
            return character;
        }
        private MapRoutePath Path(Job job, string dialogueCharacter = null)
        { return new MapRoutePath(job.Identity, job.Path, job.HasChoice, job.Uncertain, job.Gate, job.Steps, job.EntryGate, job.EntryUncertain, dialogueCharacter); }
        private void AddStory(Job job, int offset, string key)
        {
            if (!job.StoryChoice || job.Steps.Count == 0 || string.IsNullOrEmpty(job.DialogueCharacter)) return;
            var last = job.Steps[job.Steps.Count - 1];
            string identity = job.Script + "/selected:" + last.Script + ":" + last.SourceOffset + ":" + last.Index;
            MapStoryEntry entry;
            if (!storyIndex.TryGetValue(identity, out entry))
            {
                if (storyEntries.Count >= limits.MaxEntries) { limited = true; Reason("선택 이야기 항목 상한 · 일부 연결 확인 불가"); return; }
                storyEntries.Add(entry = new MapStoryEntry(identity, job.Script, offset, key, last.RawLabel, () => IsPreparing || IsPartial));
                storyIndex.Add(identity, entry);
            }
            // All lines in the same selected dialogue segment share a path.
            // Keep its first qualifying NPC witness, not a complete list of
            // conversation participants or another array for every paragraph.
            if (!entry.HasPath(job.Identity) && !entry.Add(Path(job, job.DialogueCharacter))) { limited = true; Reason("이야기 경로 수 상한 · 생략된 경로 확인 불가"); }
        }
        private void Choose(Job job, MapRouteSyntax syntax, string variable, int open, int close)
        {
            var t = syntax.Tokens; Value table;
            if (open + 1 >= close || !job.Bindings.TryGetValue(t[open + 1].Text, out table) || table.Table == null)
            {
                job.Bindings[variable] = new Value(); job.Uncertain = true; job.At = close + 1; Admit(job); Reason("선택지 테이블 확인 불가"); return;
            }
            if (!table.Table.ContainsKey("1"))
            {
                // Empty and non-array-only tables have #options == 0. Lua's
                // length for a sparse positive-index table is not inferred:
                // retain only its possible return-zero tail, as uncertain.
                bool sparse = false;
                foreach (string key in table.Table.Keys)
                    if (decimal.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal n) && n >= 2 && n == decimal.Truncate(n)) sparse = true;
                job.At = close + 1;
                job.Bindings[variable] = new Value { Literal = "0", Condition = PreviewConditions.Parse("0") };
                if (sparse) { job.Uncertain = true; Reason("희소 선택지 배열 길이 미확정 · 반환 0 후 경로 가능"); }
                Admit(job); return;
            }
            int cells = EnvironmentCells(job);
            if (job.Steps.Count >= MapRouteLimits.MaxChoiceSteps || table.Table.Count > MapRouteLimits.MaxChoiceArms || cells < 0 || (long)cells * table.Table.Count > MapRouteLimits.MaxForkCells)
            { Reason("선택 분기/복사 상한 · 해당 경로 확인 불가"); return; }
            // The supplied lua/fungus choose uses ipairs: numeric contiguous
            // keys starting at one only. Any '~' prefix disables that option.
            for (int index = 1; index <= MapRouteLimits.MaxChoiceArms; index++)
            {
                string label, key = index.ToString(CultureInfo.InvariantCulture);
                if (!table.Table.TryGetValue(key, out label)) break;
                if (admitted >= limits.MaxContexts) { limited = true; Reason("경로 대기열 상한"); break; }
                if (label.StartsWith("~", StringComparison.Ordinal)) continue;
                if (++choices > limits.MaxChoices) { limited = true; Reason("선택 노드 상한"); break; }
                Job arm = job.Copy(); arm.At = close + 1; arm.HasChoice = arm.StoryChoice = true;
                arm.Identity += "/" + job.Script + ":choose:" + t[open].Start + ":" + key;
                arm.Path += " → " + label;
                PreviewCondition gate = PreviewConditions.Constant(true);
                if (table.TableGates != null && table.TableGates.TryGetValue(key, out PreviewCondition optionGate))
                    gate = optionGate;
                arm.Gate = PreviewConditions.All(arm.Gate, gate);
                var step = new MapChoiceStep(job.Script, t[open].Start, index, label, arm.Gate, gate, job.Uncertain);
                arm.Steps.Add(step); choiceSteps.Add(step);
                if (arm.EntryGate == null) { arm.EntryGate = arm.Gate; arm.EntryUncertain = job.Uncertain; }
                arm.Bindings[variable] = new Value { Literal = key, Condition = PreviewConditions.Parse(key) };
                Admit(arm);
            }
        }
        private bool? LiteralBranch(Job job, List<PreviewToken> t, int start, int end)
        {
            while (end - start >= 2 && t[start].Text == "(" && t[end - 1].Text == ")") { start++; end--; }
            if (end - start == 1)
            { if (t[start].Text == "true") return true; if (t[start].Text == "false") return false; }
            if (end - start != 3 || t[start + 1].Text != "==" && t[start + 1].Text != "~=") return null;
            Value value; string left = job.Bindings.TryGetValue(t[start].Text, out value) ? value.Literal : null;
            if (left == null) return null;
            string right = CanonicalLiteral(t[start + 2].Text); left = CanonicalLiteral(left);
            if (left == null || right == null) return null;
            return t[start + 1].Text == "==" ? left == right : left != right;
        }
        private static string CanonicalLiteral(string raw)
        {
            if (raw == null) return null;
            if (PreviewSyntax.TryString(raw, out string value)) return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
            if (raw == "true" || raw == "false") return raw;
            decimal number;
            return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ? number.ToString("G29", CultureInfo.InvariantCulture) : null;
        }
        private static bool MultipleAssignment(MapRouteSyntax syntax, int start, int end)
        {
            var t = syntax.Tokens; bool comma = false;
            for (int i = start; i < end && i - start < 64; i++)
            {
                string text = t[i].Text;
                if (text == "=") return comma;
                if (text == "[") { if (!syntax.Closes.TryGetValue(i, out int close)) return false; i = close; continue; }
                if (text == ",") { comma = true; continue; }
                if (text == "." || t[i].IsIdentifier) continue;
                return false;
            }
            return false;
        }
        private void Fork(Job job, MapRouteSyntax syntax, MapRouteSyntax.Control control)
        {
            if (control.Branches.Count > MapRouteLimits.MaxBranchArms)
            { Reason("분기 수 상한 · 해당 경로 확인 불가"); return; }
            int cells = EnvironmentCells(job), headers = 0;
            foreach (var b in control.Branches)
            {
                int count = b.HeaderEnd - b.Header;
                if (count > MapRouteLimits.MaxHeaderTokens) { Reason("분기 조건 길이 상한 · 해당 경로 확인 불가"); return; }
                headers += Math.Max(0, count);
            }
            if (control.Branches.Count > MapRouteLimits.MaxBranchArms || cells < 0 || (long)cells * control.Branches.Count + headers > MapRouteLimits.MaxForkCells)
            { Reason("분기/복사 상한 · 해당 경로 확인 불가"); return; }
            PreviewCondition remaining = PreviewConditions.Constant(true); bool hasElse = false, provenTaken = false, speakerUnknown = false;
            foreach (var branch in control.Branches)
            {
                if (admitted >= limits.MaxContexts) { limited = true; Reason("경로 대기열 상한"); return; }
                if (branch.HeaderEnd < 0 || branch.End < branch.Start) { Reason("미완결 분기"); return; }
                bool? known = branch.Else ? (bool?)true : LiteralBranch(job, syntax.Tokens, branch.Header, branch.HeaderEnd);
                if (known == false) continue;
                Job arm = job.Copy(); arm.At = branch.Start; arm.End = branch.End;
                if (speakerUnknown) arm.DialogueCharacter = null;
                arm.Frames.Add(new Frame { At = control.End + 1, End = job.End });
                arm.Identity += "/" + job.Script + ":branch:" + syntax.Tokens[branch.Start < syntax.Tokens.Count ? branch.Start : control.Start].Start;
                if (!known.HasValue)
                {
                    var bindings = Conditions(job);
                    var predicate = PreviewConditions.ParseTokens(syntax.Tokens, branch.Header, branch.HeaderEnd, bindings);
                    if (!predicate.IsKnownSyntax) { arm.Uncertain = true; arm.DialogueCharacter = null; speakerUnknown = true; }
                    arm.Gate = PreviewConditions.All(job.Gate, PreviewConditions.All(remaining, predicate));
                    remaining = PreviewConditions.All(remaining, PreviewConditions.Not(predicate));
                }
                else arm.Gate = PreviewConditions.All(job.Gate, remaining);
                Admit(arm); hasElse |= branch.Else;
                if (known == true) { provenTaken = true; break; }
            }
            if (!hasElse && !provenTaken)
            { var tail = job.Copy(); if (speakerUnknown) tail.DialogueCharacter = null; tail.At = control.End + 1; tail.Gate = PreviewConditions.All(job.Gate, remaining); Admit(tail); }
        }
        private static int EnvironmentCells(Job job)
        {
            if (job.Bindings.Count > MapRouteLimits.MaxBindings || job.Frames.Count > MapRouteLimits.MaxFrames || job.Steps.Count > MapRouteLimits.MaxChoiceSteps) return -1;
            int cells = job.Bindings.Count + job.Steps.Count; var seen = new HashSet<Value>();
            foreach (var value in job.Bindings.Values)
            {
                if (!seen.Add(value) || value.Table == null) continue;
                if (value.Table.Count > MapRouteLimits.MaxTableCells) return -1;
                cells += value.Table.Count + (value.TableGates == null ? 0 : value.TableGates.Count);
                if (cells > MapRouteLimits.MaxEnvironmentCells) return -1;
            }
            foreach (var frame in job.Frames)
            {
                cells += frame.Locals.Count + 1;
                if (cells > MapRouteLimits.MaxEnvironmentCells) return -1;
                foreach (var value in frame.Locals.Values)
                {
                    if (value == null || !seen.Add(value) || value.Table == null) continue;
                    if (value.Table.Count > MapRouteLimits.MaxTableCells) return -1;
                    cells += value.Table.Count + (value.TableGates == null ? 0 : value.TableGates.Count);
                    if (cells > MapRouteLimits.MaxEnvironmentCells) return -1;
                }
            }
            return cells;
        }
        private static Dictionary<string, PreviewCondition> Conditions(Job job)
        {
            var result = new Dictionary<string, PreviewCondition>(StringComparer.Ordinal);
            foreach (var p in job.Bindings) if (p.Value.Condition != null) result[p.Key] = p.Value.Condition;
            return result;
        }
        private void Finish(Job job)
        {
            if (job.Pending == null) return;
            if (job.Pending.Pool != null)
            {
                var next = job.Copy(); next.Pool = job.Pending.Pool; next.PoolCursor = 0; next.PoolItems = null;
                next.DialogueCharacter = null;
                next.Identity += "/pool:" + next.Pool; next.Path += " → " + next.Pool; Admit(next);
            }
            else if (!string.IsNullOrEmpty(job.Pending.Script))
                Admit(new Job { Script = job.Pending.Script, Depth = job.Depth + 1, Gate = job.Gate, Uncertain = job.Uncertain,
                    HasChoice = job.HasChoice, StoryChoice = job.StoryChoice, Steps = new List<MapChoiceStep>(job.Steps),
                    EntryGate = job.EntryGate, EntryUncertain = job.EntryUncertain,
                    InvalidManagers = new HashSet<string>(job.InvalidManagers, StringComparer.Ordinal),
                    Ancestors = new HashSet<string>(job.Ancestors, StringComparer.OrdinalIgnoreCase),
                    Identity = job.Identity + "/script:" + job.Pending.Script, Path = job.Path + " → " + job.Pending.Script });
        }
        private bool ProcessPool(Job job, float now, out int used)
        {
            used = 0;
            if (job.PoolItems == null)
            {
                if (!poolDefinitions.TryGetValue(job.Pool, out job.PoolItems))
                {
                    float retry; if (poolRetries.TryGetValue(job.Pool, out retry) && now < retry) return false;
                    if (poolBudget <= 0) return false;
                    poolBudget--;
                    used = 1; PoolReadCount++;
                    try
                    {
                        job.PoolItems = pools == null ? null : pools(job.Pool);
                        if (job.PoolItems == null) throw new InvalidOperationException();
                        poolDefinitions[job.Pool] = job.PoolItems; poolRetries.Remove(job.Pool);
                    }
                    catch (Exception) { job.PoolItems = null; poolRetries[job.Pool] = now + 5; return false; }
                }
            }
            used = 1;
            if (job.PoolCursor >= job.PoolItems.Count) return true;
            if (job.PoolCursor >= limits.MaxEntries) { limited = true; Reason("풀 항목 상한 · 일부 연결 확인 불가"); return true; }
            MapRouteEvent item = job.PoolItems[job.PoolCursor++];
            if (item == null || item.Target == null || !ValidScript(item.ResultScript))
            { Reason("이벤트 결과 연결 확인 불가"); return false; }
            MapRouteEntry entry = null;
            foreach (var candidate in entries) if (ReferenceEquals(candidate.Event, item.Target) && candidate.ResultScript == item.ResultScript) { entry = candidate; break; }
            if (entry == null)
            {
                if (entries.Count >= limits.MaxEntries) { limited = true; Reason("종단 항목 상한 · 일부 연결 확인 불가"); return true; }
                entries.Add(entry = new MapRouteEntry(item, job.Pool, () => IsPreparing || IsPartial));
            }
            if (!entry.Add(Path(job)))
            { limited = true; Reason("항목 경로 수 상한 · 생략된 경로 확인 불가"); }
            // Keep the terminal itself, then look for later menu/Position
            // routes. The intervening event may mutate any current-state
            // input, so descendants can never inherit current certainty.
            Admit(new Job { Script = item.ResultScript, Depth = job.Depth + 1, Gate = job.Gate, Uncertain = true,
                HasChoice = job.HasChoice, Steps = new List<MapChoiceStep>(job.Steps), EntryGate = job.EntryGate, EntryUncertain = job.EntryUncertain,
                InvalidManagers = new HashSet<string>(job.InvalidManagers, StringComparer.Ordinal),
                Ancestors = new HashSet<string>(job.Ancestors, StringComparer.OrdinalIgnoreCase),
                Identity = job.Identity + "/result:" + item.ResultScript, Path = job.Path + " → " + item.ResultScript });
            return false;
        }
    }
}
