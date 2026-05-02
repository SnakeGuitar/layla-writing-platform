using System.Text;
using Layla.Core.Interfaces.Queue;
using Microsoft.Extensions.Logging;

namespace Layla.Infrastructure.Queue;

/// <summary>
/// Adapter that exposes the legacy <see cref="IEventPublisher"/> / <see cref="IEventBus"/>
/// contract on top of the new <see cref="IPublisher"/> infrastructure.
///
/// The original <c>EventBus</c> implementation was removed during the RabbitMQ
/// refactor (commit c379fea). Application services such as <c>ProjectService</c>
/// still depend on <see cref="IEventPublisher"/>, so this adapter bridges the gap
/// without forcing a service-layer rewrite.
///
/// Behaviour:
/// - <see cref="Publish{T}"/> delegates straight to <see cref="IPublisher.Publish{T}"/>
///   using the routing key supplied by the caller. The exchange parameter is
///   accepted for backwards compatibility but ignored — the underlying
///   <see cref="Publisher"/> is bound to the exchange configured in
///   <c>RabbitMQ:Exchange</c> at construction time.
/// - <see cref="PublishAsync{TEvent}"/> derives the routing key from the event
///   type name (e.g. <c>ProjectCreatedEvent</c> → <c>project.created</c>),
///   matching the convention used by the original EventBus and consumed by
///   the Node.js worldbuilding service.
/// </summary>
public sealed class EventBusAdapter : IEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<EventBusAdapter> _logger;

    public EventBusAdapter(IPublisher publisher, ILogger<EventBusAdapter> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public bool Publish<T>(T @event, string exchangeName, string routingKey = "") where T : class
    {
        try
        {
            _publisher.Publish(@event, routingKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} with routing key '{RoutingKey}'",
                typeof(T).Name, routingKey);
            return false;
        }
    }

    public Task<bool> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        var routingKey = DeriveRoutingKey(typeof(TEvent).Name);
        return Task.FromResult(Publish(@event, exchangeName: string.Empty, routingKey: routingKey));
    }

    /// <summary>
    /// Converts a PascalCase event class name into a dotted lower-case routing key,
    /// stripping a trailing <c>Event</c> suffix if present.
    /// Example: <c>ProjectCreatedEvent</c> → <c>project.created</c>.
    /// </summary>
    private static string DeriveRoutingKey(string typeName)
    {
        const string suffix = "Event";
        if (typeName.EndsWith(suffix, StringComparison.Ordinal))
            typeName = typeName[..^suffix.Length];

        var sb = new StringBuilder(typeName.Length + 4);
        for (int i = 0; i < typeName.Length; i++)
        {
            if (i > 0 && char.IsUpper(typeName[i]))
                sb.Append('.');
            sb.Append(char.ToLowerInvariant(typeName[i]));
        }
        return sb.ToString();
    }
}
