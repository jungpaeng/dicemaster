using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Owns only lexical results. Readers must treat both the returned list and
    // its tokens as immutable: PreviewSyntax's List-based APIs do not mutate
    // them. In particular, apply traversal limits without removing tokens.
    // Current state, branch paths, read failures and analysis results never
    // belong here. The owner must clear/discard this cache with its context.
    public sealed class PreviewParseCache
    {
        public const int DefaultMaxEntries = 64;
        public const int DefaultMaxCharacters = 1048576;
        public const int DefaultMaxTokens = 262144;

        sealed class Entry
        {
            internal string Source;
            internal List<PreviewToken> Tokens;
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
                Evictions++;
            }

            var node = new LinkedListNode<Entry>(new Entry { Source = source, Tokens = tokens });
            recent.AddFirst(node);
            entries.Add(source, node);
            CharacterCount += source.Length;
            TokenCount += tokens.Count;
            return tokens;
        }

        // Lifetime work counters survive a context reset, while every source
        // and token reference held by the cache is released.
        public void Clear()
        {
            entries.Clear();
            recent.Clear();
            CharacterCount = 0;
            TokenCount = 0;
        }
    }
}
