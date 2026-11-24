using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Application.Common.Abstractions;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options), IInventoryDbContext
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    public DbSet<StockReservation> Reservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        // MassTransit transactional outbox / consumer inbox tables.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();

        // SQLite cannot order by DateTimeOffset columns; store them as ticks on that provider only.
        // PostgreSQL keeps its native timestamptz mapping, so migrations are unaffected.
        if (Database.IsSqlite())
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                             .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
