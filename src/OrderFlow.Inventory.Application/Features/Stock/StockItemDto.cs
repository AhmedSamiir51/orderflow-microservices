using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Application.Features.Stock;

public sealed record StockItemDto(Guid Id, string Sku, string Name, int AvailableQuantity, int ReservedQuantity)
{
    public static StockItemDto FromDomain(StockItem item) =>
        new(item.Id, item.Sku, item.Name, item.AvailableQuantity, item.ReservedQuantity);
}
