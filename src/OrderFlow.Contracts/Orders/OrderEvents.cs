namespace OrderFlow.Contracts.Orders;

/// <summary>A single line of an order as published to the message broker.</summary>
public sealed record OrderLineItem(string Sku, int Quantity);

/// <summary>Published by the Orders service when a new order is accepted.</summary>
public sealed record OrderCreated(
    Guid OrderId,
    string CustomerEmail,
    IReadOnlyList<OrderLineItem> Lines,
    DateTimeOffset OccurredAt);

/// <summary>Published by the Orders service when stock is reserved and the order is confirmed.</summary>
public sealed record OrderConfirmed(
    Guid OrderId,
    string CustomerEmail,
    DateTimeOffset OccurredAt);

/// <summary>Published by the Orders service when an order is rejected (e.g. insufficient stock).</summary>
public sealed record OrderRejected(
    Guid OrderId,
    string CustomerEmail,
    string Reason,
    DateTimeOffset OccurredAt);
