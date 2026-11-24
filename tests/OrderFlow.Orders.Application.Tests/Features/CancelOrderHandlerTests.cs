using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application.Common.Exceptions;
using OrderFlow.Orders.Application.Features.Orders.Commands.CancelOrder;
using OrderFlow.Orders.Application.Tests.Support;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Tests.Features;

public sealed class CancelOrderHandlerTests
{
    [Fact]
    public async Task Cancels_a_pending_order()
    {
        await using var db = await TestDb.CreateAsync();
        var order = Order.Create("customer@example.com", [("SKU-1", 1, 5m)], TimeProvider.System);
        db.Context.Orders.Add(order);
        await db.Context.SaveChangesAsync();

        var handler = new CancelOrderHandler(db.Context, TimeProvider.System);
        await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        var reloaded = await db.Context.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
    }

    [Fact]
    public async Task Throws_not_found_for_unknown_order()
    {
        await using var db = await TestDb.CreateAsync();
        var handler = new CancelOrderHandler(db.Context, TimeProvider.System);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
