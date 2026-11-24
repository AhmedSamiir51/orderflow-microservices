using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts.Orders;
using OrderFlow.Inventory.Application.Features.Stock.Commands.ReserveStock;

namespace OrderFlow.Inventory.Infrastructure.Messaging.Consumers;

/// <summary>
/// Entry point of the reservation flow: translates the integration event into an
/// application command. The consumer outbox makes the command's publishes and the
/// database write commit together, and the inbox deduplicates redeliveries.
/// </summary>
public sealed class OrderCreatedConsumer(ISender mediator, ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        logger.LogInformation("Reserving stock for order {OrderId} ({LineCount} lines)", message.OrderId, message.Lines.Count);

        await mediator.Send(
            new ReserveStockCommand(
                message.OrderId,
                message.Lines.Select(l => new ReserveStockLine(l.Sku, l.Quantity)).ToList()),
            context.CancellationToken);
    }
}
