using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Owns context-independent lexical, syntax and constant-validation results.
    // Readers must treat returned token lists as immutable; traversal prefixes
    // need a private plan. Current state, branch identities, read failures and
    // analysis results never belong here. Clear with the owning game context.
    public sealed class PreviewParseCache
    {
        public const int DefaultMaxEntries = 64;
        // Measured shared-followup working set: 1,680,299 chars / 294,681
        // tokens. 2 Mi / 512 Ki fits it; 4 Mi adds no reuse in that sample.
        // These ownership bounds are independent of per-analysis trace limits.
        public const int DefaultMaxCharacters = 2097152;
        public const int DefaultMaxTokens = 524288;

        sealed class Entry
        {
            internal string Source;
            internal List<PreviewToken> Tokens;
            internal PreviewScriptPlan Plan;
        }

        readonly int maxEntries, maxCharacters, maxTokens;
        readonly Dictionary<string, LinkedListNode<Entry>> entries =
            new Dictionary<string, LinkedListNode<Entry>>(StringComparer.Ordinal);
        readonly LinkedList<Entry> recent = new LinkedList<Entry>();

        public int Entries { get { return entries.Count; } }
        public int CharacterCount { get; private set; }
        public int TokenCount { get; private set; }
        public long Tokenizations { get; private set; }
        public long CacheHits { get; private set; }
        public long Bypasses { get; private set; }
        public long Evictions { get; private set; }
        public long PlanBuilds { get; private set; }
        public long PlanHits { get; private set; }

        long retiredSyntaxBuilds, retiredSyntaxHits, retiredValidationBuilds, retiredValidationHits;
        public long SyntaxBuilds { get { long value = retiredSyntaxBuilds; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.SyntaxBuilds; return value; } }
        public long SyntaxHits { get { long value = retiredSyntaxHits; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.SyntaxHits; return value; } }
        public long ValidationBuilds { get { long value = retiredValidationBuilds; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.ValidationBuilds; return value; } }
        public long ValidationHits { get { long value = retiredValidationHits; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.ValidationHits; return value; } }
        public long MemoUnits { get { long value = 0; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.MemoUnits; return value; } }
        public long MemoCharacters { get { long value = 0; foreach (Entry entry in recent) if (entry.Plan != null) value += entry.Plan.MemoCharacters; return value; } }

        public PreviewParseCache() : this(DefaultMaxEntries, DefaultMaxCharacters, DefaultMaxTokens) { }

        public PreviewParseCache(int maxEntries, int maxCharacters, int maxTokens)
        {
            if (maxEntries < 0) throw new ArgumentOutOfRangeException("maxEntries");
            if (maxCharacters < 0) throw new ArgumentOutOfRangeException("maxCharacters");
            if (maxTokens < 0) throw new ArgumentOutOfRangeException("maxTokens");
            this.maxEntries = maxEntries;
            this.maxCharacters = maxCharacters;
            this.maxTokens = maxTokens;
        }

        // Exact ordinal source equality is the identity, rather than a story
        // name, path or hash alone. Different text can never alias on a hash
        // collision. No source text or keys are exposed by cache diagnostics.
        public List<PreviewToken> Tokens(string source)
        {
            source = source ?? "";
            if (entries.TryGetValue(source, out LinkedListNode<Entry> found))
            {
                CacheHits++;
                recent.Remove(found);
                recent.AddFirst(found);
                return found.Value.Tokens;
            }

            Tokenizations++;
            var tokens = PreviewSyntax.Tokenize(source);
            // Oversized input still has the same lexical result. It simply
            // cannot displace the reusable entries or enter the finite cache.
            if (maxEntries == 0 || source.Length > maxCharacters || tokens.Count > maxTokens)
            {
                Bypasses++;
                return tokens;
            }

            while (entries.Count >= maxEntries || source.Length > maxCharacters - CharacterCount || tokens.Count > maxTokens - TokenCount)
            {
                var oldest = recent.Last;
                recent.RemoveLast();
                entries.Remove(oldest.Value.Source);
                CharacterCount -= oldest.Value.Source.Length;
                TokenCount -= oldest.Value.Tokens.Count;
                Retire(oldest.Value.Plan);
                Evictions++;
            }

            var node = new LinkedListNode<Entry>(new Entry { Source = source, Tokens = tokens });
            recent.AddFirst(node);
            entries.Add(source, node);
            CharacterCount += source.Length;
            TokenCount += tokens.Count;
            return tokens;
        }

        public PreviewScriptPlan Plan(string source)
        {
            source = source ?? "";
            List<PreviewToken> tokens = Tokens(source);
            LinkedListNode<Entry> owned;
            if (entries.TryGetValue(source, out owned) && owned.Value.Plan != null)
            { PlanHits++; return owned.Value.Plan; }
            PlanBuilds++;
            var plan = new PreviewScriptPlan(source, tokens);
            if (owned != null) owned.Value.Plan = plan;
            return plan;
        }

        void Retire(PreviewScriptPlan plan)
        {
            if (plan == null) return;
            retiredSyntaxBuilds += plan.SyntaxBuilds; retiredSyntaxHits += plan.SyntaxHits;
            retiredValidationBuilds += plan.ValidationBuilds; retiredValidationHits += plan.ValidationHits;
        }

        // Lifetime work counters survive a context reset, while every source
        // and token reference held by the cache is released.
        public void Clear()
        {
            foreach (Entry entry in recent) Retire(entry.Plan);
            entries.Clear();
            recent.Clear();
            CharacterCount = 0;
            TokenCount = 0;
        }
    }
}
