using OrderFlow.Contracts.Orders;
using OrderFlow.Orders.Application.Features.Orders.Commands.CreateOrder;
using OrderFlow.Orders.Application.Tests.Support;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Tests.Features;

public sealed class CreateOrderHandlerTests
{
    [Fact]
    public async Task Creates_a_pending_order_and_publishes_OrderCreated()
    {
        await using var db = await TestDb.CreateAsync();
        var publisher = new RecordingEventPublisher();
        var handler = new CreateOrderHandler(db.Context, publisher, TimeProvider.System);

        var result = await handler.Handle(new CreateOrderCommand(
            "customer@example.com",
            [new CreateOrderLine("SKU-1", 2, 9.99m)]), CancellationToken.None);

        Assert.Equal("Pending", result.Status);
        Assert.Equal(19.98m, result.Total);

        var persisted = Assert.Single(db.Context.Orders);
        Assert.Equal(OrderStatus.Pending, persisted.Status);
        Assert.Equal("customer@example.com", persisted.CustomerEmail);

        var published = Assert.Single(publisher.OfType<OrderCreated>());
        Assert.Equal(result.Id, published.OrderId);
        Assert.Equal("customer@example.com", published.CustomerEmail);
        Assert.Equal("SKU-1", Assert.Single(published.Lines).Sku);
    }
}
