namespace OrderFlow.Contracts.Inventory;

/// <summary>Published by the Inventory service when every line of an order has been reserved.</summary>
public sealed record StockReserved(
    Guid OrderId,
    DateTimeOffset OccurredAt);

/// <summary>Published by the Inventory service when at least one line cannot be fulfilled.</summary>
public sealed record StockReservationFailed(
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAt);
