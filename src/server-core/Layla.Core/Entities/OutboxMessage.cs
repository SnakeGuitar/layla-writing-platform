using System;

namespace Layla.Core.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty; // e.g., "ClientEvicted"
    public string Payload { get; set; } = string.Empty; // JSON serialized payload
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; } = false;
}
