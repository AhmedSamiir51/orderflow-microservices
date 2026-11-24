using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application.Common.Abstractions;

namespace OrderFlow.Inventory.Application.Features.Stock.Queries.ListStock;

public sealed record ListStockQuery : IRequest<IReadOnlyList<StockItemDto>>;

public sealed class ListStockHandler(IInventoryDbContext db) : IRequestHandler<ListStockQuery, IReadOnlyList<StockItemDto>>
{
    public async Task<IReadOnlyList<StockItemDto>> Handle(ListStockQuery request, CancellationToken cancellationToken)
    {
        var items = await db.StockItems.AsNoTracking().OrderBy(i => i.Sku).ToListAsync(cancellationToken);
        return items.Select(StockItemDto.FromDomain).ToList();
    }
}
