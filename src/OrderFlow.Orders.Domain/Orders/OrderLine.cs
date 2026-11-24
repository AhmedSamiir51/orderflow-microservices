namespace OrderFlow.Orders.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    internal OrderLine(string sku, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("A line needs a SKU.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        Id = Guid.NewGuid();
        Sku = sku.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
