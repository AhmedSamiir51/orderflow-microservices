using OrderFlow.Orders.Domain;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Domain.Tests;

public sealed class OrderTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    private static Order NewPendingOrder() => Order.Create(
        "customer@example.com",
        [("SKU-1", 2, 10m), ("SKU-2", 1, 5.5m)],
        Time);

    [Fact]
    public void Create_computes_total_from_lines()
    {
        var order = NewPendingOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(25.5m, order.Total);
        Assert.Equal(2, order.Lines.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_customer_email(string email)
    {
        Assert.Throws<DomainException>(() => Order.Create(email, [("SKU-1", 1, 1m)], Time));
    }

    [Fact]
    public void Create_rejects_empty_lines()
    {
        Assert.Throws<DomainException>(() => Order.Create("customer@example.com", [], Time));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_rejects_non_positive_quantities(int quantity)
    {
        Assert.Throws<DomainException>(() => Order.Create("customer@example.com", [("SKU-1", quantity, 1m)], Time));
    }

    [Fact]
    public void Confirm_moves_pending_order_to_confirmed()
    {
        var order = NewPendingOrder();

        order.Confirm(Time);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public void Confirm_is_idempotent_for_redelivered_messages()
    {
        var order = NewPendingOrder();
        order.Confirm(Time);

        var exception = Record.Exception(() => order.Confirm(Time));

        Assert.Null(exception);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_rejects_a_cancelled_order()
    {
        var order = NewPendingOrder();
        order.Cancel(Time);

        Assert.Throws<DomainException>(() => order.Confirm(Time));
    }

    [Fact]
    public void Reject_records_the_reason()
    {
        var order = NewPendingOrder();

        order.Reject("Not enough stock for 'SKU-1'", Time);

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Not enough stock for 'SKU-1'", order.RejectionReason);
    }

    [Fact]
    public void Reject_is_idempotent_for_redelivered_messages()
    {
        var order = NewPendingOrder();
        order.Reject("reason", Time);

        var exception = Record.Exception(() => order.Reject("reason", Time));

        Assert.Null(exception);
    }

    [Fact]
    public void Cancel_only_works_while_pending()
    {
        var order = NewPendingOrder();
        order.Confirm(Time);

        Assert.Throws<DomainException>(() => order.Cancel(Time));
    }
}
