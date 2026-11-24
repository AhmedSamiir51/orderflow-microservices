using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Application.Common.Abstractions;

public interface IInventoryDbContext
{
    DbSet<StockItem> StockItems { get; }

    DbSet<StockReservation> Reservations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
