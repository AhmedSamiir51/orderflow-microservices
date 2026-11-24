using OrderFlow.Orders.Application.Common.Exceptions;
using OrderFlow.Orders.Application.Features.Orders.Queries.GetOrderById;
using OrderFlow.Orders.Application.Features.Orders.Queries.ListOrders;
using OrderFlow.Orders.Application.Tests.Support;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Application.Tests.Features;

public sealed class QueryHandlerTests
{
    [Fact]
    public async Task GetById_returns_the_order_with_lines()
    {
        await using var db = await TestDb.CreateAsync();
        var order = Order.Create("customer@example.com", [("SKU-1", 1, 5m), ("SKU-2", 3, 2m)], TimeProvider.System);
        db.Context.Orders.Add(order);
        await db.Context.SaveChangesAsync();

        var handler = new GetOrderByIdHandler(db.Context);
        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal(11m, result.Total);
        Assert.Equal(2, result.Lines.Count);
    }

    [Fact]
    public async Task GetById_throws_not_found_for_unknown_order()
    {
        await using var db = await TestDb.CreateAsync();
        var handler = new GetOrderByIdHandler(db.Context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task List_pages_and_filters_by_status()
    {
        await using var db = await TestDb.CreateAsync();
        for (var i = 0; i < 7; i++)
        {
            var order = Order.Create($"customer{i}@example.com", [("SKU-1", 1, 5m)], TimeProvider.System);
            if (i < 2)
            {
                order.Confirm(TimeProvider.System);
            }

            db.Context.Orders.Add(order);
        }

        await db.Context.SaveChangesAsync();

        var handler = new ListOrdersHandler(db.Context);

        var confirmed = await handler.Handle(new ListOrdersQuery(Status: "Confirmed"), CancellationToken.None);
        Assert.Equal(2, confirmed.TotalCount);
        Assert.All(confirmed.Items, item => Assert.Equal("Confirmed", item.Status));

        var pageTwo = await handler.Handle(new ListOrdersQuery(Page: 2, PageSize: 3), CancellationToken.None);
        Assert.Equal(7, pageTwo.TotalCount);
        Assert.Equal(3, pageTwo.Items.Count);
    }
}
