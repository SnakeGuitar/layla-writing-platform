namespace Layla.Core.Interfaces.Queue;

public interface IEventBus
{
    /// <summary>
    /// Publishes an event to the message broker.
    /// Returns <c>true</c> if the event was published successfully; <c>false</c> if the broker
    /// is unavailable or the publish operation failed.
    /// </summary>
    bool Publish<T>(T @event, string exchangeName, string routingKey = "") where T : class;
}
