using Layla.Core.Interfaces.Messaging;
using Microsoft.Extensions.Logging;

namespace Layla.Infrastructure.Messaging;

public class DummyEventPublisher : IEventPublisher
{
    private readonly ILogger<DummyEventPublisher> _logger;

    public DummyEventPublisher(ILogger<DummyEventPublisher> logger)
    {
        _logger = logger;
    }

    public bool Publish<T>(T @event, string exchangeName, string routingKey = "") where T : class
    {
        _logger.LogInformation("Dummy Publish: Event {EventType} published to {ExchangeName}/{RoutingKey}.", typeof(T).Name, exchangeName, routingKey);
        return true;
    }

    public Task<bool> PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        _logger.LogInformation("Dummy PublishAsync: Event {EventType} published.", typeof(TEvent).Name);
        return Task.FromResult(true);
    }
}
