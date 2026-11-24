using MassTransit;
using OrderFlow.Orders.Application.Common.Abstractions;

namespace OrderFlow.Orders.Infrastructure.Messaging;

/// <summary>Routes application events through MassTransit (and therefore through the outbox).</summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class =>
        publishEndpoint.Publish(message, cancellationToken);
}
