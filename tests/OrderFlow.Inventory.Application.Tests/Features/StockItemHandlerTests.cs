using OrderFlow.Inventory.Application.Common.Exceptions;
using OrderFlow.Inventory.Application.Features.Stock.Commands.CreateStockItem;
using OrderFlow.Inventory.Application.Features.Stock.Commands.RestockStockItem;
using OrderFlow.Inventory.Application.Tests.Support;
using OrderFlow.Inventory.Domain;

namespace OrderFlow.Inventory.Application.Tests.Features;

public sealed class StockItemHandlerTests
{
    [Fact]
    public async Task Create_persists_a_stock_item()
    {
        await using var db = await TestDb.CreateAsync();
        var handler = new CreateStockItemHandler(db.Context);

        var result = await handler.Handle(new CreateStockItemCommand("SKU-1", "Mechanical keyboard", 25), CancellationToken.None);

        Assert.Equal("SKU-1", result.Sku);
        Assert.Equal(25, result.AvailableQuantity);
        Assert.Single(db.Context.StockItems);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_sku()
    {
        await using var db = await TestDb.CreateAsync();
        var handler = new CreateStockItemHandler(db.Context);
        await handler.Handle(new CreateStockItemCommand("SKU-1", "Keyboard", 25), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateStockItemCommand("SKU-1", "Another keyboard", 5), CancellationToken.None));
    }

    [Fact]
    public async Task Restock_adds_available_quantity()
    {
        await using var db = await TestDb.CreateAsync();
        var create = new CreateStockItemHandler(db.Context);
        await create.Handle(new CreateStockItemCommand("SKU-1", "Keyboard", 5), CancellationToken.None);

        var restock = new RestockStockItemHandler(db.Context);
        var result = await restock.Handle(new RestockStockItemCommand("SKU-1", 10), CancellationToken.None);

        Assert.Equal(15, result.AvailableQuantity);
    }

    [Fact]
    public async Task Restock_throws_not_found_for_unknown_sku()
    {
        await using var db = await TestDb.CreateAsync();
        var restock = new RestockStockItemHandler(db.Context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            restock.Handle(new RestockStockItemCommand("NOPE", 10), CancellationToken.None));
    }
}
