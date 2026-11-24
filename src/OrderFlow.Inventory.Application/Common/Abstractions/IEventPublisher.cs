namespace OrderFlow.Inventory.Application.Common.Abstractions;

/// <summary>
/// Publishes integration events to the message broker. Implemented in Infrastructure on top of
/// MassTransit; messages are stored in the transactional outbox and delivered after commit.
/// </summary>
public interface IEventPublisher
{
    Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
