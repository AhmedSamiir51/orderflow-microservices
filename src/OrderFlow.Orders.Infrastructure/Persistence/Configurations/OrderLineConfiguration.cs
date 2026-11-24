using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Orders.Domain.Orders;

namespace OrderFlow.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Sku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);

        builder.Ignore(l => l.LineTotal);
    }
}
