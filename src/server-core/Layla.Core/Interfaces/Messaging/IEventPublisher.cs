namespace Layla.Core.Interfaces.Messaging;

/// <summary>
/// Extends IEventBus with async publishing capabilities.
/// Provides both synchronous (Publish) and asynchronous (PublishAsync) event publishing methods.
/// </summary>
public interface IEventPublisher : IEventBus
{
    /// <summary>
    /// Publishes an event asynchronously (async wrapper around synchronous Publish).
    /// Returns <c>true</c> if the event was published successfully; <c>false</c> if the broker
    /// is unavailable or the publish operation failed.
    /// </summary>
    Task<bool> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
