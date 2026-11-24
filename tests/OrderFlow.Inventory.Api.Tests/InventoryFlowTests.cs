using System.Net;
using System.Net.Http.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Orders;
using OrderFlow.Inventory.Application.Features.Stock;
using OrderFlow.Inventory.Application.Features.Stock.Commands.CreateStockItem;
using OrderFlow.Inventory.Infrastructure.Persistence;

namespace OrderFlow.Inventory.Api.Tests;

public sealed class InventoryFlowTests : IAsyncLifetime
{
    private readonly InventoryApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Stock_items_can_be_created_and_listed()
    {
        var create = await _client.PostAsJsonAsync("/api/stock", new CreateStockItemCommand("SKU-1", "Keyboard", 10));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/stock", new CreateStockItemCommand("SKU-1", "Keyboard", 10));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var items = await _client.GetFromJsonAsync<List<StockItemDto>>("/api/stock");
        var item = Assert.Single(items!);
        Assert.Equal(10, item.AvailableQuantity);
    }

    [Fact]
    public async Task OrderCreated_event_reserves_stock_end_to_end()
    {
        await _client.PostAsJsonAsync("/api/stock", new CreateStockItemCommand("SKU-7", "Monitor", 8));

        var orderId = Guid.NewGuid();
        await using var scope = _factory.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await bus.Publish(new OrderCreated(orderId, "customer@example.com",
            [new OrderLineItem("SKU-7", 3)], DateTimeOffset.UtcNow));
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().SaveChangesAsync();

        var item = await WaitForAsync(async () =>
        {
            var items = await _client.GetFromJsonAsync<List<StockItemDto>>("/api/stock");
            return items!.Single(i => i.Sku == "SKU-7");
        }, i => i.ReservedQuantity == 3, TimeSpan.FromSeconds(20));

        Assert.Equal(5, item.AvailableQuantity);
        Assert.Equal(3, item.ReservedQuantity);
    }

    [Fact]
    public async Task OrderCreated_beyond_available_stock_leaves_quantities_untouched()
    {
        await _client.PostAsJsonAsync("/api/stock", new CreateStockItemCommand("SKU-8", "Webcam", 2));

        await using var scope = _factory.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await bus.Publish(new OrderCreated(Guid.NewGuid(), "customer@example.com",
            [new OrderLineItem("SKU-8", 5)], DateTimeOffset.UtcNow));
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().SaveChangesAsync();

        // give the consumer time to process, then assert nothing was reserved
        await Task.Delay(TimeSpan.FromSeconds(3));
        var items = await _client.GetFromJsonAsync<List<StockItemDto>>("/api/stock");
        var item = items!.Single(i => i.Sku == "SKU-8");
        Assert.Equal(2, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
    }

    private static async Task<T> WaitForAsync<T>(Func<Task<T>> probe, Func<T, bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        T value = await probe();
        while (DateTime.UtcNow < deadline)
        {
            if (condition(value))
            {
                return value;
            }

            await Task.Delay(250);
            value = await probe();
        }

        Assert.Fail("Condition was not met within the timeout");
        return value;
    }
}
