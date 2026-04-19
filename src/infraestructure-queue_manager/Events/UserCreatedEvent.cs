namespace QueueManager.Messaging.Events;

public record UserCreatedEvent(
    Guid UserId,
    string Email,
    string Name,
    DateTimeOffset CreatedAt
);