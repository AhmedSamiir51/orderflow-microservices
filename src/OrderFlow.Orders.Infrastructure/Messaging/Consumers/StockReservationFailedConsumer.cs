using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts.Inventory;
using OrderFlow.Contracts.Orders;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Domain.Orders;
using OrderFlow.Orders.Infrastructure.Persistence;

namespace OrderFlow.Orders.Infrastructure.Messaging.Consumers;

/// <summary>Rejects the order when the Inventory service cannot reserve stock. Idempotent.</summary>
public sealed class StockReservationFailedConsumer(
    OrdersDbContext db,
    IEventPublisher eventPublisher,
    TimeProvider time,
    ILogger<StockReservationFailedConsumer> logger) : IConsumer<StockReservationFailed>
{
    public async Task Consume(ConsumeContext<StockReservationFailed> context)
    {
        var message = context.Message;
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("StockReservationFailed received for unknown order {OrderId}; ignoring", message.OrderId);
            return;
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Rejected))
        {
            logger.LogWarning("StockReservationFailed arrived for order {OrderId} in state {Status}; ignoring", order.Id, order.Status);
            return;
        }

        order.Reject(message.Reason, time);

        await eventPublisher.Publish(new OrderRejected(order.Id, order.CustomerEmail, message.Reason, time.GetUtcNow()), context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Order {OrderId} rejected: {Reason}", order.Id, message.Reason);
    }
}
