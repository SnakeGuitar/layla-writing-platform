using System.Reflection;
using System.Text.Json;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Queue;
using Layla.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Layla.Core.Tests;

// ── factory ───────────────────────────────────────────────────────────────────

file static class WorkerSutFactory
{
    internal record Components(
        IOutboxRepository OutboxRepo,
        IPublisher Publisher,
        OutboxProcessorWorker Sut);

    internal static Components Create()
    {
        var outboxRepo = Substitute.For<IOutboxRepository>();
        var publisher = Substitute.For<IPublisher>();

        // Inner scope provider — resolves IOutboxRepository and IPublisher
        var scopeProvider = Substitute.For<IServiceProvider>();
        scopeProvider.GetService(typeof(IOutboxRepository)).Returns(outboxRepo);
        scopeProvider.GetService(typeof(IPublisher)).Returns(publisher);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopeProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        // Root provider — resolves IServiceScopeFactory (used by CreateScope() extension)
        var rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        return new(outboxRepo, publisher,
            new OutboxProcessorWorker(rootProvider, NullLogger<OutboxProcessorWorker>.Instance));
    }

    // ProcessOutboxMessagesAsync is private — invoke via reflection
    internal static Task ProcessAsync(OutboxProcessorWorker worker) =>
        (Task)typeof(OutboxProcessorWorker)
            .GetMethod("ProcessOutboxMessagesAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(worker, [CancellationToken.None])!;

    internal static OutboxMessage MakeClientEvictedMessage(Guid projectId, string userId) =>
        new()
        {
            EventType = "ClientEvicted",
            Payload = JsonSerializer.Serialize(new ClientEvictedEvent { ProjectId = projectId, UserId = userId }),
        };
}

// ── no messages — publisher is not called ─────────────────────────────────────

public class OutboxProcessor_WhenNoMessages_DoesNotCallPublisher
{
    private readonly IPublisher _publisher;

    public OutboxProcessor_WhenNoMessages_DoesNotCallPublisher()
    {
        var c = WorkerSutFactory.Create();
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void PublishIsNeverCalled() =>
        _publisher.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<string>());
}

// ── no messages — SaveChanges is not called ───────────────────────────────────

public class OutboxProcessor_WhenNoMessages_DoesNotCallSaveChanges
{
    private readonly IOutboxRepository _outboxRepo;

    public OutboxProcessor_WhenNoMessages_DoesNotCallSaveChanges()
    {
        var c = WorkerSutFactory.Create();
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _outboxRepo = c.OutboxRepo;
    }

    [Fact]
    public void SaveChangesIsNeverCalled() =>
        _outboxRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
}

// ── ClientEvicted — publish is called ─────────────────────────────────────────

public class OutboxProcessor_ClientEvicted_CallsPublish
{
    private readonly IPublisher _publisher;

    public OutboxProcessor_ClientEvicted_CallsPublish()
    {
        var c = WorkerSutFactory.Create();
        var msg = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void PublishIsCalledOnce() =>
        _publisher.Received(1).Publish(Arg.Any<ClientEvictedEvent>(), Arg.Any<string>());
}

// ── ClientEvicted — routing key is "client.evicted" ──────────────────────────

public class OutboxProcessor_ClientEvicted_RoutingKeyIsCorrect
{
    private readonly IPublisher _publisher;

    public OutboxProcessor_ClientEvicted_RoutingKeyIsCorrect()
    {
        var c = WorkerSutFactory.Create();
        var msg = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void RoutingKeyIsClientEvicted() =>
        _publisher.Received(1).Publish(Arg.Any<ClientEvictedEvent>(), "client.evicted");
}

// ── ClientEvicted — projectId is forwarded correctly ─────────────────────────

public class OutboxProcessor_ClientEvicted_PublishesCorrectProjectId
{
    private readonly IPublisher _publisher;
    private readonly Guid _expectedProjectId = Guid.NewGuid();

    public OutboxProcessor_ClientEvicted_PublishesCorrectProjectId()
    {
        var c = WorkerSutFactory.Create();
        var msg = WorkerSutFactory.MakeClientEvictedMessage(_expectedProjectId, "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void ProjectIdMatchesPayload() =>
        _publisher.Received(1).Publish(
            Arg.Is<ClientEvictedEvent>(e => e.ProjectId == _expectedProjectId),
            Arg.Any<string>());
}

// ── ClientEvicted — userId is forwarded correctly ─────────────────────────────

public class OutboxProcessor_ClientEvicted_PublishesCorrectUserId
{
    private readonly IPublisher _publisher;

    public OutboxProcessor_ClientEvicted_PublishesCorrectUserId()
    {
        var c = WorkerSutFactory.Create();
        var msg = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "user-42");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void UserIdMatchesPayload() =>
        _publisher.Received(1).Publish(
            Arg.Is<ClientEvictedEvent>(e => e.UserId == "user-42"),
            Arg.Any<string>());
}

// ── ClientEvicted — message is marked as processed ───────────────────────────

public class OutboxProcessor_ClientEvicted_MarksMessageAsProcessed
{
    private readonly OutboxMessage _message;

    public OutboxProcessor_ClientEvicted_MarksMessageAsProcessed()
    {
        var c = WorkerSutFactory.Create();
        _message = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([_message]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
    }

    [Fact] public void ProcessedIsTrue() => Assert.True(_message.Processed);
}

// ── ClientEvicted — SaveChanges is called after the batch ────────────────────

public class OutboxProcessor_ClientEvicted_CallsSaveChanges
{
    private readonly IOutboxRepository _outboxRepo;

    public OutboxProcessor_ClientEvicted_CallsSaveChanges()
    {
        var c = WorkerSutFactory.Create();
        var msg = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _outboxRepo = c.OutboxRepo;
    }

    [Fact]
    public void SaveChangesIsCalledOnce() =>
        _outboxRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
}

// ── unknown event type — publisher is not called ──────────────────────────────

public class OutboxProcessor_UnknownEventType_DoesNotCallPublisher
{
    private readonly IPublisher _publisher;

    public OutboxProcessor_UnknownEventType_DoesNotCallPublisher()
    {
        var c = WorkerSutFactory.Create();
        var msg = new OutboxMessage { EventType = "ProjectDeleted", Payload = "{}" };
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([msg]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _publisher = c.Publisher;
    }

    [Fact]
    public void PublishIsNeverCalled() =>
        _publisher.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<string>());
}

// ── unknown event type — message is still marked processed ───────────────────

public class OutboxProcessor_UnknownEventType_MarksMessageAsProcessed
{
    private readonly OutboxMessage _message;

    public OutboxProcessor_UnknownEventType_MarksMessageAsProcessed()
    {
        var c = WorkerSutFactory.Create();
        _message = new OutboxMessage { EventType = "ProjectDeleted", Payload = "{}" };
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([_message]);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
    }

    [Fact] public void ProcessedIsTrue() => Assert.True(_message.Processed);
}

// ── publisher throws — message remains unprocessed ───────────────────────────

public class OutboxProcessor_WhenPublisherThrows_MessageRemainsUnprocessed
{
    private readonly OutboxMessage _message;

    public OutboxProcessor_WhenPublisherThrows_MessageRemainsUnprocessed()
    {
        var c = WorkerSutFactory.Create();
        _message = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([_message]);
        c.Publisher
            .When(p => p.Publish(Arg.Any<ClientEvictedEvent>(), Arg.Any<string>()))
            .Do(_ => throw new Exception("broker unavailable"));
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
    }

    [Fact] public void ProcessedIsFalse() => Assert.False(_message.Processed);
}

// ── publisher throws on first — second message is still processed ─────────────

public class OutboxProcessor_WhenPublisherThrows_RemainingMessageIsStillProcessed
{
    private readonly OutboxMessage _secondMessage;

    public OutboxProcessor_WhenPublisherThrows_RemainingMessageIsStillProcessed()
    {
        var c = WorkerSutFactory.Create();
        var first = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1");
        _secondMessage = WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u2");

        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first, _secondMessage]);

        var throwOnFirst = true;
        c.Publisher
            .When(p => p.Publish(Arg.Any<ClientEvictedEvent>(), Arg.Any<string>()))
            .Do(_ =>
            {
                if (throwOnFirst) { throwOnFirst = false; throw new Exception("broker unavailable"); }
            });

        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
    }

    [Fact] public void SecondMessageIsProcessed() => Assert.True(_secondMessage.Processed);
}

// ── multiple messages — SaveChanges is called exactly once ───────────────────

public class OutboxProcessor_MultipleMessages_SaveChangesCalledOnce
{
    private readonly IOutboxRepository _outboxRepo;

    public OutboxProcessor_MultipleMessages_SaveChangesCalledOnce()
    {
        var c = WorkerSutFactory.Create();
        var msgs = new[]
        {
            WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u1"),
            WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u2"),
            WorkerSutFactory.MakeClientEvictedMessage(Guid.NewGuid(), "u3"),
        };
        c.OutboxRepo.GetUnprocessedMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(msgs);
        WorkerSutFactory.ProcessAsync(c.Sut).GetAwaiter().GetResult();
        _outboxRepo = c.OutboxRepo;
    }

    [Fact]
    public void SaveChangesCalledOnce() =>
        _outboxRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
}
