using System.Collections.Generic;

namespace Layla.Client.Shared.Models;

/// <summary>
/// Lightweight DTO returned by the <c>/api/wiki/:projectId/detectable</c> endpoint.
/// Used to initialise the Aho-Corasick tokenizer on the client.
/// </summary>
public class DetectableEntity
{
    public string Id { get; set; } = string.Empty;
    public string MainToken { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public string Type { get; set; } = string.Empty;
}
