using System;
using System.Collections.Generic;
using System.Linq;
using Layla.Client.Shared.Models;

namespace Layla.Client.Shared.Tokenizer;

/// <summary>
/// High-performance multi-pattern search engine using the Aho-Corasick algorithm.
/// Scans a text body in O(n + m) time for all wiki entity names and aliases,
/// returning their positions so the UI can render them as interactive hyperlinks.
///
/// Usage:
/// <code>
///   var tokenizer = new WikiTokenizer();
///   tokenizer.Build(detectableEntities);
///   var matches = tokenizer.FindMentions("The hero Menudo walked into town.");
/// </code>
///
/// Thread-safety: Once <see cref="Build"/> completes, <see cref="FindMentions"/>
/// may be called concurrently from any thread.  Do NOT call <see cref="Build"/>
/// while other threads are calling <see cref="FindMentions"/>.
/// </summary>
public sealed class WikiTokenizer
{
    /// <summary>
    /// Internal trie node for the Aho-Corasick automaton.
    /// </summary>
    private sealed class TrieNode
    {
        internal readonly Dictionary<char, TrieNode> Children = new();
        internal TrieNode? Failure;
        internal TrieNode? DictSuffix; // dictionary-suffix link for fast output traversal
        internal readonly List<(string EntityId, string EntityType, string Pattern)> Outputs = new();
    }

    private TrieNode _root = new();
    private bool _built;

    /// <summary>
    /// Builds (or rebuilds) the Aho-Corasick automaton from the given entities.
    /// Each entity contributes its <see cref="DetectableEntity.MainToken"/> plus
    /// all <see cref="DetectableEntity.Aliases"/> as search patterns.
    /// Patterns are matched case-insensitively.
    /// </summary>
    public void Build(IReadOnlyList<DetectableEntity> entities)
    {
        _root = new TrieNode();
        _built = false;

        foreach (var entity in entities)
        {
            InsertPattern(entity.MainToken, entity.Id, entity.Type);
            foreach (var alias in entity.Aliases)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    InsertPattern(alias, entity.Id, entity.Type);
            }
        }

        BuildFailureLinks();
        _built = true;
    }

    /// <summary>
    /// Scans <paramref name="text"/> and returns all positions where wiki entities
    /// were detected.  Only whole-word matches are reported (bounded by whitespace,
    /// punctuation, or start/end of string).
    /// </summary>
    public List<TokenMatch> FindMentions(string text)
    {
        if (!_built || string.IsNullOrEmpty(text))
            return new List<TokenMatch>();

        var results = new List<TokenMatch>();
        var lowerText = text.ToLowerInvariant();
        var current = _root;

        for (int i = 0; i < lowerText.Length; i++)
        {
            char c = lowerText[i];

            while (current != _root && !current.Children.ContainsKey(c))
                current = current.Failure!;

            if (current.Children.TryGetValue(c, out var next))
                current = next;
            else
                continue;

            // Collect outputs from this node and all dictionary-suffix links
            CollectOutputs(current, i, text, lowerText, results);
        }

        // Deduplicate overlapping matches: prefer longest match at each start position
        return DeduplicateMatches(results);
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private void InsertPattern(string pattern, string entityId, string entityType)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;

        var lower = pattern.ToLowerInvariant();
        var node = _root;

        foreach (char c in lower)
        {
            if (!node.Children.TryGetValue(c, out var child))
            {
                child = new TrieNode();
                node.Children[c] = child;
            }
            node = child;
        }

        node.Outputs.Add((entityId, entityType, pattern));
    }

    private void BuildFailureLinks()
    {
        var queue = new Queue<TrieNode>();

        // Depth-1 nodes: failure → root
        foreach (var child in _root.Children.Values)
        {
            child.Failure = _root;
            child.DictSuffix = null;
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var (ch, child) in current.Children)
            {
                var fail = current.Failure;
                while (fail != _root && !fail!.Children.ContainsKey(ch))
                    fail = fail.Failure;

                child.Failure = fail!.Children.TryGetValue(ch, out var f) && f != child ? f : _root;

                // Dictionary-suffix link: nearest ancestor (via failure chain) that has outputs
                child.DictSuffix = child.Failure.Outputs.Count > 0
                    ? child.Failure
                    : child.Failure.DictSuffix;

                queue.Enqueue(child);
            }
        }
    }

    private static void CollectOutputs(
        TrieNode node, int endIndex, string originalText, string lowerText, List<TokenMatch> results)
    {
        var n = node;
        while (n != null)
        {
            foreach (var (entityId, entityType, pattern) in n.Outputs)
            {
                int startIndex = endIndex - pattern.Length + 1;
                if (startIndex < 0) continue;

                // Whole-word boundary check
                if (IsWordBoundary(lowerText, startIndex, endIndex))
                {
                    results.Add(new TokenMatch(
                        startIndex,
                        endIndex + 1,
                        entityId,
                        entityType,
                        originalText.Substring(startIndex, pattern.Length)
                    ));
                }
            }
            n = n.DictSuffix;
        }
    }

    private static bool IsWordBoundary(string text, int start, int end)
    {
        // Check left boundary
        if (start > 0)
        {
            char left = text[start - 1];
            if (char.IsLetterOrDigit(left)) return false;
        }

        // Check right boundary
        if (end < text.Length - 1)
        {
            char right = text[end + 1];
            if (char.IsLetterOrDigit(right)) return false;
        }

        return true;
    }

    private static List<TokenMatch> DeduplicateMatches(List<TokenMatch> matches)
    {
        if (matches.Count <= 1) return matches;

        // Group by start index, keep the longest match at each position
        return matches
            .GroupBy(m => m.StartIndex)
            .Select(g => g.OrderByDescending(m => m.EndIndex - m.StartIndex).First())
            .OrderBy(m => m.StartIndex)
            .ToList();
    }
}
