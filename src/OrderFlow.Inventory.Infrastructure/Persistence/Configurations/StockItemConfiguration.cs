using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Inventory.Domain.Stock;

namespace OrderFlow.Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(i => i.Sku).IsUnique();
    }
}
