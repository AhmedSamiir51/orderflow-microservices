using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Contracts.Inventory;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Application.Features.Stock.Commands.ReserveStock;

/// <summary>
/// Reserves stock for an order, all or nothing: if any line cannot be fulfilled nothing is
/// reserved and <see cref="StockReservationFailed"/> is published. Already-processed orders
/// are detected through the stored reservations, so a redelivered message just republishes
/// the original outcome instead of double-reserving.
/// </summary>
public sealed class ReserveStockHandler(
    IInventoryDbContext db,
    IPublishEndpoint publishEndpoint,
    TimeProvider time,
    ILogger<ReserveStockHandler> logger) : IRequestHandler<ReserveStockCommand>
{
    public async Task Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await db.Reservations
            .AnyAsync(r => r.OrderId == request.OrderId, cancellationToken);
        if (alreadyProcessed)
        {
            logger.LogInformation("Order {OrderId} was already reserved; republishing StockReserved", request.OrderId);
            await publishEndpoint.Publish(new StockReserved(request.OrderId, time.GetUtcNow()), cancellationToken);
            return;
        }

        var skus = request.Lines.Select(l => l.Sku).ToList();
        var items = await db.StockItems
            .Where(i => skus.Contains(i.Sku))
            .ToListAsync(cancellationToken);

        var failures = new List<string>();
        foreach (var line in request.Lines)
        {
            var item = items.FirstOrDefault(i => i.Sku == line.Sku);
            if (item is null)
            {
                failures.Add($"Unknown SKU '{line.Sku}'");
            }
            else if (!item.CanReserve(line.Quantity))
            {
                failures.Add($"Not enough stock for '{line.Sku}': requested {line.Quantity}, available {item.AvailableQuantity}");
            }
        }

        if (failures.Count > 0)
        {
            var reason = string.Join("; ", failures);
            await publishEndpoint.Publish(new StockReservationFailed(request.OrderId, reason, time.GetUtcNow()), cancellationToken);
            return;
        }

        foreach (var line in request.Lines)
        {
            var item = items.First(i => i.Sku == line.Sku);
            item.Reserve(line.Quantity);
            db.Reservations.Add(StockReservation.Create(request.OrderId, item, line.Quantity, time));
        }

        await publishEndpoint.Publish(new StockReserved(request.OrderId, time.GetUtcNow()), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
