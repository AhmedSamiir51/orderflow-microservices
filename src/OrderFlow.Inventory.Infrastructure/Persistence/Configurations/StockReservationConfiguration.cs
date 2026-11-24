using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Sku).HasMaxLength(64).IsRequired();

        // The idempotency guarantee: one reservation per (order, SKU), enforced by the database.
        builder.HasIndex(r => new { r.OrderId, r.Sku }).IsUnique();
    }
}
