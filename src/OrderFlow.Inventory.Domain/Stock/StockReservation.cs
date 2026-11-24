namespace OrderFlow.Inventory.Domain.Stock;

/// <summary>
/// Records which quantities were reserved for an order. The (OrderId, Sku) pair is unique,
/// which makes processing a redelivered OrderCreated message naturally idempotent.
/// </summary>
public sealed class StockReservation
{
    private StockReservation()
    {
    }

    private StockReservation(Guid orderId, Guid stockItemId, string sku, int quantity, TimeProvider time)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        StockItemId = stockItemId;
        Sku = sku;
        Quantity = quantity;
        CreatedAt = time.GetUtcNow();
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid StockItemId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static StockReservation Create(Guid orderId, StockItem item, int quantity, TimeProvider time) =>
        new(orderId, item.Id, item.Sku, quantity, time);
}
