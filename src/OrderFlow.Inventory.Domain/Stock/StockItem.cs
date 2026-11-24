namespace OrderFlow.Inventory.Domain.Stock;

/// <summary>A stocked product identified by SKU. Quantities move only through explicit operations.</summary>
public sealed class StockItem
{
    private StockItem()
    {
    }

    private StockItem(string sku, string name, int availableQuantity)
    {
        Id = Guid.NewGuid();
        Sku = sku;
        Name = name;
        AvailableQuantity = availableQuantity;
        ReservedQuantity = 0;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int AvailableQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public static StockItem Create(string sku, string name, int initialQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("A stock item needs a SKU.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A stock item needs a name.");
        }

        if (initialQuantity < 0)
        {
            throw new DomainException("Initial quantity cannot be negative.");
        }

        return new StockItem(sku.Trim(), name.Trim(), initialQuantity);
    }

    /// <summary>Returns false when there is not enough stock; throws never, so callers can aggregate failures.</summary>
    public bool CanReserve(int quantity) => quantity > 0 && AvailableQuantity >= quantity;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
        {
            throw new DomainException($"Cannot reserve {quantity} of {Sku}: only {AvailableQuantity} available.");
        }

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
    }

    /// <summary>Releases previously reserved stock (compensation when an order is cancelled).</summary>
    public void Release(int quantity)
    {
        if (quantity <= 0 || quantity > ReservedQuantity)
        {
            throw new DomainException($"Cannot release {quantity} of {Sku}: only {ReservedQuantity} reserved.");
        }

        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Restock quantity must be positive.");
        }

        AvailableQuantity += quantity;
    }
}
