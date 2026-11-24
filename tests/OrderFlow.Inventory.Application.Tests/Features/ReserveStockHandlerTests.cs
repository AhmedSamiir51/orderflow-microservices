using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Inventory;
using OrderFlow.Inventory.Application.Features.Stock.Commands.ReserveStock;
using OrderFlow.Inventory.Application.Tests.Support;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Application.Tests.Features;

public sealed class ReserveStockHandlerTests
{
    private static async Task<TestDb> SeedAsync(params (string Sku, int Quantity)[] items)
    {
        var db = await TestDb.CreateAsync();
        foreach (var (sku, quantity) in items)
        {
            db.Context.StockItems.Add(StockItem.Create(sku, $"Item {sku}", quantity));
        }

        await db.Context.SaveChangesAsync();
        return db;
    }

    private static ReserveStockHandler Handler(TestDb db, RecordingEventPublisher publisher) =>
        new(db.Context, publisher, TimeProvider.System, new NullLogger<ReserveStockHandler>());

    [Fact]
    public async Task Reserves_every_line_and_publishes_StockReserved()
    {
        await using var db = await SeedAsync(("SKU-1", 10), ("SKU-2", 5));
        var publisher = new RecordingEventPublisher();
        var orderId = Guid.NewGuid();

        await Handler(db, publisher).Handle(new ReserveStockCommand(orderId,
            [new ReserveStockLine("SKU-1", 4), new ReserveStockLine("SKU-2", 5)]), CancellationToken.None);

        var reserved = Assert.Single(publisher.OfType<StockReserved>());
        Assert.Equal(orderId, reserved.OrderId);

        var items = await db.Context.StockItems.AsNoTracking().OrderBy(i => i.Sku).ToListAsync();
        Assert.Equal(6, items[0].AvailableQuantity);
        Assert.Equal(4, items[0].ReservedQuantity);
        Assert.Equal(0, items[1].AvailableQuantity);
        Assert.Equal(5, items[1].ReservedQuantity);

        Assert.Equal(2, await db.Context.Reservations.CountAsync(r => r.OrderId == orderId));
    }

    [Fact]
    public async Task Publishes_failure_and_reserves_nothing_when_a_line_is_short()
    {
        await using var db = await SeedAsync(("SKU-1", 10), ("SKU-2", 1));
        var publisher = new RecordingEventPublisher();
        var orderId = Guid.NewGuid();

        await Handler(db, publisher).Handle(new ReserveStockCommand(orderId,
            [new ReserveStockLine("SKU-1", 4), new ReserveStockLine("SKU-2", 5)]), CancellationToken.None);

        var failure = Assert.Single(publisher.OfType<StockReservationFailed>());
        Assert.Equal(orderId, failure.OrderId);
        Assert.Contains("SKU-2", failure.Reason);

        // all-or-nothing: even the satisfiable line stays untouched
        var item = await db.Context.StockItems.AsNoTracking().SingleAsync(i => i.Sku == "SKU-1");
        Assert.Equal(10, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(0, await db.Context.Reservations.CountAsync());
    }

    [Fact]
    public async Task Publishes_failure_for_an_unknown_sku()
    {
        await using var db = await SeedAsync(("SKU-1", 10));
        var publisher = new RecordingEventPublisher();

        await Handler(db, publisher).Handle(new ReserveStockCommand(Guid.NewGuid(),
            [new ReserveStockLine("NOPE", 1)]), CancellationToken.None);

        var failure = Assert.Single(publisher.OfType<StockReservationFailed>());
        Assert.Contains("Unknown SKU 'NOPE'", failure.Reason);
    }

    [Fact]
    public async Task Redelivered_OrderCreated_republishes_without_double_reserving()
    {
        await using var db = await SeedAsync(("SKU-1", 10));
        var publisher = new RecordingEventPublisher();
        var orderId = Guid.NewGuid();
        var command = new ReserveStockCommand(orderId, [new ReserveStockLine("SKU-1", 4)]);

        await Handler(db, publisher).Handle(command, CancellationToken.None);
        await Handler(db, publisher).Handle(command, CancellationToken.None); // broker redelivery

        var item = await db.Context.StockItems.AsNoTracking().SingleAsync(i => i.Sku == "SKU-1");
        Assert.Equal(6, item.AvailableQuantity);
        Assert.Equal(4, item.ReservedQuantity);
        Assert.Equal(2, publisher.OfType<StockReserved>().Count);
    }
}
