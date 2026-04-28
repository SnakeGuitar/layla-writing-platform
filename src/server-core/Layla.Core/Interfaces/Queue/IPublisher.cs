namespace Layla.Core.Interfaces.Queue;

public interface IPublisher
{
    /// <summary>
    /// Publishes an event to the message broker.
    /// Returns <c>true</c> if the event was published successfully; <c>false</c> if the broker
    /// is unavailable or the publish operation failed.
    /// </summary>
    void Publish<T>(T @event, string routingKey);
}
