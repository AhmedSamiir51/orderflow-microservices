namespace OrderFlow.Orders.Domain.Orders;

/// <summary>
/// The order aggregate. State transitions are driven by integration events coming back
/// from the Inventory service, so <see cref="Confirm"/> and <see cref="Reject"/> are
/// idempotent: a redelivered message must not corrupt the order.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    private Order(string customerEmail, IEnumerable<OrderLine> lines, TimeProvider time)
    {
        Id = Guid.NewGuid();
        CustomerEmail = customerEmail;
        Status = OrderStatus.Pending;
        CreatedAt = time.GetUtcNow();
        _lines.AddRange(lines);
    }

    public Guid Id { get; private set; }

    public string CustomerEmail { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Why the order was rejected, when it was.</summary>
    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public static Order Create(string customerEmail, IEnumerable<(string Sku, int Quantity, decimal UnitPrice)> lines, TimeProvider time)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new DomainException("An order needs a customer email.");
        }

        var orderLines = lines.Select(line => new OrderLine(line.Sku, line.Quantity, line.UnitPrice)).ToList();
        if (orderLines.Count == 0)
        {
            throw new DomainException("An order needs at least one line.");
        }

        return new Order(customerEmail.Trim(), orderLines, time);
    }

    /// <summary>Marks the order confirmed after the Inventory service reserved stock. Idempotent.</summary>
    public void Confirm(TimeProvider time)
    {
        if (Status == OrderStatus.Confirmed)
        {
            return;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Cannot confirm an order that is {Status}.");
        }

        Status = OrderStatus.Confirmed;
        CompletedAt = time.GetUtcNow();
    }

    /// <summary>Marks the order rejected after the Inventory service failed to reserve stock. Idempotent.</summary>
    public void Reject(string reason, TimeProvider time)
    {
        if (Status == OrderStatus.Rejected)
        {
            return;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Cannot reject an order that is {Status}.");
        }

        Status = OrderStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Unknown reason" : reason;
        CompletedAt = time.GetUtcNow();
    }

    public void Cancel(TimeProvider time)
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Cannot cancel an order that is {Status}.");
        }

        Status = OrderStatus.Cancelled;
        CompletedAt = time.GetUtcNow();
    }
}
