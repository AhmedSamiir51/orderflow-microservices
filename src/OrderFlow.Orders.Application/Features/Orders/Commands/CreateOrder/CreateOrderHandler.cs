using MassTransit;
using MediatR;
using OrderFlow.Contracts.Orders;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Creates the order and publishes <see cref="OrderCreated"/>. The publish goes through the
/// transactional outbox, so the event is stored in the same transaction as the order and is
/// delivered to RabbitMQ only after the commit succeeds.
/// </summary>
public sealed class CreateOrderHandler(IOrdersDbContext db, IPublishEndpoint publishEndpoint, TimeProvider time)
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Create(
            request.CustomerEmail,
            request.Lines.Select(l => (l.Sku, l.Quantity, l.UnitPrice)),
            time);

        db.Orders.Add(order);

        await publishEndpoint.Publish(
            new OrderCreated(
                order.Id,
                order.CustomerEmail,
                order.Lines.Select(l => new OrderLineItem(l.Sku, l.Quantity)).ToList(),
                time.GetUtcNow()),
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return OrderDto.FromDomain(order);
    }
}
