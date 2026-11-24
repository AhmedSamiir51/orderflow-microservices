using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts.Inventory;
using OrderFlow.Contracts.Orders;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Domain.Orders;
using OrderFlow.Orders.Infrastructure.Persistence;

namespace OrderFlow.Orders.Infrastructure.Messaging.Consumers;

/// <summary>
/// Completes the order when the Inventory service reports stock reserved.
/// Idempotent: a redelivered message leaves an already-confirmed order untouched.
/// </summary>
public sealed class StockReservedConsumer(
    OrdersDbContext db,
    IEventPublisher eventPublisher,
    TimeProvider time,
    ILogger<StockReservedConsumer> logger) : IConsumer<StockReserved>
{
    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var message = context.Message;
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);

        if (order is null)
        {
            logger.LogWarning("StockReserved received for unknown order {OrderId}; ignoring", message.OrderId);
            return;
        }

        if (order.Status is OrderStatus.Cancelled)
        {
            logger.LogWarning("StockReserved arrived for cancelled order {OrderId}; stock reservation needs manual release", order.Id);
            return;
        }

        order.Confirm(time);

        await eventPublisher.Publish(new OrderConfirmed(order.Id, order.CustomerEmail, time.GetUtcNow()), context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Order {OrderId} confirmed", order.Id);
    }
}
