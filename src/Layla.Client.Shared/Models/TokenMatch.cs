namespace Layla.Client.Shared.Models;

/// <summary>
/// Result of a single match found by the Aho-Corasick tokenizer in a text body.
/// </summary>
public readonly record struct TokenMatch(
    /// <summary>Zero-based character offset where the matched keyword starts.</summary>
    int StartIndex,
    /// <summary>Zero-based character offset just past the last character of the match.</summary>
    int EndIndex,
    /// <summary>Entity ID from the wiki.</summary>
    string EntityId,
    /// <summary>Entity type (Character, Location, etc.).</summary>
    string EntityType,
    /// <summary>The exact surface form that was matched in the text.</summary>
    string MatchedText
);
