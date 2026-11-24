using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Application.Common.Abstractions;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options), IOrdersDbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

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
